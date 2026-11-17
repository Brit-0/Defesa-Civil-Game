using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Meta sempre à vista (GAMELOOP_REVISAO): quanto falta para o Centro de Operações e o próximo marco de consertos.
// Canto de baixo à esquerda, ao lado do botão de melhorias. O número sobe contando e a barra enche com tween.
public class GoalTracker : MonoBehaviour
{
    public static GoalTracker Instance;

    [Header("LAYOUT (tela de referência 1920x1080)")]
    //O topo fica a 138 do chão, como antes: mais alto, entraria embaixo do painel de melhorias aberto
    [SerializeField] private Vector2 position = new Vector2(140f, 14f);
    [SerializeField] private Vector2 size = new Vector2(540f, 124f);
    [SerializeField] private float border = 4f;
    [Tooltip("Margem interna dos lados: o texto e a barra não encostam na moldura (#531)")]
    [SerializeField] private float padding = 32f;
    [Tooltip("Margem interna de cima e de baixo")]
    [SerializeField] private float paddingY = 16f;

    [Header("COLORS")]
    [SerializeField] private Color frameColor = FeelUI.ButtonColor;
    [SerializeField] private Color readyColor = new Color(.35f, .85f, .4f);
    [SerializeField] private Color backColor = FeelUI.PanelColor;
    [SerializeField] private Color barBackColor = new Color(0f, 0f, 0f, .45f);
    [SerializeField] private Color barColor = new Color(1f, .82f, .3f);
    [SerializeField] private Color milestoneColor = new Color(.6f, .85f, 1f);

    [Header("TEXTS")]
    [SerializeField] private string titleFormat = "META: {0}";
    [SerializeField] private string readyText = "PRONTO! COMPRE EM MELHORIAS";
    [SerializeField] private string milestoneFormat = "PRÓXIMO MARCO: {0}/{1} CONSERTOS (+{2} VOLUNTÁRIO)";
    [SerializeField] private string noMilestoneText = "TODOS OS MARCOS ALCANÇADOS";

    [Header("TIMING")]
    [SerializeField] private float countTime = .4f;
    [SerializeField] private float punch = .08f;

    private RectTransform frame;
    private Image frameImage;
    private RectTransform fill;
    private TMP_Text titleLbl;
    private TMP_Text valueLbl;
    private TMP_Text milestoneLbl;
    private float shownMoney;
    private float shownRatio;
    private Tween countTween;
    private Tween readyTween;
    private bool ready;

    public RectTransform Frame => frame;

