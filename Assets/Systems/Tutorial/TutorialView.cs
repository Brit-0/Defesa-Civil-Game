using System;
using DG.Tweening;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A cara do tutorial: cartão de abertura, balão de passo embaixo, seta e aro de destaque no alvo.
// Seta feita de barras do RoundRectangle (a fonte não tem glifo de seta); aro é o Button2. Monta tudo por código.
public class TutorialView : MonoBehaviour
{
    [Header("LAYOUT (tela de referência 1920x1080)")]
    [SerializeField] private Vector2 bubbleSize = new Vector2(1040f, 150f);
    [SerializeField] private float bubbleBottom = 150f;
    [SerializeField] private Vector2 cardSize = new Vector2(1000f, 540f);
    [SerializeField] private float border = 5f;
    [SerializeField] private float textSize = 32f;
    [SerializeField] private float edgeMargin = 90f;

    [Header("COLORS")]
    [SerializeField] private Color frameColor = new Color(1f, .82f, .3f);
    [SerializeField] private Color backColor = FeelUI.PanelColor;
    [SerializeField] private Color shadeColor = new Color(0f, 0f, 0f, .6f);
    [SerializeField] private Color arrowColor = new Color(1f, .85f, .25f);
    [SerializeField] private Color arrowEdgeColor = new Color(.15f, .1f, .05f);
    [SerializeField] private Color okColor = new Color(.25f, .62f, .32f);
    [SerializeField] private Color skipColor = FeelUI.ButtonColor;

    [Header("MOTION")]
    [SerializeField] private float slideTime = .35f;
    [SerializeField] private float arrowBob = 18f;
    [SerializeField] private float arrowBobSpeed = 6f;
    [SerializeField] private float ringPulse = .1f;
    [SerializeField] private float worldRingSize = 170f;

    private RectTransform root;
    private CanvasGroup rootGroup;
    private RectTransform card;
    private RectTransform bubble;
    private TextAnimator_TMP bubbleLbl;
    private TMP_Text stepLbl;
    private Button okButton;
    private Button skipButton;
    private TMP_Text okLbl;
    private RectTransform arrow;
    private Image ring;
    private Action onOk;

    private RectTransform hudTarget;
    private Transform worldTarget;

    public event Action SkipPressed;

    public void Build()
    {
        root = FeelUI.HudLayer("Tutorial");
        if (root == null) return;

        root.SetAsLastSibling();
        rootGroup = root.gameObject.AddComponent<CanvasGroup>();
        Vector2 center = new Vector2(.5f, .5f);

        ring = FeelUI.CreateImage("Ring", root, arrowColor, FeelUI.Theme != null ? FeelUI.Theme.button2 : null);
        FeelUI.Slice(ring);
        ring.gameObject.SetActive(false);
        arrow = BuildArrow(root);
        arrow.gameObject.SetActive(false);

        // Balão do passo: moldura dourada como as dicas da cena.
        bubble = FeelUI.CreatePanel("Bubble", root, frameColor).rectTransform;
        FeelUI.Place(bubble, new Vector2(.5f, 0f), center, new Vector2(0f, -bubbleSize.y), bubbleSize);
        FeelUI.Stretch(FeelUI.CreatePanel("Back", bubble, backColor).rectTransform, border);

        stepLbl = FeelUI.CreateText("Step", bubble, "", textSize * .6f, frameColor, TextAlignmentOptions.TopLeft);
        FeelUI.Place(stepLbl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(300f, textSize));

        TMP_Text text = FeelUI.CreateText("Lbl", bubble, "", textSize, Color.white, TextAlignmentOptions.MidlineLeft);
        FeelUI.Place(text.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(24f, -8f), new Vector2(bubbleSize.x - 300f, bubbleSize.y - 50f));
        text.textWrappingMode = TextWrappingModes.Normal;
        text.enableAutoSizing = true;
        text.fontSizeMin = textSize * .6f;
        text.fontSizeMax = textSize;
        bubbleLbl = text.gameObject.AddComponent<TextAnimator_TMP>();

        okButton = FeelUI.CreateButton("Ok", bubble, "ENTENDI", new Vector2(220f, 64f), okColor, 30f);
        FeelUI.Place((RectTransform)okButton.transform, new Vector2(1f, .5f), center, new Vector2(-136f, 22f), new Vector2(220f, 64f));
        okButton.onClick.AddListener(() => onOk?.Invoke());
        okLbl = okButton.GetComponentInChildren<TMP_Text>();

        skipButton = FeelUI.CreateButton("Skip", bubble, "PULAR TUTORIAL", new Vector2(220f, 40f), skipColor, 18f);
        FeelUI.Place((RectTransform)skipButton.transform, new Vector2(1f, 0f), center, new Vector2(-136f, 32f), new Vector2(220f, 40f));
        skipButton.onClick.AddListener(() => SkipPressed?.Invoke());

        bubble.gameObject.SetActive(false);
    }

