using System;
using DG.Tweening;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Tela de fim de partida. Só escuta GameStateChanged: em Won ou Lost mostra tempo, casas consertadas e dinheiro,
//lidos do GameManager, e o botão Reiniciar. Monta a própria UI: basta soltar o prefab no UICanvas.
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
    [SerializeField] private string timeName = "Tempo";
    [SerializeField] private string housesName = "Casas consertadas";
    [SerializeField] private string moneyName = "Dinheiro arrecadado";
    [SerializeField] private string timeFormat = "{0:00}:{1:00}";
    [SerializeField] private string housesFormat = "{0}";
    [SerializeField] private string moneyFormat = "{0}";
    [SerializeField] private string restartText = "REINICIAR";

    [Header("LAYOUT (tela de referência 1920x1080)")]
    [SerializeField] private Vector2 panelSize = new Vector2(860f, 620f);
    [SerializeField] private float border = 6f;
    [SerializeField] private Vector2 ribbonSize = new Vector2(980f, 130f);
    [SerializeField] private float titleSize = 76f;
    [SerializeField] private float subtitleSize = 30f;
    [SerializeField] private float subtitleY = -116f;
    [SerializeField] private Vector2 statRowSize = new Vector2(720f, 66f);
    [SerializeField] private float statSize = 38f;
    [SerializeField] private float statTop = -196f;
    [SerializeField] private float statSpacing = 80f;
    [SerializeField] private Vector2 buttonSize = new Vector2(340f, 88f);
    [SerializeField] private float buttonBottom = 40f;

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
    [SerializeField] private float popTime = .4f;
    [SerializeField] private float dropDistance = 420f;
    [SerializeField] private float lostShake = 22f;
    [SerializeField] private float statInterval = .15f;
    [SerializeField] private float countTime = .6f;
    [SerializeField] private float valuePunch = .25f;
    [SerializeField, Range(0f, 1f)] private float statSoundVolume = .5f;
    [SerializeField, Range(0f, 1f)] private float coinVolume = .45f;

    private RectTransform screen;
    private Image dim;
    private RectTransform rays;
    private RectTransform fxLayer;
    private RectTransform panel;
    private CanvasGroup group;
    private Image ribbonFill;
    private RectTransform ribbon;
    private TextAnimator_TMP titleAnimator;
    private TMP_Text subtitleLbl;
    private RectTransform[] statRows;
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
        group = screen.gameObject.AddComponent<CanvasGroup>();

        //O fundo escuro segura os cliques que iriam para o HUD
        dim = FeelUI.CreateImage("Dim", screen, wonDimColor);
        dim.raycastTarget = true;
        FeelUI.Stretch(dim.rectTransform);

        BuildRays();

        //Confete e cinza ficam atrás do painel, para não cobrir os números
        fxLayer = FeelUI.CreateRect("Fx", screen);
        FeelUI.Stretch(fxLayer);

        panel = FeelUI.CreatePanel("Panel", screen, frameColor).rectTransform;
        FeelUI.Place(panel, center, center, Vector2.zero, panelSize);
        FeelUI.Stretch(FeelUI.CreatePanel("Back", panel, panelColor).rectTransform, border);

        //Faixa do título atravessando o topo do painel
        ribbon = FeelUI.CreatePanel("Ribbon", panel, edgeColor).rectTransform;
        FeelUI.Place(ribbon, top, center, Vector2.zero, ribbonSize);
        ribbonFill = FeelUI.CreatePanel("Fill", ribbon, wonColor);
        FeelUI.Stretch(ribbonFill.rectTransform, border);

        TMP_Text title = FeelUI.CreateText("Title", ribbon, "", titleSize, textColor);
        FeelUI.Stretch(title.rectTransform, border * 2f);
        FeelUI.Outline(title, .3f);
        title.enableAutoSizing = true;
        title.fontSizeMin = titleSize * .5f;
        title.fontSizeMax = titleSize;
        titleAnimator = title.gameObject.AddComponent<TextAnimator_TMP>();

        subtitleLbl = FeelUI.CreateText("Subtitle", panel, wonSubtitle, subtitleSize, subtitleColor);
        FeelUI.Place(subtitleLbl.rectTransform, top, center, new Vector2(0f, subtitleY), new Vector2(panelSize.x - 80f, subtitleSize * 1.4f));

        //Cada estatística numa linha: nome à esquerda, valor dourado à direita
        string[] names = { timeName, housesName, moneyName };
        statRows = new RectTransform[names.Length];
        statValues = new TMP_Text[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            RectTransform row = FeelUI.CreatePanel("Stat" + i, panel, rowColor).rectTransform;
            FeelUI.Place(row, top, center, new Vector2(0f, statTop - statSpacing * i), statRowSize);
            statRows[i] = row;

            TMP_Text nameLbl = FeelUI.CreateText("Name", row, names[i], statSize, textColor, TextAlignmentOptions.Left);
            FeelUI.Stretch(nameLbl.rectTransform, 24f);
            nameLbl.rectTransform.offsetMin = new Vector2(28f, 0f);

            TMP_Text valueLbl = FeelUI.CreateText("Value", row, "", statSize, valueColor, TextAlignmentOptions.Right);
            FeelUI.Stretch(valueLbl.rectTransform);
            valueLbl.rectTransform.offsetMax = new Vector2(-28f, 0f);
            FeelUI.Outline(valueLbl);
            statValues[i] = valueLbl;
        }

        //O botão fica num suporte: a escala do botão é do Tweener (hover), a do suporte é da entrada
        buttonHolder = FeelUI.CreateRect("ButtonHolder", panel);
        FeelUI.Place(buttonHolder, new Vector2(.5f, 0f), center, new Vector2(0f, buttonBottom + buttonSize.y * .5f), buttonSize);

        Button restart = FeelUI.CreateButton("RestartBtn", buttonHolder, restartText, buttonSize, buttonColor);
        restart.onClick.AddListener(Restart);

        screen.gameObject.SetActive(false);
    }

    //Leque de faixas do RoundRectangle saindo do centro, cada uma com o pivô na ponta de dentro
    private void BuildRays()
    {
        rays = FeelUI.CreateRect("Rays", screen);
        rays.anchorMin = rays.anchorMax = new Vector2(.5f, .5f);
        rays.sizeDelta = Vector2.zero;

        for (int i = 0; i < rayCount; i++)
        {
            RectTransform ray = FeelUI.CreatePanel("Ray" + i, rays, rayColor).rectTransform;
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
        int money = game != null ? game.TotalMoneyEarned : 0;

        shownWon = won;
        fxTime = 0f;
        fxDebt = 0f;

        dim.color = won ? wonDimColor : lostDimColor;
        ribbonFill.color = won ? wonColor : lostColor;
        subtitleLbl.text = won ? wonSubtitle : lostSubtitle;

        foreach (RectTransform row in statRows) row.localScale = new Vector3(1f, 0f, 1f);

        screen.gameObject.SetActive(true);
        screen.parent.SetAsLastSibling();
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

        AppendStat(0, elapsed, value => string.Format(timeFormat, Mathf.FloorToInt(value / 60f), Mathf.FloorToInt(value % 60f)));
        AppendStat(1, houses, value => string.Format(housesFormat, Mathf.RoundToInt(value)));
        AppendStat(2, money, value => string.Format(moneyFormat, Mathf.RoundToInt(value)));

        sequence.AppendInterval(statInterval);
        sequence.AppendCallback(() =>
        {
            group.interactable = true;
            if (won) ConfettiBurst();
        });
        sequence.Append(buttonHolder.DOScale(1f, popTime).SetEase(Ease.OutBack));
    }

    private static string Tagged(string text, string effect)
    {
        return string.IsNullOrEmpty(effect) ? text : "<" + effect + ">" + text + "</" + effect + ">";
    }

    //Cada linha abre, o valor conta de zero até o total e dá um pulo com moeda no fim
    private void AppendStat(int index, float target, Func<float, string> format)
    {
        RectTransform row = statRows[index];
        TMP_Text label = statValues[index];
        float shown = 0f;
        label.text = format(0f);

        sequence.AppendInterval(statInterval);
        sequence.AppendCallback(() => AudioManager.PlaySfx(AudioManager.Sfx?.click, statSoundVolume, .05f));
        sequence.Append(row.DOScaleY(1f, popTime * .5f).SetEase(Ease.OutBack));
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
    }

    private void Restart()
    {
        if (!ScreenTransition.Busy) ScreenTransition.Restart();
    }

    [Button]
    private void DebugShow(bool won = true)
    {
        Hide();
        Show(won);
    }
}
