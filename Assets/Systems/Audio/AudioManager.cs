using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

//Categorias do mix: cada som cai numa, pelo campo da biblioteca em que está
public enum SoundCategory { Interface, Gameplay, Disaster, Ambience, Music }

[System.Serializable]
public class SfxLibrary
{
    [Header("INTERFACE")]
    public AudioClip click;
    public AudioClip hover;
    public AudioClip buy;
    public AudioClip error;
    public AudioClip whoosh;
    public AudioClip popup;

    [Header("CALLS")]
    public AudioClip dispatch;
    public AudioClip hammer;
    public AudioClip repaired;
    public AudioClip coin;

    [Header("DISASTERS")]
    public AudioClip siren;
    public AudioClip earthquake;
    public AudioClip thunder;
    public AudioClip extinguish;

    [Header("END")]
    public AudioClip victory;
    public AudioClip defeat;
    public AudioClip confetti;

    [Header("LOOPS")]
    public AudioClip rainLoop;
    public AudioClip fireLoop;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    //Clipes do jogo, para quem toca som não precisar de referência própria
    public static SfxLibrary Sfx => Instance != null ? Instance.sfx : null;

    [Header("SFX")]
    [SerializeField] private SfxLibrary sfx = new SfxLibrary();
    [SerializeField] private int poolSize = 12;
    [SerializeField] private float minRepeatInterval = .05f;

