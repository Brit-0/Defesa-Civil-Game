using DG.Tweening;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Faixa de aviso de desastre no topo do HUD. Só escuta: em DisasterWarning desce com o nome do desastre
//(Text Animator) e a contagem; em DisasterStarted troca o texto, pisca e sai. A sirene toca pelo GameAudio.
//Monta a própria UI: basta soltar o prefab no UICanvas.
public class DisasterBanner : MonoBehaviour
{
    [Header("TEXTS (tags do Text Animator: <shake>, <wave>, <bounce>, <incr>)")]
    [SerializeField] private string warningFormat = "<wave>{0} CHEGANDO!</wave>";
    [SerializeField] private string startedFormat = "<shake>{0}!</shake>";
    [SerializeField] private string fallbackName = "DESASTRE";
    [SerializeField] private string zeroText = "!";

    [Header("LAYOUT (tela de referência 1920x1080)")]
    [SerializeField] private Vector2 size = new Vector2(860f, 92f);
    [SerializeField] private float topMargin = 88f;
    [SerializeField] private float border = 5f;
    [SerializeField] private float fontSize = 46f;
    [SerializeField] private float countFontSize = 60f;
    [SerializeField] private float iconSize = 64f;

    [Header("COLORS")]
    [SerializeField] private Color backColor = FeelUI.PanelColor;
    [SerializeField] private Color earthquakeColor = new Color(.9f, .58f, .2f);
    [SerializeField] private Color stormColor = new Color(.3f, .58f, .98f);
    [SerializeField] private Color fireColor = new Color(.95f, .3f, .2f);
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private Color textColor = Color.white;

    [Header("TIMING")]
    [SerializeField] private float slideTime = .35f;
    [SerializeField] private float blinkTime = .3f;
    [SerializeField] private float startedHoldTime = 1.8f;
    [SerializeField] private float tickPunch = .35f;
    [SerializeField] private float tickTime = .25f;
    [SerializeField] private float tickVolume = .5f;

    private RectTransform bar;
    private Image frame;
    private Image icon;
    private TMP_Text titleLbl;
    private TextAnimator_TMP titleAnimator;
    private TMP_Text countLbl;
    private Tween hideTween;
    private Color typeColor;
    private bool counting;
    private float timeLeft;
    private int secondsShown;

    private float ShownY => -(topMargin + size.y * .5f);
    private float HiddenY => size.y;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        RectTransform parent = FeelUI.ViewRoot(this, "DisasterBanner");
        if (parent == null) return;

        Vector2 center = new Vector2(.5f, .5f);

        frame = FeelUI.CreatePanel("Banner", parent, earthquakeColor);
        bar = frame.rectTransform;
        FeelUI.Place(bar, new Vector2(.5f, 1f), center, new Vector2(0f, HiddenY), size);
        FeelUI.Stretch(FeelUI.CreatePanel("Back", bar, backColor).rectTransform, border);

        icon = FeelUI.CreateImage("Icon", bar, Color.white);
        icon.preserveAspect = true;
        FeelUI.Place(icon.rectTransform, new Vector2(0f, .5f), center, new Vector2(border + 16f + iconSize * .5f, 0f), Vector2.one * iconSize);

        countLbl = FeelUI.CreateText("Count", bar, "", countFontSize, textColor);
        FeelUI.Place(countLbl.rectTransform, new Vector2(1f, .5f), center, new Vector2(-(border + 16f + size.y * .5f), 0f), Vector2.one * size.y);

        titleLbl = FeelUI.CreateText("Title", bar, "", fontSize, textColor);
        FeelUI.Stretch(titleLbl.rectTransform);
        titleAnimator = titleLbl.gameObject.AddComponent<TextAnimator_TMP>();

        bar.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.DisasterWarning += OnDisasterWarning;
        GameEvents.DisasterStarted += OnDisasterStarted;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.DisasterWarning -= OnDisasterWarning;
        GameEvents.DisasterStarted -= OnDisasterStarted;
        GameEvents.GameStateChanged -= OnGameStateChanged;

