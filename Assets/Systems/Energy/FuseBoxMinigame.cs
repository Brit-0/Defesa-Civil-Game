using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Minigame do RELIGAR (docs/jam/ENERGIA_DIA_NOITE.md): no apagão, o botão RELIGAR abre o quadro de força ao lado dele.
// Cada chave tem um número; ligar de 1 até o fim religa a rede (EnergyManager.Restore). Chave fora da ordem = falha
// leve: as ligadas descem, o quadro treme e trava um instante. A regra fica no FuseBox; aqui só a tela, o som e o feel.
// A rede que volta sozinha (tempo do apagão) fecha o quadro sem festa.
public class FuseBoxMinigame : MonoBehaviour
{
    public static FuseBoxMinigame Instance;

    [Header("LAYOUT (unidades do HUD em 1080p)")]
    [SerializeField] private float columnGap = 18f;
    [SerializeField] private float padding = 22f;
    [SerializeField] private float gapFromButton = 14f;

    [Header("TEXTS")]
    [SerializeField] private string titleText = "QUADRO DE FORÇA";
    [SerializeField] private string orderText = "LIGUE NA ORDEM";
    [SerializeField] private string wrongText = "FORA DA ORDEM!";
    [SerializeField] private string doneText = "LUZ!";

    [Header("COLORS")]
    [SerializeField] private Color tagColor = new Color(1f, .82f, .3f, 1f);
    [SerializeField] private Color tagDoneColor = new Color(.45f, .9f, .4f, 1f);
    [SerializeField] private Color lockedTint = new Color(.6f, .6f, .65f, 1f);
    [SerializeField] private Color flashColor = new Color(1f, .95f, .7f, .55f);
    [SerializeField] private Color wrongColor = new Color(1f, .32f, .25f, 1f);

    [Header("SOUND")]
    [SerializeField, Range(0f, 1f)] private float volume = .85f;
    [SerializeField] private float pitchStep = .12f;

    private const string LayerName = "FuseBox";
    private const string TutorialLayer = "Tutorial";

    private class SwitchView
    {
        public Image body;
        public Image led;
        public TMP_Text number;
    }

    private readonly List<SwitchView> views = new();
    private RectTransform root;
    private RectTransform box;
    private Image flash;
    private FuseBox fuse;
    private AudioSource source;
    private bool finishing;
    private bool wasLocked;
    private int opens;

    private static EnergyManager Grid => EnergyManager.Instance;
    private static FuseBoxData Data => FuseBoxData.Instance;

    public bool IsOpen => root != null && fuse != null;
    // Alvos da seta do tutorial da energia: a próxima chave a ligar e o quadro.
    public RectTransform NextSwitch => IsOpen && fuse.NextIndex >= 0 ? views[fuse.NextIndex].body.rectTransform : null;
    public RectTransform Box => box;
    public FuseBox Fuse => fuse;

