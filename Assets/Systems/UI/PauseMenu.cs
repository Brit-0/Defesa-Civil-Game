using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

//Pausa: botão no canto do HUD e as teclas Esc e P chamam GameManager.Pause/Resume; a tela aparece em GameState.Paused.
//Com um chamado aberto, o Esc primeiro fecha o chamado (CancelCall); o próximo pausa (#535). Monta a própria UI, num
//Canvas por cima de todo o HUD, e anima fora do timeScale. CONFIGURAÇÕES abre o SettingsPanel (v6D) por cima; com ele
//aberto a pausa some e as teclas são dele.
public class PauseMenu : MonoBehaviour
{
    [Header("TEXTS")]
    [SerializeField] private string title = "PAUSADO";
    [SerializeField] private string resumeText = "CONTINUAR";
    [SerializeField] private string restartText = "REINICIAR";
    [SerializeField] private string settingsText = "CONFIGURAÇÕES";
    [SerializeField] private string quitText = "SAIR";
    [SerializeField] private string hintText = "Esc ou P pausa e continua";

    [Header("LAYER")]
    [Tooltip("Acima do texto de instrução (100) e do resto do HUD, abaixo da tela de fim (20000) e da transição (30000)")]
    [SerializeField] private int sortingOrder = 15000;
    [SerializeField] private string pauseButtonText = "II";

    [Header("PAUSE BUTTON (canto do HUD, tela de referência 1920x1080)")]
    [SerializeField] private Vector2 buttonAnchor = new Vector2(0f, 0f);
    [SerializeField] private Vector2 buttonPosition = new Vector2(60f, 60f);
    [SerializeField] private Vector2 buttonSize = new Vector2(72f, 72f);

    [Header("PANEL")]
    [SerializeField] private Vector2 panelSize = new Vector2(620f, 460f);
    [SerializeField] private float border = 6f;
    [SerializeField] private float titleSize = 84f;
    [SerializeField] private float hintSize = 26f;
    [SerializeField] private Vector2 optionSize = new Vector2(360f, 84f);

    [Header("COLORS")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, .6f);
    [SerializeField] private Color frameColor = FeelUI.ButtonColor;
    [SerializeField] private Color panelColor = FeelUI.PanelColor;
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color hintColor = new Color(.7f, .73f, .8f);
    [SerializeField] private Color resumeColor = FeelUI.ButtonColor;
    [SerializeField] private Color restartColor = FeelUI.ButtonColor;
    [SerializeField] private Color pauseButtonColor = FeelUI.PanelColor;

    [Header("TIMING")]
    [SerializeField] private float fadeTime = .2f;
    [SerializeField] private float popTime = .3f;

    private RectTransform screen;
    private RectTransform panel;
    private CanvasGroup group;
    private Canvas screenCanvas;
    private GameObject pauseButton;
    //Chamado aberto no fim do quadro anterior: o Esc deste quadro já fechou o chamado (CancelCall roda antes do Update)
    private bool callWasOpen;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        RectTransform parent = FeelUI.ViewRoot(this, "PauseMenu");
        if (parent == null) return;

        Vector2 center = new Vector2(.5f, .5f);
        Vector2 top = new Vector2(.5f, 1f);
        Vector2 bottom = new Vector2(.5f, 0f);

        //Kit pixel: botão redondo com o ícone de pausa no lugar do "II"
        FeelUITheme px = FeelUI.Pixel;
        Button pause = FeelUI.CreateRoundButton("PauseBtn", parent, pauseButtonText, buttonSize, pauseButtonColor, 36f, px != null ? px.iconPause : null);
        FeelUI.Place((RectTransform)pause.transform, buttonAnchor, center, buttonPosition, buttonSize);
        pause.onClick.AddListener(Toggle);
        pauseButton = pause.gameObject;

        screen = FeelUI.CreateRect("Screen", parent);
        FeelUI.Stretch(screen);
        group = screen.gameObject.AddComponent<CanvasGroup>();
        //Canvas próprio por cima de todo o HUD (balão do tutorial, instrução do topo); com raycaster para os botões
        screenCanvas = screen.gameObject.AddComponent<Canvas>();
        screen.gameObject.AddComponent<GraphicRaycaster>();

        Image dim = FeelUI.CreateImage("Dim", screen, dimColor);
        dim.raycastTarget = true;
        FeelUI.Stretch(dim.rectTransform);

        //Três opções, quatro com o Sair: o painel cresce uma linha por opção além de duas (o tamanho do prefab é o de duas)
        bool canQuit = QuitAvailable(Application.platform);
        int options = canQuit ? 4 : 3;
        float step = optionSize.y + 14f;
        Vector2 size = panelSize + new Vector2(0f, step * (options - 2));
        float firstY = 10f + step * (options - 2) * .5f;
        if (px != null)
        {
            panel = FeelUI.CreateWindow("Panel", screen).rectTransform;
            FeelUI.Place(panel, center, center, Vector2.zero, size);
        }
        else
        {
            panel = FeelUI.CreatePanel("Panel", screen, frameColor).rectTransform;
            FeelUI.Place(panel, center, center, Vector2.zero, size);
            FeelUI.Stretch(FeelUI.CreatePanel("Back", panel, panelColor).rectTransform, border);
        }

        TMP_Text titleLbl = FeelUI.CreateText("Title", panel, title, titleSize, titleColor);
        FeelUI.Place(titleLbl.rectTransform, top, center, new Vector2(0f, -90f), new Vector2(panelSize.x, titleSize * 1.2f));