        HideNow();
    }

    private void OnDisasterWarning(DisasterData disaster, float seconds)
    {
        if (bar == null) return;

        Show(disaster, warningFormat);

        counting = true;
        timeLeft = seconds;
        secondsShown = Mathf.CeilToInt(seconds);
        countLbl.text = secondsShown.ToString();

        //Enquanto avisa, a moldura pisca
        frame.DOColor(flashColor, blinkTime).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(bar.gameObject);
    }

    private void OnDisasterStarted(DisasterData disaster, Vector3 epicenter)
    {
        if (bar == null) return;

        Show(disaster, startedFormat);

        counting = false;
        countLbl.text = "";

        //Um clarão e a faixa sai sozinha
        frame.color = flashColor;
        frame.DOColor(typeColor, blinkTime).SetLink(bar.gameObject);
        bar.DOPunchScale(Vector3.one * tickPunch * .5f, tickTime * 2f, 6).SetLink(bar.gameObject);
        hideTween = DOVirtual.DelayedCall(startedHoldTime, Hide, false).SetLink(bar.gameObject);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.Playing && state != GameState.Paused) HideNow();
    }

    private void Update()
    {
        if (!counting) return;

        timeLeft -= Time.deltaTime;
        int seconds = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
        if (seconds == secondsShown) return;

        secondsShown = seconds;
        countLbl.text = seconds > 0 ? seconds.ToString() : zeroText;

        Transform count = countLbl.transform;
        count.DOKill();
        count.localScale = Vector3.one;
        count.DOPunchScale(Vector3.one * tickPunch, tickTime, 4).SetLink(bar.gameObject);

        if (AudioManager.Sfx != null) AudioManager.PlaySfx(AudioManager.Sfx.hover, tickVolume);

        //Se o desastre não começar, a faixa não fica presa na tela
        if (seconds == 0)
        {
            counting = false;
            hideTween?.Kill();
            hideTween = DOVirtual.DelayedCall(startedHoldTime, Hide, false).SetLink(bar.gameObject);
        }
    }

    private void Show(DisasterData disaster, string format)
    {
        typeColor = ColorFor(disaster);
        string disasterName = disaster != null && !string.IsNullOrEmpty(disaster.displayName) ? disaster.displayName : fallbackName;

        hideTween?.Kill();
        hideTween = null;
        bar.DOKill();
        frame.DOKill();
        frame.color = typeColor;
        bar.localScale = Vector3.one;

        bool wasHidden = !bar.gameObject.activeSelf;
        bar.gameObject.SetActive(true);
        bar.parent.SetAsLastSibling();

        Sprite sprite = disaster != null ? disaster.icon : null;
        icon.sprite = sprite;
        icon.enabled = sprite != null;

        titleAnimator.SetText(string.Format(format, disasterName.ToUpperInvariant()));

        if (wasHidden) bar.anchoredPosition = new Vector2(0f, HiddenY);
        bar.DOAnchorPosY(ShownY, slideTime).SetEase(Ease.OutBack).SetLink(bar.gameObject);
    }

    private Color ColorFor(DisasterData disaster)
    {
        if (disaster == null) return earthquakeColor;

        switch (disaster.type)
        {
            case DisasterType.Storm: return stormColor;
            case DisasterType.Fire: return fireColor;
            default: return earthquakeColor;
        }
    }

    private void Hide()
    {
        if (bar == null || !bar.gameObject.activeSelf) return;

        counting = false;
        bar.DOKill();
        frame.DOKill();
        bar.DOAnchorPosY(HiddenY, slideTime).SetEase(Ease.InBack).SetLink(bar.gameObject).OnComplete(() => bar.gameObject.SetActive(false));
    }

    private void HideNow()
    {
        counting = false;
        hideTween?.Kill();
        hideTween = null;

        if (bar == null) return;

        bar.DOKill();
        frame.DOKill();
        bar.gameObject.SetActive(false);
    }

    [Button]
    private void DebugWarning(float seconds = 5f)
    {
        OnDisasterWarning(null, seconds);
    }

    [Button]
    private void DebugStarted()
    {
        OnDisasterStarted(null, Vector3.zero);
    }
}