    [Header("MIX (volume por categoria, vezes o master)")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float interfaceVolume = .8f;
    [SerializeField, Range(0f, 1f)] private float gameplayVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float disasterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = .8f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = .45f;

    [Header("DUCKING (a música abaixa sob sirene, tremor, trovão e vinhetas)")]
    [SerializeField, Range(0f, 1f)] private float duckLevel = .45f;
    [Tooltip("Só abaixa quando o som grande toca pelo menos neste volume (a sirene baixa do caos alto não abaixa)")]
    [SerializeField, Range(0f, 1f)] private float duckMinVolume = .5f;
    [SerializeField] private float duckAttackTime = .08f;
    [SerializeField] private float duckMaxHold = 1.5f;
    [SerializeField] private float duckReleaseTime = .9f;

    [Header("MUSIC (calma e tensa cruzam pelo caos; perigo entra por cima perto do fim)")]
    [SerializeField] private AudioClip calmMusic;
    [SerializeField] private AudioClip tenseMusic;
    [SerializeField] private AudioClip dangerMusic;
    [SerializeField, Range(0f, 1f)] private float dangerLayerVolume = .8f;
    [SerializeField] private float tensionFadeTime = 2f;
    [SerializeField] private bool playMusicOnStart = true;

    [Header("LIFETIME")]
    [SerializeField] private bool persistAcrossReload = true;

    private class LoopChannel
    {
        public AudioSource source;
        public float level;
        public float targetLevel;
        public float fadeSpeed;
    }

    private readonly List<AudioSource> pool = new List<AudioSource>();
    private readonly List<LoopChannel> loops = new List<LoopChannel>();
    private readonly Dictionary<AudioClip, float> lastPlayTime = new Dictionary<AudioClip, float>();
    private readonly Dictionary<AudioClip, SoundCategory> categories = new Dictionary<AudioClip, SoundCategory>();
    private readonly HashSet<AudioClip> duckers = new HashSet<AudioClip>();
    private int nextSource;
    private AudioSource calmSource;
    private AudioSource tenseSource;
    private AudioSource dangerSource;
    private float tension;
    private float targetTension;
    private float danger;
    private float targetDanger;
    private float musicLevel = 1f;
    private float targetMusicLevel = 1f;
    private float musicLevelSpeed = 1f;
    private float duck = 1f;
    private float duckUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        //O reinício recarrega a cena: o primeiro AudioManager fica e a música não corta
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossReload)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        BuildSources();
        BuildCategories();
    }

    private void Start()
    {
        if (Instance != this) return;

        if (playMusicOnStart) PlayMusic();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnValidate()
    {
        if (Instance == this) BuildCategories();
    }

    private void Update()
    {
        if (Instance != this) return;

        float delta = Time.unscaledDeltaTime;
        float fade = delta / Mathf.Max(.01f, tensionFadeTime);

        tension = Mathf.MoveTowards(tension, targetTension, fade);
        danger = Mathf.MoveTowards(danger, targetDanger, fade);
        musicLevel = Mathf.MoveTowards(musicLevel, targetMusicLevel, delta * musicLevelSpeed);

        bool ducking = Time.unscaledTime < duckUntil;
        float duckTime = ducking ? duckAttackTime : duckReleaseTime;
        duck = Mathf.MoveTowards(duck, ducking ? duckLevel : 1f, delta * (1f - duckLevel) / Mathf.Max(.01f, duckTime));

        //Todo quadro: mexer no mix pelo Inspector durante o Play vale na hora
        ApplyMusicVolumes();
        UpdateLoops(delta);
    }

    private void BuildSources()
    {
        for (int i = 0; i < poolSize; i++)
        {
            pool.Add(CreateSource("Sfx " + i, false));
        }

        calmSource = CreateSource("Music Calm", true);
        tenseSource = CreateSource("Music Tense", true);
        dangerSource = CreateSource("Music Danger", true);
    }

    private AudioSource CreateSource(string sourceName, bool loop)
    {
        GameObject child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    #region MIX

    private void BuildCategories()
    {
        categories.Clear();
        duckers.Clear();

        Map(SoundCategory.Interface, sfx.click, sfx.hover, sfx.buy, sfx.error, sfx.whoosh, sfx.popup);
        Map(SoundCategory.Gameplay, sfx.dispatch, sfx.hammer, sfx.repaired, sfx.coin, sfx.victory, sfx.defeat, sfx.confetti);
        Map(SoundCategory.Disaster, sfx.siren, sfx.earthquake, sfx.thunder, sfx.extinguish);
        Map(SoundCategory.Ambience, sfx.rainLoop, sfx.fireLoop);

        foreach (AudioClip clip in new[] { sfx.siren, sfx.earthquake, sfx.thunder, sfx.victory, sfx.defeat })
        {
            if (clip != null) duckers.Add(clip);
        }
    }

    private void Map(SoundCategory category, params AudioClip[] clips)
    {
        foreach (AudioClip clip in clips)
        {
            if (clip != null) categories[clip] = category;
        }
    }

    //Volume final de uma categoria (já com o master)
    public static float CategoryVolume(SoundCategory category)
    {
        return Instance != null ? Instance.Volume(category) : 1f;
    }

    //Para um menu de opções futuro: muda o volume de uma categoria na hora (Music também)
    public static void SetCategoryVolume(SoundCategory category, float value)
    {
        if (Instance == null) return;

        value = Mathf.Clamp01(value);
        switch (category)
        {
            case SoundCategory.Interface: Instance.interfaceVolume = value; break;
            case SoundCategory.Gameplay: Instance.gameplayVolume = value; break;
            case SoundCategory.Disaster: Instance.disasterVolume = value; break;
            case SoundCategory.Ambience: Instance.ambienceVolume = value; break;
            case SoundCategory.Music: Instance.musicVolume = value; break;
        }
    }

    public static void SetMasterVolume(float value)
    {
        if (Instance != null) Instance.masterVolume = Mathf.Clamp01(value);
    }

    private float Volume(SoundCategory category)
    {
        switch (category)
        {
            case SoundCategory.Interface: return masterVolume * interfaceVolume;
            case SoundCategory.Disaster: return masterVolume * disasterVolume;
            case SoundCategory.Ambience: return masterVolume * ambienceVolume;
            case SoundCategory.Music: return masterVolume * musicVolume;
            default: return masterVolume * gameplayVolume;
        }
    }

    private SoundCategory CategoryOf(AudioClip clip)
    {
        return categories.TryGetValue(clip, out SoundCategory category) ? category : SoundCategory.Gameplay;
    }

    #endregion

    #region SFX

    //A categoria sai do campo da biblioteca em que o clipe está (clipe de fora conta como Gameplay)
    public static void PlaySfx(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
    {
        if (Instance == null || clip == null) return;

        Instance.PlayFromPool(clip, Instance.CategoryOf(clip), volume, pitchVariation);
    }

    public static void PlaySfx(AudioClip clip, SoundCategory category, float volume = 1f, float pitchVariation = 0f)
    {
        if (Instance == null || clip == null) return;

        Instance.PlayFromPool(clip, category, volume, pitchVariation);
    }

    private void PlayFromPool(AudioClip clip, SoundCategory category, float volume, float pitchVariation)
    {
        //O mesmo som disparado várias vezes no mesmo instante vira um só
        float now = Time.unscaledTime;
        if (lastPlayTime.TryGetValue(clip, out float last) && now - last < minRepeatInterval) return;
        lastPlayTime[clip] = now;

        AudioSource source = GetFreeSource();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume) * Volume(category);
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.Play();

        if (volume >= duckMinVolume && duckers.Contains(clip)) Duck(Mathf.Min(clip.length, duckMaxHold));
    }

    //Abaixa a música por um tempo (os sons grandes já chamam sozinhos)
    public static void DuckMusic(float seconds)
    {
        if (Instance != null) Instance.Duck(seconds);
    }

    private void Duck(float seconds)
    {
        duckUntil = Mathf.Max(duckUntil, Time.unscaledTime + seconds);
    }

    private AudioSource GetFreeSource()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            int index = (nextSource + i) % pool.Count;
            if (pool[index].isPlaying) continue;

            nextSource = (index + 1) % pool.Count;
            return pool[index];
        }

        //Todas ocupadas: reaproveita a mais antiga
        AudioSource oldest = pool[nextSource];
        nextSource = (nextSource + 1) % pool.Count;
        return oldest;
    }

    #endregion

    #region MUSIC

    //0 = só a calma, 1 = só a tensa (cruzamento)
    public static void SetTension(float value)
    {
        if (Instance == null) return;

        Instance.targetTension = Mathf.Clamp01(value);
    }

    //0 a 1: a camada de perigo (um zumbido grave) sobe por cima das outras duas
    public static void SetDanger(float value)
    {
        if (Instance == null) return;

        Instance.targetDanger = Mathf.Clamp01(value);
    }

    //Volume geral da música, de 0 a 1 (menu, pausa, fim de partida), por cima do cruzamento das camadas
    public static void SetMusicLevel(float level, float fadeTime = .5f)
    {
        if (Instance == null) return;

        Instance.targetMusicLevel = Mathf.Clamp01(level);
        Instance.musicLevelSpeed = 1f / Mathf.Max(.01f, fadeTime);
    }

    public static void PlayMusic()
    {
        if (Instance == null) return;

        Instance.ApplyMusicVolumes();
        //As três começam juntas e ficam em loop; o volume decide qual se ouve
        Instance.StartLayer(Instance.calmSource, Instance.calmMusic);
        Instance.StartLayer(Instance.tenseSource, Instance.tenseMusic);
        Instance.StartLayer(Instance.dangerSource, Instance.dangerMusic);
    }

    public static void StopMusic()
    {
        if (Instance == null) return;

        Instance.calmSource.Stop();
        Instance.tenseSource.Stop();
        Instance.dangerSource.Stop();
    }

    private void StartLayer(AudioSource source, AudioClip clip)
    {
        if (clip == null || (source.isPlaying && source.clip == clip)) return;

        source.clip = clip;
        source.Play();
    }

    private void ApplyMusicVolumes()
    {
        float volume = Volume(SoundCategory.Music) * musicLevel * duck;

        //Cruzamento de potência constante: a soma das duas camadas não afunda no meio
        float angle = tension * Mathf.PI * .5f;
        calmSource.volume = volume * Mathf.Cos(angle);
        tenseSource.volume = volume * Mathf.Sin(angle);
        dangerSource.volume = volume * danger * dangerLayerVolume;
    }

    #endregion

    #region LOOPS

    //Ambiente em loop (chuva, fogo). Volume 0 some com fade e para.
    public static void SetLoop(AudioClip clip, float volume, float fadeTime = .5f)
    {
        if (Instance == null || clip == null) return;

        Instance.SetLoopChannel(clip, volume, fadeTime);
    }

    private void SetLoopChannel(AudioClip clip, float volume, float fadeTime)
    {
        LoopChannel channel = loops.Find(c => c.source.clip == clip);

        if (channel == null)
        {
            if (volume <= 0f) return;

            channel = new LoopChannel { source = CreateSource("Loop " + clip.name, true) };
            channel.source.clip = clip;
            channel.source.volume = 0f;
            loops.Add(channel);
        }

        channel.targetLevel = Mathf.Clamp01(volume);
        channel.fadeSpeed = 1f / Mathf.Max(.01f, fadeTime);

        if (channel.targetLevel > 0f && !channel.source.isPlaying) channel.source.Play();
    }

    private void UpdateLoops(float delta)
    {
        float ambience = Volume(SoundCategory.Ambience);

        for (int i = 0; i < loops.Count; i++)
        {
            LoopChannel channel = loops[i];
            channel.level = Mathf.MoveTowards(channel.level, channel.targetLevel, delta * channel.fadeSpeed);
            channel.source.volume = channel.level * ambience;

            if (channel.level <= 0f && channel.targetLevel <= 0f && channel.source.isPlaying) channel.source.Stop();
        }
    }

    #endregion

    #region DEBUG

    [Button]
    private void DebugSetTension(float value)
    {
        SetTension(value);
    }

    [Button]
    private void DebugSetDanger(float value)
    {
        SetDanger(value);
    }

    [Button]
    private void DebugPlayAllSfx()
    {
        StartCoroutine(PlayAllSfxRoutine());
    }

    private IEnumerator PlayAllSfxRoutine()
    {
        AudioClip[] clips =
        {
            sfx.click, sfx.hover, sfx.buy, sfx.error, sfx.whoosh, sfx.popup, sfx.dispatch, sfx.hammer, sfx.repaired, sfx.coin,
            sfx.siren, sfx.earthquake, sfx.thunder, sfx.extinguish, sfx.victory, sfx.defeat, sfx.confetti
        };

        foreach (AudioClip clip in clips)
        {
            if (clip == null) continue;

            Debug.Log("AudioManager: " + clip.name + " (" + CategoryOf(clip) + ")");
            PlaySfx(clip);
            yield return new WaitForSecondsRealtime(clip.length + .2f);
        }
    }

    #endregion

