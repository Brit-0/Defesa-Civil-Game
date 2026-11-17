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

    [Header("TICK (o número cresce um pouco a cada valor novo da contagem)")]
    [SerializeField] private float tickScale = .12f;
    [SerializeField] private float tickTime = .12f;

    [Header("DELTA (\"+3\" e \"-50\" saindo do contador, unidades do HUD)")]
    [SerializeField] private bool showDelta = true;
    [Tooltip("O ganho de dinheiro já tem o \"+N\" que voa da casa (MoneyPopups)")]
    [SerializeField] private bool showMoneyGain = false;
    [SerializeField] private Vector2 deltaOffset = new Vector2(0f, -46f);
    [SerializeField] private float deltaFontSize = 34f;
    [SerializeField] private float deltaDistance = 40f;
    [SerializeField] private float deltaTime = .8f;

    private class Counter
    {
        public TMP_Text label;
        public Transform icon;
        public Vector3 iconScale;
        public Vector3 labelScale;
        public Tween tickTween;
        public bool isMoney;
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
        counter.isMoney = counter == money;
        if (label != null)
        {
            counter.labelColor = label.color;
            counter.labelScale = label.transform.localScale;
        }
        if (icon != null) counter.iconScale = icon.localScale;
    }

    private void OnEnable()
    {
        GameEvents.MoneyChanged += OnMoneyChanged;
        GameEvents.MaterialChanged += OnMaterialChanged;
        GameEvents.WorkersChanged += OnWorkersChanged;
        GameEvents.ActionDenied += OnActionDenied;
    }

    private void OnDisable()
    {
        GameEvents.MoneyChanged -= OnMoneyChanged;
        GameEvents.MaterialChanged -= OnMaterialChanged;
        GameEvents.WorkersChanged -= OnWorkersChanged;
        GameEvents.ActionDenied -= OnActionDenied;
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

    //Ação negada: o contador do que faltou pula e pisca vermelho
    private void OnActionDenied(DeniedReason reason, Vector3 worldPosition)
    {
        Counter counter = reason == DeniedReason.NoWorkers ? workers
            : reason == DeniedReason.NoMaterial ? material
            : reason == DeniedReason.NoMoney ? money
            : null;
        if (counter == null) return;

        Punch(counter);
        Flash(counter, lossColor);
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
        ShowDelta(counter, delta);
    }

    //"+N" sobe ou "-N" desce embaixo do número e some
    private void ShowDelta(Counter counter, int delta)
    {
        if (!showDelta || counter.label == null || (counter.isMoney && delta > 0 && !showMoneyGain)) return;

        RectTransform layer = FeelUI.HudLayer("FeelPopups");
        if (layer == null) return;

        bool gain = delta > 0;
        TMP_Text text = FeelUI.CreateText("Delta", layer, (gain ? "+" : "") + delta, deltaFontSize, gain ? gainColor : lossColor);
        FeelUI.Outline(text);
        RectTransform rect = text.rectTransform;
        rect.sizeDelta = new Vector2(240f, deltaFontSize * 1.4f);
        rect.anchoredPosition = FeelUI.HudPosition(counter.label.transform) + deltaOffset;

        GameObject go = text.gameObject;
        rect.localScale = Vector3.one * .5f;
        DOTween.Sequence().SetUpdate(true).SetLink(go)
            .Append(rect.DOScale(1f, deltaTime * .2f).SetEase(Ease.OutBack))
            .Join(rect.DOAnchorPosY(rect.anchoredPosition.y + (gain ? deltaDistance : -deltaDistance), deltaTime).SetEase(Ease.OutCubic))
            .Insert(deltaTime * .5f, text.DOFade(0f, deltaTime * .5f))
            .OnComplete(() => Destroy(go));
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

        if (value != counter.shown && counter.countTween != null && counter.countTween.IsActive()) Tick(counter);
        counter.shown = value;
        counter.forceWrite = false;
        //Trabalhadores aparecem como livres/total
        counter.label.text = counter == workers && ResourcesManager.Instance != null ? ResourcesManager.FormatWorkers(value) : value.ToString();
    }

    //Cada número novo da contagem dá um pulinho no rótulo; contagem rápida segura o número grande até parar
    private void Tick(Counter counter)
    {
        if (tickScale <= 0f) return;

        Transform label = counter.label.transform;
        counter.tickTween?.Kill();
        label.localScale = counter.labelScale * (1f + tickScale);
        counter.tickTween = label.DOScale(counter.labelScale, tickTime).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
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
