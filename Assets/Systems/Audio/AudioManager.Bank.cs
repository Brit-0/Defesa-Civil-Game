using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// v5 (docs/jam/SFX_V5.md): sons por nome do SoundBank, grupos do mixer, música em 4 camadas que entram com o caos,
// loops por dono (chuva, jato de cada casa), atraso pelo GameClock e a pausa que abafa a música.
// Uso: AudioManager.Play("thunder_near", posição); AudioManager.StartLoop("fire_jet_loop", casa); StopLoop(...).
public partial class AudioManager
{
    private const int LogCapacity = 600;
    private static readonly string[] LayerIds = { "music_layer_0", "music_layer_1", "music_layer_2", "music_layer_3" };

    [Header("V5: BANCO DE SONS (Resources/Audio/SoundBank)")]
    [SerializeField] private int bankPoolSize = 24;
    [Tooltip("Abertura do lado do som pela posição na tela (0 = no meio, 1 = lado inteiro)")]
    [SerializeField, Range(0f, 1f)] private float panWidth = .55f;
    [Tooltip("Som a uma tela de distância da câmera cai este tanto (dB)")]
    [SerializeField] private float offscreenDb = -7f;
    [SerializeField] private bool logToConsole;

    [Header("V5: MÚSICA EM CAMADAS")]
    [SerializeField] private float layerFadeTime = 3f;

    [Header("V5: DUCKING DO AMBIENTE (parte do abaixar da música que vale para chuva, fogo e cidade)")]
    [SerializeField, Range(0f, 1f)] private float ambienceDuckShare = .6f;

    [Header("V5: PAUSA (abafa a música; no WebGL o filtro não existe e ela só abaixa)")]
    [SerializeField] private float pauseCutoffHz = 800f;
    [SerializeField] private float pauseFadeTime = .3f;
    [SerializeField, Range(0f, 1f)] private float webglPauseLevel = .55f;
    [SerializeField] private float strayScanInterval = 2f;

    private static readonly List<string> log = new List<string>();
    private static readonly Dictionary<string, int> playCounts = new Dictionary<string, int>();

    private SoundBank bank;
    private AudioMixerGroup musicGroup, sfxGroup, ambienceGroup, uiGroup;
    private readonly List<AudioSource> bankPool = new List<AudioSource>();
    private readonly Dictionary<string, List<AudioSource>> voices = new Dictionary<string, List<AudioSource>>();
    private readonly Dictionary<string, float> lastEntryTime = new Dictionary<string, float>();
    private readonly Dictionary<string, int> lastIndex = new Dictionary<string, int>();
    private readonly Dictionary<string, int> ladderSteps = new Dictionary<string, int>();
    private readonly List<Delayed> delayed = new List<Delayed>();
    private AudioSource[] layerSources;
    private readonly float[] layerLevels = new float[4];
    private readonly float[] layerTargets = new float[4];
    private float activity;
    private float muffle;
    private float nextStrayScan;
    private int nextBankSource;

    private struct Delayed
    {
        public float at;
        public string id;
        public Vector3? position;
        public float volume;
    }

    // Últimos sons tocados ("12.40 thunder_near x=3.1"), para conferir sem escutar
    public static IReadOnlyList<string> EventLog => log;
    public static IReadOnlyDictionary<string, int> PlayCounts => playCounts;
    public static SoundBank Bank => Instance != null && Instance.bank != null ? Instance.bank : Resources.Load<SoundBank>(SoundBank.ResourcePath);
    public static bool HasSound(string id) => Bank != null && Bank.Find(id) != null;
    public static bool LayeredMusic => Instance != null && Instance.layerSources != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetBankStatics()
    {
        log.Clear();
        playCounts.Clear();
    }

    private void InitBank()
    {
        bank = Resources.Load<SoundBank>(SoundBank.ResourcePath);
        if (bank != null && bank.mixer != null)
        {
            musicGroup = FindGroup("Musica");
            sfxGroup = FindGroup("SFX");
            ambienceGroup = FindGroup("Ambiente");
            uiGroup = FindGroup("UI");
        }

        for (int i = 0; i < bankPoolSize; i++)
        {
            bankPool.Add(CreateSource("Bank " + i, false));
        }

        calmSource.outputAudioMixerGroup = tenseSource.outputAudioMixerGroup = dangerSource.outputAudioMixerGroup = musicGroup;
    }

    private AudioMixerGroup FindGroup(string groupName)
    {
        AudioMixerGroup[] found = bank.mixer.FindMatchingGroups(groupName);
        return found.Length > 0 ? found[0] : null;
    }