#if UNITY_EDITOR
    private void Reset()
    {
        AutoFillClips();
    }

    //Liga cada campo ao arquivo de mesmo nome em Assets/Audio (nomes de ART_REQUESTS.md)
    [ContextMenu("Preencher clipes de Assets/Audio")]
    public void AutoFillClips()
    {
        //Só preenche campo vazio: clipe trocado à mão no Inspector fica como está
        Fill(ref sfx.click, "Sfx_Click");
        Fill(ref sfx.hover, "Sfx_Hover");
        Fill(ref sfx.buy, "Sfx_Buy");
        Fill(ref sfx.error, "Sfx_Error");
        Fill(ref sfx.whoosh, "Sfx_Whoosh");
        Fill(ref sfx.popup, "Sfx_Popup");
        Fill(ref sfx.dispatch, "Sfx_Dispatch");
        Fill(ref sfx.hammer, "Sfx_Hammer");
        Fill(ref sfx.repaired, "Sfx_Repaired");
        Fill(ref sfx.coin, "Sfx_Coin");
        Fill(ref sfx.siren, "Sfx_Siren");
        Fill(ref sfx.earthquake, "Sfx_Earthquake");
        Fill(ref sfx.thunder, "Sfx_Thunder");
        Fill(ref sfx.extinguish, "Sfx_Extinguish");
        Fill(ref sfx.victory, "Sfx_Victory");
        Fill(ref sfx.defeat, "Sfx_Defeat");
        Fill(ref sfx.confetti, "Sfx_Confetti");
        Fill(ref sfx.rainLoop, "Amb_Rain");
        Fill(ref sfx.fireLoop, "Amb_Fire");
        Fill(ref calmMusic, "Music_Calm");
        Fill(ref tenseMusic, "Music_Tense");
        Fill(ref dangerMusic, "Music_Danger");

        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static void Fill(ref AudioClip field, string clipName)
    {
        if (field == null) field = FindClip(clipName);
    }

    private static AudioClip FindClip(string clipName)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets(clipName + " t:AudioClip", new[] { "Assets/Audio" });

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == clipName) return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        Debug.LogWarning("AudioManager: não achei " + clipName + " em Assets/Audio");
        return null;
    }
#endif
}
