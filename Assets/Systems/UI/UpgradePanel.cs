using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Painel de melhorias (lista Para Arrumar, itens 10, 11, 13, 14 e 15). Veste em runtime a UpgradeTab da cena:
//janela maior que sai de cima do botão que a abre, título com margem, abas na base (Equipe, Automação, Expansão e
//Energia, pela UpgradeData.category), lista que rola quando a aba passa do espaço, linhas maiores com o hover e o
//clique dos outros botões e uma janelinha com a descrição no hover (UpgradeTooltip).
//Os botões continuam nascendo no Layout (UpgradesManager); daqui cada um vai para a página da sua aba.
public class UpgradePanel : MonoBehaviour
{
    public const string TabObjectName = "UpgradeTab";
    private const int CategoryCount = 4;

    [Header("TEXTS (uma por UpgradeCategory, na ordem do enum)")]
    [SerializeField] private string[] tabNames = { "EQUIPE", "AUTOMAÇÃO", "EXPANSÃO", "ENERGIA" };

    [Header("LAYOUT (tela de referência 1920x1080, a partir do canto de baixo à esquerda)")]
    [SerializeField] private Vector2 panelPosition = new Vector2(18f, 138f);
    [SerializeField] private Vector2 panelSize = new Vector2(690f, 672f);
    [SerializeField] private float padding = 24f;
    [SerializeField] private float titleLeft = 33f;
    [SerializeField] private float titleSize = 45f;
    [SerializeField] private float headerHeight = 99f;
    [SerializeField] private float closeSize = 57f;
    [SerializeField] private float tabHeight = 90f;
    [SerializeField] private float tabSpacing = 12f;
    [SerializeField] private float tabFontSize = 24f;
    [SerializeField] private float tabIconSize = 42f;
    [SerializeField] private float rowSpacing = 9f;
    [SerializeField] private float rowInset = 12f;
    [SerializeField] private UpgradeRowStyle rowStyle = new UpgradeRowStyle();

    [Header("HOVER DAS ABAS")]
    [SerializeField] private float tabHoverScale = 1.06f;
    [SerializeField] private float tabPressScale = .94f;

    [Header("COLORS")]
    [SerializeField] private Color listColor = new Color(0f, 0f, 0f, .3f);

    [Header("TIMING")]
    [SerializeField] private float openTime = .22f;
    [SerializeField] private float closeTime = .12f;
    [SerializeField] private float openFromScale = .85f;
    [SerializeField] private float refreshInterval = .25f;

    private RectTransform rect;
    private CanvasGroup group;
    private RectTransform layout;
    private ScrollRect scroll;
    private RectTransform[] pages;
    private Button[] tabs;
    private TMP_Text[] tabLabels;
    private Image[] tabIcons;
    private Image[] tabDots;
    private int[] tabSkinned;
    private UpgradeTooltip tooltip;
    private Sequence anim;
    private int current;
    private bool built;
    private bool closing;
    private float refreshTimer;

    public static UpgradePanel Instance { get; set; }
    public UpgradeTooltip Tooltip => tooltip;

    #region OPEN AND CLOSE

    public void Toggle()
    {
        if (gameObject.activeSelf && !closing) Close();
        else Open();
    }

    public void Open()
    {
        closing = false;
        gameObject.SetActive(true);
        Build();

        //Sai do botão de abrir: cresce a partir do canto de baixo à esquerda
        anim?.Kill();
        rect.localScale = Vector3.one * openFromScale;
        group.alpha = 0f;
        anim = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        anim.Join(rect.DOScale(1f, openTime).SetEase(Ease.OutBack));
        anim.Join(group.DOFade(1f, openTime * .6f));
    }

    public void Close()
    {
        if (!gameObject.activeSelf || closing) return;

        closing = true;
        if (tooltip != null) tooltip.Hide();

        anim?.Kill();
        anim = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        anim.Join(rect.DOScale(Mathf.Lerp(openFromScale, 1f, .5f), closeTime).SetEase(Ease.InQuad));
        anim.Join(group.DOFade(0f, closeTime));
        anim.OnComplete(() =>
        {
            closing = false;
            gameObject.SetActive(false);
        });
    }

