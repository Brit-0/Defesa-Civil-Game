using DG.Tweening;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Dicas dos primeiros passos, numa faixa embaixo do HUD. Só escuta eventos e só na primeira volta da sessão:
//casa com problema -> chamado aberto -> conserto pronto -> pagamento recebido. Monta a própria UI.
//Nos passos em que há uma casa para clicar, uma seta pula em cima dela e um aro pulsa em volta; com a casa
//fora da tela, a seta vai para a borda e aponta para onde ela está.
public class TutorialTips : MonoBehaviour
{
    [Header("TEXTS (aceitam tags do Text Animator)")]
    [SerializeField] private string openCallTip = "Uma casa precisa de ajuda! <bounce>Clique nela</bounce> para abrir um chamado.";
    [SerializeField] private string sendTeamTip = "Escolha a <bounce>base</bounce>, monte a equipe e clique em <bounce>Enviar</bounce>.";
    [SerializeField] private string collectTip = "Conserto pronto! <bounce>Clique na casa</bounce> para receber o pagamento.";
    [SerializeField] private string doneTip = "Boa! Use o dinheiro para comprar <bounce>melhorias</bounce>.";

    [Header("LAYOUT (tela de referência 1920x1080)")]
    [SerializeField] private Vector2 size = new Vector2(1240f, 84f);
    [SerializeField] private float bottomMargin = 110f;
    [SerializeField] private float border = 4f;
    [SerializeField] private float fontSize = 34f;
    [SerializeField] private float dotSize = 18f;
    [SerializeField] private float dotSpacing = 10f;

    [Header("POINTER (seta e aro em cima da casa)")]
    [SerializeField] private bool showPointer = true;
    [SerializeField] private Vector2 chevronSize = new Vector2(46f, 12f);
    [SerializeField] private float pointerGap = 26f;
    [SerializeField] private float bobHeight = 12f;
    [SerializeField] private float bobSpeed = 6f;
    [SerializeField] private float ringScale = 1.25f;
    [Tooltip("Quanto o aro apaga no pulso (0 a 1)")]
    [SerializeField] private float ringPulse = .25f;
    [SerializeField] private float ringPulseTime = .7f;
    [SerializeField] private float edgeMargin = 70f;

    [Header("COLORS")]
    [SerializeField] private Color frameColor = new Color(1f, .82f, .3f);
    [SerializeField] private Color backColor = FeelUI.PanelColor;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color dotOffColor = new Color(1f, 1f, 1f, .2f);
    [SerializeField] private Color outlineColor = new Color(0f, 0f, 0f, .85f);

    [Header("TIMING")]
    [SerializeField] private float slideTime = .35f;
    [SerializeField] private float changePunch = .08f;
    [SerializeField] private float doneTime = 5f;
    [SerializeField, Range(0f, 1f)] private float popupVolume = .6f;

    private enum Step { WaitProblem, OpenCall, SendTeam, WaitWork, Collect, Done }

    //Vale pela sessão: depois do Reiniciar as dicas não voltam (sem disco, por causa do WebGL)
    private static bool completed;

    private RectTransform bar;
    private TextAnimator_TMP label;
    private Image[] dots;
    private Tween hideTween;
    private Step step;

    private RectTransform pointer;
    private RectTransform chevron;
    private Image ring;
    private RectTransform target;
    private readonly Vector3[] corners = new Vector3[4];

