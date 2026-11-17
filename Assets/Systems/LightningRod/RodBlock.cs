using System.Collections.Generic;
using DefesaCivil.CityGen;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Um quarteirão do para-raio (PARARAIO.md): a área protegida no mundo (borda tracejada e preenchimento), o marcador no
// HUD (mesma placa, seta e barra de obra dos terrenos da expansão) e o para-raio no telhado mais alto.
// No modo de escolha (obra paga pela aba), cada quarteirão livre na tela mostra o marcador; o mouse em cima do
// quarteirão ou do marcador acende a área e a dica, e o clique começa a obra. A regra fica na LightningRodManager.
public class RodBlock : MonoBehaviour
{
    private const string LotSprites = "Expansion/";
    private const string RodSprites = "LightningRod/";
    private const float ArtPpu = 32f;                // pixels da arte por unidade do mundo (casas e kit)
    private const float PixelScale = 3f;             // 1 pixel da arte = 3 px numa tela de 1080
    private const float CanvasReferencePpu = 100f;   // referencePixelsPerUnit dos CanvasScaler da cena
    private const float RoofSink = 0.3f;             // o pé fica no telhado, 30% da altura da casa abaixo do topo...
    private const float MaxRoofSink = 0.7f;          // ...até 0,7 unidade (casa alta), para não descer na fachada
    private const float HintInterval = 0.5f;
    private const float ArrowSize = 32f;
    private const float SignLift = 0.2f;             // na obra, folga entre a ponta do para-raio e a seta da placa
    private const float GhostAlpha = 0.6f;           // para-raio fantasma no telhado durante a prévia

    private static readonly Color Gold = new Color32(0xFF, 0xD1, 0x4D, 0xFF);
    private static readonly Color Shield = new Color32(0x3F, 0x7F, 0xE0, 0xFF);
    private static readonly Color Paid = new Color32(0x5C, 0xC4, 0x5E, 0xFF);
    private static readonly Color Cream = new Color32(0xF2, 0xEB, 0xDD, 0xFF);
    private static readonly Color BarBack = new Color32(0x14, 0x12, 0x18, 0xFF);
    private static readonly Dictionary<Vector2Int, Sprite> borderCache = new();

    public RodBlockSlot Slot { get; private set; }
    public Rect Area { get; private set; }
    public LotState State { get; private set; }
    public bool IsFree => State == LotState.Free;
    public IReadOnlyList<House> Houses => houses;
    public House Anchor { get; private set; }                // casa com o para-raio no telhado
    public Vector3 SignWorldPosition { get; private set; }   // centro do quarteirão (na obra a placa vai para cima da ponta)
    public Vector3 RodTip { get; private set; }              // ponta do para-raio (onde o raio cai)
    public Vector3 RodFoot { get; private set; }             // pé do para-raio, no telhado
    public RectTransform Effects => root;                    // camada por cima das casas, para faísca e arco
    public Image Rod { get; private set; }
    public float Progress01 { get; private set; }
    public int Strikes { get; private set; }

    private readonly List<House> houses = new();
    private LightningRodManager manager;
    private RectTransform root, sign, plate, bar, hintPlate;
    private Canvas rootCanvas;
    private CanvasGroup signGroup;
    private Image fill, border, icon, barFill;
    private TMP_Text title, subtitle, hint;
    private float flash, nextHint;
    private bool hover;
    private Camera cam;

    #region SETUP

    public void Init(LightningRodManager owner, RodBlockSlot slot, List<House> members, Transform world)
    {
        manager = owner;
        Slot = slot;
        Area = slot.Area;
        houses.AddRange(members);

        // Área do quarteirão por cima das casas e abaixo dos painéis de informação (como a prévia da expansão).
        root = (RectTransform)transform;
        root.SetParent(world, false);
        PlaceWorld(root, Area.center, Area.size);
        rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.overrideSorting = true;
        Canvas parentCanvas = world.GetComponentInParent<Canvas>();
        if (parentCanvas != null) rootCanvas.sortingLayerID = parentCanvas.rootCanvas.sortingLayerID;
        rootCanvas.sortingOrder = YSortCanvas.OverlayOrder - 1;
        gameObject.AddComponent<GraphicRaycaster>();

        fill = NewImage("Preenchimento", root, null);
        Stretch(fill.rectTransform);
        fill.color = Color.clear;
        fill.gameObject.AddComponent<RodPointer>().block = this;
        border = NewImage("Borda", root, BorderSprite(Area.size, owner.Data));
        Stretch(border.rectTransform);
        border.color = Color.clear;

        PickAnchor();
        SignWorldPosition = new Vector3(Area.center.x, Area.center.y, root.position.z);
        BuildSign();
    }