    private void Awake()
    {
        Instance = this;
        Build();
    }

    private void OnEnable()
    {
        GameEvents.MoneyChanged += OnMoneyChanged;
        GameEvents.UpgradePurchased += OnUpgradePurchased;
        refreshTimer = 0f;
    }

    //Fechou por fora (a expansão desliga a aba): volta ao estado parado
    private void OnDisable()
    {
        GameEvents.MoneyChanged -= OnMoneyChanged;
        GameEvents.UpgradePurchased -= OnUpgradePurchased;

        anim?.Kill();
        closing = false;
        if (rect != null) rect.localScale = Vector3.one;
        if (group != null) group.alpha = 1f;
        if (tooltip != null) tooltip.Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #endregion

    #region BUILD

    private void Build()
    {
        if (built) return;
        built = true;

        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = panelPosition;
        rect.sizeDelta = panelSize;

        //O contorno da cena desenharia uma segunda borda em volta da moldura pixel
        Outline outline = GetComponent<Outline>();
        if (outline != null && FeelUI.Pixel != null) outline.enabled = false;

        layout = FeelUI.FindChild(transform, "Layout") as RectTransform;
        if (layout != null) layout.sizeDelta = Vector2.zero;

        StyleHeader();
        BuildList();
        BuildTabs();
        tooltip = UpgradeTooltip.Create(rect, panelSize.y);

        Adopt();
        SelectTab(0);
    }

    //Título com margem do topo e da esquerda, alinhado ao meio do X
    private void StyleHeader()
    {
        float headerCenter = -(padding + closeSize * .5f);

        Transform titleChild = FeelUI.FindChild(transform, "UpgradeLbl");
        TMP_Text title = titleChild != null ? titleChild.GetComponent<TMP_Text>() : null;
        if (title != null)
        {
            RectTransform t = title.rectTransform;
            t.anchorMin = t.anchorMax = new Vector2(0f, 1f);
            t.pivot = new Vector2(0f, .5f);
            t.anchoredPosition = new Vector2(titleLeft, headerCenter);
            t.sizeDelta = new Vector2(panelSize.x - titleLeft - closeSize - padding * 2f, titleSize * 1.3f);
            title.enableAutoSizing = false;
            title.fontSize = titleSize;
            title.alignment = TextAlignmentOptions.Left;
            title.textWrappingMode = TextWrappingModes.NoWrap;
        }

        RectTransform close = FeelUI.FindChild(transform, "CloseBtn") as RectTransform;
        if (close != null)
        {
            close.anchorMin = close.anchorMax = Vector2.one;
            close.pivot = new Vector2(.5f, .5f);
            close.sizeDelta = Vector2.one * closeSize;
            close.anchoredPosition = new Vector2(-(padding + closeSize * .5f), headerCenter);
            close.SetAsLastSibling();

            //Ícone 2/3 do disco, acima do lábio (como o FeelUI.CreateRoundButton)
            RectTransform icon = close.Find("Icon") as RectTransform;
            if (icon != null)
            {
                icon.sizeDelta = Vector2.one * closeSize * (2f / 3f);
                icon.anchoredPosition = new Vector2(0f, closeSize / 24f);
            }
        }
    }

    //Área funda entre o título e as abas; cada aba tem sua página, e a lista rola se a página passar da altura
    private void BuildList()
    {
        RectTransform list = FeelUI.CreateRect("List", transform);
        list.anchorMin = Vector2.zero;
        list.anchorMax = Vector2.one;
        list.offsetMin = new Vector2(padding, padding + tabHeight + tabSpacing);
        list.offsetMax = new Vector2(-padding, -headerHeight);

        FeelUI.Stretch(FeelUI.CreateInset("Back", list, listColor).rectTransform);

        RectTransform viewport = FeelUI.CreateRect("Viewport", list);
        FeelUI.Stretch(viewport, 2f * FeelUI.PixelSize);
        Image hit = viewport.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>();

        scroll = list.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.scrollSensitivity = rowStyle.height * .5f;

        pages = new RectTransform[CategoryCount];
        for (int i = 0; i < CategoryCount; i++)
        {
            RectTransform page = FeelUI.CreateRect("Page_" + (UpgradeCategory)i, viewport);
            page.anchorMin = new Vector2(0f, 1f);
            page.anchorMax = Vector2.one;
            page.pivot = new Vector2(.5f, 1f);
            page.anchoredPosition = Vector2.zero;
            page.sizeDelta = Vector2.zero;

            VerticalLayoutGroup rows = page.gameObject.AddComponent<VerticalLayoutGroup>();
            int inset = Mathf.RoundToInt(rowInset);
            rows.padding = new RectOffset(inset, inset, inset, inset);
            rows.spacing = rowSpacing;
            rows.childAlignment = TextAnchor.UpperCenter;
            rows.childControlWidth = true;
            rows.childControlHeight = true;
            rows.childForceExpandWidth = true;
            rows.childForceExpandHeight = false;
            page.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            page.gameObject.SetActive(false);
            pages[i] = page;
        }
    }

    //Abas na base do painel: ícone da primeira melhoria da aba em cima, nome embaixo, bolinha quando há compra
    private void BuildTabs()
    {
        RectTransform bar = FeelUI.CreateRect("Tabs", transform);
        bar.anchorMin = Vector2.zero;
        bar.anchorMax = new Vector2(1f, 0f);
        bar.pivot = new Vector2(.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, padding);
        bar.sizeDelta = new Vector2(-padding * 2f, tabHeight);

        HorizontalLayoutGroup row = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = tabSpacing;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = true;

        FeelUITheme px = FeelUI.Pixel;
        float pixel = FeelUI.PixelSize;
        tabs = new Button[CategoryCount];
        tabLabels = new TMP_Text[CategoryCount];
        tabIcons = new Image[CategoryCount];
        tabDots = new Image[CategoryCount];
        tabSkinned = new int[CategoryCount];

        for (int i = 0; i < CategoryCount; i++)
        {
            string label = i < tabNames.Length ? tabNames[i] : ((UpgradeCategory)i).ToString();
            Button tab = FeelUI.CreateButton("Tab_" + (UpgradeCategory)i, bar, label, new Vector2(0f, tabHeight), FeelUI.ButtonColor, tabFontSize);
            tab.GetComponent<Tweener>().SetScales(tabHoverScale, tabPressScale);

            //Nome na faixa de baixo, acima do lábio. Muda só a âncora de cima: a posição que o ButtonPress guardou
            //para subir o texto no hover continua valendo.
            TMP_Text text = tab.GetComponentInChildren<TMP_Text>();
            RectTransform textRect = text.rectTransform;
            textRect.anchorMax = new Vector2(1f, (2f * pixel + tabFontSize * 1.25f) / tabHeight);
            textRect.offsetMin = new Vector2(2f * pixel, textRect.offsetMin.y);
            textRect.offsetMax = new Vector2(-2f * pixel, 0f);
            text.enableAutoSizing = true;
            text.fontSizeMin = tabFontSize * .7f;
            text.fontSizeMax = tabFontSize;

            Image icon = FeelUI.CreateImage("Icon", tab.transform, Color.white);
            icon.preserveAspect = true;
            FeelUI.Place(icon.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -3f * pixel), Vector2.one * tabIconSize);

            Image dot = FeelUI.CreateImage("Ready", tab.transform, px != null ? Color.white : FeelUI.PixelGold, px != null ? px.dotOn : null);
            Vector2 dotSize = px != null && px.dotOn != null ? px.dotOn.rect.size * pixel : Vector2.one * 18f;
            FeelUI.Place(dot.rectTransform, Vector2.one, Vector2.one, new Vector2(-3f * pixel, -3f * pixel), dotSize);
            dot.enabled = false;

            int index = i;
            tab.onClick.AddListener(() => SelectTab(index));

            tabs[i] = tab;
            tabLabels[i] = text;
            tabIcons[i] = icon;
            tabDots[i] = dot;
        }
    }

