using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Barra de caos em pé, na direita do HUD. Só escuta ChaosChanged: enche de baixo para cima, muda de cor e pulsa
//acima do limite de perigo. Saiu do topo porque cobria o texto de instrução (lista Para Arrumar, item 8).
//Monta a própria UI: basta soltar o prefab no UICanvas (posição e tamanho ficam nos campos abaixo).
public class ChaosBar : MonoBehaviour
{
    [Header("LAYOUT (tela de referência 1920x1080)")]
    [SerializeField] private Vector2 barSize = new Vector2(48f, 420f);
    [Tooltip("Distância da borda direita da tela até a barra")]
    [SerializeField] private float rightMargin = 40f;
    [Tooltip("Altura do centro da barra em relação ao centro da tela (abaixo do dinheiro e da pausa)")]
    [SerializeField] private float centerY = 10f;
    [SerializeField] private float border = 4f;
    [SerializeField] private float fontSize = 26f;
    [SerializeField] private string title = "CAOS";

    [Header("COLORS")]
    [SerializeField] private Color frameColor = FeelUI.ButtonColor;
    [SerializeField] private Color backColor = FeelUI.PanelColor;
    [SerializeField] private Color calmColor = new Color(.35f, .8f, .4f);
    [SerializeField] private Color warnColor = new Color(.95f, .8f, .25f);
    [SerializeField] private Color dangerColor = new Color(.9f, .22f, .2f);
    [SerializeField] private Color labelColor = Color.white;
    [SerializeField] private Color dangerLabelColor = new Color(1f, .9f, .5f);
    //Marcas de 25% no trilho (kit pixel)
    [SerializeField] private Color tickColor = new Color(0f, 0f, 0f, .35f);

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

        Vector2 center = new Vector2(.5f, .5f);
        FeelUITheme px = FeelUI.Pixel;
        float pixel = FeelUI.PixelSize;
        bar = (px != null ? FeelUI.CreateSliced("Bar", parent, px.barFrame, Color.white) : FeelUI.CreatePanel("Bar", parent, frameColor)).rectTransform;
        FeelUI.Place(bar, new Vector2(1f, .5f), center, new Vector2(-(rightMargin + barSize.x * .5f), centerY), barSize);
        group = bar.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        //No kit pixel a moldura e o trilho escuro já vêm no sprite: o preenchimento entra 3 pixels para dentro
        Image back = px != null ? FeelUI.CreateImage("Back", bar, Color.clear) : FeelUI.CreatePanel("Back", bar, backColor);
        FeelUI.Stretch(back.rectTransform, px != null ? 3f * pixel : border);

        //Enche de baixo para cima
        fill = px != null ? FeelUI.CreateSliced("Fill", back.transform, px.barFill, calmColor) : FeelUI.CreatePanel("Fill", back.transform, calmColor);
        fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1f, 0f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        if (px != null) BuildTicks(back.rectTransform, px);

        //Rótulo embaixo da barra, numa plaquinha escura: em pé, "CAOS" não cabe dentro
        RectTransform tag = (px != null ? FeelUI.CreateSliced("Tag", bar, px.inset, Color.white) : FeelUI.CreatePanel("Tag", bar, backColor)).rectTransform;
        FeelUI.Place(tag, new Vector2(.5f, 0f), new Vector2(.5f, 1f), new Vector2(0f, -pixel), new Vector2(fontSize * 3.6f, fontSize + 6f * pixel));

        //Kit pixel: sombra de 1 pixel embaixo do rótulo
        if (px != null)
        {
            TMP_Text shadow = FeelUI.CreateText("Shadow", tag, title, fontSize, FeelUI.PixelInk);
            FeelUI.Stretch(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(pixel, -pixel);
        }

        label = FeelUI.CreateText("Lbl", tag, title, fontSize, labelColor);
        FeelUI.Stretch(label.rectTransform);
        FeelUI.Outline(label);
    }

    //Marcas de 25 em 25% e a do perigo em vermelho, mais o ícone de alerta em cima da barra
    private void BuildTicks(RectTransform track, FeelUITheme px)
    {
        float pixel = FeelUI.PixelSize;

        for (int i = 1; i < 4; i++)
        {
            Image tick = FeelUI.CreateImage("Tick", track, tickColor);
            RectTransform rect = tick.rectTransform;
            rect.anchorMin = new Vector2(0f, i * .25f);
            rect.anchorMax = new Vector2(1f, i * .25f);
            rect.sizeDelta = new Vector2(0f, pixel);
        }

        Image danger = FeelUI.CreateImage("DangerMark", track, dangerColor);
        danger.rectTransform.anchorMin = new Vector2(0f, pulseThreshold);
        danger.rectTransform.anchorMax = new Vector2(1f, pulseThreshold);
        danger.rectTransform.sizeDelta = new Vector2(0f, pixel);

        if (px.iconChaos == null) return;

        Image icon = FeelUI.CreateIcon("Icon", bar, px.iconChaos);
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, iconRect.sizeDelta.y * .5f - pixel);
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

        fillRect.anchorMax = new Vector2(1f, chaos);
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