    // O telhado mais alto do quarteirão (empate: o mais perto do meio) recebe o para-raio.
    private void PickAnchor()
    {
        float best = float.MinValue;
        foreach (House house in houses)
        {
            if (!TryRoof(house, out Vector3 top, out float drawn)) continue;
            float score = top.y - Mathf.Abs(top.x - Area.center.x) * 0.01f;
            if (score <= best) continue;
            best = score;
            Anchor = house;
            RodFoot = top + Vector3.down * Mathf.Min(MaxRoofSink, drawn * RoofSink);
        }
        if (Anchor == null && houses.Count > 0)
        {
            Anchor = houses[0];
            RodFoot = Anchor.transform.position;
        }

        Sprite pole = Load(RodSprites, "Rod_Pole");
        float height = pole != null ? pole.rect.height / ArtPpu : 1.25f;
        RodTip = RodFoot + Vector3.up * height;
    }

    // Topo do desenho da casa (sem a margem transparente), no meio do retângulo do "Visual", e a altura desenhada.
    private static bool TryRoof(House house, out Vector3 top, out float height)
    {
        top = default;
        height = 0f;
        Transform visual = house != null ? house.transform.Find("Visual") : null;
        Image image = visual != null ? visual.GetComponent<Image>() : null;
        if (image == null || image.sprite == null) return false;

        Rect r = image.rectTransform.rect;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (Vector2 v in image.sprite.vertices)
        {
            minY = Mathf.Min(minY, v.y);
            maxY = Mathf.Max(maxY, v.y);
        }
        float yTop = r.yMax, yBottom = r.yMin;
        if (maxY > float.MinValue)
        {
            float rows = image.sprite.rect.height, ppu = image.sprite.pixelsPerUnit, pivot = image.sprite.pivot.y;
            yTop = r.yMin + Mathf.Clamp01((maxY * ppu + pivot) / rows) * r.height;
            yBottom = r.yMin + Mathf.Clamp01((minY * ppu + pivot) / rows) * r.height;
        }
        top = image.rectTransform.TransformPoint(new Vector3(r.center.x, yTop, 0f));
        height = top.y - image.rectTransform.TransformPoint(new Vector3(r.center.x, yBottom, 0f)).y;
        return true;
    }

    private void BuildSign()
    {
        float multiplier = CanvasReferencePpu / (ArtPpu * PixelScale);
        sign = NewRect("Quarteirão " + Slot.id, manager.SignLayer);
        sign.anchorMin = sign.anchorMax = new Vector2(0.5f, 0.5f);
        sign.pivot = new Vector2(0.5f, 0f);
        signGroup = sign.gameObject.AddComponent<CanvasGroup>();
        signGroup.alpha = 0f;
        signGroup.blocksRaycasts = false;

        plate = NewRect("Plate", sign);
        SetBox(plate, new Vector2(0.5f, 0f), new Vector2(0f, ArrowSize), new Vector2(212f, 84f));
        Image plateImage = plate.gameObject.AddComponent<Image>();
        plateImage.sprite = Load(LotSprites, "Lot_Plate");
        plateImage.type = Image.Type.Sliced;
        plateImage.pixelsPerUnitMultiplier = multiplier;
        plate.gameObject.AddComponent<RodPointer>().block = this;

        icon = NewImage("Icon", plate, Load(RodSprites, "Rod_Icon"));
        SetBox(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(64f, 64f));
        title = NewText("Title", plate, 20f, Cream);
        SetBox(title.rectTransform, new Vector2(0f, 1f), new Vector2(78f, -10f), new Vector2(124f, 26f));
        subtitle = NewText("Houses", plate, 26f, Gold);
        SetBox(subtitle.rectTransform, new Vector2(0f, 0f), new Vector2(78f, 8f), new Vector2(124f, 34f));

        Image back = NewImage("Bar", plate, Load(LotSprites, "Lot_Dot"));
        back.color = BarBack;
        bar = back.rectTransform;
        SetBox(bar, new Vector2(0f, 0f), new Vector2(78f, 16f), new Vector2(118f, 16f));
        barFill = NewImage("Fill", bar, Load(LotSprites, "Lot_Dot"));
        barFill.color = Paid;
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        Stretch(barFill.rectTransform);
        barFill.rectTransform.offsetMin = new Vector2(3f, 3f);
        barFill.rectTransform.offsetMax = new Vector2(-3f, -3f);
        bar.gameObject.SetActive(false);

        hintPlate = NewRect("Hint", sign);
        SetBox(hintPlate, new Vector2(0.5f, 0f), new Vector2(0f, ArrowSize + 90f), new Vector2(250f, 66f));
        Image hintImage = hintPlate.gameObject.AddComponent<Image>();
        hintImage.sprite = Load(LotSprites, "Lot_Plate");
        hintImage.type = Image.Type.Sliced;
        hintImage.pixelsPerUnitMultiplier = multiplier;
        hintImage.raycastTarget = false;
        hint = NewText("Text", hintPlate, 20f, Cream);
        hint.alignment = TextAlignmentOptions.Center;
        Stretch(hint.rectTransform);
        hint.rectTransform.offsetMin = new Vector2(10f, 8f);
        hint.rectTransform.offsetMax = new Vector2(-10f, -8f);
        hintPlate.gameObject.SetActive(false);

        Image arrow = NewImage("Arrow", sign, Load(LotSprites, "Lot_Arrow"));
        arrow.color = Gold;
        arrow.rectTransform.sizeDelta = new Vector2(ArrowSize, ArrowSize);
        arrow.rectTransform.localPosition = new Vector3(0f, ArrowSize * 0.5f, 0f);
        arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
    }