    #endregion

    #region ROWS

    //Leva para a página da aba cada botão que o UpgradesManager criou no Layout
    private void Adopt()
    {
        if (layout == null || pages == null || layout.childCount == 0) return;

        bool moved = false;
        for (int i = layout.childCount - 1; i >= 0; i--)
        {
            UpgradeButton row = layout.GetChild(i).GetComponent<UpgradeButton>();
            if (row == null || row.Data == null) continue;

            int category = Mathf.Clamp((int)row.Data.category, 0, CategoryCount - 1);
            row.transform.SetParent(pages[category], false);
            rowStyle.Apply(row);
            moved = true;
        }

        if (!moved) return;

        SortPages();
        RefreshTabs();
    }

    private void SortPages()
    {
        foreach (RectTransform page in pages)
        {
            List<UpgradeButton> rows = new List<UpgradeButton>(page.GetComponentsInChildren<UpgradeButton>(true));
            rows.Sort((a, b) => a.Data.order != b.Data.order ? a.Data.order.CompareTo(b.Data.order) : string.CompareOrdinal(a.Data.id, b.Data.id));
            for (int i = 0; i < rows.Count; i++) rows[i].transform.SetSiblingIndex(i);
        }

        for (int i = 0; i < CategoryCount; i++)
        {
            UpgradeButton first = pages[i].GetComponentInChildren<UpgradeButton>(true);
            Sprite icon = first != null ? IconOf(first.Data) : null;
            tabIcons[i].sprite = icon;
            tabIcons[i].enabled = icon != null;
        }
    }

