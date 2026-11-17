using System.Collections.Generic;
using DefesaCivil.CityGen;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum LotState { Free, Building, Built }

// Um terreno de expansão (EXPANSAO.md): chão de terra batida no mundo, placa com preço no HUD e prévia do alcance.
// A placa fica em cima do terreno quando ele está na tela; enquanto o jogador escolhe onde construir (obra paga pela
// aba), a placa do tipo certo pulsa e, fora da tela, fica presa na borda com uma seta apontando para o terreno.
// A regra (compra, obra, prédio novo) fica na ExpansionManager; aqui é o que se vê e o clique.
public class BuildLot : MonoBehaviour
{
    private const string SpritePath = "Expansion/";
    private const float PixelScale = 3f;             // 1 pixel da arte = 3 px numa tela de 1080
    private const float CanvasReferencePpu = 100f;   // referencePixelsPerUnit dos CanvasScaler da cena
    private const float PatchWidth = 3.75f;          // Lot_Patch: 120 x 76 px a 32 px por unidade
    private const float PatchHeight = 2.375f;
    private const float PatchDrop = 0.15f;           // o terreno começa um pouco abaixo da base do prédio
    private const float HintInterval = 0.5f;
    private const float ArrowSize = 36f;
    // margens da tela para a placa presa na borda não cobrir o HUD (as mesmas do MapMarkers)
    private const float MarginLeft = 150f, MarginRight = 120f, MarginTop = 230f, MarginBottom = 185f;

    private static readonly Color BaseColor = new Color32(0x3F, 0x7F, 0xE0, 0xFF);
    private static readonly Color DepotColor = new Color32(0xF2, 0x8C, 0x28, 0xFF);
    private static readonly Color PriceOk = new Color32(0xFF, 0xD1, 0x4D, 0xFF);
    private static readonly Color PriceNo = new Color32(0xD9, 0x41, 0x2F, 0xFF);
    private static readonly Color PaidColor = new Color32(0x5C, 0xC4, 0x5E, 0xFF);
    private static readonly Color Cream = new Color32(0xF2, 0xEB, 0xDD, 0xFF);
    private static readonly Color BarBack = new Color32(0x14, 0x12, 0x18, 0xFF);

    public LotSlot Slot { get; private set; }
    public LotKind Kind => Slot.kind;
    public LotState State { get; private set; }
    public bool IsFree => State == LotState.Free;
    public Waypoint Entry { get; private set; }
    public Location Building { get; private set; }
    public Vector2 SitePosition { get; private set; }      // base visível do prédio que vai nascer
    public Vector3 SignWorldPosition { get; private set; } // onde a placa se apoia, em cima do terreno
    public float Progress01 { get; private set; }

    private ExpansionManager manager;
    private Image ground;
    private RectTransform preview, sign, plate, arrow, bar, hintPlate;
    private CanvasGroup signGroup, previewGroup;
    private Image icon, coin, barFill;
    private TMP_Text title, price, hint;
    private readonly List<Image> dots = new();
    private readonly List<float> distances = new();
    private readonly List<Vector2> path = new();
    private bool hover, wasPlacing;
    private float nextHint;
    private Camera cam;

    #region SETUP

    public void Init(ExpansionManager owner, LotSlot slot, Waypoint entry, float baseOffset, Transform housesParent, Transform trees)
    {
        manager = owner;
        Slot = slot;
        Entry = entry;

        float baseY = slot.entry.y + baseOffset;
        float patchBottom = baseY - PatchDrop;
        SitePosition = new Vector2(slot.entry.x, baseY);
        SignWorldPosition = new Vector3(slot.entry.x, patchBottom + PatchHeight + 0.2f, entry.transform.position.z);

        // Chão do terreno logo antes de Houses: em cima do chão assado, embaixo de casas e árvores (que têm Canvas
        // próprio com ordem por Y). O material das árvores dá a mesma luz 2D do resto da cidade.
        Transform world = housesParent.parent;
        var rect = (RectTransform)transform;
        rect.SetParent(world, false);
        rect.SetSiblingIndex(housesParent.GetSiblingIndex());
        PlaceWorld(rect, new Vector2(slot.entry.x, patchBottom + PatchHeight * 0.5f), new Vector2(PatchWidth, PatchHeight));
        ground = gameObject.AddComponent<Image>();
        ground.sprite = Load("Lot_Patch");
        if (manager.WorldMaterial != null) ground.material = manager.WorldMaterial;
        gameObject.AddComponent<LotPointer>().lot = this;

        ClearTrees(trees, baseOffset);
        BuildPreview(world);
        BuildSign();
    }