    #endregion

    #region STATE (LightningRodManager)

    public void BeginBuild()
    {
        State = LotState.Building;
        Progress01 = 0f;
        hover = false;
        if (sign != null) sign.DOPunchScale(Vector3.one * 0.2f, 0.35f, 8).SetLink(sign.gameObject);

        // O para-raio sobe no telhado junto com a barra da obra, apagado até ficar pronto.
        if (Rod == null) Rod = MakeRod();
        if (Rod != null)
        {
            Rod.gameObject.SetActive(true);
            Rod.type = Image.Type.Filled;
            Rod.fillMethod = Image.FillMethod.Vertical;
            Rod.fillOrigin = (int)Image.OriginVertical.Bottom;
            Rod.fillAmount = 0f;
            Rod.color = new Color(1f, 1f, 1f, 0.45f);
        }
    }

    public void SetProgress(float progress01)
    {
        Progress01 = Mathf.Clamp01(progress01);
        if (Rod != null) Rod.fillAmount = Progress01;
    }

    public void FinishBuild()
    {
        State = LotState.Built;
        Progress01 = 1f;
        fill.raycastTarget = false;
        if (Rod == null) Rod = MakeRod();
        if (Rod != null)
        {
            Rod.gameObject.SetActive(true);
            Rod.fillAmount = 1f;
            Rod.color = Color.white;
        }
    }

    public void CountStrike() => Strikes++;

    // Clarão da área protegida (o raio absorvido acende o quarteirão por um instante).
    public void Flash() => flash = 1f;

    public void OnPointer(bool enter) => hover = enter && IsFree;

    public void OnClick() => manager.OnBlockClicked(this);

