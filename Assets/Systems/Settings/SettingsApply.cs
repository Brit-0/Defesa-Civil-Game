using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Liga as configurações do jogador nos sistemas (v6D, docs/jam/CONFIG_V6D.md): volumes nos grupos do mixer da v5D,
// partículas pelo VfxQuality, contador de FPS, tamanho do HUD pelo CanvasScaler e vibração. O tremor de tela e o modo
// daltônico leem o SettingsStore na hora (CameraShaker, CallPalette). Nasce sozinho a cada cena, sem mexer na cena.
public class SettingsApply : MonoBehaviour
{
    public static SettingsApply Instance;

    [Header("FPS (canto de cima, tela de referência 1920x1080)")]
    [SerializeField] private float fpsInterval = .5f;
    [SerializeField] private float fpsSize = 26f;
    [SerializeField] private Vector2 fpsAnchor = new Vector2(1f, 0f);
    [SerializeField] private Vector2 fpsPosition = new Vector2(-24f, 18f);
    [SerializeField] private Color fpsColor = FeelUI.PixelCream;
    [SerializeField] private Color fpsSlowColor = new Color32(0xFF, 0x8A, 0x5C, 0xFF);
    [SerializeField] private float fpsSlowBelow = 30f;
    [SerializeField] private int fpsSortingOrder = 19000;

    private readonly Dictionary<CanvasScaler, Vector2> baseReference = new Dictionary<CanvasScaler, Vector2>();
    private TMP_Text fpsLbl;
    private Canvas fpsCanvas;
    private float fpsElapsed;
    private int fpsFrames;
    private bool particlesApplied;
    private bool lastFullParticles;
    private float lastUiScale = -1f;

    #region BOOT

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    // Um por cena com HUD: o objeto das configurações com o painel
    public static void Ensure()
    {
        if (FindAnyObjectByType<SettingsApply>() != null || FeelUI.HudCanvas == null) return;

        GameObject root = new GameObject("[Configurações]");
        root.AddComponent<SettingsApply>();
        root.AddComponent<SettingsPanel>();
    }

    #endregion

    private void Awake()
    {
        Instance = this;
        BuildFps();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        SettingsStore.Changed += Apply;
    }

    private void OnDisable()
    {
        SettingsStore.Changed -= Apply;
        SettingsStore.Save();
    }

    //No Start, depois do Awake de todos: o mixer ignora SetFloat no primeiro Awake e o VfxQuality põe o nível dele
    private void Start()
    {
        Apply(SettingsStore.Current);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SettingsStore.Save();
    }

    private void OnApplicationQuit()
    {
        SettingsStore.Save();
    }

    public void Apply(GameSettings s)
    {
        ApplyAudio(s);
        ApplyParticles(s);
        ApplyUiScale(GameSettings.UiScale(s.uiSize));
        GameHaptics.Apply(s);

        ShowFps(s.showFps);
    }

    // Os efeitos da interface (UiVolume) seguem o volume de efeitos
    private static void ApplyAudio(GameSettings s)
    {
        AudioManager.SetMixerVolume("MasterVolume", s.masterMuted ? 0f : s.master);
        AudioManager.SetMixerVolume("MusicVolume", s.musicMuted ? 0f : s.music);
        AudioManager.SetMixerVolume("SfxVolume", s.sfxMuted ? 0f : s.sfx);
        AudioManager.SetMixerVolume("UiVolume", s.sfxMuted ? 0f : s.sfx);
        AudioManager.SetMixerVolume("AmbienceVolume", s.ambienceMuted ? 0f : s.ambience);
    }

    // Reduzido = nível baixo do VfxQuality. Só mexe quando a opção muda: arrastar um volume não desfaz a descida
    // automática do VfxQuality, e "completo" no começo deixa o nível que ele escolheu para a plataforma
    private void ApplyParticles(GameSettings s)
    {
        if (particlesApplied && lastFullParticles == s.fullParticles) return;

        bool first = !particlesApplied;
        particlesApplied = true;
        lastFullParticles = s.fullParticles;

        if (!s.fullParticles) VfxQuality.SetLevel(VfxLevel.Low);
        else if (!first) VfxQuality.SetLevel(Application.platform == RuntimePlatform.WebGLPlayer ? VfxLevel.Medium : VfxLevel.High);
    }

    // Tamanho do HUD: a resolução de referência dos Canvas de tela (1920x1080) dividida pela escala
    private void ApplyUiScale(float scale)
    {
        if (Mathf.Approximately(scale, lastUiScale)) return;

        lastUiScale = scale;
        foreach (CanvasScaler scaler in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include))
        {
            Canvas canvas = scaler.GetComponent<Canvas>();
            if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;

            if (!baseReference.TryGetValue(scaler, out Vector2 reference))
            {
                reference = scaler.referenceResolution;
                baseReference[scaler] = reference;
            }
            scaler.referenceResolution = reference / scale;
        }
    }

    #region FPS

    private void BuildFps()
    {
        RectTransform layer = FeelUI.HudLayer("SettingsFps");
        if (layer == null) return;

        //Canvas próprio por cima do HUD, das janelas e da pausa (abaixo da tela de fim, 20000, e da transição)
        fpsCanvas = layer.gameObject.AddComponent<Canvas>();
        fpsLbl = FeelUI.CreateText("Fps", layer, "-- FPS", fpsSize, fpsColor, TextAlignmentOptions.BottomRight);
        FeelUI.Outline(fpsLbl, .3f);
        FeelUI.Place(fpsLbl.rectTransform, fpsAnchor, fpsAnchor, fpsPosition, new Vector2(220f, fpsSize * 1.3f));
        layer.gameObject.SetActive(false);
    }

    private void ShowFps(bool show)
    {
        if (fpsLbl == null) return;

        fpsCanvas.gameObject.SetActive(show);
        if (!show) return;

        //O Canvas aninhado só guarda a ordem com o objeto ativo; a camada de ordenação é a do HUD (UI)
        fpsCanvas.overrideSorting = true;
        fpsCanvas.sortingLayerID = fpsCanvas.rootCanvas.sortingLayerID;
        fpsCanvas.sortingOrder = fpsSortingOrder;
    }

    private void Update()
    {
        if (fpsLbl == null || !fpsLbl.gameObject.activeInHierarchy) return;

        //Tempo real (a pausa também desenha quadros)
        fpsFrames++;
        fpsElapsed += Time.unscaledDeltaTime;
        if (fpsElapsed < fpsInterval) return;

        float fps = fpsFrames / Mathf.Max(.0001f, fpsElapsed);
        fpsFrames = 0;
        fpsElapsed = 0f;
        fpsLbl.text = Mathf.RoundToInt(fps) + " FPS";
        fpsLbl.color = fps < fpsSlowBelow ? fpsSlowColor : fpsColor;
    }

    #endregion
}