    // Árvores, postes e flores com o pé dentro do terreno (ou entre ele e a rua) somem: antes era praça ou mata.
    private void ClearTrees(Transform trees, float baseOffset)
    {
        if (trees == null) return;

        var corners = new Vector3[4];
        foreach (Transform tree in trees)
        {
            if (!tree.gameObject.activeSelf || tree is not RectTransform treeRect) continue;
            treeRect.GetWorldCorners(corners);
            // Pé no meio da borda de baixo do retângulo: no Start as árvores já perderam o YSortCanvas (TreeCanvasMerge)
            var foot = new Vector2((corners[0].x + corners[2].x) * 0.5f, corners[0].y);
            if (Clears(Slot.entry, baseOffset, foot)) tree.gameObject.SetActive(false);
        }
    }

    // Regra do que o terreno esconde, a mesma do chão assado (CityGround.HiddenByLots), que não pinta a sombra nem o
    // canteiro delas: 'baseOffset' é a base visível do prédio novo menos a entrada (y), 'foot' o pé da decoração.
    public static bool Clears(Vector2 slotEntry, float baseOffset, Vector2 foot)
    {
        float patchBottom = slotEntry.y + baseOffset - PatchDrop;
        return Mathf.Abs(foot.x - slotEntry.x) <= PatchWidth * 0.5f + 0.2f
            && foot.y >= slotEntry.y + 0.25f && foot.y <= patchBottom + PatchHeight + 0.5f;
    }

    // Pontos do anel (base) ou do caminho até a base (armazém): por cima da cidade e abaixo dos painéis de informação.
    private void BuildPreview(Transform world)
    {
        preview = NewRect("Alcance " + Slot.label + " " + ExpansionManager.KindName(Kind), world);
        preview.anchorMin = preview.anchorMax = new Vector2(0.5f, 0.5f);
        var canvas = preview.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = YSortCanvas.OverlayOrder - 1;
        previewGroup = preview.gameObject.AddComponent<CanvasGroup>();
        previewGroup.blocksRaycasts = false;
        previewGroup.interactable = false;
        previewGroup.alpha = 0f;
    }

    private void BuildSign()
    {
        float multiplier = CanvasReferencePpu / (32f * PixelScale);
        sign = NewRect("Terreno " + Slot.label + " " + ExpansionManager.KindName(Kind), manager.SignLayer);
        sign.anchorMin = sign.anchorMax = new Vector2(0.5f, 0.5f);
        sign.pivot = new Vector2(0.5f, 0f);
        signGroup = sign.gameObject.AddComponent<CanvasGroup>();
        signGroup.alpha = 0f;

        plate = NewRect("Plate", sign);
        SetBox(plate, new Vector2(0.5f, 0f), new Vector2(0f, ArrowSize), new Vector2(232f, 96f));
        Image plateImage = plate.gameObject.AddComponent<Image>();
        plateImage.sprite = Load("Lot_Plate");
        plateImage.type = Image.Type.Sliced;
        plateImage.pixelsPerUnitMultiplier = multiplier;
        plate.gameObject.AddComponent<LotPointer>().lot = this;

        icon = NewImage("Icon", plate, Load(Kind == LotKind.Base ? "Lot_IconBase" : "Lot_IconDepot"));
        SetBox(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(48f, 48f));
        title = NewText("Title", plate, 22f, Cream);
        SetBox(title.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -12f), new Vector2(152f, 28f));
        coin = NewImage("Coin", plate, Load("Lot_IconCoin"));
        SetBox(coin.rectTransform, new Vector2(0f, 0f), new Vector2(68f, 12f), new Vector2(32f, 32f));
        price = NewText("Price", plate, 32f, PriceOk);
        SetBox(price.rectTransform, new Vector2(0f, 0f), new Vector2(104f, 8f), new Vector2(118f, 40f));

        Image back = NewImage("Bar", plate, Load("Lot_Dot"));
        back.color = BarBack;
        bar = back.rectTransform;
        SetBox(bar, new Vector2(0f, 0f), new Vector2(70f, 18f), new Vector2(148f, 16f));
        barFill = NewImage("Fill", bar, Load("Lot_Dot"));
        barFill.color = PaidColor;
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.rectTransform.anchorMin = Vector2.zero;
        barFill.rectTransform.anchorMax = Vector2.one;
        barFill.rectTransform.offsetMin = new Vector2(3f, 3f);
        barFill.rectTransform.offsetMax = new Vector2(-3f, -3f);
        bar.gameObject.SetActive(false);

