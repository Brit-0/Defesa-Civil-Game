using System.Collections.Generic;
using DefesaCivil.CityGen;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Um quarteirão da guarita (GUARITAS.md): a área atendida no mundo (borda tracejada e preenchimento), o marcador no HUD
// (a mesma placa, seta e barra de obra do para-raio) e a guarita no canto do quarteirão que menos cobre as casas.
// No modo de escolha (obra paga pela aba), cada quarteirão livre na tela mostra o marcador; o mouse em cima acende a
// área, a guarita meio apagada e a dica, e o clique começa a obra. Pronta, a luz da guarita pisca verde (de prontidão)
// ou âmbar (chamado esperando gente). A regra fica na OutpostManager.
public class OutpostBlock : MonoBehaviour
{
    private const string LotSprites = "Expansion/";
    private const string OutpostSprites = "Outpost/";
    private const float ArtPpu = 32f;                // pixels da arte por unidade do mundo (casas e kit)
    private const float PixelScale = 3f;             // 1 pixel da arte = 3 px numa tela de 1080
    private const float CanvasReferencePpu = 100f;   // referencePixelsPerUnit dos CanvasScaler da cena
    private const float HintInterval = 0.5f;
    private const float ArrowSize = 32f;
    private const float SignLift = 0.3f;             // na obra, folga entre o topo da guarita e a seta da placa
    private const float GhostAlpha = 0.6f;           // guarita fantasma durante a prévia
    private const float WaitingShow = 2.5f;          // segundos que a luz fica âmbar depois de um chamado sem gente
    private static readonly Vector2 LampAt = new Vector2(0.263f, 0.927f);   // lâmpada verde do Outpost_Booth.png (fração do desenho, fin5a.py)

    public static readonly Color Gold = new Color32(0xFF, 0xD1, 0x4D, 0xFF);
    public static readonly Color Watch = new Color32(0x5C, 0xC4, 0x5E, 0xFF);     // verde da guarita (área atendida)
    public static readonly Color Amber = new Color32(0xFF, 0x9A, 0x2E, 0xFF);
    private static readonly Color Cream = new Color32(0xF2, 0xEB, 0xDD, 0xFF);
    private static readonly Color BarBack = new Color32(0x14, 0x12, 0x18, 0xFF);

    public RodBlockSlot Slot { get; private set; }
    public Rect Area { get; private set; }
    public LotState State { get; private set; }
    public bool IsFree => State == LotState.Free;
    public IReadOnlyList<House> Houses => houses;
    public Vector3 SignWorldPosition { get; private set; }   // centro do quarteirão (na obra a placa vai para cima da guarita)
    public Vector3 BoothFoot { get; private set; }           // pé da guarita, no canto do quarteirão
    public Vector3 BoothTop { get; private set; }            // topo do desenho (luz e ondas de rádio)
    public RectTransform Effects => root;                    // camada por cima das casas, para ondas e faíscas
    public Image Booth { get; private set; }
    public float Progress01 { get; private set; }
    public int Dispatches { get; private set; }
    public float Cooldown { get; set; }                      // segundos até a guarita olhar o quarteirão de novo

    private readonly List<House> houses = new();
    private OutpostManager manager;
    private RectTransform root, sign, bar, hintPlate, boothRoot;
    private CanvasGroup signGroup;
    private Image fill, border, barFill, lamp;
    private TMP_Text title, subtitle, hint;
    private Vector2 boothSize;
    private float flash, nextHint, waitingUntil;
    private bool hover;
    private Camera cam;

    #region SETUP

