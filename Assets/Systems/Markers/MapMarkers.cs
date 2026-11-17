using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Marcadores do mapa em tamanho de tela fixo (o mapa é grande e o zoom padrão é aberto):
//- casa com problema: pino grande com contorno grosso, cor e ícone do tipo (fogo, dano, tempestade), pulsando e
//  soltando ondas; pronto para coletar: pino verde que quica; equipe a caminho: pino menor e calmo;
//- casa fora da câmera: o mesmo pino preso na borda com uma seta apontando para ela; clicar leva a câmera até lá;
//- Base da Guarda e Armazéns: placa com ícone e nome sempre em cima do prédio, e pino calmo na borda fora da tela.
//Só apresentação: lê o estado das casas e escuta GameEvents; não muda regra nenhuma. Um na cena ("[Markers]").
public class MapMarkers : MonoBehaviour
{
    public static MapMarkers Instance;

    private enum Kind { None, Fire, Damage, Storm, Power, Done, Guard, Depot }

    [Header("SPRITES (Tools/City/Ligar Marcadores preenche)")]
    [SerializeField] private Sprite discSpr;
    [SerializeField] private Sprite fillSpr;
    [SerializeField] private Sprite ringSpr;
    [SerializeField] private Sprite arrowSpr;
    [SerializeField] private Sprite badgeSpr;
    [SerializeField] private Sprite plateSpr;
    [SerializeField] private Sprite fireIcon;
    [SerializeField] private Sprite damageIcon;
    [SerializeField] private Sprite stormIcon;
    [SerializeField] private Sprite doneIcon;
    [SerializeField] private Sprite guardIcon;
    [SerializeField] private Sprite depotIcon;
    [SerializeField] private TMP_FontAsset font;

    [Header("COLORS")]
    [SerializeField] private Color fireColor = new Color32(0xD9, 0x41, 0x2F, 0xFF);
    [SerializeField] private Color damageColor = new Color32(0xF2, 0x8C, 0x28, 0xFF);
    [SerializeField] private Color stormColor = new Color32(0x3F, 0x7F, 0xE0, 0xFF);
    [SerializeField] private Color doneColor = new Color32(0x3F, 0xB0, 0x4A, 0xFF);
    [SerializeField] private Color guardColor = new Color32(0x2A, 0x5F, 0xD0, 0xFF);
    [SerializeField] private Color depotColor = new Color32(0xC9, 0x8A, 0x2B, 0xFF);
    [SerializeField] private Color powerColor = new Color32(0xE8, 0xC5, 0x2A, 0xFF);

    [Header("SIZE (px numa tela de 1080 de altura)")]
    [Tooltip("Lado de um pixel da arte do pino; 3 = pino de 78 px")]
    [SerializeField] private float pixelScale = 3f;
    //Base e Armazém com o mesmo disco dos chamados: o ícone v2 de 64 px cabe nele (era 2, disco de 52 px)
    [SerializeField] private float landmarkDiscScale = 3f;
    [Tooltip("Tamanho do pino com a câmera toda aberta, em relação ao normal (era .78: pino maior, item 4)")]
    [SerializeField, Range(.4f, 1f)] private float farPinScale = .92f;
    [SerializeField, Range(.4f, 1f)] private float enRouteScale = .7f;
    [SerializeField, Range(0f, 1f)] private float enRouteAlpha = .8f;
    [SerializeField] private float gapAboveHouse = 4f;
    [SerializeField] private float plateFontSize = 30f;
    [SerializeField] private string guardName = "BASE DA GUARDA";
    [SerializeField] private string depotName = "ARMAZÉM";

    [Header("SCREEN EDGE (margens para não cobrir o HUD)")]
    [SerializeField] private float marginLeft = 150f;
    [SerializeField] private float marginRight = 120f;
    [SerializeField] private float marginTop = 230f;
    [SerializeField] private float marginBottom = 185f;
    [SerializeField] private int maxEdgePins = 14;
    [SerializeField] private float panTime = .45f;

