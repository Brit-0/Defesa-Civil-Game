using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Janela de configurações (v6D, docs/jam/CONFIG_V6D.md): abas SOM, TELA, JOGO e CRÉDITOS no kit pixel do HUD.
// Abre pela engrenagem do HUD (ao lado da pausa, também no menu inicial) e pelo botão do menu de pausa. Aberta durante
// a partida, o jogo pausa pelo GameManager (timeScale 0 e GameClock parado) e volta ao fechar; aberta pela pausa, volta
// para a pausa. Esc fecha. Monta a própria UI num Canvas acima da pausa; as linhas ficam no SettingsPanel.Rows.cs.
public partial class SettingsPanel : MonoBehaviour
{
    public static SettingsPanel Instance;
    // true ao abrir, false ao fechar (o menu de pausa some e volta)
    public static event Action<bool> OpenChanged;

    public static bool IsOpen => Instance != null && Instance.open;
    // O Esc deste quadro já foi do painel (fechou): a pausa não usa o mesmo aperto
    public static bool BlocksEscape => IsOpen || closedFrame == Time.frameCount;

    [Header("TEXTS")]
    [SerializeField] private string title = "CONFIGURAÇÕES";
    [SerializeField] private string[] tabNames = { "SOM", "TELA", "JOGO", "CRÉDITOS" };
    [SerializeField] private string restoreText = "RESTAURAR PADRÕES";
    [SerializeField] private string backText = "VOLTAR";
    [SerializeField] private string restoredText = "PADRÕES RESTAURADOS";
    [SerializeField] private string versionFormat = "VERSÃO {0}";

    [Header("LAYER")]
    [Tooltip("Acima da pausa (15000), abaixo da tela de fim (20000) e da transição (30000)")]
    [SerializeField] private int sortingOrder = 15100;
    [SerializeField] private int gearSortingOrder = 14000;

    [Header("GEAR (canto do HUD, ao lado da pausa, tela de referência 1920x1080)")]
    [SerializeField] private Vector2 gearAnchor = new Vector2(1f, 1f);
    [SerializeField] private Vector2 gearPosition = new Vector2(-146f, -152f);
    [SerializeField] private Vector2 gearSize = new Vector2(72f, 72f);

    [Header("PANEL")]
    [SerializeField] private Vector2 panelSize = new Vector2(1160f, 800f);
    [SerializeField] private float screenMargin = 32f;
    [SerializeField] private float titleSize = 56f;
    [SerializeField] private Vector2 tabSize = new Vector2(232f, 72f);
    [SerializeField] private float tabFont = 28f;
    [SerializeField] private float tabGap = 14f;
    [SerializeField] private float sideMargin = 48f;
    [SerializeField] private float pageTop = 196f;
    [SerializeField] private float pageBottom = 132f;
    [SerializeField] private Vector2 restoreSize = new Vector2(400f, 80f);
    [SerializeField] private Vector2 backSize = new Vector2(260f, 80f);
    [SerializeField] private float bottomFont = 28f;
    [SerializeField] private float footerSize = 22f;