    private void Awake()
    {
        Instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    private void OnEnable()
    {
        GameEvents.BlackoutChanged += OnBlackoutChanged;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.BlackoutChanged -= OnBlackoutChanged;
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    private void Update()
    {
        if (!IsOpen) return;

        fuse.Tick(GameClock.DeltaTime);
        if (wasLocked && !fuse.Locked) Tint(Color.white);
        wasLocked = fuse.Locked;
    }

    #region OPEN / CLOSE

    // Botão RELIGAR: abre o quadro (ou fecha, se já está aberto).
    public void Toggle(RectTransform anchor)
    {
        if (IsOpen)
        {
            if (!finishing) Close(false);
            return;
        }
        Open(anchor);
    }

    public void Open(RectTransform anchor)
    {
        if (IsOpen || Grid == null || !Grid.Blackout) return;

        RectTransform layer = FeelUI.HudLayer(LayerName);
        if (layer == null) return;

        // Por cima do HUD, mas embaixo do tutorial: a seta e o aro apontam a próxima chave por cima do quadro.
        layer.SetAsLastSibling();
        Transform tutorial = layer.parent.Find(TutorialLayer);
        if (tutorial != null) tutorial.SetAsLastSibling();
        fuse = new FuseBox(Data.switches, Data.wrongLock, new System.Random());
        finishing = false;
        wasLocked = false;
        Build(layer, anchor);
        opens++;

        box.localScale = Vector3.one * .6f;
        box.DOScale(1f, .22f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(box.gameObject);
        for (int i = 0; i < views.Count; i++)
        {
            RectTransform body = views[i].body.rectTransform;
            body.localScale = new Vector3(1f, 0f, 1f);
            body.DOScaleY(1f, .18f).SetDelay(.08f + i * .05f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(body.gameObject);
        }
        if (opens <= Data.hintTimes) PulseNext();
        Play(FireEnergyArt.SfxFuseOpen, 1f);
    }

    private void Close(bool success)
    {
        if (root == null) return;

        GameObject old = root.gameObject;
        root = null;
        box = null;
        fuse = null;
        views.Clear();

        RectTransform rect = (RectTransform)old.transform.GetChild(0);
        rect.DOKill();
        float time = success ? .25f : .15f;
        rect.DOScale(success ? 1.1f : .7f, time).SetEase(success ? Ease.OutQuad : Ease.InBack).SetUpdate(true).SetLink(old);
        CanvasGroup group = old.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.DOFade(0f, time).SetUpdate(true).SetLink(old).OnComplete(() => Destroy(old));
    }

    #endregion

    #region BUILD

    private void Build(RectTransform layer, RectTransform anchor)
    {
        views.Clear();
        float pixel = FeelUI.PixelSize;
        Sprite off = FireEnergyArt.Load(FireEnergyArt.FuseSwitchOff);
        Vector2 switchSize = (off != null ? off.rect.size : new Vector2(20f, 32f)) * pixel;
        float ledSize = 10f * pixel;
        float tagHeight = 36f;
        int count = fuse.Count;

        float width = padding * 2f + count * switchSize.x + (count - 1) * columnGap;
        width = Mathf.Max(width, 300f);
        float top = 4f * pixel + 10f;
        float titleHeight = 30f, orderHeight = 24f;
        float columnsTop = top + titleHeight + orderHeight + 10f;
        float height = columnsTop + tagHeight + 8f + switchSize.y + 10f + ledSize + padding;

        root = FeelUI.CreateRect("FuseBox", layer);
        root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(width, height);
        root.anchoredPosition = Place(layer, anchor, new Vector2(width, height));
        root.gameObject.AddComponent<CanvasGroup>();

        Image window = FeelUI.CreateWindow("Box", root);
        window.raycastTarget = true;
        box = window.rectTransform;
        FeelUI.Place(box, new Vector2(0f, 1f), new Vector2(.5f, .5f), new Vector2(width * .5f, -height * .5f), new Vector2(width, height));

        // Faixa de alerta amarela e preta no alto da tampa.
        Sprite stripeSprite = FireEnergyArt.Load(FireEnergyArt.FuseStripe);
        if (stripeSprite != null)
        {
            Image stripe = FeelUI.CreateImage("Stripe", box, Color.white, stripeSprite);
            stripe.type = Image.Type.Tiled;
            // Um pixel da arte = PixelSize unidades do HUD, como o resto do kit.
            stripe.pixelsPerUnitMultiplier = stripe.canvas.referencePixelsPerUnit / (stripeSprite.pixelsPerUnit * pixel);
            FeelUI.Place(stripe.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(3f * pixel, -3f * pixel), new Vector2(width - 6f * pixel, 4f * pixel));
        }

        Image bolt = FeelUI.CreateImage("Bolt", box, Color.white, FireEnergyArt.Load(FireEnergyArt.Bolt));
        bolt.preserveAspect = true;
        FeelUI.Place(bolt.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(padding - 6f, -top + 2f), Vector2.one * 34f);

        TMP_Text title = FeelUI.CreateText("Title", box, titleText, 24f, FeelUI.PixelCream, TextAlignmentOptions.Left);
        FeelUI.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(padding + 32f, -top), new Vector2(width - padding * 2f - 32f, titleHeight));

        TMP_Text order = FeelUI.CreateText("Order", box, $"{orderText} DE 1 A {count}", 18f, FeelUI.PixelLilac, TextAlignmentOptions.Left);
        FeelUI.Place(order.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(padding, -top - titleHeight), new Vector2(width - padding * 2f, orderHeight));

        float rowWidth = count * switchSize.x + (count - 1) * columnGap;
        float left = (width - rowWidth) * .5f;
        for (int i = 0; i < count; i++)
        {
            float x = left + i * (switchSize.x + columnGap) + switchSize.x * .5f;
            views.Add(BuildSwitch(i, x, columnsTop, tagHeight, switchSize, ledSize, off));
        }

        flash = FeelUI.CreateImage("Flash", box, new Color(flashColor.r, flashColor.g, flashColor.b, 0f));
        FeelUI.Stretch(flash.rectTransform, 3f * pixel);
    }

    private SwitchView BuildSwitch(int index, float x, float top, float tagHeight, Vector2 switchSize, float ledSize, Sprite off)
    {
        SwitchView view = new SwitchView();

        Image tag = FeelUI.CreateInset("Tag" + index, box, new Color(0f, 0f, 0f, .6f));
        FeelUI.Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(.5f, 1f), new Vector2(x, -top), new Vector2(switchSize.x - 6f, tagHeight));
        view.number = FeelUI.CreateText("Number", tag.transform, fuse.NumberOf(index).ToString(), 28f, tagColor);
        FeelUI.Stretch(view.number.rectTransform);

        view.body = FeelUI.CreateImage("Switch" + index, box, Color.white, off);
        view.body.raycastTarget = true;
        FeelUI.Place(view.body.rectTransform, new Vector2(0f, 1f), new Vector2(.5f, .5f), new Vector2(x, -top - tagHeight - 8f - switchSize.y * .5f), switchSize);
        Button button = view.body.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        int captured = index;
        button.onClick.AddListener(() => Press(captured));

        view.led = FeelUI.CreateImage("Led" + index, box, Color.white, FireEnergyArt.Load(FireEnergyArt.FuseLedOff));
        FeelUI.Place(view.led.rectTransform, new Vector2(0f, 1f), new Vector2(.5f, 1f), new Vector2(x, -top - tagHeight - 8f - switchSize.y - 10f), Vector2.one * ledSize);

        return view;
    }

    // À direita do botão RELIGAR, com o topo na altura dele; preso dentro da tela.
    private Vector2 Place(RectTransform layer, RectTransform anchor, Vector2 size)
    {
        Rect area = layer.rect;
        Vector2 at = new Vector2(area.center.x - size.x * .5f, area.center.y + size.y * .5f);
        if (anchor != null)
        {
            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Vector2 topRight = layer.InverseTransformPoint(corners[2]);
            at = new Vector2(topRight.x + gapFromButton, topRight.y);
        }

        at.x = Mathf.Clamp(at.x, area.xMin + 8f, area.xMax - size.x - 8f);
        at.y = Mathf.Clamp(at.y, area.yMin + size.y + 8f, area.yMax - 8f);
        return at;
    }

    #endregion

    #region PLAY

    public void Press(int index)
    {
        if (!IsOpen || finishing || GameManager.InputBlocked) return;

        FusePress result = fuse.Press(index);
        switch (result)
        {
            case FusePress.Correct:
                FlipOn(index);
                Play(FireEnergyArt.SfxFuseSwitch, 1f + (fuse.Lit - 1) * pitchStep);
                if (opens <= Data.hintTimes) PulseNext();
                break;
            case FusePress.Done:
                FlipOn(index);
                Play(FireEnergyArt.SfxFuseSwitch, 1f + (fuse.Lit - 1) * pitchStep);
                Finish();
                break;
            case FusePress.Wrong:
                Wrong(index);
                break;
        }
    }

    private void FlipOn(int index)
    {
        SwitchView view = views[index];
        view.body.sprite = FireEnergyArt.Load(FireEnergyArt.FuseSwitchOn);
        RectTransform body = view.body.rectTransform;
        body.DOKill(true);
        body.DOPunchScale(new Vector3(.12f, .2f, 0f), .2f, 6).SetUpdate(true).SetLink(body.gameObject);

        view.led.sprite = FireEnergyArt.Load(FireEnergyArt.FuseLedOn);
        RectTransform led = view.led.rectTransform;
        led.DOKill(true);
        led.localScale = Vector3.one * 1.6f;
        led.DOScale(1f, .2f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(led.gameObject);

        view.number.color = tagDoneColor;
        view.number.rectTransform.DOKill();
        view.number.rectTransform.localScale = Vector3.one;
        Sparks(body.anchoredPosition + new Vector2(0f, body.sizeDelta.y * .25f), FeelUI.PixelGold, 6);
    }

    private void Wrong(int index)
    {
        Play(FireEnergyArt.SfxFuseWrong, 1f);
        box.DOKill(true);
        box.DOShakeAnchorPos(.3f, 12f, 30).SetUpdate(true).SetLink(box.gameObject);

        for (int i = 0; i < views.Count; i++)
        {
            SwitchView view = views[i];
            bool wasOn = view.body.sprite == FireEnergyArt.Load(FireEnergyArt.FuseSwitchOn);
            view.body.sprite = FireEnergyArt.Load(FireEnergyArt.FuseSwitchOff);
            view.led.sprite = FireEnergyArt.Load(i == index ? FireEnergyArt.FuseLedBad : FireEnergyArt.FuseLedOff);
            view.number.color = tagColor;
            if (wasOn) view.body.rectTransform.DOPunchScale(new Vector3(0f, -.15f, 0f), .18f, 4).SetDelay(i * .03f).SetUpdate(true).SetLink(view.body.gameObject);
        }

        SwitchView bad = views[index];
        Sparks(bad.body.rectTransform.anchoredPosition, wrongColor, 8);
        Image badLed = bad.led;
        DOVirtual.DelayedCall(Data.wrongLock, () => { if (badLed != null) badLed.sprite = FireEnergyArt.Load(FireEnergyArt.FuseLedOff); }, true).SetLink(badLed.gameObject);
        Tint(lockedTint);
        wasLocked = true;
        Popup(wrongText, wrongColor);
        if (opens <= Data.hintTimes) DOVirtual.DelayedCall(Data.wrongLock, PulseNext, true).SetLink(box.gameObject);
    }

    // Todas ligadas: LEDs piscam juntos, a tampa clareia, o zumbido sobe e a rede volta no estalo.
    private void Finish()
    {
        finishing = true;
        Play(FireEnergyArt.SfxFuseDone, 1f);
        Popup(doneText, tagDoneColor);

        for (int i = 0; i < views.Count; i++)
        {
            RectTransform led = views[i].led.rectTransform;
            led.DOKill(true);
            led.DOPunchScale(Vector3.one * .5f, .3f, 4).SetDelay(.05f + i * .05f).SetUpdate(true).SetLink(led.gameObject);
        }

        flash.DOKill();
        Sequence glow = DOTween.Sequence().SetUpdate(true).SetLink(flash.gameObject);
        glow.Append(flash.DOFade(flashColor.a, Data.restoreDelay).SetEase(Ease.InQuad));
        glow.Append(flash.DOFade(0f, .25f));

        DOVirtual.DelayedCall(Data.restoreDelay, () =>
        {
            if (Grid != null && Grid.Blackout) Grid.Restore();
            Close(true);
        }, true).SetLink(root.gameObject);
    }

    private void PulseNext()
    {
        if (!IsOpen || fuse.NextIndex < 0) return;

        foreach (SwitchView view in views)
        {
            view.number.rectTransform.DOKill();
            view.number.rectTransform.localScale = Vector3.one;
        }
        RectTransform number = views[fuse.NextIndex].number.rectTransform;
        number.DOScale(1.25f, .35f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(number.gameObject);
    }

    private void Tint(Color color)
    {
        foreach (SwitchView view in views) view.body.color = color;
    }

    #endregion

    #region FEEL

    // Faíscas de pixel saindo do ponto dado (coordenadas da tampa).
    private void Sparks(Vector2 at, Color color, int count)
    {
        Sprite sprite = FireEnergyArt.Load(FireEnergyArt.FuseSpark);
        float pixel = FeelUI.PixelSize;
        for (int i = 0; i < count; i++)
        {
            Image spark = FeelUI.CreateImage("Spark", box, color, sprite);
            RectTransform rect = spark.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = Vector2.one * 7f * pixel * Random.Range(.6f, 1f);
            rect.anchoredPosition = BoxCenterOffset(at);
            float angle = (i / (float)count + Random.Range(-.06f, .06f)) * Mathf.PI * 2f;
            Vector2 to = rect.anchoredPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(40f, 70f);
            rect.DOAnchorPos(to, .3f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(spark.gameObject);
            rect.DOScale(.2f, .3f).SetEase(Ease.InQuad).SetUpdate(true).SetLink(spark.gameObject);
            spark.DOFade(0f, .3f).SetEase(Ease.InQuad).SetUpdate(true).SetLink(spark.gameObject).OnComplete(() => Destroy(spark.gameObject));
        }
    }

    // As peças da tampa usam âncora no canto de cima à esquerda; as faíscas, o centro.
    private Vector2 BoxCenterOffset(Vector2 topLeftPosition)
    {
        return topLeftPosition + new Vector2(-box.sizeDelta.x * .5f, box.sizeDelta.y * .5f);
    }

    private void Popup(string text, Color color)
    {
        TextMeshProUGUI label = FeelUI.CreateText("FusePopup", root, text, 30f, color);
        FeelUI.Outline(label, .3f);
        RectTransform rect = label.rectTransform;
        FeelUI.Place(rect, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, -10f), new Vector2(360f, 40f));
        rect.localScale = Vector3.zero;

        Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(label.gameObject);
        sequence.Append(rect.DOScale(1f, .18f).SetEase(Ease.OutBack));
        sequence.Join(rect.DOAnchorPosY(-40f, .9f).SetEase(Ease.OutCubic));
        sequence.Insert(.6f, label.DOFade(0f, .3f));
        sequence.OnComplete(() => Destroy(label.gameObject));
    }

    private void Play(string key, float pitch)
    {
        AudioClip clip = FireEnergyArt.Clip(key);
        if (clip == null) return;

        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }

    #endregion

    #region EVENTS

    private void OnBlackoutChanged(bool active, Vector3 worldPosition)
    {
        if (!active && IsOpen && !finishing) Close(false);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Won || state == GameState.Lost) Close(false);
    }

    #endregion
}
