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
    [SerializeField] private Vector2 bubbleSize = new Vector2(1060f, 172f);
    [Tooltip("Margem interna do balão dos lados e em cima/embaixo")]
    [SerializeField] private float textPadding = 44f;
    [SerializeField] private float textPaddingY = 20f;
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
    [Tooltip("Quanto o aro apaga no pulso (0 a 1)")]
    [SerializeField] private float ringPulse = .45f;
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
    private bool bubbleShown;
    private float bubbleY;

    public event Action SkipPressed;

    public void Build()
    {
        root = FeelUI.HudLayer("Tutorial");
        if (root == null) return;

        root.SetAsLastSibling();
        rootGroup = root.gameObject.AddComponent<CanvasGroup>();
        Vector2 center = new Vector2(.5f, .5f);

        // Aro fino do Mk_Ring, Simple e quadrado (o Button2 fatiado esticava e quadriculava, #531).
        ring = FeelUI.CreateRing("Ring", root, arrowColor);
        ring.gameObject.SetActive(false);
        arrow = BuildArrow(root);
        arrow.gameObject.SetActive(false);

        // Balão do passo: moldura dourada como as dicas da cena.
        bubble = FeelUI.CreatePanel("Bubble", root, frameColor).rectTransform;
        FeelUI.Place(bubble, new Vector2(.5f, 0f), center, new Vector2(0f, -bubbleSize.y), bubbleSize);
        FeelUI.Stretch(FeelUI.CreatePanel("Back", bubble, backColor).rectTransform, border);

        // Margem interna: texto longe da moldura dos lados e de cima, e abaixo da linha do passo (#531).
        stepLbl = FeelUI.CreateText("Step", bubble, "", textSize * .6f, frameColor, TextAlignmentOptions.TopLeft);
        FeelUI.Place(stepLbl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(textPadding, -textPaddingY), new Vector2(300f, textSize));

        float textTop = textPaddingY + textSize;
        TMP_Text text = FeelUI.CreateText("Lbl", bubble, "", textSize, Color.white, TextAlignmentOptions.MidlineLeft);
        FeelUI.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(textPadding, -textTop),
            new Vector2(bubbleSize.x - textPadding - 290f, bubbleSize.y - textTop - textPaddingY));
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

        // Seta por cima do balão: alvo embaixo à esquerda (META) deixava a seta escondida atrás dele.
        arrow.SetAsLastSibling();
        bubble.gameObject.SetActive(false);
    }

    // Ponta na borda do aro, medida na direção da seta (aro largo e baixo não afasta a seta por cima).
    private static Vector2 TipFor(Vector2 position, Vector2 direction, Vector2 ringSize, float size, float bob)
    {
        float reachX = Mathf.Abs(direction.x) > .001f ? ringSize.x * .5f / Mathf.Abs(direction.x) : float.MaxValue;
        float reachY = Mathf.Abs(direction.y) > .001f ? ringSize.y * .5f / Mathf.Abs(direction.y) : float.MaxValue;
        return position - direction * (Mathf.Min(reachX, reachY, size * .5f) + 10f + bob);
    }

    // A seta (uns 100 de comprimento atrás da ponta, 40 de largura) passaria por cima do balão aberto?
    private bool ArrowHitsBubble(Vector2 tip, Vector2 direction)
    {
        if (!bubbleShown || bubble == null || !bubble.gameObject.activeInHierarchy) return false;

        Bounds box = RectTransformUtility.CalculateRelativeRectTransformBounds(root, bubble);
        box.Expand(new Vector3(40f, 40f, 0f));
        for (float back = 0f; back <= 100f; back += 25f)
        {
            Vector2 point = tip - direction * back;
            if (box.Contains(new Vector3(point.x, point.y, box.center.z))) return true;
        }

        return false;
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
        // Cor chapada: o painel do tema pixel é escuro e apagava o amarelo da seta.
        RectTransform bar = FeelUI.CreateImage("Bar", parent, color).rectTransform;
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
        FeelUI.Place(titleLbl.rectTransform, new Vector2(.5f, 1f), center, new Vector2(0f, -76f), new Vector2(cardSize.x - textPadding * 3f, 80f));

        TMP_Text bodyLbl = FeelUI.CreateText("Body", panel, body, 32f, Color.white);
        FeelUI.Place(bodyLbl.rectTransform, center, center, new Vector2(0f, 20f), new Vector2(cardSize.x - textPadding * 4f, 260f));
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

        bubbleShown = true;
        bubbleY = BubbleY;
        bubble.DOAnchorPosY(bubbleY, slideTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(bubble.gameObject);
    }

    // Com o painel da equipe aberto, o balão sobe e fica acima dele (HudAvoid).
    private float BubbleY => HudAvoid.ShownY(bubble, bubbleBottom + bubbleSize.y * .5f);

    private void FollowGuardTab()
    {
        if (!bubbleShown || bubble == null || !bubble.gameObject.activeSelf) return;

        float y = BubbleY;
        if (Mathf.Abs(y - bubbleY) < 1f) return;

        bubbleY = y;
        bubble.DOAnchorPosY(y, slideTime * .6f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(bubble.gameObject);
    }

    public void HideBubble()
    {
        SetTarget(null, null);
        if (bubble == null || !bubble.gameObject.activeSelf) return;

        bubbleShown = false;
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
        FollowGuardTab();
        if (root == null || arrow == null) return;

        bool hasHud = hudTarget != null && hudTarget.gameObject.activeInHierarchy;
        bool hasWorld = worldTarget != null && worldTarget.gameObject.activeInHierarchy;
        if (!hasHud && !hasWorld)
        {
            arrow.gameObject.SetActive(false);
            ring.gameObject.SetActive(false);
            return;
        }

        // Aro sempre redondo, no passo de 32 do sprite (FeelUI.RingSide). Painel inteiro do HUD (maior que o aro
        // maior) fica só com a seta, sem aro por cima.
        // A seta encosta no aro ou, sem aro, na borda do painel (ringSize é só o alcance dela).
        Vector2 position;
        bool ringFits = true;
        float side;
        Vector2 ringSize;
        if (hasHud)
        {
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, hudTarget);
            position = bounds.center;
            ringFits = FeelUI.RingFits(bounds.size);
            side = FeelUI.RingSide(bounds.size);
            ringSize = ringFits ? Vector2.one * side : (Vector2)bounds.size + Vector2.one * 24f;
        }
        else
        {
            position = MiniGameFeel.HudCenter(worldTarget, out Vector2 targetSize);
            side = targetSize.sqrMagnitude > 0f ? FeelUI.RingSide(targetSize) : FeelUI.RingSide(Vector2.one * worldRingSize, 0f);
            ringSize = Vector2.one * side;
        }
        float size = Mathf.Max(ringSize.x, ringSize.y);

        // Fora da tela (só casas e prédios; o HUD está sempre à vista): a seta fica na borda apontando para lá, sem aro.
        Vector2 half = root.rect.size * .5f - Vector2.one * edgeMargin;
        bool offScreen = !hasHud && (Mathf.Abs(position.x) > half.x || Mathf.Abs(position.y) > half.y);
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
            tip = TipFor(position, direction, ringSize, size, bob);
            // Alvo do HUD logo abaixo do balão (a META): a seta por cima cobria o texto, então vem de lado (#531).
            if (hasHud && ArrowHitsBubble(tip, direction))
            {
                direction = new Vector2(position.x < 0f ? -1f : 1f, 0f);
                tip = TipFor(position, direction, ringSize, size, bob);
            }
        }

        arrow.gameObject.SetActive(true);
        arrow.anchoredPosition = tip;
        arrow.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        ring.gameObject.SetActive(!offScreen && ringFits);
        ring.rectTransform.anchoredPosition = position;
        ring.rectTransform.sizeDelta = ringSize;
        // Pulso no brilho, não na escala: o aro fica sempre do mesmo tamanho e o pixel dele não treme.
        Color pulse = arrowColor;
        pulse.a *= 1f - ringPulse * (.5f + .5f * Mathf.Sin(Time.unscaledTime * arrowBobSpeed));
        ring.color = pulse;
    }

    #endregion
}