    //Com o painel da equipe aberto, a faixa sobe e fica acima dele (HudAvoid)
    private float ShownY => HudAvoid.ShownY(bar, bottomMargin + size.y * .5f);
    private float HiddenY => -size.y;
    private bool showing;
    private float shownAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        completed = false;
    }

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        RectTransform parent = FeelUI.ViewRoot(this, "TutorialTips");
        if (parent == null) return;

        Vector2 center = new Vector2(.5f, .5f);

        BuildPointer(parent);

        //Kit pixel: a moldura dourada já traz o fundo; as bolinhas são o sprite aceso e o apagado
        FeelUITheme px = FeelUI.Pixel;
        float edge = px != null ? 5f * FeelUI.PixelSize : border;
        float dot = px != null && px.dotOn != null ? px.dotOn.rect.width * FeelUI.PixelSize : dotSize;

        if (px != null)
        {
            bar = FeelUI.CreateSliced("Tip", parent, px.goldFrame, Color.white).rectTransform;
            FeelUI.Place(bar, new Vector2(.5f, 0f), center, new Vector2(0f, HiddenY), size);
        }
        else
        {
            bar = FeelUI.CreatePanel("Tip", parent, frameColor).rectTransform;
            FeelUI.Place(bar, new Vector2(.5f, 0f), center, new Vector2(0f, HiddenY), size);
            FeelUI.Stretch(FeelUI.CreatePanel("Back", bar, backColor).rectTransform, border);
        }

        //Três bolinhas à esquerda: os passos da primeira volta (abrir, enviar, receber)
        dots = new Image[3];
        //Margem interna: duas molduras de folga além da borda pixel (#531)
        float pad = edge * (px != null ? 3f : 4f);
        float left = pad + dot * .5f;
        for (int i = 0; i < dots.Length; i++)
        {
            dots[i] = px != null ? FeelUI.CreateIcon("Dot" + i, bar, px.dotOff) : FeelUI.CreateCircle("Dot" + i, bar, dotOffColor);
            FeelUI.Place(dots[i].rectTransform, new Vector2(0f, .5f), center, new Vector2(left + i * (dot + dotSpacing), 0f), Vector2.one * dot);
        }

        TMP_Text text = FeelUI.CreateText("Lbl", bar, "", fontSize, textColor);
        FeelUI.Stretch(text.rectTransform, pad);
        text.rectTransform.offsetMax = new Vector2(-pad, -edge * (px != null ? 2f : 4f));
        text.rectTransform.offsetMin = new Vector2(left + dots.Length * (dot + dotSpacing), edge * (px != null ? 2f : 4f));
        //Dica comprida encolhe para caber na faixa
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * .5f;
        text.fontSizeMax = fontSize;
        label = text.gameObject.AddComponent<TextAnimator_TMP>();

        bar.gameObject.SetActive(false);
    }

    //Seta em V feita de duas barras do RoundRectangle (com sombra preta) e o aro do Button2
    private void BuildPointer(RectTransform parent)
    {
        pointer = FeelUI.CreateRect("TipPointer", parent);
        FeelUI.Stretch(pointer);

        //Kit pixel: seta pixel (sem o kit, o V de duas barras). O aro é o mesmo do tutorial: o Mk_Ring fino,
        //Simple e quadrado, no passo de 32 (FeelUI.RingSide), dourado por tint (#531)
        FeelUITheme px = FeelUI.Pixel;
        ring = FeelUI.CreateRing("Ring", pointer, px != null ? FeelUI.PixelGold : frameColor);
        ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(.5f, .5f);

        chevron = FeelUI.CreateRect("Chevron", pointer);
        chevron.anchorMin = chevron.anchorMax = new Vector2(.5f, .5f);
        chevron.sizeDelta = Vector2.one * chevronSize.x;

        if (px != null && px.iconArrowDown != null)
        {
            Image arrow = FeelUI.CreateIcon("Arrow", chevron, px.iconArrowDown);
            FeelUI.Stretch(arrow.rectTransform);
        }
        else
        {
            AddChevronBars(outlineColor, 4f);
            AddChevronBars(frameColor, 0f);
        }

        pointer.gameObject.SetActive(false);
    }

    private void AddChevronBars(Color color, float grow)
    {
        float half = chevronSize.x * .3f;

        for (int side = -1; side <= 1; side += 2)
        {
            Image barImage = FeelUI.CreatePanel("Bar", chevron, color);
            RectTransform rect = barImage.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(chevronSize.x * .7f + grow, chevronSize.y + grow);
            //As duas pernas do V se encontram embaixo, na ponta que aponta para a casa
            rect.anchoredPosition = new Vector2(side * half * .7f, half * .55f);
            rect.localRotation = Quaternion.Euler(0f, 0f, side * 45f);
        }
    }

    private void OnEnable()
    {
        GameEvents.HouseStateChanged += OnHouseStateChanged;
        GameEvents.CallOpened += OnCallOpened;
        GameEvents.TeamDispatched += OnTeamDispatched;
        GameEvents.WorkCompleted += OnWorkCompleted;
        GameEvents.CallResolved += OnCallResolved;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.HouseStateChanged -= OnHouseStateChanged;
        GameEvents.CallOpened -= OnCallOpened;
        GameEvents.TeamDispatched -= OnTeamDispatched;
        GameEvents.WorkCompleted -= OnWorkCompleted;
        GameEvents.CallResolved -= OnCallResolved;
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    private void OnHouseStateChanged(House house, State before, State after)
    {
        if (completed || step != Step.WaitProblem) return;
        if (after != State.Damaged && after != State.OnFire) return;

        Show(Step.OpenCall, openCallTip);
        Point(house);
    }

    private void OnCallOpened(Call call)
    {
        if (completed || step > Step.OpenCall) return;

        Show(Step.SendTeam, sendTeamTip);
        Point(null);
    }

    private void OnTeamDispatched(Call call)
    {
        if (completed || step > Step.SendTeam) return;

        step = Step.WaitWork;
        Point(null);
        Hide();
    }

    private void OnWorkCompleted(Call call)
    {
        //Só o conserto: a viagem ao Armazém também termina com WorkCompleted
        if (completed || step > Step.WaitWork || call == null || call.type != CallType.Problem) return;

        Show(Step.Collect, collectTip);
        Point(call.house);
    }

    private void OnCallResolved(Call call, int moneyEarned)
    {
        if (completed || (call != null && call.type != CallType.Problem)) return;

        //A primeira volta fechou: a última dica fica um tempo e as dicas acabam
        completed = true;
        Show(Step.Done, doneTip);
        Point(null);
        if (bar != null) hideTween = DOVirtual.DelayedCall(doneTime, Hide, true).SetLink(bar.gameObject);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.Won && state != GameState.Lost && state != GameState.Menu) return;

        Point(null);
        Hide();
    }

    private void Show(Step newStep, string text)
    {
        step = newStep;
        if (bar == null) return;

        hideTween?.Kill();
        hideTween = null;

        bool wasHidden = !bar.gameObject.activeSelf;
        bar.gameObject.SetActive(true);
        label.SetText(text);
        UpdateDots();
        AudioManager.PlaySfx(AudioManager.Sfx?.popup, popupVolume, .05f);

        bar.DOKill();
        bar.localScale = Vector3.one;

        if (wasHidden) bar.anchoredPosition = new Vector2(0f, HiddenY);
        else bar.DOPunchScale(Vector3.one * changePunch, slideTime, 4, .5f).SetUpdate(true).SetLink(bar.gameObject);

        showing = true;
        shownAt = ShownY;
        bar.DOAnchorPosY(shownAt, slideTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(bar.gameObject);
    }

    //O painel da equipe abriu ou fechou com a faixa na tela: ela desliza para a nova altura
    private void FollowGuardTab()
    {
        if (!showing || bar == null || !bar.gameObject.activeSelf) return;

        float y = ShownY;
        if (Mathf.Abs(y - shownAt) < 1f) return;

        shownAt = y;
        bar.DOAnchorPosY(y, slideTime * .6f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(bar.gameObject);
    }

    //Passo atual acende e pulsa; os já feitos ficam acesos
    private void UpdateDots()
    {
        int current = step == Step.OpenCall ? 0 : step == Step.SendTeam ? 1 : step == Step.Collect ? 2 : dots.Length;

        for (int i = 0; i < dots.Length; i++)
        {
            Image dot = dots[i];
            dot.DOKill();
            dot.transform.DOKill();
            dot.transform.localScale = Vector3.one;
            FeelUITheme px = FeelUI.Pixel;
            if (px != null && px.dotOn != null)
            {
                dot.sprite = i <= current ? px.dotOn : px.dotOff;
                dot.color = Color.white;
            }
            else
            {
                dot.color = i <= current ? frameColor : dotOffColor;
            }

            if (i == current)
            {
                dot.transform.DOScale(1.35f, .4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(dot.gameObject);
            }
        }
    }

    private void Hide()
    {
        hideTween?.Kill();
        hideTween = null;

        if (bar == null || !bar.gameObject.activeSelf) return;

        showing = false;
        bar.DOKill();
        bar.DOAnchorPosY(HiddenY, slideTime).SetEase(Ease.InBack).SetUpdate(true).SetLink(bar.gameObject)
            .OnComplete(() => bar.gameObject.SetActive(false));
    }

    #region POINTER

    private void Point(House house)
    {
        target = house != null ? house.transform as RectTransform : null;
        if (pointer == null) return;

        bool on = showPointer && target != null;
        pointer.gameObject.SetActive(on);
        ring.DOKill();
        ring.transform.DOKill();
        if (!on) return;

        pointer.SetAsLastSibling();
        ring.transform.localScale = Vector3.one;
        //Pulso no brilho, não na escala: o aro fica do tamanho do passo de 32 e o pixel não treme (#531)
        ring.color = FeelUI.Pixel != null ? FeelUI.PixelGold : frameColor;
        ring.DOFade(1f - ringPulse, ringPulseTime).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(ring.gameObject);
        chevron.localScale = Vector3.zero;
        chevron.DOScale(1f, .3f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(chevron.gameObject);

        LateUpdate();
    }

    //Segue a casa pela câmera; fora da tela, a seta fica na borda apontando para ela
    private void LateUpdate()
    {
        FollowGuardTab();
        if (target == null || pointer == null || !pointer.gameObject.activeSelf) return;

        target.GetWorldCorners(corners);
        Vector2 bottom = FeelUI.WorldToHud((corners[0] + corners[3]) * .5f);
        Vector2 top = FeelUI.WorldToHud((corners[1] + corners[2]) * .5f);
        Vector2 middle = (bottom + top) * .5f;
        float height = Mathf.Abs(top.y - bottom.y);

        Vector2 half = pointer.rect.size * .5f - Vector2.one * edgeMargin;
        bool onScreen = Mathf.Abs(middle.x) <= half.x && Mathf.Abs(middle.y) <= half.y;
        float bob = Mathf.Sin(GameClock.Time * bobSpeed) * bobHeight;

        ring.enabled = onScreen;

        if (onScreen)
        {
            ring.rectTransform.anchoredPosition = middle;
            ring.rectTransform.sizeDelta = Vector2.one * FeelUI.RingSide(Vector2.one * height * ringScale, 0f);
            chevron.anchoredPosition = top + Vector2.up * (pointerGap + chevronSize.x * .5f + bob);
            chevron.localRotation = Quaternion.identity;
            return;
        }

        //Na borda, na direção da casa; o V aponta para fora da tela
        Vector2 direction = middle.normalized;
        float scale = Mathf.Min(half.x / Mathf.Max(.001f, Mathf.Abs(direction.x)), half.y / Mathf.Max(.001f, Mathf.Abs(direction.y)));
        chevron.anchoredPosition = direction * (scale - Mathf.Abs(bob));
        chevron.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f);
    }

    #endregion
}