        //Continuar é a ação principal: o botão dourado
        Button resume = FeelUI.CreateButton("ResumeBtn", panel, resumeText, optionSize, resumeColor, 40f, true);
        FeelUI.Place((RectTransform)resume.transform, center, center, new Vector2(0f, firstY), optionSize);
        resume.onClick.AddListener(Resume);

        Button settings = FeelUI.CreateButton("SettingsBtn", panel, settingsText, optionSize, restartColor);
        FeelUI.Place((RectTransform)settings.transform, center, center, new Vector2(0f, firstY - step), optionSize);
        settings.onClick.AddListener(OpenSettings);

        Button restart = FeelUI.CreateButton("RestartBtn", panel, restartText, optionSize, restartColor);
        FeelUI.Place((RectTransform)restart.transform, center, center, new Vector2(0f, firstY - step * 2f), optionSize);
        restart.onClick.AddListener(Restart);

        //Sair fecha o jogo; no navegador (WebGL) não há o que fechar, então o botão nem aparece
        if (canQuit)
        {
            Button quit = FeelUI.CreateButton("QuitBtn", panel, quitText, optionSize, restartColor);
            FeelUI.Place((RectTransform)quit.transform, center, center, new Vector2(0f, firstY - step * 3f), optionSize);
            quit.onClick.AddListener(Quit);
        }

        TMP_Text hintLbl = FeelUI.CreateText("Hint", panel, hintText, hintSize, hintColor);
        FeelUI.Place(hintLbl.rectTransform, bottom, center, new Vector2(0f, 34f), new Vector2(panelSize.x, hintSize * 1.4f));

        screen.gameObject.SetActive(false);
        pauseButton.SetActive(false);
    }

    private void Start()
    {
        if (GameManager.Instance != null) OnGameStateChanged(GameManager.Instance.State);
    }

    private void OnEnable()
    {
        GameEvents.GameStateChanged += OnGameStateChanged;
        SettingsPanel.OpenChanged += OnSettingsOpenChanged;
    }

    private void OnDisable()
    {
        GameEvents.GameStateChanged -= OnGameStateChanged;
        SettingsPanel.OpenChanged -= OnSettingsOpenChanged;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        //Configurações abertas (ou fechadas pelo Esc deste quadro): as teclas são delas
        if (keyboard == null || SettingsPanel.BlocksEscape) return;

        bool paused = GameManager.Instance != null && GameManager.Instance.State == GameState.Paused;
        if (keyboard.pKey.wasPressedThisFrame || (keyboard.escapeKey.wasPressedThisFrame && (paused || !callWasOpen))) Toggle();
    }

    private void LateUpdate()
    {
        callWasOpen = GuardManager.Instance != null && GuardManager.Instance.CurrentCall != null;
    }

    private void OnGameStateChanged(GameState state)
    {
        if (screen == null) return;

        //O botão de pausa só existe durante a partida
        pauseButton.SetActive(state == GameState.Playing);

        if (state == GameState.Paused) Show();
        else Hide();
    }

    private void Show()
    {
        if (screen.gameObject.activeSelf || SettingsPanel.IsOpen) return;

        screen.gameObject.SetActive(true);
        screen.parent.SetAsLastSibling();
        SortAboveHud();

        group.DOKill();
        group.alpha = 0f;
        group.DOFade(1f, fadeTime).SetUpdate(true).SetLink(screen.gameObject);

        panel.DOKill();
        panel.localScale = Vector3.one * .85f;
        panel.DOScale(1f, popTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(screen.gameObject);
    }

    //Mesma camada de ordenação do HUD (UI), numa ordem acima dele. Feito ao abrir: o Canvas aninhado só guarda o
    //overrideSorting quando já está ativo
    private void SortAboveHud()
    {
        Canvas parentCanvas = screen.parent != null ? screen.parent.GetComponentInParent<Canvas>() : null;
        screenCanvas.overrideSorting = true;
        if (parentCanvas != null) screenCanvas.sortingLayerID = parentCanvas.rootCanvas.sortingLayerID;
        screenCanvas.sortingOrder = sortingOrder;
    }

    private void Hide()
    {
        if (!screen.gameObject.activeSelf) return;

        group.DOKill();
        panel.DOKill();
        screen.gameObject.SetActive(false);
    }

    private void Toggle()
    {
        GameManager game = GameManager.Instance;
        if (game == null || ScreenTransition.Busy) return;

        if (game.State == GameState.Playing) game.Pause();
        else if (game.State == GameState.Paused) game.Resume();
    }

    private void Resume()
    {
        if (GameManager.Instance != null) GameManager.Instance.Resume();
    }

    //Configurações por cima: a pausa some enquanto elas estão abertas e volta ao fechar (o jogo segue pausado)
    private void OpenSettings()
    {
        if (SettingsPanel.Instance != null) SettingsPanel.Instance.Open();
    }

    private void OnSettingsOpenChanged(bool settingsOpen)
    {
        if (screen == null) return;

        if (settingsOpen) Hide();
        else if (GameManager.Instance != null && GameManager.Instance.State == GameState.Paused) Show();
    }

    private void Restart()
    {
        if (GameManager.Instance != null && !ScreenTransition.Busy) ScreenTransition.Restart();
    }

    //No WebGL o Application.Quit não fecha a aba do navegador: lá o Sair fica de fora
    public static bool QuitAvailable(RuntimePlatform platform) => platform != RuntimePlatform.WebGLPlayer;

    private void Quit()
    {
        if (!QuitAvailable(Application.platform)) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
