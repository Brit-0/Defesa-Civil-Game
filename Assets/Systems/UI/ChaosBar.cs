using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Barra de caos no topo do HUD. Só escuta ChaosChanged: enche, muda de cor e pulsa acima do limite de perigo.
//Monta a própria UI: basta soltar o prefab no UICanvas (posição e tamanho ficam nos campos abaixo).
public class ChaosBar : MonoBehaviour
{
    [Header("LAYOUT (tela de referência 1920x1080)")]
    [SerializeField] private Vector2 size = new Vector2(560f, 44f);
    [SerializeField] private float topMargin = 26f;
    [SerializeField] private float border = 4f;
    [SerializeField] private float fontSize = 30f;
    [SerializeField] private string title = "CAOS";

    [Header("COLORS")]
    [SerializeField] private Color frameColor = new Color(.07f, .07f, .1f);
    [SerializeField] private Color backColor = new Color(.2f, .2f, .26f);
    [SerializeField] private Color calmColor = new Color(.35f, .8f, .4f);
    [SerializeField] private Color warnColor = new Color(.95f, .8f, .25f);
    [SerializeField] private Color dangerColor = new Color(.9f, .22f, .2f);
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private Color dangerLabelColor = new Color(1f, .9f, .5f);

    [Header("PULSE")]
    [SerializeField, Range(0f, 1f)] private float pulseThreshold = .6f;
    [SerializeField] private float pulseScale = 1.07f;
    [SerializeField] private float pulseTime = .45f;
    [SerializeField] private float pulseSpeedAtFull = 3f;
    [SerializeField] private float settleTime = .15f;

    [Header("VISIBILITY")]
    [SerializeField] private float fadeTime = .3f;

    private RectTransform bar;
    private RectTransform fillRect;
    private Image fill;
    private TMP_Text label;
    private CanvasGroup group;
    private Tween pulseTween;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        RectTransform parent = FeelUI.ViewRoot(this, "ChaosBar");
        if (parent == null) return;

        Vector2 top = new Vector2(.5f, 1f);
        bar = FeelUI.CreateImage("Bar", parent, frameColor).rectTransform;
        FeelUI.Place(bar, top, new Vector2(.5f, .5f), new Vector2(0f, -(topMargin + size.y * .5f)), size);
        group = bar.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        Image back = FeelUI.CreateImage("Back", bar, backColor);
        FeelUI.Stretch(back.rectTransform, border);

        fill = FeelUI.CreateImage("Fill", back.transform, calmColor);
        fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        label = FeelUI.CreateText("Lbl", bar, title, fontSize, labelColor);
        FeelUI.Stretch(label.rectTransform);
        //O rótulo fica por cima do preenchimento, que muda de cor
        FeelUI.Outline(label);
    }

    private void Start()
    {
        Apply(ChaosManager.Instance != null ? ChaosManager.Instance.Chaos : 0f);

        //No menu inicial a barra fica escondida
        bool inMenu = GameManager.Instance != null && GameManager.Instance.State == GameState.Menu;
        if (group != null) group.alpha = inMenu ? 0f : 1f;
    }

    private void OnEnable()
    {
        GameEvents.ChaosChanged += Apply;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.ChaosChanged -= Apply;
        GameEvents.GameStateChanged -= OnGameStateChanged;

        if (bar != null)
        {
            bar.DOKill();
            bar.localScale = Vector3.one;
        }
    }

    private void OnGameStateChanged(GameState state)
    {
        if (group == null) return;

        group.DOKill();
        group.DOFade(state == GameState.Menu ? 0f : 1f, fadeTime).SetUpdate(true).SetLink(bar.gameObject);
    }

    private void Apply(float value)
    {
        if (bar == null) return;

        float chaos = Mathf.Clamp01(value);
        bool danger = chaos >= pulseThreshold;
        float dangerAmount = Mathf.InverseLerp(pulseThreshold, 1f, chaos);

        fillRect.anchorMax = new Vector2(chaos, 1f);
        fill.color = danger
            ? Color.Lerp(warnColor, dangerColor, dangerAmount)
            : Color.Lerp(calmColor, warnColor, pulseThreshold > 0f ? chaos / pulseThreshold : 1f);
        label.color = danger ? dangerLabelColor : labelColor;

        if (danger)
        {
            if (pulseTween == null) StartPulse();
            //Quanto mais perto de encher, mais rápido pulsa
            pulseTween.timeScale = Mathf.Lerp(1f, pulseSpeedAtFull, dangerAmount);
        }
        else if (pulseTween != null)
        {
            StopPulse();
        }
    }

    private void StartPulse()
    {
        bar.DOKill();
        pulseTween = bar.DOScale(pulseScale, pulseTime).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true).SetLink(bar.gameObject).OnKill(() => pulseTween = null);
    }

    private void StopPulse()
    {
        bar.DOKill();
        bar.DOScale(1f, settleTime).SetUpdate(true).SetLink(bar.gameObject);
    }
}