        hintPlate = NewRect("Hint", sign);
        SetBox(hintPlate, new Vector2(0.5f, 0f), new Vector2(0f, ArrowSize + 102f), new Vector2(262f, 66f));
        Image hintImage = hintPlate.gameObject.AddComponent<Image>();
        hintImage.sprite = Load("Lot_Plate");
        hintImage.type = Image.Type.Sliced;
        hintImage.pixelsPerUnitMultiplier = multiplier;
        hintImage.raycastTarget = false;
        hint = NewText("Text", hintPlate, 20f, Cream);
        hint.alignment = TextAlignmentOptions.Center;
        hint.rectTransform.anchorMin = Vector2.zero;
        hint.rectTransform.anchorMax = Vector2.one;
        hint.rectTransform.offsetMin = new Vector2(10f, 8f);
        hint.rectTransform.offsetMax = new Vector2(-10f, -8f);
        hintPlate.gameObject.SetActive(false);

        Image arrowImage = NewImage("Arrow", sign, Load("Lot_Arrow"));
        arrowImage.color = Kind == LotKind.Base ? BaseColor : DepotColor;
        arrow = arrowImage.rectTransform;
        arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);
    }

    #endregion

    #region STATE (ExpansionManager)

    public void BeginBuild()
    {
        State = LotState.Building;
        Progress01 = 0f;
        hover = false;
        ShowPreview(false);
        if (icon != null) icon.sprite = Load("Lot_IconBuild");
        if (sign != null) sign.DOPunchScale(Vector3.one * 0.2f, 0.35f, 8).SetLink(sign.gameObject);
    }

    public void SetProgress(float progress01) => Progress01 = Mathf.Clamp01(progress01);

    public void FinishBuild(Location building)
    {
        State = LotState.Built;
        Building = building;
        Progress01 = 1f;
        ground.raycastTarget = false;
        ShowPreview(false);
        // O chão assado não tem a sombra do prédio novo: vai por cima do terreno, com a regra da assada
        if (building != null) StructureShadow.Cast(building, transform);
    }

    public void OnPointer(bool enter) => hover = enter && IsFree;

    public void OnClick() => manager.OnLotClicked(this);

    #endregion

    #region VIEW

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null || sign == null || manager == null) return;

        GameManager game = GameManager.Instance;
        bool live = game == null || game.State == GameState.Playing || game.State == GameState.Paused;
        bool placingMine = IsFree && manager.Placing == Kind;
        bool otherPlacing = manager.Placing.HasValue && manager.Placing != Kind;
        UpgradeData upgrade = manager.GetUpgrade(Kind);
        UpgradesManager upgrades = UpgradesManager.Instance;
        bool maxed = upgrade == null || upgrades == null || (upgrades.IsMaxLevel(upgrade) && !manager.HasCredit(Kind));
        bool wanted = live && State != LotState.Built && !(IsFree && (maxed || otherPlacing));

        Rect area = manager.SignLayer.rect;
        var safe = Rect.MinMaxRect(area.xMin + MarginLeft, area.yMin + MarginBottom, area.xMax - MarginRight, area.yMax - MarginTop);
        Vector2 anchor = ToLayer(SignWorldPosition, area);
        bool onScreen = area.Contains(anchor);
        float angle = 0f;
        bool clamped = false;
        Vector2 position = anchor;
        if (placingMine && !safe.Contains(anchor)) position = ExpansionMath.ClampToEdge(anchor, safe, out angle, out clamped);
        else if (!onScreen) wanted = false;

        float now = GameClock.Time;
        float scale = placingMine ? 1f + 0.08f * Mathf.Sin(now * 6f) : hover ? 1.06f : 1f;
        sign.localPosition = position;
        sign.localScale = Vector3.one * scale;
        signGroup.alpha = Mathf.MoveTowards(signGroup.alpha, wanted ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        signGroup.blocksRaycasts = wanted;
        PlaceArrow(clamped, angle);

        if (placingMine && !wasPlacing) sign.DOPunchScale(Vector3.one * 0.25f, 0.4f, 6).SetLink(sign.gameObject);
        wasPlacing = placingMine;
        if (!wanted && signGroup.alpha <= 0f)
        {
            ShowPreview(false);
            return;
        }

        PaintSign(upgrade, upgrades);
        bool showHint = IsFree && (hover || placingMine);
        hintPlate.gameObject.SetActive(showHint);
        ShowPreview(showHint);
        if (showHint && now >= nextHint)
        {
            nextHint = now + HintInterval;
            RefreshHint();
        }
    }

    private void PaintSign(UpgradeData upgrade, UpgradesManager upgrades)
    {
        bool building = State == LotState.Building;
        bar.gameObject.SetActive(building);
        coin.gameObject.SetActive(!building && !manager.HasCredit(Kind));
        price.gameObject.SetActive(!building);
        if (building)
        {
            title.text = "EM OBRA";
            barFill.fillAmount = Progress01;
            return;
        }

        title.text = Kind == LotKind.Base ? "NOVA BASE" : "NOVO ARMAZÉM";
        if (manager.HasCredit(Kind))
        {
            price.text = "PAGO";
            price.color = PaidColor;
        }
        else if (upgrade != null && upgrades != null)
        {
            price.text = upgrades.GetCost(upgrade).ToString();
            price.color = upgrades.CanBuy(upgrade) ? PriceOk : PriceNo;
        }
    }

    // Na tela a seta fica embaixo da placa apontando o terreno; presa na borda, ela vai para o lado de fora.
    private void PlaceArrow(bool clamped, float angle)
    {
        if (!clamped)
        {
            arrow.localPosition = new Vector3(0f, ArrowSize * 0.5f, 0f);
            arrow.localRotation = Quaternion.Euler(0f, 0f, 180f);
            return;
        }
        Vector2 center = new Vector2(0f, ArrowSize + plate.sizeDelta.y * 0.5f);
        Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
        float reach = Mathf.Abs(direction.x) * (plate.sizeDelta.x * 0.5f) + Mathf.Abs(direction.y) * (plate.sizeDelta.y * 0.5f) + ArrowSize * 0.5f;
        arrow.localPosition = center + direction * reach;
        arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void RefreshHint()
    {
        ExpansionData data = manager.Data;
        if (Kind == LotKind.Base)
        {
            int served = manager.ServedHouses(this, distances, out int troubled);
            hint.text = served > 0 ? $"Atende {served} casas\n{troubled} com problema agora" : "Nenhuma casa fica\nmais perto daqui";
            float radius = ExpansionMath.Percentile(distances, data.reachPercentile);
            DrawRing(SitePosition, radius, data);
        }
        else
        {
            float seconds = manager.WalkToNearestBase(this, path);
            hint.text = path.Count > 0 ? $"Base mais perto\na {Mathf.RoundToInt(seconds)} s a pé" : "Sem caminho\naté uma base";
            DrawPath(path, data);
        }
    }

    private void DrawRing(Vector2 center, float radius, ExpansionData data)
    {
        int count = ExpansionMath.RingDots(radius, data.dotSpacing);
        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.PI * 2f / count;
            PlaceDot(i, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, data.dotSize, BaseColor);
        }
        HideDots(count);
    }

    private void DrawPath(List<Vector2> points, ExpansionData data)
    {
        int used = 0;
        float carry = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            Vector2 a = points[i - 1], b = points[i];
            float length = Vector2.Distance(a, b);
            for (float d = carry; d < length; d += data.dotSpacing) PlaceDot(used++, Vector2.Lerp(a, b, d / length), data.dotSize, DepotColor);
            carry = Mathf.Repeat(carry - length, data.dotSpacing);
        }
        HideDots(used);
    }

    private void PlaceDot(int index, Vector2 world, float size, Color color)
    {
        while (dots.Count <= index)
        {
            Image dot = NewImage("Dot", preview, Load("Lot_Dot"));
            dot.raycastTarget = false;
            dots.Add(dot);
        }
        Image image = dots[index];
        image.gameObject.SetActive(true);
        image.color = color;
        PlaceWorld(image.rectTransform, world, new Vector2(size, size));
    }

    private void HideDots(int from)
    {
        for (int i = from; i < dots.Count; i++) dots[i].gameObject.SetActive(false);
    }

    private void ShowPreview(bool visible)
    {
        if (previewGroup == null) return;
        float pulse = 0.75f + 0.25f * Mathf.Sin(GameClock.Time * 4f);
        previewGroup.alpha = visible ? pulse : 0f;
        if (!visible) nextHint = 0f;
    }

    private Vector2 ToLayer(Vector3 world, Rect area)
    {
        Vector3 screen = cam.WorldToScreenPoint(world);
        float unitsPerPixel = area.height / Mathf.Max(1, cam.pixelHeight);
        return new Vector2(screen.x * unitsPerPixel + area.xMin, screen.y * unitsPerPixel + area.yMin);
    }

    #endregion

    #region HELPERS

    private static Sprite Load(string spriteName) => Resources.Load<Sprite>(SpritePath + spriteName);

    // Retângulo em unidades do mundo dentro de um Canvas World Space (o tamanho passa pela escala do pai).
    private static void PlaceWorld(RectTransform rect, Vector2 center, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        Transform parent = rect.parent;
        float z = parent != null ? parent.position.z : 0f;
        float scale = parent != null && parent.lossyScale.x != 0f ? parent.lossyScale.x : 1f;
        rect.position = new Vector3(center.x, center.y, z);
        rect.sizeDelta = size / scale;
    }

    private static RectTransform NewRect(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image NewImage(string objectName, Transform parent, Sprite sprite)
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
    private static void SetBox(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    #endregion
}

// Clique e hover do terreno (no chão do mundo e na placa do HUD) vão para o BuildLot.
public class LotPointer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public BuildLot lot;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && lot != null) lot.OnClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (lot != null) lot.OnPointer(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (lot != null) lot.OnPointer(false);
    }
}