    public void Init(OutpostManager owner, RodBlockSlot slot, List<House> members, Transform world, Transform housesParent, List<Rect> obstacles)
    {
        manager = owner;
        Slot = slot;
        Area = slot.Area;
        houses.AddRange(members);

        // Área do quarteirão por cima das casas e abaixo dos painéis de informação (como a prévia do para-raio).
        root = (RectTransform)transform;
        root.SetParent(world, false);
        RodBlock.PlaceWorld(root, Area.center, Area.size);
        Canvas rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.overrideSorting = true;
        Canvas parentCanvas = world.GetComponentInParent<Canvas>();
        if (parentCanvas != null) rootCanvas.sortingLayerID = parentCanvas.rootCanvas.sortingLayerID;
        rootCanvas.sortingOrder = YSortCanvas.OverlayOrder - 1;
        gameObject.AddComponent<GraphicRaycaster>();

        fill = RodBlock.NewImage("Preenchimento", root, null);
        RodBlock.Stretch(fill.rectTransform);
        fill.color = Color.clear;
        fill.gameObject.AddComponent<OutpostPointer>().site = this;
        border = RodBlock.NewImage("Borda", root, RodBlock.BorderSprite(Area.size, owner.Blocks));
        RodBlock.Stretch(border.rectTransform);
        border.color = Color.clear;

        Sprite booth = RodBlock.Load(OutpostSprites, "Outpost_Booth");
        boothSize = booth != null ? new Vector2(booth.rect.width, booth.rect.height) / ArtPpu : new Vector2(1.5f, 2f);
        BoothFoot = OutpostMath.PickCorner(Area, obstacles, boothSize, owner.Data.cornerInset);
        BoothFoot = new Vector3(BoothFoot.x, BoothFoot.y, root.position.z);
        BoothTop = BoothFoot + new Vector3((LampAt.x - 0.5f) * boothSize.x, LampAt.y * boothSize.y, 0f);
        SignWorldPosition = new Vector3(Area.center.x, Area.center.y, root.position.z);
        MakeBooth(housesParent, booth);
        BuildSign();
    }

    // A guarita é uma estrutura do mundo como as casas: Canvas com ordem por Y (YSortCanvas) e a luz 2D delas.
    private void MakeBooth(Transform parent, Sprite sprite)
    {
        boothRoot = RodBlock.NewRect("[Guarita] " + Slot.id, parent);
        float scale = parent.lossyScale.x != 0f ? parent.lossyScale.x : 1f;
        boothRoot.anchorMin = boothRoot.anchorMax = new Vector2(0.5f, 0.5f);
        boothRoot.pivot = new Vector2(0.5f, 0f);
        boothRoot.position = BoothFoot;
        boothRoot.sizeDelta = boothSize / scale;

        Canvas canvas = boothRoot.gameObject.AddComponent<Canvas>();
        Canvas parentCanvas = parent.GetComponentInParent<Canvas>();
        if (parentCanvas != null) canvas.sortingLayerID = parentCanvas.rootCanvas.sortingLayerID;
        YSortCanvas sort = boothRoot.gameObject.AddComponent<YSortCanvas>();
        sort.kind = YSortCanvas.Kind_House;
        sort.Apply();

        Booth = RodBlock.NewImage("Visual", boothRoot, sprite);
        RodBlock.Stretch(Booth.rectTransform);
        Image houseVisual = houses.Count > 0 && houses[0].transform.Find("Visual") != null ? houses[0].transform.Find("Visual").GetComponent<Image>() : null;
        if (houseVisual != null && houseVisual.material != null && houseVisual.material != Graphic.defaultGraphicMaterial) Booth.material = houseVisual.material;

        lamp = RodBlock.NewImage("Luz", boothRoot, PixelFx.Dot);
        RectTransform lampRect = lamp.rectTransform;
        lampRect.anchorMin = lampRect.anchorMax = LampAt;
        lampRect.pivot = new Vector2(0.5f, 0.5f);
        lampRect.sizeDelta = Vector2.one * (3f / ArtPpu) / scale;
        lampRect.anchoredPosition = Vector2.zero;
        lamp.color = Color.clear;

        boothRoot.gameObject.SetActive(false);
    }