    private Image MakeRod()
    {
        Sprite pole = Load(RodSprites, "Rod_Pole");
        if (Anchor == null || pole == null) return null;

        // Filho da casa: fica na ordem por Y dela, por cima do telhado, e treme e apaga junto com ela.
        Image image = NewImage("Para-raio", Anchor.transform, pole);
        Image visual = Anchor.transform.Find("Visual") != null ? Anchor.transform.Find("Visual").GetComponent<Image>() : null;
        if (visual != null && visual.material != null && visual.material != Graphic.defaultGraphicMaterial) image.material = visual.material;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0f);
        float scale = Anchor.transform.lossyScale.x != 0f ? Anchor.transform.lossyScale.x : 1f;
        rect.position = RodFoot;
        rect.sizeDelta = new Vector2(pole.rect.width, pole.rect.height) / ArtPpu / scale;
        rect.SetAsLastSibling();
        return image;
    }

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
        PaintGhost(pulse);
        PaintSign(choosing, live);
    }

    // Azul = área protegida: a prévia do quarteirão sob o mouse, os já protegidos na hora de escolher e o clarão do raio.
    private void PaintArea(bool choosing, bool live, float pulse)
    {
        Color fillColor = Color.clear, borderColor = Color.clear;
        if (choosing)
        {
            fillColor = hover ? WithAlpha(Shield, manager.Data.fillAlpha * pulse) : Color.clear;
            borderColor = WithAlpha(Gold, hover ? pulse : 0.45f);
        }
        else if (live && State == LotState.Built && manager.Placing)
        {
            fillColor = WithAlpha(Shield, manager.Data.fillAlpha * 0.5f);
            borderColor = WithAlpha(Shield, 0.6f);
        }

        if (flash > 0f)
        {
            fillColor = Color.Lerp(fillColor, WithAlpha(Shield, 0.4f), flash);
            borderColor = Color.Lerp(borderColor, Shield, flash);
            flash = Mathf.MoveTowards(flash, 0f, GameClock.DeltaTime * 1.6f);
        }

        fill.color = fillColor;
        border.color = borderColor;
        fill.raycastTarget = choosing;
    }

    // Prévia: com o mouse no quarteirão livre, o para-raio aparece meio apagado no telhado onde vai ficar.
    private void PaintGhost(float pulse)
    {
        if (!IsFree) return;
        if (hover && Rod == null) Rod = MakeRod();
        if (Rod == null) return;

        Rod.gameObject.SetActive(hover);
        Rod.color = new Color(1f, 1f, 1f, GhostAlpha * pulse);
    }

    private void PaintSign(bool choosing, bool live)
    {
        bool building = State == LotState.Building;
        Rect area = manager.SignLayer.rect;
        Vector2 anchor = ToLayer(building ? RodTip + Vector3.up * SignLift : SignWorldPosition, area);
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
            title.text = "PARA-RAIO";
            subtitle.text = houses.Count == 1 ? "1 casa" : $"{houses.Count} casas";
        }

        hintPlate.gameObject.SetActive(hover);
        if (hover && Time.unscaledTime >= nextHint)
        {
            nextHint = Time.unscaledTime + HintInterval;
            int share = Mathf.RoundToInt(manager.ShareIfBuilt(this) * 100f);
            hint.text = $"Protege {houses.Count} casas\natrai {share}% dos raios";
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

    internal static Sprite Load(string folder, string spriteName) => Resources.Load<Sprite>(folder + spriteName);

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    // Borda tracejada do tamanho do quarteirão, pixel art (32 por unidade): traço claro com 1 pixel de contorno escuro.
    // Uma textura por tamanho (o mapa tem dois), tingida pela cor da Image.
    internal static Sprite BorderSprite(Vector2 size, LightningRodData data)
    {
        var key = new Vector2Int(Mathf.RoundToInt(size.x * ArtPpu), Mathf.RoundToInt(size.y * ArtPpu));
        if (borderCache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        int dash = Mathf.Max(2, Mathf.RoundToInt(data.dashSize * ArtPpu));
        var texture = new Texture2D(key.x, key.y, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[key.x * key.y];
        var points = new List<Vector2>();
        float inset = dash * 0.5f + 1f;
        LightningRodMath.OutlineDashes(Rect.MinMaxRect(inset, inset, key.x - inset, key.y - inset), data.dashSpacing * ArtPpu, points);
        foreach (Vector2 p in points) Square(pixels, key, p, dash + 2, new Color32(0x14, 0x12, 0x18, 0xFF));
        foreach (Vector2 p in points) Square(pixels, key, p, dash, new Color32(0xFF, 0xFF, 0xFF, 0xFF));
        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, key.x, key.y), new Vector2(0.5f, 0.5f), ArtPpu);
        borderCache[key] = sprite;
        return sprite;
    }

    private static void Square(Color32[] pixels, Vector2Int size, Vector2 center, int side, Color32 color)
    {
        int x0 = Mathf.RoundToInt(center.x - side * 0.5f), y0 = Mathf.RoundToInt(center.y - side * 0.5f);
        for (int y = Mathf.Max(0, y0); y < Mathf.Min(size.y, y0 + side); y++)
        for (int x = Mathf.Max(0, x0); x < Mathf.Min(size.x, x0 + side); x++)
            pixels[y * size.x + x] = color;
    }

    // Retângulo em unidades do mundo dentro de um Canvas World Space (o tamanho passa pela escala do pai).
    internal static void PlaceWorld(RectTransform rect, Vector2 center, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        Transform parent = rect.parent;
        float z = parent != null ? parent.position.z : 0f;
        float scale = parent != null && parent.lossyScale.x != 0f ? parent.lossyScale.x : 1f;
        rect.position = new Vector3(center.x, center.y, z);
        rect.sizeDelta = size / scale;
    }

    internal static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    internal static RectTransform NewRect(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    internal static Image NewImage(string objectName, Transform parent, Sprite sprite)
    {
        Image image = NewRect(objectName, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text NewText(string objectName, Transform parent, float size, Color color)
    {
        TextMeshProUGUI text = NewRect(objectName, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (manager.Font != null) text.font = manager.Font;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.alignment = TextAlignmentOptions.Left;
        return text;
    }

    // anchor = pivot = canto de referência dentro do pai
    internal static void SetBox(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    #endregion
}
