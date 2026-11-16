using DG.Tweening;
using TMPro;
using UnityEngine;

//Números do HUD contam até o valor novo e o ícone dá um pulo. Só escuta GameEvents.
public class HudJuice : MonoBehaviour
{
    public static HudJuice Instance;

    [Header("REFERENCES (vazio = acha pelo nome no UICanvas)")]
    [SerializeField] private TMP_Text moneyLbl;
    [SerializeField] private Transform moneyIcon;
    [SerializeField] private TMP_Text materialLbl;
    [SerializeField] private Transform materialIcon;
    [SerializeField] private TMP_Text workersLbl;
    [SerializeField] private Transform workersIcon;

    [Header("COUNT")]
    [SerializeField] private float countTime = .5f;
    [SerializeField] private Ease countEase = Ease.OutCubic;

    [Header("PUNCH")]
    [SerializeField] private float punchScale = .35f;
    [SerializeField] private float punchTime = .3f;
    [SerializeField] private int punchVibrato = 6;

    [Header("COLOR FLASH")]
    [SerializeField] private Color gainColor = new Color(.45f, 1f, .45f);
    [SerializeField] private Color lossColor = new Color(1f, .4f, .35f);
    [SerializeField] private float colorTime = .45f;

    private class Counter
    {
        public TMP_Text label;
        public Transform icon;
        public Vector3 iconScale;
        public Color labelColor;
        public float displayed;
        public int shown;
        public int target;
        public bool initialized;
        public bool forceWrite;
        public Tween countTween;
        public Tween punchTween;
        public Tween colorTween;
    }

    private readonly Counter money = new Counter();
    private readonly Counter material = new Counter();
    private readonly Counter workers = new Counter();

    public Transform MoneyTarget => money.icon != null ? money.icon : (money.label != null ? money.label.transform : null);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Transform hud = FeelUI.HudCanvas != null ? FeelUI.HudCanvas.transform : null;

        Setup(money, moneyLbl, moneyIcon, hud, "Money");
        Setup(material, materialLbl, materialIcon, hud, "Resources", "BuildMaterial");
        Setup(workers, workersLbl, workersIcon, hud, "Resources", "Workers");
    }

    private void Setup(Counter counter, TMP_Text label, Transform icon, Transform hud, params string[] path)
    {
        Transform group = hud != null ? FeelUI.FindChild(hud, path) : null;

        if (label == null && group != null) label = group.GetComponentInChildren<TMP_Text>(true);
        if (icon == null && group != null) icon = FeelUI.FindChild(group, "Icon");

        counter.label = label;
        counter.icon = icon;
        if (label != null) counter.labelColor = label.color;
        if (icon != null) counter.iconScale = icon.localScale;
    }

    private void OnEnable()
    {
        GameEvents.MoneyChanged += OnMoneyChanged;
        GameEvents.MaterialChanged += OnMaterialChanged;
        GameEvents.WorkersChanged += OnWorkersChanged;
    }

    private void OnDisable()
    {
        GameEvents.MoneyChanged -= OnMoneyChanged;
        GameEvents.MaterialChanged -= OnMaterialChanged;
        GameEvents.WorkersChanged -= OnWorkersChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnMoneyChanged(int before, int after)
    {
        if (!money.initialized)
        {
            money.initialized = true;
            money.displayed = before;
            money.target = before;
        }

        Animate(money, after);
    }

    private void OnMaterialChanged(int amount)
    {
        Animate(material, amount);
    }

    private void OnWorkersChanged(int amount)
    {
        Animate(workers, amount);
    }

    private void Animate(Counter counter, int value)
    {
        counter.forceWrite = true;

        //O primeiro valor (estoque inicial) entra sem contagem
        if (!counter.initialized)
        {
            counter.initialized = true;
            counter.displayed = value;
            counter.target = value;
            return;
        }

        int delta = value - counter.target;
        counter.target = value;
        if (delta == 0) return;

        counter.countTween?.Kill();
        counter.countTween = DOTween.To(() => counter.displayed, x => counter.displayed = x, value, countTime)
            .SetEase(countEase).SetUpdate(true).SetLink(gameObject);

        Punch(counter);
        Flash(counter, delta > 0 ? gainColor : lossColor);
    }

    //Depois de tudo que mexe no rótulo no frame (o ResourcesManager escreve o valor final direto)
    private void LateUpdate()
    {
        Write(money);
        Write(material);
        Write(workers);
    }

    private void Write(Counter counter)
    {
        if (!counter.initialized || counter.label == null) return;

        int value = Mathf.RoundToInt(counter.displayed);
        if (value == counter.shown && !counter.forceWrite) return;

        counter.shown = value;
        counter.forceWrite = false;
        counter.label.text = value.ToString();
    }

    //Pulo do ícone do dinheiro, para quando um popup "+N" chega nele
    public void PunchMoney()
    {
        Punch(money);
    }

    private void Punch(Counter counter)
    {
        if (counter.icon == null) return;

        counter.punchTween?.Kill();
        counter.icon.localScale = counter.iconScale;
        counter.punchTween = counter.icon.DOPunchScale(counter.iconScale * punchScale, punchTime, punchVibrato, .6f)
            .SetUpdate(true).SetLink(gameObject);
    }

    private void Flash(Counter counter, Color color)
    {
        if (counter.label == null) return;

        counter.colorTween?.Kill();
        counter.label.color = color;
        counter.colorTween = counter.label.DOColor(counter.labelColor, colorTime).SetUpdate(true).SetLink(gameObject);
    }
}
