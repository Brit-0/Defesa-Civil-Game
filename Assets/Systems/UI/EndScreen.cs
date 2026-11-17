using System;
using DG.Tweening;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Tela de fim de partida. Só escuta GameStateChanged: em Won ou Lost mostra tempo, casas consertadas, desastres
//enfrentados e dinheiro ganho, lidos do GameManager, e os botões Jogar de novo e Menu. Monta a própria UI: basta soltar
//o prefab no UICanvas. Tem Canvas próprio acima de todo o HUD, que some por trás (lista Para Arrumar: game over
//com UI sobreposta).
//Vitória: faixa verde, raios girando atrás do painel, confete dos cantos e chuva de confete.
//Derrota: faixa vermelha, painel cai com tremida, título treme e cinza caindo enquanto a tela está aberta.
public class EndScreen : MonoBehaviour
{
    [Header("TEXTS (o efeito é uma tag do Text Animator: wave, bounce, shake...; vazio = sem efeito)")]
    [SerializeField] private string wonTitle = "CIDADE SALVA!";
    [SerializeField] private string wonSubtitle = "A Defesa Civil deu conta do recado.";
    [SerializeField] private string wonTitleEffect = "wave";
    [SerializeField] private string lostTitle = "O CAOS VENCEU";
    [SerializeField] private string lostSubtitle = "A cidade ficou tempo demais sem socorro.";
    [SerializeField] private string lostTitleEffect = "shake";
    [Tooltip("Tempo, casas, desastres e dinheiro, nessa ordem")]
    [SerializeField] private string[] statNames = { "Tempo de jogo", "Casas consertadas", "Desastres enfrentados", "Dinheiro ganho" };
    [SerializeField] private string clockFormat = "{0:00}:{1:00}";
    [SerializeField] private string countFormat = "{0}";
    [SerializeField] private string playAgainText = "JOGAR DE NOVO";
    [SerializeField] private string menuText = "MENU";

    [Header("LAYOUT (tela de referência 1920x1080)")]
    [Tooltip("Acima de todo o HUD e abaixo da transição de tela (30000)")]
    [SerializeField] private int sortingOrder = 20000;
    [SerializeField] private Vector2 windowSize = new Vector2(900f, 660f);
    [SerializeField] private float border = 6f;
    [SerializeField] private Vector2 ribbonSize = new Vector2(980f, 130f);
    [SerializeField] private float titleSize = 76f;
    [SerializeField] private float subtitleSize = 30f;
    [SerializeField] private float subtitleY = -116f;
    [SerializeField] private Vector2 cardSize = new Vector2(396f, 132f);
    [SerializeField] private float cardGap = 24f;
    [SerializeField] private float cardsTop = -168f;
    [SerializeField] private float cardIconHeight = 66f;
    [SerializeField] private float cardNameSize = 26f;
    [SerializeField] private float cardValueSize = 52f;
    [SerializeField] private Vector2 playSize = new Vector2(390f, 96f);
    [SerializeField] private Vector2 menuSize = new Vector2(240f, 96f);
    [SerializeField] private float buttonsGap = 24f;
    [SerializeField] private float buttonsBottom = 42f;
    [SerializeField] private float buttonFontSize = 40f;