    public AudioMixerGroup Group(SoundCategory category)
    {
        switch (category)
        {
            case SoundCategory.Interface: return uiGroup;
            case SoundCategory.Ambience: return ambienceGroup;
            case SoundCategory.Music: return musicGroup;
            default: return sfxGroup;
        }
    }

    // Volume de um grupo do mixer (MasterVolume, MusicVolume, SfxVolume, AmbienceVolume, UiVolume), de 0 a 1
    public static void SetMixerVolume(string parameter, float linear)
    {
        if (Instance == null || Instance.bank == null || Instance.bank.mixer == null) return;

        Instance.bank.mixer.SetFloat(parameter, 20f * Mathf.Log10(Mathf.Max(.0001f, linear)));
    }

    #region PLAY

    public static AudioSource Play(string id, Vector3? position = null, float volume = 1f, float pitch = 1f)
    {
        if (Instance == null || Instance.bank == null) return null;

        SoundEntry entry = Instance.bank.Find(id);
        if (entry == null)
        {
            if (Application.isEditor) Debug.LogWarning("AudioManager: som " + id + " não está no SoundBank");
            return null;
        }

        return Instance.PlayEntry(entry, position, volume, pitch, null);
    }

    // Toca depois de `delay` segundos do GameClock (o atraso para junto com a pausa)
    public static void PlayDelayed(string id, float delay, Vector3? position = null, float volume = 1f)
    {
        if (Instance == null) return;

        Instance.delayed.Add(new Delayed { at = GameClock.Time + delay, id = id, position = position, volume = volume });
    }

    private bool TryPlayLegacy(AudioClip clip, float volume)
    {
        if (bank == null || !bank.TryLegacy(clip, out LegacySound link)) return false;

        PlayEntry(bank.Find(link.id), null, volume * link.volume, 1f, clip.name);
        return true;
    }

    private AudioSource PlayEntry(SoundEntry entry, Vector3? position, float volume, float pitch, string legacyName)
    {
        if (entry.clips == null || entry.clips.Length == 0) return null;

        float now = Time.unscaledTime;
        bool hasLast = lastEntryTime.TryGetValue(entry.id, out float last);
        if (hasLast && now - last < entry.cooldown) return null;
        lastEntryTime[entry.id] = now;

        if (entry.ladderStep > 0f)
        {
            ladderSteps.TryGetValue(entry.id, out int step);
            step = hasLast ? SoundMath.NextLadderStep(step, now - last, entry.ladderReset, entry.ladderMax) : 0;
            ladderSteps[entry.id] = step;
            pitch *= SoundMath.SemitonesToPitch(step * entry.ladderStep);
        }

        lastIndex.TryGetValue(entry.id, out int previous);
        int index = SoundMath.PickIndex(entry.clips.Length, hasLast ? previous : -1, Random.value);
        lastIndex[entry.id] = index;
        AudioClip clip = entry.clips[index];
        if (clip == null) return null;

        AudioSource source = TakeVoice(entry);
        float gain = entry.volume * Mathf.Clamp(volume, 0f, 2f) * Volume(entry.category)
                     * SoundMath.DbToLinear(-Random.Range(0f, entry.volumeJitterDb));
        float pan = 0f;
        Camera cam = Camera.main;
        if (position.HasValue && cam != null && cam.orthographic)
        {
            Vector3 p = position.Value;
            Vector2 half = new Vector2(cam.orthographicSize * cam.aspect, cam.orthographicSize);
            pan = SoundMath.Pan(p.x, cam.transform.position.x, half.x, panWidth);
            gain *= SoundMath.OffscreenGain(p.x, p.y, cam.transform.position, half, offscreenDb);
        }

        source.Stop();
        source.clip = clip;
        source.loop = false;
        source.volume = Mathf.Clamp01(gain);
        source.pitch = pitch * SoundMath.SemitonesToPitch(Random.Range(-entry.pitchJitter, entry.pitchJitter));
        source.panStereo = pan;
        source.outputAudioMixerGroup = Group(entry.category);
        source.ignoreListenerPause = entry.category == SoundCategory.Interface;
        source.Play();

        if (entry.ducks && volume >= duckMinVolume) Duck(Mathf.Min(clip.length, duckMaxHold));

        Record(entry.id + (position.HasValue ? " x=" + position.Value.x.ToString("0.0") : "") + (legacyName != null ? " (" + legacyName + ")" : "")
               + (entry.ladderStep > 0f ? " degrau " + ladderSteps[entry.id] : ""), entry.id);
        return source;
    }

