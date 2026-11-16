using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

//Pausa: botão no canto do HUD e tecla P chamam GameManager.Pause/Resume; a tela aparece em GameState.Paused.
//O Esc fica de fora: já é o cancelar do chamado. Monta a própria UI e anima fora do timeScale.
public class PauseMenu : MonoBehaviour
{
    [Header("TEXTS")]
    [SerializeField] private string title = "PAUSADO";
    [SerializeField] private string resumeText = "CONTINUAR";
    [SerializeField] private string restartText = "REINICIAR";
    [SerializeField] private string hintText = "Tecla P pausa e continua";
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
    private GameObject pauseButton;

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

        Button pause = FeelUI.CreateRoundButton("PauseBtn", parent, pauseButtonText, buttonSize, pauseButtonColor, 36f);
        FeelUI.Place((RectTransform)pause.transform, buttonAnchor, center, buttonPosition, buttonSize);
        pause.onClick.AddListener(Toggle);
        pauseButton = pause.gameObject;

        screen = FeelUI.CreateRect("Screen", parent);
        FeelUI.Stretch(screen);
        group = screen.gameObject.AddComponent<CanvasGroup>();

        Image dim = FeelUI.CreateImage("Dim", screen, dimColor);
        dim.raycastTarget = true;
        FeelUI.Stretch(dim.rectTransform);

        panel = FeelUI.CreatePanel("Panel", screen, frameColor).rectTransform;
        FeelUI.Place(panel, center, center, Vector2.zero, panelSize);
        FeelUI.Stretch(FeelUI.CreatePanel("Back", panel, panelColor).rectTransform, border);

        TMP_Text titleLbl = FeelUI.CreateText("Title", panel, title, titleSize, titleColor);
        FeelUI.Place(titleLbl.rectTransform, top, center, new Vector2(0f, -90f), new Vector2(panelSize.x, titleSize * 1.2f));

        Button resume = FeelUI.CreateButton("ResumeBtn", panel, resumeText, optionSize, resumeColor);
        FeelUI.Place((RectTransform)resume.transform, center, center, new Vector2(0f, 10f), optionSize);
        resume.onClick.AddListener(Resume);

        Button restart = FeelUI.CreateButton("RestartBtn", panel, restartText, optionSize, restartColor);
        FeelUI.Place((RectTransform)restart.transform, center, center, new Vector2(0f, -10f - optionSize.y - 14f), optionSize);
        restart.onClick.AddListener(Restart);

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
    }

    private void OnDisable()
    {
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.pKey.wasPressedThisFrame) Toggle();
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
        if (screen.gameObject.activeSelf) return;

        screen.gameObject.SetActive(true);
        screen.parent.SetAsLastSibling();

        group.DOKill();
        group.alpha = 0f;
        group.DOFade(1f, fadeTime).SetUpdate(true).SetLink(screen.gameObject);

        panel.DOKill();
        panel.localScale = Vector3.one * .85f;
        panel.DOScale(1f, popTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(screen.gameObject);
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
        if (game == null) return;

        if (game.State == GameState.Playing) game.Pause();
        else if (game.State == GameState.Paused) game.Resume();
    }

    private void Resume()
    {
        if (GameManager.Instance != null) GameManager.Instance.Resume();
    }

    private void Restart()
    {
        if (GameManager.Instance != null) GameManager.Instance.Restart();
    }
}