    private void BuildSign()
    {
        float multiplier = CanvasReferencePpu / (ArtPpu * PixelScale);
        sign = RodBlock.NewRect("Quarteirão " + Slot.id, manager.SignLayer);
        sign.anchorMin = sign.anchorMax = new Vector2(0.5f, 0.5f);
        sign.pivot = new Vector2(0.5f, 0f);
        signGroup = sign.gameObject.AddComponent<CanvasGroup>();
        signGroup.alpha = 0f;
        signGroup.blocksRaycasts = false;

        RectTransform plate = RodBlock.NewRect("Plate", sign);
        RodBlock.SetBox(plate, new Vector2(0.5f, 0f), new Vector2(0f, ArrowSize), new Vector2(212f, 84f));
        Image plateImage = plate.gameObject.AddComponent<Image>();
        plateImage.sprite = RodBlock.Load(LotSprites, "Lot_Plate");
        plateImage.type = Image.Type.Sliced;
        plateImage.pixelsPerUnitMultiplier = multiplier;
        plate.gameObject.AddComponent<OutpostPointer>().site = this;

        Image icon = RodBlock.NewImage("Icon", plate, RodBlock.Load(OutpostSprites, "Outpost_Icon"));
        RodBlock.SetBox(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(64f, 64f));
        title = NewText("Title", plate, 20f, Cream);
        RodBlock.SetBox(title.rectTransform, new Vector2(0f, 1f), new Vector2(78f, -10f), new Vector2(124f, 26f));
        subtitle = NewText("Houses", plate, 26f, Gold);
        RodBlock.SetBox(subtitle.rectTransform, new Vector2(0f, 0f), new Vector2(78f, 8f), new Vector2(124f, 34f));

        Image back = RodBlock.NewImage("Bar", plate, RodBlock.Load(LotSprites, "Lot_Dot"));
        back.color = BarBack;
        bar = back.rectTransform;
        RodBlock.SetBox(bar, new Vector2(0f, 0f), new Vector2(78f, 16f), new Vector2(118f, 16f));
        barFill = RodBlock.NewImage("Fill", bar, RodBlock.Load(LotSprites, "Lot_Dot"));
        barFill.color = Watch;
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        RodBlock.Stretch(barFill.rectTransform);
        barFill.rectTransform.offsetMin = new Vector2(3f, 3f);
        barFill.rectTransform.offsetMax = new Vector2(-3f, -3f);
        bar.gameObject.SetActive(false);

        hintPlate = RodBlock.NewRect("Hint", sign);
        RodBlock.SetBox(hintPlate, new Vector2(0.5f, 0f), new Vector2(0f, ArrowSize + 90f), new Vector2(270f, 66f));
        Image hintImage = hintPlate.gameObject.AddComponent<Image>();
        hintImage.sprite = RodBlock.Load(LotSprites, "Lot_Plate");
        hintImage.type = Image.Type.Sliced;
        hintImage.pixelsPerUnitMultiplier = multiplier;
        hintImage.raycastTarget = false;
        hint = NewText("Text", hintPlate, 20f, Cream);
        hint.alignment = TextAlignmentOptions.Center;
        RodBlock.Stretch(hint.rectTransform);
        hint.rectTransform.offsetMin = new Vector2(10f, 8f);
        hint.rectTransform.offsetMax = new Vector2(-10f, -8f);
        hintPlate.gameObject.SetActive(false);

        Image arrow = RodBlock.NewImage("Arrow", sign, RodBlock.Load(LotSprites, "Lot_Arrow"));
        arrow.color = Gold;
        arrow.rectTransform.sizeDelta = new Vector2(ArrowSize, ArrowSize);
        arrow.rectTransform.localPosition = new Vector3(0f, ArrowSize * 0.5f, 0f);
        arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
    }

    #endregion

    #region STATE (OutpostManager)

    public void BeginBuild()
    {
        State = LotState.Building;
        Progress01 = 0f;
        hover = false;
        if (sign != null) sign.DOPunchScale(Vector3.one * 0.2f, 0.35f, 8).SetLink(sign.gameObject);

        // A guarita sobe do chão junto com a barra da obra, apagada até ficar pronta.
        boothRoot.gameObject.SetActive(true);
        Booth.type = Image.Type.Filled;
        Booth.fillMethod = Image.FillMethod.Vertical;
        Booth.fillOrigin = (int)Image.OriginVertical.Bottom;
        Booth.fillAmount = 0f;
        Booth.color = new Color(1f, 1f, 1f, 0.45f);
    }

    public void SetProgress(float progress01)
    {
        Progress01 = Mathf.Clamp01(progress01);
        Booth.fillAmount = Progress01;
    }

    public void FinishBuild()
    {
        State = LotState.Built;
        Progress01 = 1f;
        fill.raycastTarget = false;
        boothRoot.gameObject.SetActive(true);
        Booth.fillAmount = 1f;
        Booth.color = Color.white;
        // O chão assado não tem a sombra dela: vai na camada logo acima do chão, com a regra da assada
        if (manager.Shadows != null) StructureShadow.Cast(boothRoot, manager.Shadows);
    }

    public void CountDispatch()
    {
        Dispatches++;
        waitingUntil = 0f;
    }

    // Chamado no quarteirão sem gente, material ou energia para mandar: a luz fica âmbar por um tempo.
    public void MarkWaiting() => waitingUntil = GameClock.Time + WaitingShow;

    public bool Waiting => GameClock.Time < waitingUntil;

    // Clarão da área atendida (a guarita despachou).
    public void Flash() => flash = 1f;

    public void OnPointer(bool enter) => hover = enter && IsFree;

    public void OnClick() => manager.OnSiteClicked(this);

    #endregion