    // Respeita o limite de vozes do som: no limite, a cópia mais velha dele cede a vez
    private AudioSource TakeVoice(SoundEntry entry)
    {
        if (!voices.TryGetValue(entry.id, out List<AudioSource> list))
        {
            list = new List<AudioSource>();
            voices[entry.id] = list;
        }

        list.RemoveAll(s => s == null || !s.isPlaying || !Owns(entry, s.clip));
        if (list.Count >= Mathf.Max(1, entry.maxVoices))
        {
            AudioSource oldest = list[0];
            list.RemoveAt(0);
            list.Add(oldest);
            return oldest;
        }

        AudioSource source = FreeBankSource();
        foreach (List<AudioSource> other in voices.Values) other.Remove(source);
        list.Add(source);
        return source;
    }

    private static bool Owns(SoundEntry entry, AudioClip clip)
    {
        return System.Array.IndexOf(entry.clips, clip) >= 0;
    }

    private AudioSource FreeBankSource()
    {
        for (int i = 0; i < bankPool.Count; i++)
        {
            int index = (nextBankSource + i) % bankPool.Count;
            if (bankPool[index].isPlaying) continue;

            nextBankSource = (index + 1) % bankPool.Count;
            return bankPool[index];
        }

        AudioSource oldest = bankPool[nextBankSource];
        nextBankSource = (nextBankSource + 1) % bankPool.Count;
        return oldest;
    }

    private void Record(string line, string id)
    {
        playCounts.TryGetValue(id, out int count);
        playCounts[id] = count + 1;

        string entry = GameClock.Time.ToString("0.00") + " " + line;
        log.Add(entry);
        if (log.Count > LogCapacity) log.RemoveRange(0, log.Count - LogCapacity);
        if (logToConsole) Debug.Log("[Som] " + entry);
    }

    #endregion

    #region LOOPS BY OWNER

    // Loop do banco com dono (a chuva da tempestade, o jato de cada casa). Chamar de novo só muda o volume.
    public static void StartLoop(string id, Object owner = null, float volume = 1f, float fadeTime = .5f)
    {
        if (Instance == null || Instance.bank == null) return;

        SoundEntry entry = Instance.bank.Find(id);
        if (entry == null || entry.clips == null || entry.clips.Length == 0 || entry.clips[0] == null) return;

        Instance.SetBankLoop(entry, owner, volume, fadeTime);
    }

    public static void StopLoop(string id, Object owner = null, float fadeTime = .5f)
    {
        if (Instance == null) return;

        LoopChannel channel = Instance.loops.Find(c => c.id == id && c.owner == KeyOf(owner));
        if (channel != null) channel.targetLevel = 0f;
        if (channel != null) channel.fadeSpeed = 1f / Mathf.Max(.01f, fadeTime);
    }

    // Para o loop de todos os donos (fim de partida, reinício: casas que sumiram não deixam jato tocando)
    public static void StopAllLoops(string id, float fadeTime = .5f)
    {
        if (Instance == null) return;

        foreach (LoopChannel channel in Instance.loops)
        {
            if (channel.id != id) continue;

            channel.targetLevel = 0f;
            channel.fadeSpeed = 1f / Mathf.Max(.01f, fadeTime);
        }
    }

    public static bool IsLoopPlaying(string id, Object owner = null)
    {
        return Instance != null && Instance.loops.Exists(c => c.id == id && c.owner == KeyOf(owner) && c.targetLevel > 0f);
    }

    //Hash do objeto (o id da instância): o dono que já foi destruído não confunde com outro
    private static int KeyOf(Object owner) => ReferenceEquals(owner, null) ? 0 : owner.GetHashCode();

    private void SetBankLoop(SoundEntry entry, Object owner, float volume, float fadeTime)
    {
        int key = KeyOf(owner);
        LoopChannel channel = loops.Find(c => c.id == entry.id && c.owner == key);
        if (channel == null)
        {
            if (volume <= 0f) return;

            channel = new LoopChannel { source = CreateSource("Loop " + entry.id + " " + key, true), id = entry.id, owner = key };
            channel.source.clip = entry.clips[Random.Range(0, entry.clips.Length)];
            channel.source.volume = 0f;
            channel.source.outputAudioMixerGroup = Group(entry.category);
            channel.category = entry.category;
            loops.Add(channel);
            Record(entry.id + " (loop " + (owner != null ? owner.name : "geral") + ")", entry.id);
        }

        channel.entryVolume = entry.volume;
        channel.targetLevel = Mathf.Clamp01(volume);
        channel.fadeSpeed = 1f / Mathf.Max(.01f, fadeTime);

        if (channel.targetLevel > 0f && !channel.source.isPlaying)
        {
            //Cada loop começa num ponto diferente do arquivo: dois jatos juntos não soam em fase
            channel.source.time = Random.Range(0f, channel.source.clip.length * .9f);
            channel.source.Play();
        }
    }