    // Seta apontando para a direita (+x), ponta na origem: haste e duas barras da cabeça, com contorno escuro.
    private RectTransform BuildArrow(Transform parent)
    {
        RectTransform holder = FeelUI.CreateRect("Arrow", parent);
        holder.sizeDelta = Vector2.zero;

        foreach (bool edge in new[] { true, false })
        {
            Color color = edge ? arrowEdgeColor : arrowColor;
            float grow = edge ? 8f : 0f;
            Bar(holder, color, new Vector2(-48f, 0f), new Vector2(84f + grow, 20f + grow), 0f);
            Bar(holder, color, new Vector2(-17f, 17f), new Vector2(52f + grow, 20f + grow), -45f);
            Bar(holder, color, new Vector2(-17f, -17f), new Vector2(52f + grow, 20f + grow), 45f);
        }

        return holder;
    }

    private static void Bar(Transform parent, Color color, Vector2 position, Vector2 size, float angle)
    {
        RectTransform bar = FeelUI.CreatePanel("Bar", parent, color).rectTransform;
        FeelUI.Place(bar, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, size);
        bar.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    #region CARD

    // Cartão de abertura com fundo escurecido: segura o clique no mapa até o jogador escolher.
    public void ShowCard(string title, string body, string startLabel, string skipLabel, Action onStart, Action onSkip)
    {
        if (root == null) return;

        Image shade = FeelUI.CreateImage("Shade", root, shadeColor);
        shade.raycastTarget = true;
        FeelUI.Stretch(shade.rectTransform);
        card = shade.rectTransform;

        Vector2 center = new Vector2(.5f, .5f);
        RectTransform panel = FeelUI.CreatePanel("Card", card, frameColor).rectTransform;
        FeelUI.Place(panel, center, center, Vector2.zero, cardSize);
        FeelUI.Stretch(FeelUI.CreatePanel("Back", panel, backColor).rectTransform, border);

        TMP_Text titleLbl = FeelUI.CreateText("Title", panel, title, 64f, frameColor);
        FeelUI.Place(titleLbl.rectTransform, new Vector2(.5f, 1f), center, new Vector2(0f, -70f), new Vector2(cardSize.x - 60f, 80f));

        TMP_Text bodyLbl = FeelUI.CreateText("Body", panel, body, 32f, Color.white);
        FeelUI.Place(bodyLbl.rectTransform, center, center, new Vector2(0f, 20f), new Vector2(cardSize.x - 100f, 260f));
        bodyLbl.textWrappingMode = TextWrappingModes.Normal;
        bodyLbl.lineSpacing = 12f;

        Button start = FeelUI.CreateButton("Start", panel, startLabel, new Vector2(320f, 84f), okColor, 40f);
        FeelUI.Place((RectTransform)start.transform, new Vector2(.5f, 0f), center, new Vector2(-180f, 80f), new Vector2(320f, 84f));
        start.onClick.AddListener(() => onStart?.Invoke());

        Button skip = FeelUI.CreateButton("Skip", panel, skipLabel, new Vector2(320f, 84f), skipColor, 30f);
        FeelUI.Place((RectTransform)skip.transform, new Vector2(.5f, 0f), center, new Vector2(180f, 80f), new Vector2(320f, 84f));
        skip.onClick.AddListener(() => onSkip?.Invoke());

        panel.localScale = Vector3.one * .6f;
        panel.DOScale(1f, slideTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(panel.gameObject);
    }

    public void HideCard()
    {
        if (card == null) return;

        GameObject old = card.gameObject;
        card = null;
        old.transform.DOScale(.8f, .15f).SetUpdate(true).SetLink(old).OnComplete(() => Destroy(old));
    }

    #endregion

    #region BUBBLE

    // header: "PASSO 3/13" no tutorial, "DICA" nas dicas de contexto (sem o Pular).
    public void ShowStep(string header, string text, string okLabel, Action ok, bool canSkip)
    {
        if (bubble == null) return;

        onOk = ok;
        stepLbl.text = header;
        bubbleLbl.SetText(text);
        skipButton.gameObject.SetActive(canSkip);
        okButton.gameObject.SetActive(!string.IsNullOrEmpty(okLabel));
        if (okLbl != null && !string.IsNullOrEmpty(okLabel)) okLbl.text = okLabel;

        bool wasHidden = !bubble.gameObject.activeSelf;
        bubble.gameObject.SetActive(true);
        bubble.DOKill();
        bubble.localScale = Vector3.one;

        if (wasHidden) bubble.anchoredPosition = new Vector2(0f, -bubbleSize.y);
        else bubble.DOPunchScale(Vector3.one * .06f, slideTime, 4, .5f).SetUpdate(true).SetLink(bubble.gameObject);

        bubble.DOAnchorPosY(bubbleBottom + bubbleSize.y * .5f, slideTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(bubble.gameObject);
    }

    public void HideBubble()
    {
        SetTarget(null, null);
        if (bubble == null || !bubble.gameObject.activeSelf) return;

        bubble.DOKill();
        bubble.DOAnchorPosY(-bubbleSize.y, slideTime).SetEase(Ease.InBack).SetUpdate(true).SetLink(bubble.gameObject)
            .OnComplete(() => bubble.gameObject.SetActive(false));
    }

    public void SetVisible(bool visible)
    {
        if (rootGroup == null) return;

        rootGroup.alpha = visible ? 1f : 0f;
        rootGroup.blocksRaycasts = visible;
    }

    #endregion

    #region TARGET

    // Um alvo por vez: elemento do HUD ou objeto do mundo (casa, base, Armazém). Nulo esconde seta e aro.
    public void SetTarget(RectTransform hud, Transform world)
    {
        hudTarget = hud;
        worldTarget = world;
    }

    private void LateUpdate()
    {
        if (root == null || arrow == null) return;

        bool hasHud = hudTarget != null && hudTarget.gameObject.activeInHierarchy;
        bool hasWorld = worldTarget != null && worldTarget.gameObject.activeInHierarchy;
        if (!hasHud && !hasWorld)
        {
            arrow.gameObject.SetActive(false);
            ring.gameObject.SetActive(false);
            return;
        }

        Vector2 position;
        float size;
        if (hasHud)
        {
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, hudTarget);
            position = bounds.center;
            size = Mathf.Max(bounds.size.x, bounds.size.y) + 40f;
        }
        else
        {
            position = MiniGameFeel.HudCenter(worldTarget, out Vector2 targetSize);
            size = MiniGameFeel.RingSize(targetSize, worldRingSize);
        }

        // Fora da tela: a seta fica na borda apontando para lá, sem aro.
        Vector2 half = root.rect.size * .5f - Vector2.one * edgeMargin;
        bool offScreen = Mathf.Abs(position.x) > half.x || Mathf.Abs(position.y) > half.y;
        float bob = arrowBob * (.5f + .5f * Mathf.Sin(Time.unscaledTime * arrowBobSpeed));

        Vector2 direction;
        Vector2 tip;
        if (offScreen)
        {
            direction = position.normalized;
            tip = new Vector2(Mathf.Clamp(position.x, -half.x, half.x), Mathf.Clamp(position.y, -half.y, half.y)) - direction * bob;
        }
        else
        {
            // A seta vem do lado do meio da tela; alvo perto do meio recebe a seta por cima.
            direction = position.sqrMagnitude > 250f * 250f ? position.normalized : Vector2.down;
            tip = position - direction * (size * .5f + 10f + bob);
        }

        arrow.gameObject.SetActive(true);
        arrow.anchoredPosition = tip;
        arrow.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        ring.gameObject.SetActive(!offScreen);
        ring.rectTransform.anchoredPosition = position;
        ring.rectTransform.sizeDelta = Vector2.one * size;
        ring.rectTransform.localScale = Vector3.one * (1f + ringPulse * Mathf.Sin(Time.unscaledTime * arrowBobSpeed));
    }

    #endregion
}