    [Header("COLORS")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, .65f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color labelColor = FeelUI.PixelCream;
    [SerializeField] private Color hintColor = FeelUI.PixelLilac;
    [SerializeField] private Color accentColor = FeelUI.PixelGold;

    [Header("TIMING")]
    [SerializeField] private float fadeTime = .2f;
    [SerializeField] private float popTime = .3f;
    [SerializeField] private float pageTime = .15f;
    [SerializeField] private float toastTime = 2.4f;

    private static int closedFrame = -1;

    private RectTransform screen;
    private RectTransform holder;
    private RectTransform panel;
    private CanvasGroup group;
    private Canvas screenCanvas;
    private Canvas gearCanvas;
    private GameObject gearRoot;
    private TMP_Text footerLbl;
    private string versionText;
    private float toastUntil = -1f;
    private readonly List<Button> tabButtons = new List<Button>();
    private readonly List<RectTransform> pages = new List<RectTransform>();
    private readonly List<Action> refreshers = new List<Action>();
    private int page;
    private bool open;
    private bool pausedHere;

    private void Awake()
    {
        Instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (GameManager.Instance != null) OnGameStateChanged(GameManager.Instance.State);
    }

    private void OnEnable()
    {
        GameEvents.GameStateChanged += OnGameStateChanged;
        SettingsStore.Changed += OnSettingsChanged;
    }

    private void OnDisable()
    {
        GameEvents.GameStateChanged -= OnGameStateChanged;
        SettingsStore.Changed -= OnSettingsChanged;
    }

    #region BUILD

    private void Build()
    {
        RectTransform layer = FeelUI.HudLayer("Settings");
        if (layer == null) return;

        Vector2 center = new Vector2(.5f, .5f);
        Vector2 top = new Vector2(.5f, 1f);
        FeelUITheme px = FeelUI.Pixel;

        //Engrenagem num Canvas próprio: fica acima do menu inicial, que é a última camada do HUD
        RectTransform gearLayer = FeelUI.CreateRect("Gear", layer);
        FeelUI.Stretch(gearLayer);
        gearCanvas = gearLayer.gameObject.AddComponent<Canvas>();
        gearLayer.gameObject.AddComponent<GraphicRaycaster>();
        Button gear = FeelUI.CreateRoundButton("GearBtn", gearLayer, "OP", gearSize, FeelUI.PanelColor, 30f, SettingsIcons.Gear);
        FeelUI.Place((RectTransform)gear.transform, gearAnchor, center, gearPosition, gearSize);
        gear.onClick.AddListener(() => Open());
        gearRoot = gearLayer.gameObject;

        screen = FeelUI.CreateRect("Screen", layer);
        FeelUI.Stretch(screen);
        group = screen.gameObject.AddComponent<CanvasGroup>();
        screenCanvas = screen.gameObject.AddComponent<Canvas>();
        screen.gameObject.AddComponent<GraphicRaycaster>();

        Image dim = FeelUI.CreateImage("Dim", screen, dimColor);
        dim.raycastTarget = true;
        FeelUI.Stretch(dim.rectTransform);

        //O suporte encolhe o painel se a tela for estreita demais; o painel pula ao abrir
        holder = FeelUI.CreateRect("Holder", screen);
        FeelUI.Place(holder, center, center, Vector2.zero, panelSize);
        panel = FeelUI.CreateWindow("Panel", holder).rectTransform;
        FeelUI.Stretch(panel);

        TMP_Text titleLbl = FeelUI.CreateText("Title", panel, title, titleSize, titleColor);
        FeelUI.Place(titleLbl.rectTransform, top, center, new Vector2(24f, -66f), new Vector2(panelSize.x - 240f, titleSize * 1.2f));
        Image titleIcon = FeelUI.CreateIcon("TitleIcon", panel, SettingsIcons.Gear);
        float titleWidth = titleLbl.GetPreferredValues(title).x;
        FeelUI.Place(titleIcon.rectTransform, top, center, new Vector2(24f - titleWidth * .5f - 44f, -64f), titleIcon.rectTransform.sizeDelta);

        Button close = FeelUI.CreateRoundButton("CloseBtn", panel, "X", new Vector2(72f, 72f), FeelUI.PanelColor, 36f, px != null ? px.iconClose : null);
        FeelUI.Place((RectTransform)close.transform, Vector2.one, center, new Vector2(-62f, -62f), new Vector2(72f, 72f));
        close.onClick.AddListener(Close);

        BuildTabs();
        BuildPages();
        BuildFooter();

        screen.gameObject.SetActive(false);
        gearRoot.SetActive(false);
    }

    private void BuildTabs()
    {
        float total = tabNames.Length * tabSize.x + (tabNames.Length - 1) * tabGap;
        for (int i = 0; i < tabNames.Length; i++)
        {
            int index = i;
            Button tab = FeelUI.CreateButton("Tab " + tabNames[i], panel, tabNames[i], tabSize, FeelUI.ButtonColor, tabFont);
            float x = -total * .5f + tabSize.x * .5f + i * (tabSize.x + tabGap);
            FeelUI.Place((RectTransform)tab.transform, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(x, -150f), tabSize);
            tab.onClick.AddListener(() => SelectPage(index, true));
            tabButtons.Add(tab);
        }
    }

    private void BuildPages()
    {
        Image inset = FeelUI.CreateInset("Pages", panel, FeelUI.PanelColor);
        RectTransform area = inset.rectTransform;
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(sideMargin, pageBottom);
        area.offsetMax = new Vector2(-sideMargin, -pageTop);

        foreach (string tabName in tabNames)
        {
            RectTransform pageRect = FeelUI.CreateRect("Page " + tabName, area);
            FeelUI.Stretch(pageRect);
            pageRect.gameObject.AddComponent<CanvasGroup>();
            pages.Add(pageRect);
        }

        BuildSoundPage(pages[0]);
        BuildScreenPage(pages[1]);
        BuildGamePage(pages[2]);
        BuildCreditsPage(pages[3]);
    }

    private void BuildFooter()
    {
        Vector2 middle = new Vector2(.5f, .5f);
        float y = pageBottom * .5f;

        Button restore = FeelUI.CreateButton("RestoreBtn", panel, restoreText, restoreSize, FeelUI.ButtonColor, bottomFont);
        FeelUI.Place((RectTransform)restore.transform, Vector2.zero, middle, new Vector2(sideMargin + restoreSize.x * .5f, y), restoreSize);
        restore.onClick.AddListener(RestoreDefaults);

        Button back = FeelUI.CreateButton("BackBtn", panel, backText, backSize, FeelUI.ButtonColor, bottomFont, true);
        FeelUI.Place((RectTransform)back.transform, new Vector2(1f, 0f), middle, new Vector2(-sideMargin - backSize.x * .5f, y), backSize);
        back.onClick.AddListener(Close);

        //Entre os dois botões: a versão, ou o aviso da última ação por uns segundos
        versionText = string.Format(versionFormat, Application.version);
        float width = panelSize.x - sideMargin * 2f - restoreSize.x - backSize.x - 32f;
        footerLbl = FeelUI.CreateText("Footer", panel, versionText, footerSize, hintColor);
        FeelUI.Place(footerLbl.rectTransform, new Vector2(.5f, 0f), middle, new Vector2((restoreSize.x - backSize.x) * .5f, y), new Vector2(width, footerSize * 1.4f));
    }

    #endregion

    #region OPEN AND CLOSE

    // Do HUD (engrenagem) ou da pausa. Durante a partida, pausa o jogo e devolve ao fechar.
    public void Open()
    {
        if (open || screen == null || ScreenTransition.Busy) return;

        open = true;
        pausedHere = false;
        closedFrame = -1;
        OpenChanged?.Invoke(true);

        //Pausa antes dos tweens do painel: o GameClock.Freeze pausa todo tween que está tocando
        GameManager game = GameManager.Instance;
        if (game != null && game.State == GameState.Playing)
        {
            game.Pause();
            pausedHere = game.State == GameState.Paused;
        }

        screen.gameObject.SetActive(true);
        SortAbove(screenCanvas, sortingOrder);
        SelectPage(page, false);
        Refresh();
        ShowFooter(versionText, hintColor, -1f);

        group.DOKill();
        group.alpha = 0f;
        group.DOFade(1f, fadeTime).SetUpdate(true).SetLink(screen.gameObject);

        panel.DOKill();
        panel.localScale = Vector3.one * .85f;
        panel.DOScale(1f, popTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(screen.gameObject);
        FitToScreen();

        if (AudioManager.HasSound("ui_popup")) AudioManager.Play("ui_popup");
    }

    public void Close()
    {
        if (!open) return;

        open = false;
        closedFrame = Time.frameCount;
        SettingsStore.Save();

        group.DOKill();
        panel.DOKill();
        panel.localScale = Vector3.one;
        screen.gameObject.SetActive(false);

        GameManager game = GameManager.Instance;
        if (pausedHere && game != null && game.State == GameState.Paused) game.Resume();
        pausedHere = false;

        OpenChanged?.Invoke(false);
    }

    private void Update()
    {
        if (!open) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (toastUntil > 0f && Time.unscaledTime >= toastUntil) ShowFooter(versionText, hintColor, -1f);
        RefreshLive();
    }

    private void LateUpdate()
    {
        if (open) FitToScreen();
    }

    // Tela estreita (janela do navegador em pé) ou HUD grande: o painel encolhe para caber, nunca cresce
    private void FitToScreen()
    {
        Rect area = screen.rect;
        if (area.width <= 0f || area.height <= 0f) return;

        float fit = Mathf.Min(1f, (area.width - screenMargin * 2f) / panelSize.x, (area.height - screenMargin * 2f) / panelSize.y);
        holder.localScale = Vector3.one * Mathf.Max(.3f, fit);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (gearRoot == null) return;

        bool showGear = state == GameState.Playing || state == GameState.Menu;
        gearRoot.SetActive(showGear);
        if (showGear) SortAbove(gearCanvas, gearSortingOrder);

        //Outro sistema tirou da pausa ou a partida acabou com o painel aberto: fecha sem mexer no estado
        if (open && state != GameState.Paused && (pausedHere || state != GameState.Menu))
        {
            pausedHere = false;
            Close();
        }
    }

    // Mesma camada de ordenação do HUD (UI), numa ordem acima dele. Só com o objeto ativo o Canvas guarda o override.
    private static void SortAbove(Canvas canvas, int order)
    {
        canvas.overrideSorting = true;
        canvas.sortingLayerID = canvas.rootCanvas.sortingLayerID;
        canvas.sortingOrder = order;
    }

    #endregion

    #region PAGES

    private void SelectPage(int index, bool animate)
    {
        page = Mathf.Clamp(index, 0, pages.Count - 1);
        for (int i = 0; i < pages.Count; i++)
        {
            bool selected = i == page;
            pages[i].gameObject.SetActive(selected);
            SetSelected(tabButtons[i], selected);
        }

        if (!animate) return;

        CanvasGroup pageGroup = pages[page].GetComponent<CanvasGroup>();
        pageGroup.DOKill();
        pageGroup.alpha = 0f;
        pageGroup.DOFade(1f, pageTime).SetUpdate(true).SetLink(pages[page].gameObject);
        pages[page].DOKill();
        pages[page].anchoredPosition = new Vector2(0f, -12f);
        pages[page].DOAnchorPos(Vector2.zero, pageTime).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(pages[page].gameObject);
        if (AudioManager.HasSound("ui_tab")) AudioManager.Play("ui_tab");
    }

    // Selecionado = botão dourado com texto escuro (como a ação principal das outras telas)
    private void SetSelected(Button button, bool selected)
    {
        FeelUI.SkinButton(button, selected);
        TMP_Text lbl = button.GetComponentInChildren<TMP_Text>(true);
        if (lbl != null) lbl.color = selected && FeelUI.Pixel != null ? FeelUI.PanelColor : Color.white;
    }

    private void OnSettingsChanged(GameSettings s)
    {
        if (open) Refresh();
    }

    private void Refresh()
    {
        foreach (Action refresh in refreshers) refresh();
    }

    private void RestoreDefaults()
    {
        SettingsStore.RestoreDefaults();
        ShowFooter(restoredText, accentColor, toastTime);
        Punch(panel, .03f);
    }

    private void ShowFooter(string text, Color color, float time)
    {
        if (footerLbl == null) return;

        footerLbl.text = text;
        footerLbl.color = color;
        toastUntil = time > 0f ? Time.unscaledTime + time : -1f;
        if (time > 0f) Punch(footerLbl.rectTransform, .15f);
    }

    private static void Punch(Transform target, float amount)
    {
        target.DOKill(true);
        target.localScale = Vector3.one;
        target.DOPunchScale(Vector3.one * amount, .25f, 6, .6f).SetUpdate(true).SetLink(target.gameObject);
    }

    #endregion
}