    [Header("ANIMATION")]
    [SerializeField] private float pulseAmount = .08f;
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private float wobbleDegrees = 9f;
    [SerializeField] private float waveInterval = 1.1f;
    [SerializeField] private float fireWaveInterval = .65f;
    [SerializeField] private float waveScale = 2f;
    [SerializeField] private float bouncePixels = 10f;
    [SerializeField] private float popTime = .22f;
    [Tooltip("Segundos de cada passo do quique da seta da Base e do Armazém (4 passos por ciclo)")]
    [SerializeField] private float pointerStepTime = .16f;

    [Header("NATIVE MARKERS")]
    [Tooltip("Esconde o balão pequeno da própria casa enquanto o pino está em cima dela")]
    [SerializeField] private bool hideNativeMarkers = true;

    private class Pin
    {
        public House house;
        public bool landmark;
        public Vector3 top, center;         // mundo: topo visível e meio do prédio
        public CanvasGroup nativeCall, nativeDone;
        public RectTransform root, body, arrow, ring, icon, plate, pointer;
        public CanvasGroup group;
        public Image fill, ringImg, arrowImg, iconImg, hit;
        public GameObject badge;
        public TMP_Text amount;
        public float plateWidth;
        public Kind kind, shownKind;
        public float shown;                 // 0 escondido, 1 inteiro (entrada com estalo)
        public float phase;
        public Vector2 pos, target;
        public bool edge, wanted, attention;
        public bool nativeVisible = true;
        public int lastAmount = -1;
    }

    private readonly List<Pin> pins = new List<Pin>();
    private readonly List<Pin> edgePins = new List<Pin>();
    private readonly Dictionary<House, DisasterType> causes = new Dictionary<House, DisasterType>();
    private static readonly Vector3[] corners = new Vector3[4];
    private Canvas canvas;
    private RectTransform backLayer, edgeLayer;
    private Camera cam;
    private bool disasterActive;
    private DisasterType disasterType;
    private Coroutine pan;
    private System.Comparison<Pin> byPriority;

    private void Awake()
    {
        Instance = this;
        byPriority = (a, b) => Priority(b).CompareTo(Priority(a));
        if (GetComponent<WorkRingLayout>() == null) gameObject.AddComponent<WorkRingLayout>();
    }

    private void OnEnable()
    {
        GameEvents.DisasterStarted += OnDisasterStarted;
        GameEvents.DisasterEnded += OnDisasterEnded;
        GameEvents.HouseStateChanged += OnHouseStateChanged;
        ExpansionEvents.LocationBuilt += OnLocationBuilt;
    }

    private void OnDisable()
    {
        GameEvents.DisasterStarted -= OnDisasterStarted;
        GameEvents.DisasterEnded -= OnDisasterEnded;
        GameEvents.HouseStateChanged -= OnHouseStateChanged;
        ExpansionEvents.LocationBuilt -= OnLocationBuilt;
        foreach (Pin p in pins)
        {
            ShowNative(p, true);
            if (p.root != null) p.root.gameObject.SetActive(false);
            p.shown = 0f;
        }
    }

    private void Start()
    {
        cam = Camera.main;
        canvas = FindHudCanvas();
        if (canvas == null || discSpr == null)
        {
            enabled = false;
            return;
        }

        backLayer = MakeLayer("MapMarkers", 0);
        // pinos de borda por cima dos contadores do canto e por baixo de loja, avisos, dicas e menus
        Transform money = canvas.transform.Find("Money");
        edgeLayer = MakeLayer("MapMarkersEdge", money != null ? money.GetSiblingIndex() + 1 : 1);

        foreach (House house in FindObjectsByType<House>(FindObjectsSortMode.None)) pins.Add(MakePin(house));
    }

    // Base ou armazém construído num terreno da expansão (EXPANSAO.md): ganha placa e pino de borda como os do mapa.
    private void OnLocationBuilt(Location location)
    {
        if (canvas == null || location == null || pins.Exists(p => p.house == location)) return;
        pins.Add(MakePin(location));
    }