    private void Awake()
    {
        Instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Build()
    {
        RectTransform parent = FeelUI.ViewRoot(this, "GoalTracker");
        if (parent == null) return;

        Vector2 zero = Vector2.zero;
        frameImage = FeelUI.CreatePanel("Goal", parent, frameColor);
        frame = frameImage.rectTransform;
        FeelUI.Place(frame, zero, zero, position, size);
        FeelUI.Stretch(FeelUI.CreatePanel("Back", frame, backColor).rectTransform, border);

        titleLbl = FeelUI.CreateText("Title", frame, "", 22f, Color.white, TextAlignmentOptions.MidlineLeft);
        float inner = size.x - padding * 2f;
        FeelUI.Place(titleLbl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(padding, -paddingY), new Vector2(inner, 28f));
        titleLbl.enableAutoSizing = true;
        titleLbl.fontSizeMin = 14f;
        titleLbl.fontSizeMax = 22f;

        Image barBack = FeelUI.CreatePanel("BarBack", frame, barBackColor);
        FeelUI.Place(barBack.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(padding, -paddingY - 32f), new Vector2(inner, 30f));

        Image fillImage = FeelUI.CreatePanel("Fill", barBack.rectTransform, barColor);
        fill = fillImage.rectTransform;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        valueLbl = FeelUI.CreateText("Value", barBack.rectTransform, "", 22f, Color.white);
        FeelUI.Stretch(valueLbl.rectTransform);
        FeelUI.Outline(valueLbl, .3f);

        milestoneLbl = FeelUI.CreateText("Milestone", frame, "", 17f, milestoneColor, TextAlignmentOptions.MidlineLeft);
        FeelUI.Place(milestoneLbl.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(padding, paddingY - 2f), new Vector2(inner, 24f));
        milestoneLbl.enableAutoSizing = true;
        milestoneLbl.fontSizeMin = 12f;
        milestoneLbl.fontSizeMax = 17f;
    }

    private void OnEnable()
    {
        GameEvents.MoneyChanged += OnMoneyChanged;
        GameEvents.CallResolved += OnCallResolved;
        GameEvents.MilestoneReached += OnMilestoneReached;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.MoneyChanged -= OnMoneyChanged;
        GameEvents.CallResolved -= OnCallResolved;
        GameEvents.MilestoneReached -= OnMilestoneReached;
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    private void Start()
    {
        shownMoney = ResourcesManager.Money;
        Refresh(false);
        RefreshMilestone();
        if (GameManager.Instance != null) OnGameStateChanged(GameManager.Instance.State);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (frame != null) frame.gameObject.SetActive(state == GameState.Playing || state == GameState.Paused);
    }

    private void OnMoneyChanged(int before, int after) => Refresh(true);
    private void OnCallResolved(Call call, int money) => RefreshMilestone();

    private void OnMilestoneReached(int housesRepaired, int volunteers)
    {
        RefreshMilestone();
        Punch();
    }

    private UpgradeData Goal => UpgradesManager.Instance != null ? UpgradesManager.Instance.GetUpgrade(UpgradeIds.Goal) : null;

    private void Refresh(bool animate)
    {
        if (frame == null) return;

        UpgradeData goal = Goal;
        int cost = goal != null ? UpgradesManager.Instance.GetCost(goal) : 0;
        titleLbl.text = string.Format(titleFormat, goal != null ? goal.displayName.ToUpperInvariant() : "CENTRO DE OPERAÇÕES");

        int money = ResourcesManager.Money;
        float ratio = cost > 0 ? Mathf.Clamp01((float)money / cost) : 0f;
        SetReady(cost > 0 && money >= cost);

        countTween?.Kill();
        if (!animate)
        {
            shownMoney = money;
            shownRatio = ratio;
            Apply(cost);
        }
        else
        {
            float fromMoney = shownMoney;
            float fromRatio = shownRatio;
            countTween = DOVirtual.Float(0f, 1f, countTime, t =>
            {
                shownMoney = Mathf.Lerp(fromMoney, money, t);
                shownRatio = Mathf.Lerp(fromRatio, ratio, t);
                Apply(cost);
            }).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(frame.gameObject);
        }
    }

    private void Apply(int cost)
    {
        fill.anchorMax = new Vector2(shownRatio, 1f);
        valueLbl.text = ready ? readyText : $"{Mathf.RoundToInt(shownMoney)} / {cost}";
    }

    private void RefreshMilestone()
    {
        if (milestoneLbl == null || GameManager.Instance == null) return;

        int next = GameManager.Instance.NextMilestone;
        milestoneLbl.text = next > 0
            ? string.Format(milestoneFormat, GameManager.Instance.HousesRepaired, next, Stats.Balance.volunteersPerMilestone)
            : noMilestoneText;
    }

    // Dá para comprar: moldura verde pulsando até gastar.
    private void SetReady(bool value)
    {
        if (value == ready) return;

        ready = value;
        readyTween?.Kill();
        frameImage.color = ready ? readyColor : frameColor;
        frame.localScale = Vector3.one;
        if (ready)
        {
            Punch();
            readyTween = frame.DOScale(1.04f, .5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(frame.gameObject);
        }
    }

    private void Punch()
    {
        if (frame == null || ready) return;

        frame.DOKill(true);
        frame.DOPunchScale(Vector3.one * punch, .3f, 6, .6f).SetUpdate(true).SetLink(frame.gameObject);
    }
}