    [Header("COLORS")]
    [SerializeField] private Color wonDimColor = new Color(0f, .02f, .04f, .72f);
    [SerializeField] private Color lostDimColor = new Color(.12f, 0f, 0f, .78f);
    [SerializeField] private Color frameColor = FeelUI.ButtonColor;
    [SerializeField] private Color panelColor = FeelUI.PanelColor;
    [SerializeField] private Color edgeColor = new Color(0f, 0f, 0f, .85f);
    [SerializeField] private Color wonColor = new Color(.25f, .62f, .33f);
    [SerializeField] private Color lostColor = new Color(.72f, .2f, .17f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color subtitleColor = new Color(.75f, .78f, .85f);
    [SerializeField] private Color rowColor = new Color(0f, 0f, 0f, .3f);
    [SerializeField] private Color statNameColor = FeelUI.PixelLilac;
    [SerializeField] private Color valueColor = new Color(1f, .82f, .3f);
    [SerializeField] private Color buttonColor = FeelUI.ButtonColor;

    [Header("RAYS (vitória: faixas de luz girando atrás do painel)")]
    [SerializeField] private bool showRays = true;
    [SerializeField] private int rayCount = 12;
    [SerializeField] private float rayLength = 1100f;
    [SerializeField] private float rayWidth = 110f;
    [SerializeField] private Color rayColor = new Color(1f, .85f, .4f, .1f);
    [SerializeField] private float raySpinTime = 30f;

    [Header("CONFETTI (vitória)")]
    [SerializeField] private bool confetti = true;
    [Tooltip("Canhões nos cantos de baixo: posição em fração da largura, a partir do centro")]
    [SerializeField] private float cannonInset = .45f;
    [Tooltip("Inclinação do canhão a partir da vertical, em graus")]
    [SerializeField] private float cannonTilt = 25f;
    [SerializeField] private float rainPerSecond = 10f;
    [SerializeField] private float rainTime = 4f;
    [SerializeField, Range(0f, 1f)] private float confettiVolume = .7f;

    [Header("ASH (derrota)")]
    [SerializeField] private bool ash = true;
    [SerializeField] private float ashPerSecond = 14f;

    [Header("TIMING")]
    [SerializeField] private float showDelay = .9f;
    [SerializeField] private float fadeTime = .35f;
    [Tooltip("O HUD da partida some por trás da tela de fim")]
    [SerializeField] private float hudFadeTime = .3f;
    [SerializeField] private float popTime = .4f;
    [SerializeField] private float dropDistance = 420f;
    [SerializeField] private float lostShake = 22f;
    [SerializeField] private float statInterval = .15f;
    [SerializeField] private float countTime = .6f;
    [SerializeField] private float valuePunch = .25f;
    [SerializeField, Range(0f, 1f)] private float statSoundVolume = .5f;
    [SerializeField, Range(0f, 1f)] private float coinVolume = .45f;

    private RectTransform screen;
    private Canvas screenCanvas;
    private Image dim;
    private RectTransform rays;
    private RectTransform fxLayer;
    private RectTransform panel;
    private CanvasGroup group;
    private CanvasGroup hudGroup;
    private Image ribbonFill;
    private Image[] ribbonIcons;
    private RectTransform ribbon;
    private TextAnimator_TMP titleAnimator;
    private TMP_Text subtitleLbl;
    private RectTransform[] statCards;
    private TMP_Text[] statValues;
    private RectTransform buttonHolder;
    private Sequence sequence;

    private bool shownWon;
    private float fxTime;
    private float fxDebt;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        RectTransform parent = FeelUI.ViewRoot(this, "EndScreen");
        if (parent == null) return;

        Vector2 center = new Vector2(.5f, .5f);
        Vector2 top = new Vector2(.5f, 1f);

        screen = FeelUI.CreateRect("Screen", parent);
        FeelUI.Stretch(screen);
        //Canvas próprio: nenhuma camada do HUD criada depois (dicas, avisos, minigame) passa por cima
        screenCanvas = screen.gameObject.AddComponent<Canvas>();
        screen.gameObject.AddComponent<GraphicRaycaster>();
        group = screen.gameObject.AddComponent<CanvasGroup>();
        group.ignoreParentGroups = true;

        //O fundo escuro segura os cliques que iriam para a cidade
        dim = FeelUI.CreateImage("Dim", screen, wonDimColor);
        dim.raycastTarget = true;
        FeelUI.Stretch(dim.rectTransform);

        BuildRays();

        //Confete e cinza ficam atrás do painel, para não cobrir os números
        fxLayer = FeelUI.CreateRect("Fx", screen);
        FeelUI.Stretch(fxLayer);

        //Kit pixel: janela com rebites e a faixa de rabo de andorinha tingida de verde ou vermelho
        FeelUITheme px = FeelUI.Pixel;
        float pixel = FeelUI.PixelSize;

        if (px != null)
        {
            panel = FeelUI.CreateWindow("Panel", screen).rectTransform;
            FeelUI.Place(panel, center, center, Vector2.zero, windowSize);

            //Altura do sprite em pixels inteiros: o V das pontas não estica
            Vector2 size = new Vector2(ribbonSize.x, px.ribbon.rect.height * pixel);
            ribbonFill = FeelUI.CreateSliced("Ribbon", panel, px.ribbon, wonColor);
            ribbon = ribbonFill.rectTransform;
            FeelUI.Place(ribbon, top, center, Vector2.zero, size);

            //Estrela na vitória, fogo na derrota, perto das pontas
            ribbonIcons = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                ribbonIcons[i] = FeelUI.CreateIcon("Badge", ribbon, px.iconStar);
                ribbonIcons[i].rectTransform.anchoredPosition = new Vector2((i == 0 ? -1f : 1f) * (size.x * .5f - 16f * pixel - 30f), pixel);
            }
        }
        else
        {
            panel = FeelUI.CreatePanel("Panel", screen, frameColor).rectTransform;
            FeelUI.Place(panel, center, center, Vector2.zero, windowSize);
            FeelUI.Stretch(FeelUI.CreatePanel("Back", panel, panelColor).rectTransform, border);

            //Faixa do título atravessando o topo do painel
            ribbon = FeelUI.CreatePanel("Ribbon", panel, edgeColor).rectTransform;
            FeelUI.Place(ribbon, top, center, Vector2.zero, ribbonSize);
            ribbonFill = FeelUI.CreatePanel("Fill", ribbon, wonColor);
            FeelUI.Stretch(ribbonFill.rectTransform, border);
        }