    // O HUD é o Canvas de tela chamado UICanvas; GameObject.Find("UICanvas") chegava a devolver o Canvas do mundo
    // (UI/Canvas), e os pinos ficavam invisíveis dentro da cidade.
    private static Canvas FindHudCanvas()
    {
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.isRootCanvas && c.renderMode != RenderMode.WorldSpace && c.name == "UICanvas") return c;
        }
        return null;
    }

    private RectTransform MakeLayer(string layerName, int sibling)
    {
        var go = new GameObject(layerName, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.SetSiblingIndex(sibling);
        return rt;
    }

    #region BUILD

    private Pin MakePin(House house)
    {
        var p = new Pin { house = house, landmark = house is Location, phase = Random.value * 10f };

        Transform visual = house.transform.Find("Visual");
        var rect = (visual != null ? visual : house.transform) as RectTransform;
        var img = rect != null ? rect.GetComponent<Image>() : null;
        if (rect != null)
        {
            rect.GetWorldCorners(corners);
            float top01 = VisibleTop01(img != null ? img.sprite : null);
            Vector3 bottom = (corners[0] + corners[3]) * .5f, topEdge = (corners[1] + corners[2]) * .5f;
            p.top = Vector3.LerpUnclamped(bottom, topEdge, top01);
            p.center = Vector3.LerpUnclamped(bottom, topEdge, top01 * .5f);
        }
        else p.top = p.center = house.transform.position;

        p.nativeCall = NativeGroup(house.transform.Find("Canvas/Call"));
        p.nativeDone = NativeGroup(house.transform.Find("Canvas/Complete"));

        p.root = NewRect("Pin " + house.name, backLayer);
        p.group = p.root.gameObject.AddComponent<CanvasGroup>();
        p.hit = p.root.gameObject.AddComponent<Image>();
        p.hit.color = Color.clear;
        p.hit.raycastTarget = false;
        var click = p.root.gameObject.AddComponent<MapMarkerClick>();
        click.owner = this;
        click.house = house;

        float s = p.landmark ? landmarkDiscScale : pixelScale;
        p.root.sizeDelta = Size(discSpr, s);
        p.ringImg = NewImage("Ring", p.root, ringSpr, s);
        p.ring = p.ringImg.rectTransform;
        p.arrowImg = NewImage("Arrow", p.root, arrowSpr, s);
        p.arrow = p.arrowImg.rectTransform;
        p.body = NewRect("Body", p.root);
        NewImage("Disc", p.body, discSpr, s);
        p.fill = NewImage("Fill", p.body, fillSpr, s);
        p.iconImg = NewImage("Icon", p.body, damageIcon, s);
        p.icon = p.iconImg.rectTransform;

        if (!p.landmark)
        {
            Image badge = NewImage("Badge", p.body, badgeSpr, s);
            badge.rectTransform.anchoredPosition = new Vector2(10f, -10f) * s;
            p.badge = badge.gameObject;
            p.amount = NewText("Amount", badge.rectTransform, 8f * s);
            p.amount.rectTransform.anchoredPosition = new Vector2(.5f, 0f) * s;
        }
        else
        {
            bool guard = house is CityGuard;
            Image plate = NewImage("Plate", backLayer, plateSpr, landmarkDiscScale);
            plate.name = "Plate " + house.name;
            plate.type = Image.Type.Sliced;
            plate.pixelsPerUnitMultiplier = 1f / landmarkDiscScale;
            p.plate = plate.rectTransform;
            TMP_Text label = NewText("Name", p.plate, plateFontSize);
            label.text = guard ? guardName : depotName;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            Vector2 disc = Size(discSpr, landmarkDiscScale);
            float textWidth = label.GetPreferredValues(label.text).x;
            p.plateWidth = disc.x * .5f + 10f + textWidth + 16f;
            p.plate.sizeDelta = new Vector2(p.plateWidth, disc.y * .8f);
            p.plate.pivot = new Vector2(0f, .5f);
            label.rectTransform.pivot = new Vector2(0f, .5f);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0f, .5f);
            label.rectTransform.sizeDelta = new Vector2(textWidth + 4f, disc.y);
            label.rectTransform.anchoredPosition = new Vector2(disc.x * .5f + 10f, 1f);
            Sprite pointer = FeelUI.Theme != null ? FeelUI.Theme.highlightArrow : null;
            if (pointer != null) p.pointer = NewImage("Pointer", p.plate, pointer, 1f).rectTransform;
            p.plate.gameObject.SetActive(false);
            p.root.SetAsLastSibling();
        }

        p.root.gameObject.SetActive(false);
        return p;
    }

    // Topo visível do sprite (0 = base, 1 = topo do retângulo): a malha justa ignora a sobra transparente.
    private static float VisibleTop01(Sprite sprite)
    {
        if (sprite == null || sprite.rect.height <= 0f) return 1f;
        float max = float.MinValue;
        foreach (Vector2 v in sprite.vertices) max = Mathf.Max(max, v.y);
        return Mathf.Clamp01((max * sprite.pixelsPerUnit + sprite.pivot.y) / sprite.rect.height);
    }

    private static CanvasGroup NativeGroup(Transform marker)
    {
        if (marker == null) return null;
        CanvasGroup group = marker.GetComponent<CanvasGroup>();
        return group != null ? group : marker.gameObject.AddComponent<CanvasGroup>();
    }

    private static RectTransform NewRect(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = Vector2.zero;
        return rt;
    }

    private static Vector2 Size(Sprite sprite, float scale) => sprite != null ? sprite.rect.size * scale : Vector2.zero;

    private static Image NewImage(string objectName, Transform parent, Sprite sprite, float scale)
    {
        RectTransform rt = NewRect(objectName, parent);
        var image = rt.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        rt.sizeDelta = Size(sprite, scale);
        return image;
    }

    private TMP_Text NewText(string objectName, Transform parent, float size)
    {
        RectTransform rt = NewRect(objectName, parent);
        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        rt.sizeDelta = new Vector2(size * 2f, size * 1.4f);
        return text;
    }

    #endregion

    #region EVENTS

    private void OnDisasterStarted(DisasterData disaster, Vector3 epicenter)
    {
        disasterActive = disaster != null;
        if (disaster != null) disasterType = disaster.type;
    }

    private void OnDisasterEnded(DisasterData disaster) => disasterActive = false;

    private void OnHouseStateChanged(House house, State before, State after)
    {
        if (house == null) return;
        if (after == State.Intact) causes.Remove(house);
        else if (before == State.Intact && disasterActive) causes[house] = disasterType;
    }

    #endregion

    #region STATE

    private Kind KindOf(Pin p)
    {
        House h = p.house;
        if (h.IsComplete) return Kind.Done;
        if (p.landmark) return h is CityGuard ? Kind.Guard : Kind.Depot;
        if (h.IsWorking) return Kind.None;
        switch (h.currentState)
        {
            case State.OnFire: return Kind.Fire;
            case State.PowerOutage: return Kind.Power;
            case State.Damaged:
                return causes.TryGetValue(h, out DisasterType cause) && cause == DisasterType.Storm ? Kind.Storm : Kind.Damage;
            default: return Kind.None;
        }
    }

    private static bool IsLandmarkKind(Kind kind) => kind == Kind.Guard || kind == Kind.Depot;

    private Color ColorOf(Kind kind)
    {
        switch (kind)
        {
            case Kind.Fire: return fireColor;
            case Kind.Storm: return stormColor;
            case Kind.Power: return powerColor;
            case Kind.Done: return doneColor;
            case Kind.Guard: return guardColor;
            case Kind.Depot: return depotColor;
            default: return damageColor;
        }
    }

    // Ícone v2 do tema (docs/jam/ICONES_V2.md, 64 px a 1:1) ou, sem ele, o Mk_* de 14 px da cena
    private Sprite IconOf(Kind kind)
    {
        FeelUITheme t = FeelUI.Theme;
        switch (kind)
        {
            case Kind.Fire: return V2(t != null ? t.callFire : null, fireIcon);
            case Kind.Storm: return V2(t != null ? t.callStorm : null, stormIcon);
            case Kind.Power: return V2(t != null ? t.callPower : null, stormIcon);
            case Kind.Done: return V2(t != null ? t.callDoneCheck : null, doneIcon);
            case Kind.Guard: return V2(t != null ? t.landmarkBase : null, guardIcon);
            case Kind.Depot: return V2(t != null ? t.landmarkDepot : null, depotIcon);
            default: return V2(t != null ? t.callRepair : null, damageIcon);
        }
    }

    private static Sprite V2(Sprite v2, Sprite old) => v2 != null ? v2 : old;

    // O Mk_* de 14 px cresce com o pixel do pino; o v2 de 64 px já vem no tamanho (aumentar quebraria o pixel)
    private Vector2 IconSize(Sprite sprite, bool landmark) =>
        Size(sprite, sprite != null && sprite.rect.width >= 32f ? 1f : landmark ? landmarkDiscScale : pixelScale);

    // Quem fica com as vagas da borda: fogo, depois chamado esperando, coleta, e por último Base e Armazém.
    private static int Priority(Pin p)
    {
        switch (p.kind)
        {
            case Kind.Fire: return 5;
            case Kind.Damage:
            case Kind.Storm:
            case Kind.Power: return 4;
            case Kind.Done: return 3;
            default: return 1;
        }
    }

    private void ShowNative(Pin p, bool visible)
    {
        if (!hideNativeMarkers) visible = true;
        if (p.nativeVisible == visible) return;
        p.nativeVisible = visible;
        float alpha = visible ? 1f : 0f;
        if (p.nativeCall != null) p.nativeCall.alpha = alpha;
        if (p.nativeDone != null) p.nativeDone.alpha = alpha;
    }

    #endregion

    #region UPDATE

    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        GameManager game = GameManager.Instance;
        bool live = game == null || game.State == GameState.Playing || game.State == GameState.Paused;
        Rect area = backLayer.rect;
        var safe = Rect.MinMaxRect(area.xMin + marginLeft, area.yMin + marginBottom, area.xMax - marginRight, area.yMax - marginTop);
        float unitsPerPixel = area.height / Mathf.Max(1, cam.pixelHeight);
        float zoom01 = Mathf.InverseLerp(8f, 16f, cam.orthographicSize);
        float zoomScale = Mathf.Lerp(1f, farPinScale, zoom01);
        float dt = Time.unscaledDeltaTime;
        float now = Time.unscaledTime;

        edgePins.Clear();
        foreach (Pin p in pins)
        {
            if (p.house == null) continue;
            p.kind = live ? KindOf(p) : Kind.None;
            p.attention = p.kind != Kind.None && !IsLandmarkKind(p.kind) && (p.kind == Kind.Done || !p.house.HasTeam);
            p.wanted = p.kind != Kind.None;
            if (!p.wanted) continue;

            Vector2 anchor = ToLayer(p.top, area, unitsPerPixel);
            Vector2 middle = ToLayer(p.center, area, unitsPerPixel);
            float scale = BaseScale(p, zoomScale);
            float lift = gapAboveHouse + (p.root.sizeDelta.y * .5f + ArrowReach(p)) * scale;
            p.target = anchor + Vector2.up * lift;
            p.edge = !safe.Contains(middle) && !safe.Contains(p.target);
            if (p.edge)
            {
                // fora da tela só o que pede ação (e os prédios de referência); equipe a caminho não ocupa a borda
                if (p.attention || IsLandmarkKind(p.kind)) edgePins.Add(p);
                else p.wanted = false;
            }
            else if (IsLandmarkKind(p.kind)) p.target = anchor + Vector2.up * (gapAboveHouse + p.plate.sizeDelta.y * .5f);
            p.target = Clamp(p.target, safe);
            p.pos = p.target;
        }

        if (edgePins.Count > maxEdgePins)
        {
            edgePins.Sort(byPriority);
            for (int i = maxEdgePins; i < edgePins.Count; i++) edgePins[i].wanted = false;
            edgePins.RemoveRange(maxEdgePins, edgePins.Count - maxEdgePins);
        }
        Spread(edgePins, safe, zoomScale);

        foreach (Pin p in pins)
        {
            if (p.house == null) continue;
            Animate(p, area, unitsPerPixel, zoomScale, now, dt);
        }
    }

    private Vector2 ToLayer(Vector3 world, Rect area, float unitsPerPixel)
    {
        Vector3 screen = cam.WorldToScreenPoint(world);
        return new Vector2(screen.x * unitsPerPixel + area.xMin, screen.y * unitsPerPixel + area.yMin);
    }

    private static Vector2 Clamp(Vector2 point, Rect rect) =>
        new Vector2(Mathf.Clamp(point.x, rect.xMin, rect.xMax), Mathf.Clamp(point.y, rect.yMin, rect.yMax));

    private float BaseScale(Pin p, float zoomScale)
    {
        float scale = zoomScale;
        if (!p.landmark && p.kind != Kind.Done && p.house.HasTeam) scale *= enRouteScale;
        return scale;
    }

    // Distância do meio do disco até a ponta da seta, em px do pino sem escala.
    private float ArrowReach(Pin p) => p.arrow.sizeDelta.y * .78f;

    // Pinos de borda que cairiam um em cima do outro se afastam ao longo da borda.
    private void Spread(List<Pin> list, Rect safe, float zoomScale)
    {
        for (int pass = 0; pass < 4; pass++)
        {
            bool moved = false;
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    Pin a = list[i], b = list[j];
                    float min = (a.root.sizeDelta.x + b.root.sizeDelta.x) * .5f * zoomScale + 6f;
                    Vector2 delta = b.pos - a.pos;
                    float distance = delta.magnitude;
                    if (distance >= min) continue;

                    bool sideEdge = Mathf.Approximately(a.pos.x, safe.xMin) || Mathf.Approximately(a.pos.x, safe.xMax);
                    Vector2 along = sideEdge ? Vector2.up : Vector2.right;
                    float sign = Vector2.Dot(delta, along) >= 0f ? 1f : -1f;
                    Vector2 push = along * (sign * (min - distance) * .5f);
                    a.pos = Clamp(a.pos - push, safe);
                    b.pos = Clamp(b.pos + push, safe);
                    moved = true;
                }
            }
            if (!moved) break;
        }
    }

    private void Animate(Pin p, Rect area, float unitsPerPixel, float zoomScale, float now, float dt)
    {
        // Base e Armazém dentro da tela: placa com o nome, e o disco do ícone na ponta dela
        bool plateMode = p.wanted && IsLandmarkKind(p.kind) && !p.edge;
        if (p.plate != null)
        {
            if (p.plate.gameObject.activeSelf != plateMode) p.plate.gameObject.SetActive(plateMode);
            if (plateMode)
            {
                PlacePlate(p, area, unitsPerPixel);
                ShowNative(p, true);
                return;
            }
        }

        bool showPin = p.wanted;
        p.shown = Mathf.MoveTowards(p.shown, showPin ? 1f : 0f, dt / Mathf.Max(.01f, popTime));
        if (p.shown <= 0f)
        {
            if (p.root.gameObject.activeSelf) p.root.gameObject.SetActive(false);
            ShowNative(p, true);
            p.shownKind = Kind.None;
            return;
        }

        if (!p.root.gameObject.activeSelf) p.root.gameObject.SetActive(true);
        float pop = Mathf.Sin(p.shown * Mathf.PI * .5f);
        pop += Mathf.Sin(p.shown * Mathf.PI) * .18f;
        float scale = BaseScale(p, zoomScale);
        p.root.localScale = Vector3.one * (scale * pop);
        if (!showPin) return;   // saindo: só encolhe no lugar

        Paint(p);
        bool onEdge = p.edge;
        Transform layer = onEdge ? edgeLayer : backLayer;
        if (p.root.parent != layer) p.root.SetParent(layer, false);
        p.hit.raycastTarget = onEdge;
        if (!p.arrowImg.enabled) p.arrowImg.enabled = true;
        ShowNative(p, onEdge || IsLandmarkKind(p.kind));

        float t = now + p.phase;
        bool urgent = p.attention;
        float pulse = urgent ? 1f + pulseAmount * Mathf.Sin(t * pulseSpeed) : 1f;
        p.group.alpha = (!p.landmark && p.house.HasTeam && p.kind != Kind.Done ? enRouteAlpha : 1f) * Mathf.Clamp01(p.shown * 2f);

        Vector2 pos = p.pos;
        if (!onEdge)
        {
            if (p.kind == Kind.Done) pos.y += Mathf.Abs(Mathf.Sin(t * 5f)) * bouncePixels;
            else if (urgent) pos.y += Mathf.Sin(t * 3f) * 2f;
        }
        p.root.anchoredPosition = pos;

        p.body.localScale = Vector3.one * pulse;
        p.icon.localRotation = Quaternion.Euler(0f, 0f, urgent && p.kind != Kind.Done ? Mathf.Sin(t * 5f) * wobbleDegrees : 0f);

        // seta: do pino para o prédio (reta para baixo quando está em cima dele)
        Vector2 anchor = ToLayer(onEdge ? p.center : p.top, area, unitsPerPixel);
        Vector2 dir = anchor - pos;
        dir = dir.sqrMagnitude > 1f ? dir.normalized : Vector2.down;
        float reach = p.root.sizeDelta.y * .5f + p.arrow.sizeDelta.y * .28f;
        if (onEdge && urgent) reach += Mathf.Abs(Mathf.Sin(t * 5f)) * 4f;
        p.arrow.anchoredPosition = dir * reach;
        p.arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);

        // onda que abre e some, só em quem pede ação
        if (urgent)
        {
            float interval = p.kind == Kind.Fire ? fireWaveInterval : waveInterval;
            float wave = Mathf.Repeat(t, interval) / interval;
            p.ring.localScale = Vector3.one * Mathf.Lerp(.75f, waveScale, wave);
            Color c = ColorOf(p.kind);
            c.a = (1f - wave) * .9f;
            p.ringImg.color = c;
        }
        if (p.ringImg.enabled != urgent) p.ringImg.enabled = urgent;

        if (p.badge != null)
        {
            int amount = p.kind == Kind.Damage || p.kind == Kind.Storm ? p.house.damageAmount : 0;
            if (amount != p.lastAmount)
            {
                p.lastAmount = amount;
                p.badge.SetActive(amount > 0);
                if (amount > 0) p.amount.SetText("{0}", amount);
            }
        }
    }

    private void Paint(Pin p)
    {
        if (p.shownKind == p.kind) return;
        p.shownKind = p.kind;
        p.fill.color = ColorOf(p.kind);
        p.iconImg.sprite = IconOf(p.kind);
        p.icon.sizeDelta = IconSize(p.iconImg.sprite, p.landmark);
        p.arrowImg.color = ColorOf(p.kind);
    }

    private void PlacePlate(Pin p, Rect area, float unitsPerPixel)
    {
        Vector2 anchor = ToLayer(p.top, area, unitsPerPixel);
        Vector2 size = p.plate.sizeDelta;
        Vector2 disc = Size(discSpr, landmarkDiscScale);
        float left = anchor.x - (p.plateWidth + disc.x * .5f) * .5f + disc.x * .5f;
        float y = anchor.y + gapAboveHouse + disc.y * .5f;
        p.plate.anchoredPosition = new Vector2(Mathf.Round(left), Mathf.Round(y));
        p.plate.localScale = Vector3.one;

        // o disco com o ícone fica na ponta esquerda da placa, sem seta
        if (!p.root.gameObject.activeSelf) p.root.gameObject.SetActive(true);
        if (p.root.parent != backLayer) p.root.SetParent(backLayer, false);
        Paint(p);
        p.shown = 1f;
        p.group.alpha = 1f;
        p.hit.raycastTarget = false;
        p.root.localScale = Vector3.one;
        p.root.anchoredPosition = new Vector2(Mathf.Round(left), Mathf.Round(y));
        p.body.localScale = Vector3.one;
        p.icon.localRotation = Quaternion.identity;
        p.arrowImg.enabled = false;
        p.ringImg.enabled = false;
        PlacePointer(p, anchor.x - Mathf.Round(left) - p.plateWidth * .5f, size.y * .5f);
    }

    // Seta dourada v2 em cima da placa, apontando o prédio: quica em passos de pixel inteiro (0, 2, 4, 2 px) e
    // dobra o passo e o ritmo quando o prédio pede ação (escolher a base, Armazém sem material). Item 17.
    private void PlacePointer(Pin p, float x, float plateHalfHeight)
    {
        if (p.pointer == null) return;

        bool attention = LandmarkAttention(p);
        float step = attention ? pointerStepTime * .5f : pointerStepTime;
        int phase = Mathf.FloorToInt((Time.unscaledTime + p.phase) / Mathf.Max(.01f, step)) % 4;
        float bounce = (phase == 2 ? 4f : phase == 0 ? 0f : 2f) * (attention ? 2f : 1f);
        p.pointer.anchoredPosition = new Vector2(Mathf.Round(x), plateHalfHeight + gapAboveHouse + p.pointer.sizeDelta.y * .5f + bounce);
    }

    private static bool LandmarkAttention(Pin p)
    {
        if (p.house is CityGuard) return GuardManager.selectBaseMode;
        return ResourcesManager.Instance != null && ResourcesManager.BuildMaterial <= 0 && !p.house.HasTeam;
    }

    #endregion

    #region CAMERA

    // Clique num pino de borda: a câmera desliza até a casa, sem mudar o zoom.
    public void FocusOn(House house)
    {
        if (house == null || cam == null) return;
        if (pan != null) StopCoroutine(pan);
        pan = StartCoroutine(Pan(house.transform.position));
    }

    private IEnumerator Pan(Vector3 world)
    {
        Transform t = cam.transform;
        Vector3 from = t.position;
        Vector3 to = ClampToMap(new Vector3(world.x, world.y, from.z));
        for (float e = 0f; e < panTime; e += Time.unscaledDeltaTime)
        {
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(e / panTime), 3f);
            t.position = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
        t.position = to;
        pan = null;
    }

    // Mesmo limite do CameraController (o SpriteRenderer "limiter" dele), sem mexer naquele script.
    private Vector3 ClampToMap(Vector3 target)
    {
        CameraController controller = CameraController.Instance;
        if (controller == null) return target;
        var field = typeof(CameraController).GetField("limiter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var limiter = field != null ? field.GetValue(controller) as SpriteRenderer : null;
        if (limiter == null) return target;

        Bounds b = limiter.bounds;
        float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
        float minX = b.min.x + halfW, maxX = b.max.x - halfW, minY = b.min.y + halfH, maxY = b.max.y - halfH;
        target.x = minX <= maxX ? Mathf.Clamp(target.x, minX, maxX) : b.center.x;
        target.y = minY <= maxY ? Mathf.Clamp(target.y, minY, maxY) : b.center.y;
        return target;
    }

    #endregion
}

// Clique no pino de borda (o pino em cima da casa não recebe clique: quem recebe é a casa).
public class MapMarkerClick : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public MapMarkers owner;
    [HideInInspector] public House house;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null) owner.FocusOn(house);
    }
}
