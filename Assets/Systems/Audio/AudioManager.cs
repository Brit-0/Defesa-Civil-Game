using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class SfxLibrary
{
    [Header("INTERFACE")]
    public AudioClip click;
    public AudioClip hover;
    public AudioClip buy;
    public AudioClip error;

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
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField] private float minRepeatInterval = .05f;

    [Header("MUSIC")]
    [SerializeField] private AudioClip calmMusic;
    [SerializeField] private AudioClip tenseMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = .45f;
    [SerializeField] private float tensionFadeTime = 2f;
    [SerializeField] private bool playMusicOnStart = true;

    [Header("LIFETIME")]
    [SerializeField] private bool persistAcrossReload = true;

    private class LoopChannel
    {
        public AudioSource source;
        public float targetVolume;
        public float fadeSpeed;
    }

    private readonly List<AudioSource> pool = new List<AudioSource>();
    private readonly List<LoopChannel> loops = new List<LoopChannel>();
    private readonly Dictionary<AudioClip, float> lastPlayTime = new Dictionary<AudioClip, float>();
    private int nextSource;
    private AudioSource calmSource;
    private AudioSource tenseSource;
    private float tension;
    private float targetTension;

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

    private void Update()
    {
        if (Instance != this) return;

        if (!Mathf.Approximately(tension, targetTension))
        {
            tension = Mathf.MoveTowards(tension, targetTension, Time.unscaledDeltaTime / Mathf.Max(.01f, tensionFadeTime));
            ApplyMusicVolumes();
        }

        UpdateLoops();
    }

    private void BuildSources()
    {
        for (int i = 0; i < poolSize; i++)
        {
            pool.Add(CreateSource("Sfx " + i, false));
        }

        calmSource = CreateSource("Music Calm", true);
        tenseSource = CreateSource("Music Tense", true);
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

    #region SFX

    public static void PlaySfx(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
    {
        if (Instance == null || clip == null) return;

        Instance.PlayFromPool(clip, volume, pitchVariation);
    }

    private void PlayFromPool(AudioClip clip, float volume, float pitchVariation)
    {
        //O mesmo som disparado várias vezes no mesmo instante vira um só
        float now = Time.unscaledTime;
        if (lastPlayTime.TryGetValue(clip, out float last) && now - last < minRepeatInterval) return;
        lastPlayTime[clip] = now;

        AudioSource source = GetFreeSource();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume) * sfxVolume;
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.Play();
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

    public static void SetTension(float value)
    {
        if (Instance == null) return;

        Instance.targetTension = Mathf.Clamp01(value);
    }

    public static void PlayMusic()
    {
        if (Instance == null) return;

        Instance.ApplyMusicVolumes();
        Instance.StartLayer(Instance.calmSource, Instance.calmMusic);
        Instance.StartLayer(Instance.tenseSource, Instance.tenseMusic);
    }

    public static void StopMusic()
    {
        if (Instance == null) return;

        Instance.calmSource.Stop();
        Instance.tenseSource.Stop();
    }

    private void StartLayer(AudioSource source, AudioClip clip)
    {
        if (clip == null || (source.isPlaying && source.clip == clip)) return;

        source.clip = clip;
        source.Play();
    }

    private void ApplyMusicVolumes()
    {
        //Cruzamento de potência constante: a soma das duas camadas não afunda no meio
        float angle = tension * Mathf.PI * .5f;
        calmSource.volume = musicVolume * Mathf.Cos(angle);
        tenseSource.volume = musicVolume * Mathf.Sin(angle);
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

        channel.targetVolume = Mathf.Clamp01(volume) * sfxVolume;
        channel.fadeSpeed = 1f / Mathf.Max(.01f, fadeTime);

        if (channel.targetVolume > 0f && !channel.source.isPlaying) channel.source.Play();
    }

    private void UpdateLoops()
    {
        for (int i = 0; i < loops.Count; i++)
        {
            LoopChannel channel = loops[i];
            if (Mathf.Approximately(channel.source.volume, channel.targetVolume)) continue;

            channel.source.volume = Mathf.MoveTowards(channel.source.volume, channel.targetVolume, Time.unscaledDeltaTime * channel.fadeSpeed);

            if (channel.source.volume <= 0f && channel.targetVolume <= 0f) channel.source.Stop();
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
    private void DebugPlayAllSfx()
    {
        StartCoroutine(PlayAllSfxRoutine());
    }

    private IEnumerator PlayAllSfxRoutine()
    {
        AudioClip[] clips =
        {
            sfx.click, sfx.hover, sfx.buy, sfx.error, sfx.dispatch, sfx.hammer, sfx.repaired, sfx.coin,
            sfx.siren, sfx.earthquake, sfx.thunder, sfx.extinguish, sfx.victory, sfx.defeat
        };

        foreach (AudioClip clip in clips)
        {
            if (clip == null) continue;

            Debug.Log("AudioManager: " + clip.name);
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
        Fill(ref sfx.rainLoop, "Amb_Rain");
        Fill(ref sfx.fireLoop, "Amb_Fire");
        Fill(ref calmMusic, "Music_Calm");
        Fill(ref tenseMusic, "Music_Tense");

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