    #endregion

    #region LAYERED MUSIC

    // 0 a 1: algo acontecendo (desastre no ar). Liga a camada do ritmo mesmo com o caos baixo.
    public static void SetMusicActivity(float value)
    {
        if (Instance != null) Instance.activity = Mathf.Clamp01(value);
    }

    private bool StartLayeredMusic()
    {
        if (bank == null) return false;

        AudioClip[] clips = new AudioClip[LayerIds.Length];
        for (int i = 0; i < clips.Length; i++)
        {
            SoundEntry entry = bank.Find(LayerIds[i]);
            if (entry == null || entry.clips.Length == 0 || entry.clips[0] == null) return false;
            clips[i] = entry.clips[0];
        }

        if (layerSources == null)
        {
            layerSources = new AudioSource[clips.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                layerSources[i] = CreateSource("Music Layer " + i, true);
                layerSources[i].ignoreListenerPause = true;
                layerSources[i].outputAudioMixerGroup = musicGroup;
            }
        }

        if (layerSources[0].isPlaying) return true;

        //As quatro começam na mesma amostra e têm o mesmo tamanho: o volume decide qual se ouve
        double start = AudioSettings.dspTime + .15;
        for (int i = 0; i < clips.Length; i++)
        {
            layerSources[i].clip = clips[i];
            layerSources[i].volume = 0f;
            layerSources[i].PlayScheduled(start);
        }
        Record("música em camadas", "music_layers");
        return true;
    }

    private void UpdateLayers(float delta)
    {
        if (layerSources == null) return;

        SoundMath.LayerTargets(activity, targetTension, targetDanger, layerTargets);
        float step = delta / Mathf.Max(.01f, layerFadeTime);
        for (int i = 0; i < layerLevels.Length; i++)
        {
            layerLevels[i] = Mathf.MoveTowards(layerLevels[i], layerTargets[i], step);
        }
    }

    private void ApplyLayerVolumes(float volume)
    {
        for (int i = 0; i < layerSources.Length; i++)
        {
            float level = layerLevels[i] * (i == 3 ? dangerLayerVolume : 1f);
            //Raiz quadrada: o fade soa linear ao ouvido e a camada entra sem buraco
            layerSources[i].volume = volume * Mathf.Sqrt(level);
        }
    }

    public static float LayerLevel(int layer) => Instance != null && layer >= 0 && layer < 4 ? Instance.layerLevels[layer] : 0f;

    #endregion

    #region PAUSE, DELAYS AND STRAY SOURCES

    private void UpdateBank(float delta)
    {
        UpdateLayers(delta);

        //Pausa (GameClock): a música abafa pelo passa-baixa do grupo Musica; os outros sons já param pelo AudioListener
        muffle = Mathf.MoveTowards(muffle, GameClock.Frozen ? 1f : 0f, delta / Mathf.Max(.01f, pauseFadeTime));
#if !UNITY_WEBGL || UNITY_EDITOR
        if (bank != null && bank.mixer != null) bank.mixer.SetFloat("MusicCutoff", SoundMath.CutoffAt(muffle, 22000f, pauseCutoffHz));
#endif

        for (int i = delayed.Count - 1; i >= 0; i--)
        {
            if (GameClock.Time < delayed[i].at) continue;

            Delayed item = delayed[i];
            delayed.RemoveAt(i);
            Play(item.id, item.position, item.volume);
        }

        if (Time.unscaledTime >= nextStrayScan)
        {
            nextStrayScan = Time.unscaledTime + strayScanInterval;
            RouteStraySources();
        }
    }

    // No WebGL o filtro não existe: a pausa só abaixa a música
    private float PauseLevel()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return Mathf.Lerp(1f, webglPauseLevel, muffle);
#else
        return 1f;
#endif
    }

    // AudioSource de outros sistemas (energia, mini games, tutorial) sem grupo: entram no SFX (ou UI, se tocam na pausa)
    private void RouteStraySources()
    {
        if (sfxGroup == null) return;

        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (source.outputAudioMixerGroup == null) source.outputAudioMixerGroup = source.ignoreListenerPause ? uiGroup : sfxGroup;
        }
    }

    public static void ClearDelayed()
    {
        if (Instance != null) Instance.delayed.Clear();
    }

    #endregion
}