    public static Sprite IconOf(UpgradeData data)
    {
        if (data == null) return null;

        Sprite lab = FeelUI.Theme != null ? FeelUI.Theme.LabIconFor("upgrade_" + data.id) : null;
        return lab != null ? lab : data.icon;
    }

    #endregion

    #region TABS

    public void SelectTab(int index)
    {
        if (pages == null || index < 0 || index >= CategoryCount) return;

        current = index;
        for (int i = 0; i < CategoryCount; i++) pages[i].gameObject.SetActive(i == index);

        scroll.content = pages[index];
        scroll.verticalNormalizedPosition = 1f;
        if (tooltip != null) tooltip.Hide();
        RefreshTabs();
    }

    //Aba sem nenhuma linha ligada (Energia antes da rede chegar) fica desligada; a escolhida fica dourada
    private void RefreshTabs()
    {
        if (tabs == null) return;

        UpgradesManager manager = UpgradesManager.Instance;
        for (int i = 0; i < CategoryCount; i++)
        {
            bool unlocked = false;
            bool ready = false;
            foreach (UpgradeButton row in pages[i].GetComponentsInChildren<UpgradeButton>(true))
            {
                if (!row.gameObject.activeSelf || row.Data == null) continue;

                unlocked = true;
                if (manager != null && manager.CanBuy(row.Data)) ready = true;
            }

            bool selected = i == current;
            Button tab = tabs[i];
            if (tab.interactable != unlocked)
            {
                tab.interactable = unlocked;
                if (unlocked) tab.transform.DOPunchScale(Vector3.one * .15f, .35f, 6, .6f).SetUpdate(true).SetLink(tab.gameObject);
            }

            //Trocar o sprite apaga o hover da hora: só quando a escolha muda
            if (tabSkinned[i] != (selected ? 1 : 0))
            {
                tabSkinned[i] = selected ? 1 : 0;
                FeelUI.SkinButton(tab, selected);
            }
            tabLabels[i].color = !unlocked ? FeelUI.PixelLilac : selected && FeelUI.Pixel != null ? FeelUI.PanelColor : Color.white;
            tabIcons[i].color = unlocked ? Color.white : new Color(1f, 1f, 1f, .4f);
            tabDots[i].enabled = ready && !selected;
        }

        //A aba escolhida perdeu todas as linhas: volta para a primeira que tem
        if (!tabs[current].interactable)
        {
            for (int i = 0; i < CategoryCount; i++)
            {
                if (tabs[i].interactable && i != current)
                {
                    SelectTab(i);
                    return;
                }
            }
        }
    }

    #endregion

    private void LateUpdate()
    {
        Adopt();

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer > 0f) return;

        refreshTimer = refreshInterval;
        RefreshTabs();
    }

    private void OnMoneyChanged(int before, int after) => refreshTimer = 0f;

    private void OnUpgradePurchased(UpgradeData upgrade, int newLevel) => refreshTimer = 0f;
}