    #region VIEW

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || sign == null || manager == null) return;

        GameManager game = GameManager.Instance;
        bool live = game == null || game.State == GameState.Playing || game.State == GameState.Paused;
        bool choosing = live && IsFree && manager.Placing;
        if (!choosing) hover = false;

        float pulse = 0.75f + 0.25f * Mathf.Sin(GameClock.Time * 5f);
        PaintArea(choosing, live, pulse);
        PaintBooth(pulse);
        PaintSign(choosing, live);
    }

    // Verde = área atendida: a prévia do quarteirão sob o mouse, as guaritas prontas na hora de escolher e o clarão.
    private void PaintArea(bool choosing, bool live, float pulse)
    {
        Color fillColor = Color.clear, borderColor = Color.clear;
        if (choosing)
        {
            fillColor = hover ? WithAlpha(Watch, manager.Data.fillAlpha * pulse) : Color.clear;
            borderColor = WithAlpha(Gold, hover ? pulse : 0.45f);
        }
        else if (live && State == LotState.Built && manager.Placing)
        {
            fillColor = WithAlpha(Watch, manager.Data.fillAlpha * 0.5f);
            borderColor = WithAlpha(Watch, 0.6f);
        }

        if (flash > 0f)
        {
            fillColor = Color.Lerp(fillColor, WithAlpha(Watch, manager.Data.fillAlpha), flash);
            borderColor = Color.Lerp(borderColor, WithAlpha(Watch, 0.8f), flash);
            flash = Mathf.MoveTowards(flash, 0f, GameClock.DeltaTime * 1.4f);
        }

        fill.color = fillColor;
        border.color = borderColor;
        fill.raycastTarget = choosing;
    }

    // Prévia: com o mouse no quarteirão livre, a guarita aparece meio apagada no canto. Pronta: a luz de prontidão.
    private void PaintBooth(float pulse)
    {
        if (IsFree)
        {
            boothRoot.gameObject.SetActive(hover);
            Booth.color = new Color(1f, 1f, 1f, GhostAlpha * pulse);
            lamp.color = Color.clear;
            return;
        }
        if (State != LotState.Built) return;

        bool blink = Mathf.Repeat(GameClock.Time, 1.2f) < 0.6f;
        lamp.color = Waiting ? WithAlpha(Amber, blink ? 1f : 0.35f) : WithAlpha(Watch, blink ? 1f : 0.55f);
    }

    private void PaintSign(bool choosing, bool live)
    {
        bool building = State == LotState.Building;
        Rect area = manager.SignLayer.rect;
        Vector2 anchor = ToLayer(building ? BoothTop + Vector3.up * SignLift : SignWorldPosition, area);
        bool wanted = live && (choosing || building) && area.Contains(anchor);

        float scale = hover ? 1.08f : choosing ? 1f + 0.05f * Mathf.Sin(GameClock.Time * 6f + Slot.id.GetHashCode() % 7) : 1f;
        sign.localPosition = anchor;
        sign.localScale = Vector3.one * scale;
        signGroup.alpha = Mathf.MoveTowards(signGroup.alpha, wanted ? 1f : 0f, GameClock.DeltaTime * 8f);
        signGroup.blocksRaycasts = wanted && choosing;
        if (!wanted && signGroup.alpha <= 0f)
        {
            hintPlate.gameObject.SetActive(false);
            return;
        }

        bar.gameObject.SetActive(building);
        subtitle.gameObject.SetActive(!building);
        if (building)
        {
            title.text = "EM OBRA";
            barFill.fillAmount = Progress01;
        }
        else
        {
            title.text = "GUARITA";
            subtitle.text = houses.Count == 1 ? "1 casa" : $"{houses.Count} casas";
        }

        hintPlate.gameObject.SetActive(hover);
        if (hover && GameClock.Time >= nextHint)
        {
            nextHint = GameClock.Time + HintInterval;
            hint.text = $"Despacha sozinha os\nchamados de {houses.Count} casas";
        }
        if (!hover) nextHint = 0f;
    }

    private Vector2 ToLayer(Vector3 world, Rect area)
    {
        Vector3 screen = cam.WorldToScreenPoint(world);
        float unitsPerPixel = area.height / Mathf.Max(1, cam.pixelHeight);
        return new Vector2(screen.x * unitsPerPixel + area.xMin, screen.y * unitsPerPixel + area.yMin);
    }

    #endregion

    #region HELPERS

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private TMP_Text NewText(string objectName, Transform parent, float size, Color color)
    {
        TextMeshProUGUI text = RodBlock.NewRect(objectName, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (manager.Font != null) text.font = manager.Font;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.alignment = TextAlignmentOptions.Left;
        return text;
    }

    #endregion
}
