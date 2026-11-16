using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum VfxLevel { Low, Medium, High }

//Qualidade dos efeitos: quantas partículas, quanto ambiente e quantas luzes. Começa em Medium no WebGL e High no resto;
//se o jogo ficar abaixo de minFps por checkTime segundos, desce um nível sozinho (nunca sobe sozinho, para não oscilar).
//Um na cena (Feel.prefab); sem ele valem os padrões do código.
public class VfxQuality : MonoBehaviour
{
    public static VfxQuality Instance;
    public static event Action<VfxLevel> LevelChanged;

    [Header("START")]
    [SerializeField] private VfxLevel desktopLevel = VfxLevel.High;
    [SerializeField] private VfxLevel webLevel = VfxLevel.Medium;

    [Header("AUTO (mede o FPS só durante a partida)")]
    [SerializeField] private bool autoLower = true;
    [SerializeField] private float minFps = 50f;
    [SerializeField] private float checkTime = 3f;

    [Header("DENSITY (fração das partículas por nível)")]
    [SerializeField] private float lowDensity = .35f;
    [SerializeField] private float mediumDensity = .65f;
    [SerializeField] private float highDensity = 1f;

    private static VfxLevel level = DefaultLevel;
    private float slowTime;
    private float smoothedDelta = 1f / 60f;

    private static VfxLevel DefaultLevel => Application.platform == RuntimePlatform.WebGLPlayer ? VfxLevel.Medium : VfxLevel.High;

    public static VfxLevel Level => level;

    //Multiplica quantidades e taxas de emissão
    public static float Density
    {
        get
        {
            if (Instance == null) return level == VfxLevel.Low ? .35f : level == VfxLevel.Medium ? .65f : 1f;
            return level == VfxLevel.Low ? Instance.lowDensity : level == VfxLevel.Medium ? Instance.mediumDensity : Instance.highDensity;
        }
    }

    //Quantidade escalada, com piso: um efeito pedido nunca some de todo
    public static int Scale(int count, int minimum = 1)
    {
        if (count <= 0) return 0;
        return Mathf.Max(Mathf.Min(minimum, count), Mathf.RoundToInt(count * Density));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        LevelChanged = null;
        level = DefaultLevel;
    }

    //Os VFX novos entram sozinhos no objeto do Feel ao carregar a cena, mesmo antes de alguém rodar de novo o
    //Tools/Jam/Create Feel Prefabs (que os grava no Feel.prefab para ajustar no Inspector). Sem Feel na cena, nada.
    private static readonly Type[] Effects = { typeof(VfxQuality), typeof(DisasterFx), typeof(TeamFx), typeof(LandmarkFx), typeof(RewardFx), typeof(AmbientFx) };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AddMissingEffects();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddMissingEffects();
    }

    private static void AddMissingEffects()
    {
        FeelParticles feel = FindAnyObjectByType<FeelParticles>();
        if (feel == null) return;

        foreach (Type effect in Effects)
        {
            if (feel.GetComponent(effect) == null) feel.gameObject.AddComponent(effect);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        SetLevel(Application.platform == RuntimePlatform.WebGLPlayer ? webLevel : desktopLevel);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void SetLevel(VfxLevel newLevel)
    {
        if (level == newLevel) return;

        level = newLevel;
        LevelChanged?.Invoke(level);
    }

    private void Update()
    {
        if (!autoLower || level == VfxLevel.Low) return;

        //Só conta com o jogo rodando: menu, pausa e troca de tela têm picos que não são dos efeitos
        GameManager game = GameManager.Instance;
        if (game == null || game.State != GameState.Playing || Time.unscaledDeltaTime > .25f)
        {
            slowTime = 0f;
            return;
        }

        smoothedDelta = Mathf.Lerp(smoothedDelta, Time.unscaledDeltaTime, .05f);
        slowTime = 1f / smoothedDelta < minFps ? slowTime + Time.unscaledDeltaTime : 0f;
        if (slowTime < checkTime) return;

        slowTime = 0f;
        smoothedDelta = 1f / 60f;
        SetLevel(level - 1);
        Debug.Log($"VfxQuality: FPS abaixo de {minFps} por {checkTime} s, efeitos em {level}.");
    }

    [Button]
    private void DebugSetLevel(VfxLevel newLevel = VfxLevel.Low)
    {
        SetLevel(newLevel);
    }
}