        TMP_Text title = FeelUI.CreateText("Title", ribbon, "", titleSize, textColor);
        FeelUI.Stretch(title.rectTransform, border * 2f);
        //Na faixa pixel o texto fica entre as estrelas e acima da sombra de baixo
        if (px != null)
        {
            title.rectTransform.offsetMin = new Vector2(20f * pixel + 60f, 4f * pixel);
            title.rectTransform.offsetMax = new Vector2(-(20f * pixel + 60f), -2f * pixel);
        }
        FeelUI.Outline(title, .3f);
        title.enableAutoSizing = true;
        title.fontSizeMin = titleSize * .5f;
        title.fontSizeMax = titleSize;
        titleAnimator = title.gameObject.AddComponent<TextAnimator_TMP>();

        subtitleLbl = FeelUI.CreateText("Subtitle", panel, wonSubtitle, subtitleSize, subtitleColor);
        FeelUI.Place(subtitleLbl.rectTransform, top, center, new Vector2(0f, subtitleY), new Vector2(windowSize.x - 80f, subtitleSize * 1.4f));

        BuildCards(px);
        BuildButtons();

        screen.gameObject.SetActive(false);
    }

    //Grade 2x2 de cartões: ícone à esquerda, nome pequeno em cima e o valor grande em dourado embaixo
    private void BuildCards(FeelUITheme px)
    {
        Sprite[] icons = px == null ? new Sprite[4] : new[] { px.iconClock, px.iconHouse, px.iconEarthquake, px.iconCoin };
        int count = Mathf.Min(statNames.Length, icons.Length);
        statCards = new RectTransform[count];
        statValues = new TMP_Text[count];

        Vector2 top = new Vector2(.5f, 1f);
        for (int i = 0; i < count; i++)
        {
            float x = (i % 2 == 0 ? -1f : 1f) * (cardSize.x + cardGap) * .5f;
            float y = cardsTop - cardSize.y * .5f - (i / 2) * (cardSize.y + cardGap);
            RectTransform card = FeelUI.CreateInset("Stat" + i, panel, rowColor).rectTransform;
            FeelUI.Place(card, top, new Vector2(.5f, .5f), new Vector2(x, y), cardSize);
            statCards[i] = card;

            float textLeft = 24f;
            if (icons[i] != null)
            {
                Image icon = FeelUI.CreateIcon("Icon", card, icons[i], cardIconHeight / (icons[i].rect.height * FeelUI.PixelSize));
                RectTransform iconRect = icon.rectTransform;
                iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, .5f);
                iconRect.anchoredPosition = new Vector2(21f + iconRect.sizeDelta.x * .5f, 0f);
                textLeft = 21f + iconRect.sizeDelta.x + 18f;
            }

            //O nome ocupa a metade de cima e o valor a de baixo, os dois alinhados à esquerda depois do ícone
            TMP_Text nameLbl = FeelUI.CreateText("Name", card, statNames[i], cardNameSize, statNameColor, TextAlignmentOptions.BottomLeft);
            AnchorText(nameLbl.rectTransform, .56f, 1f, textLeft);
            nameLbl.textWrappingMode = TextWrappingModes.NoWrap;

            TMP_Text valueLbl = FeelUI.CreateText("Value", card, "", cardValueSize, valueColor, TextAlignmentOptions.TopLeft);
            AnchorText(valueLbl.rectTransform, 0f, .56f, textLeft);
            FeelUI.Outline(valueLbl);
            statValues[i] = valueLbl;
        }
    }

    private static void AnchorText(RectTransform label, float bottom, float top, float left)
    {
        label.anchorMin = new Vector2(0f, bottom);
        label.anchorMax = new Vector2(1f, top);
        label.offsetMin = new Vector2(left, bottom > 0f ? 0f : 12f);
        label.offsetMax = new Vector2(-18f, top < 1f ? 0f : -12f);
    }

    //Os dois botões ficam num suporte: a escala dos botões é do Tweener (hover), a do suporte é da entrada
    private void BuildButtons()
    {
        float width = playSize.x + buttonsGap + menuSize.x;
        float height = Mathf.Max(playSize.y, menuSize.y);
        buttonHolder = FeelUI.CreateRect("ButtonHolder", panel);
        FeelUI.Place(buttonHolder, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, buttonsBottom + height * .5f), new Vector2(width, height));

        //Jogar de novo é o dourado (a ação principal); Menu volta para a tela inicial
        Button play = FeelUI.CreateButton("PlayAgainBtn", buttonHolder, playAgainText, playSize, buttonColor, buttonFontSize, true);
        play.GetComponent<RectTransform>().anchoredPosition = new Vector2(-width * .5f + playSize.x * .5f, 0f);
        play.onClick.AddListener(() => Restart(false));

        Button menu = FeelUI.CreateButton("MenuBtn", buttonHolder, menuText, menuSize, buttonColor, buttonFontSize);
        menu.GetComponent<RectTransform>().anchoredPosition = new Vector2(width * .5f - menuSize.x * .5f, 0f);
        menu.onClick.AddListener(() => Restart(true));
    }

    //Leque de faixas do RoundRectangle saindo do centro, cada uma com o pivô na ponta de dentro
    private void BuildRays()
    {
        rays = FeelUI.CreateRect("Rays", screen);
        rays.anchorMin = rays.anchorMax = new Vector2(.5f, .5f);
        rays.sizeDelta = Vector2.zero;

        for (int i = 0; i < rayCount; i++)
        {
            //No kit pixel a faixa é reta (o canto arredondado do RoundRectangle não combina com o pixel)
            RectTransform ray = (FeelUI.Pixel != null ? FeelUI.CreateImage("Ray" + i, rays, rayColor) : FeelUI.CreatePanel("Ray" + i, rays, rayColor)).rectTransform;
            ray.anchorMin = ray.anchorMax = new Vector2(.5f, .5f);
            ray.pivot = new Vector2(0f, .5f);
            ray.sizeDelta = new Vector2(rayLength, rayWidth);
            ray.anchoredPosition = Vector2.zero;
            ray.localRotation = Quaternion.Euler(0f, 0f, 360f / rayCount * i);
            ray.GetComponent<Image>().raycastTarget = false;
        }
    }

    private void Start()
    {
        //Caso a tela entre na cena com a partida já encerrada
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

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Won) Show(true);
        else if (state == GameState.Lost) Show(false);
        else Hide();
    }

    private void Show(bool won)
    {
        if (screen == null || screen.gameObject.activeSelf) return;

        GameManager game = GameManager.Instance;
        float elapsed = game != null ? game.ElapsedTime : 0f;
        int houses = game != null ? game.HousesRepaired : 0;
        int disasters = game != null ? game.DisastersFaced : 0;
        int money = game != null ? game.TotalMoneyEarned : 0;

        shownWon = won;
        fxTime = 0f;
        fxDebt = 0f;

        dim.color = won ? wonDimColor : lostDimColor;
        ribbonFill.color = won ? wonColor : lostColor;
        if (ribbonIcons != null && FeelUI.Pixel != null)
        {
            foreach (Image badge in ribbonIcons) badge.sprite = won ? FeelUI.Pixel.iconStar : FeelUI.Pixel.iconFire;
        }
        subtitleLbl.text = won ? wonSubtitle : lostSubtitle;

        foreach (RectTransform card in statCards) card.localScale = new Vector3(1f, 0f, 1f);

        screen.gameObject.SetActive(true);
        SortAboveHud();
        if (UpgradePanel.Instance != null) UpgradePanel.Instance.Close();
        FadeHud(true);
        titleAnimator.SetText(Tagged(won ? wonTitle : lostTitle, won ? wonTitleEffect : lostTitleEffect));
        group.alpha = 0f;
        group.interactable = false;
        buttonHolder.localScale = Vector3.zero;
        ribbon.localScale = Vector3.zero;
        rays.gameObject.SetActive(won && showRays);
        rays.localScale = Vector3.zero;

        //A partida acabou: tudo aqui roda fora do timeScale
        sequence?.Kill();
        panel.DOKill();
        rays.DOKill();
        sequence = DOTween.Sequence().SetUpdate(true).SetLink(screen.gameObject);
        sequence.AppendInterval(showDelay);
        sequence.Append(group.DOFade(1f, fadeTime));

        if (won)
        {
            //Painel sobe pulando, raios abrem e o confete sai dos cantos
            panel.anchoredPosition = new Vector2(0f, -dropDistance);
            panel.localScale = Vector3.one * .8f;
            sequence.Join(panel.DOAnchorPosY(0f, popTime).SetEase(Ease.OutBack));
            sequence.Join(panel.DOScale(1f, popTime).SetEase(Ease.OutBack));
            sequence.Join(rays.DOScale(1f, popTime * 2f).SetEase(Ease.OutCubic));
            sequence.InsertCallback(showDelay + popTime * .5f, ConfettiBurst);
            rays.localRotation = Quaternion.identity;
            if (raySpinTime > 0f)
            {
                rays.DOLocalRotate(new Vector3(0f, 0f, -360f), raySpinTime, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear).SetLoops(-1).SetUpdate(true).SetLink(rays.gameObject);
            }
        }
        else
        {
            //Painel cai pesado e a batida sacode
            panel.anchoredPosition = new Vector2(0f, dropDistance);
            panel.localScale = Vector3.one;
            sequence.Join(panel.DOAnchorPosY(0f, popTime).SetEase(Ease.InQuad));
            sequence.Append(panel.DOShakeAnchorPos(popTime, new Vector2(lostShake, lostShake * .6f), 18, 90f, false, true));
        }

        //A faixa do título carimba por cima do painel
        sequence.Append(ribbon.DOScale(1f, popTime).SetEase(Ease.OutBack));

        AppendStat(0, elapsed, value => string.Format(clockFormat, Mathf.FloorToInt(value / 60f), Mathf.FloorToInt(value % 60f)));
        AppendStat(1, houses, Count);
        AppendStat(2, disasters, Count);
        AppendStat(3, money, Count);

        sequence.AppendInterval(statInterval);
        sequence.AppendCallback(() =>
        {
            group.interactable = true;
            if (won) ConfettiBurst();
        });
        sequence.Append(buttonHolder.DOScale(1f, popTime).SetEase(Ease.OutBack));
    }

    private string Count(float value) => string.Format(countFormat, Mathf.RoundToInt(value));

    //Mesma camada de ordenação do HUD, numa ordem acima dele. Feito ao abrir: o Canvas aninhado só guarda o
    //overrideSorting quando já está ativo
    private void SortAboveHud()
    {
        Canvas parentCanvas = screen.parent != null ? screen.parent.GetComponentInParent<Canvas>() : null;
        screenCanvas.overrideSorting = true;
        if (parentCanvas != null) screenCanvas.sortingLayerID = parentCanvas.rootCanvas.sortingLayerID;
        screenCanvas.sortingOrder = sortingOrder;
    }

    //O HUD da partida apaga por trás e para de receber clique; a tela de fim ignora o grupo dele
    private void FadeHud(bool hide)
    {
        Canvas hud = FeelUI.HudCanvas;
        if (hud == null) return;

        if (hudGroup == null)
        {
            hudGroup = hud.GetComponent<CanvasGroup>();
            if (hudGroup == null) hudGroup = hud.gameObject.AddComponent<CanvasGroup>();
        }

        hudGroup.DOKill();
        hudGroup.blocksRaycasts = !hide;
        hudGroup.interactable = !hide;
        hudGroup.DOFade(hide ? 0f : 1f, hudFadeTime).SetUpdate(true).SetLink(gameObject);
    }

    private static string Tagged(string text, string effect)
    {
        return string.IsNullOrEmpty(effect) ? text : "<" + effect + ">" + text + "</" + effect + ">";
    }

    //Cada cartão abre, o valor conta de zero até o total e dá um pulo com moeda no fim
    private void AppendStat(int index, float target, Func<float, string> format)
    {
        if (index >= statCards.Length) return;

        RectTransform card = statCards[index];
        TMP_Text label = statValues[index];
        float shown = 0f;
        label.text = format(0f);

        sequence.AppendInterval(statInterval);
        sequence.AppendCallback(() => AudioManager.PlaySfx(AudioManager.Sfx?.click, statSoundVolume, .05f));
        sequence.Append(card.DOScaleY(1f, popTime * .5f).SetEase(Ease.OutBack));
        sequence.Append(DOTween.To(() => shown, value => { shown = value; label.text = format(value); }, target, countTime).SetEase(Ease.OutQuad));
        sequence.AppendCallback(() =>
        {
            label.text = format(target);
            label.transform.DOKill();
            label.transform.localScale = Vector3.one;
            label.transform.DOPunchScale(Vector3.one * valuePunch, popTime, 6, .5f).SetUpdate(true).SetLink(label.gameObject);
            if (target > 0f) AudioManager.PlaySfx(AudioManager.Sfx?.coin, coinVolume, .08f);
        });
    }

    #region FX

    private void ConfettiBurst()
    {
        if (!confetti || fxLayer == null) return;

        float height = fxLayer.rect.height;
        float x = fxLayer.rect.width * cannonInset;
        FeelParticles.Emit(FxKind.Confetti, fxLayer, new Vector2(-x, -height * .5f), height, -1, 90f - cannonTilt);
        FeelParticles.Emit(FxKind.Confetti, fxLayer, new Vector2(x, -height * .5f), height, -1, 90f + cannonTilt);
        AudioManager.PlaySfx(AudioManager.Sfx?.confetti, confettiVolume, .05f);
    }

    //Chuva de confete do alto por um tempo na vitória; cinza caindo o tempo todo na derrota
    private void Update()
    {
        if (screen == null || !screen.gameObject.activeSelf || group.alpha <= 0f) return;

        fxTime += Time.unscaledDeltaTime;
        bool raining = shownWon ? confetti && fxTime < rainTime : ash;
        if (!raining) return;

        fxDebt += (shownWon ? rainPerSecond : ashPerSecond) * Time.unscaledDeltaTime;
        if (fxDebt < 1f) return;

        int count = Mathf.FloorToInt(fxDebt);
        fxDebt -= count;

        Rect area = fxLayer.rect;
        if (shownWon)
        {
            FeelParticles.Emit(FxKind.Confetti, fxLayer, new Vector2(0f, area.height * .55f), area.height, count, 270f, area.width);
            return;
        }

        //A cinza nasce em qualquer altura da metade de cima e aparece devagar (fadeIn do preset)
        for (int i = 0; i < count; i++)
        {
            Vector2 position = new Vector2(0f, UnityEngine.Random.Range(0f, area.height * .5f));
            FeelParticles.Emit(FxKind.Ash, fxLayer, position, area.height, 1, float.NaN, area.width);
        }
    }

    #endregion

    private void Hide()
    {
        if (screen == null || !screen.gameObject.activeSelf) return;

        sequence?.Kill();
        rays.DOKill();
        screen.gameObject.SetActive(false);
        FadeHud(false);
    }

    //toMenu: a cena recarrega no menu inicial; senão a partida recomeça direto
    private void Restart(bool toMenu)
    {
        if (!ScreenTransition.Busy) ScreenTransition.Restart(toMenu);
    }

    [Button]
    private void DebugShow(bool won = true)
    {
        Hide();
        Show(won);
    }
}
