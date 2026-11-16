using System.Collections;
using System.Collections.Generic;
using DefesaCivil.CityGen;
using DG.Tweening;
using TMPro;
using UnityEngine;

// Expansão (EXPANSAO.md): terrenos vazios onde o jogador constrói base da Guarda e armazém novos.
// - Os terrenos vêm do ExpansionData; cada um aceita um tipo (base ou armazém) e fica num lado da cidade.
// - A compra é a das melhorias: UpgradesManager.TryBuy dos upgrades base2 e depot2 (preço, crescimento e máximo no
//   asset). Clicar na placa do terreno compra e constrói ali; comprar pela aba deixa a obra paga e a placa dos
//   terrenos daquele tipo pulsa (com seta na borda se estiver fora da tela) até o jogador escolher onde.
// - O prédio novo é cópia da Guarda (ou do Armazém) da cena, com uma entrada nova ligada na rua: o GuardManager
//   e o GetNearestGuard já atendem quantas bases houver.
// Nasce sozinho (ExpansionBootstrap); pôr o componente na cena só serve para mudar os textos.
public class ExpansionManager : MonoBehaviour
{
    public static ExpansionManager Instance;

    public const string BaseUpgradeId = UpgradeIds.Base2;
    public const string DepotUpgradeId = "depot2";
    private const float MaxSnapDistance = 1.5f; // entrada a mais que isso de uma rua não é ligada no meio dela

    [Header("TEXTS")]
    [SerializeField] private string chooseBaseText = "Obra paga: clique num terreno para a nova base";
    [SerializeField] private string chooseDepotText = "Obra paga: clique num terreno para o novo armazém";

    public ExpansionData Data { get; private set; }
    public IReadOnlyList<BuildLot> Lots => lots;
    public LotKind? Placing { get; private set; }
    public TMP_FontAsset Font { get; private set; }
    public RectTransform SignLayer { get; private set; }
    public Material WorldMaterial { get; private set; }

    private readonly List<BuildLot> lots = new();
    private readonly Dictionary<LotKind, int> credits = new() { { LotKind.Base, 0 }, { LotKind.Depot, 0 } };
    private readonly Dictionary<LotKind, Location> templates = new();
    private readonly Dictionary<LotKind, Vector3> buildingOffsets = new(); // posição do prédio - entrada
    private readonly Dictionary<LotKind, float> baseOffsets = new();       // base visível do prédio - entrada (y)
    private readonly List<Vector2> housePoints = new();
    private readonly List<Vector2> basePoints = new();
    private BuildLot pendingLot;
    private Transform housesParent, waypointsParent, treesParent;
    private GameObject upgradeTab;

    private void Awake()
    {
        Instance = this;
        Data = ExpansionData.Load();
    }

    private void OnEnable()
    {
        GameEvents.UpgradePurchased += OnUpgradePurchased;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.UpgradePurchased -= OnUpgradePurchased;
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    private void Start()
    {
        if (!Setup()) enabled = false;
    }

    #region SETUP

    private bool Setup()
    {
        LocationsManager locations = LocationsManager.Instance;
        UpgradesManager upgrades = UpgradesManager.Instance;
        if (locations == null || upgrades == null || locations.Guards.Count == 0 || locations.Guards[0].entry == null)
        {
            Debug.LogWarning("Expansão desligada: falta LocationsManager, UpgradesManager ou a Guarda com entrada na cena.");
            return false;
        }
        if (upgrades.GetUpgrade(BaseUpgradeId) == null && upgrades.GetUpgrade(DepotUpgradeId) == null)
        {
            Debug.LogWarning("Expansão desligada: sem UpgradeData base2 e depot2 em Resources/Upgrades. Rode Tools/Jam/Expansão/Criar dados.");
            return false;
        }

        CityGuard guard = locations.Guards[0];
        housesParent = guard.transform.parent;
        waypointsParent = guard.entry.transform.parent;
        treesParent = housesParent.parent != null ? housesParent.parent.Find("Trees") : null;
        WorldMaterial = FindWorldMaterial();

        var holder = new GameObject("[Expansão] Modelos");
        holder.SetActive(false);
        holder.transform.SetParent(transform, false);
        RegisterTemplate(LotKind.Base, guard, holder.transform);
        if (locations.Warehouses.Count > 0 && locations.Warehouses[0].entry != null)
            RegisterTemplate(LotKind.Depot, locations.Warehouses[0], holder.transform);

        if (!MakeSignLayer()) return false;

        var graph = new List<Waypoint>(FindObjectsByType<Waypoint>());
        foreach (LotSlot slot in Data.lots)
        {
            if (slot == null || !templates.ContainsKey(slot.kind) || GetUpgrade(slot.kind) == null) continue;

            Waypoint entry = MakeEntry(slot, graph);
            var go = new GameObject($"[Expansão] Terreno {slot.label} {KindName(slot.kind)}", typeof(RectTransform));
            BuildLot lot = go.AddComponent<BuildLot>();
            lot.Init(this, slot, entry, baseOffsets[slot.kind], housesParent, treesParent);
            lots.Add(lot);
        }
        return lots.Count > 0;
    }

    // Cópia inativa da Guarda (ou do Armazém) feita antes de a partida mexer nela: chamado, obra e ícones zerados.
    private void RegisterTemplate(LotKind kind, Location source, Transform holder)
    {
        Location copy = Instantiate(source, holder);
        copy.name = $"Modelo {KindName(kind)}";
        templates[kind] = copy;

        Vector3 entry = source.entry.transform.position;
        buildingOffsets[kind] = source.transform.position - entry;
        YSortCanvas sort = source.GetComponentInChildren<YSortCanvas>();
        baseOffsets[kind] = (sort != null ? sort.BaseY() : source.transform.position.y) - entry.y;
    }

    // Camada das placas no HUD, por baixo dos contadores e menus (como os pinos do MapMarkers).
    private bool MakeSignLayer()
    {
        GameObject ui = GameObject.Find("UICanvas");
        if (ui == null)
        {
            Debug.LogWarning("Expansão desligada: sem UICanvas na cena para as placas dos terrenos.");
            return false;
        }

        var go = new GameObject("ExpansionSigns", typeof(RectTransform));
        SignLayer = (RectTransform)go.transform;
        SignLayer.SetParent(ui.transform, false);
        SignLayer.anchorMin = Vector2.zero;
        SignLayer.anchorMax = Vector2.one;
        SignLayer.offsetMin = SignLayer.offsetMax = Vector2.zero;
        SignLayer.SetSiblingIndex(0);

        TMP_Text sample = ui.GetComponentInChildren<TMP_Text>(true);
        Font = sample != null ? sample.font : TMP_Settings.defaultFontAsset;
        upgradeTab = FindChild(ui.transform, "UpgradeTab");
        return true;
    }

    // Material das árvores: o terreno recebe a mesma luz 2D que o resto do chão de cima.
    private Material FindWorldMaterial()
    {
        if (treesParent == null) return null;
        var image = treesParent.GetComponentInChildren<UnityEngine.UI.Image>();
        return image != null ? image.material : null;
    }

    // Waypoint novo na rua em frente ao terreno, ligado às duas pontas do trecho de rua onde ele cai.
    private Waypoint MakeEntry(LotSlot slot, List<Waypoint> graph)
    {
        var go = new GameObject($"[Expansão] Entrada {slot.label} {KindName(slot.kind)}");
        go.transform.SetParent(waypointsParent, false);
        go.transform.position = new Vector3(slot.entry.x, slot.entry.y, waypointsParent.position.z);
        Waypoint entry = go.AddComponent<Waypoint>();

        var index = new Dictionary<Waypoint, int>();
        var positions = new List<Vector2>();
        var neighbors = new List<List<int>>();
        foreach (Waypoint waypoint in graph)
        {
            index[waypoint] = positions.Count;
            positions.Add(waypoint.transform.position);
        }
        foreach (Waypoint waypoint in graph)
        {
            var list = new List<int>();
            foreach (Waypoint neighbor in waypoint.neighbors)
            {
                if (neighbor != null && index.TryGetValue(neighbor, out int i)) list.Add(i);
            }
            neighbors.Add(list);
        }

        if (ExpansionMath.NearestEdge(positions, neighbors, slot.entry, out int a, out int b, out float distance) && distance <= MaxSnapDistance)
        {
            Connect(entry, graph[a]);
            Connect(entry, graph[b]);
        }
        else
        {
            Waypoint nearest = null;
            float best = float.MaxValue;
            foreach (Waypoint waypoint in graph)
            {
                float d = Vector2.Distance(waypoint.transform.position, slot.entry);
                if (d < best) { best = d; nearest = waypoint; }
            }
            if (nearest != null) Connect(entry, nearest);
            Debug.LogWarning($"Terreno {slot.label} ({KindName(slot.kind)}): entrada a {distance:0.0} u da rua mais perto; ligada só ao waypoint {(nearest != null ? nearest.name : "nenhum")}. Confira ExpansionData.lots.");
        }

        graph.Add(entry);
        return entry;
    }

    private static void Connect(Waypoint a, Waypoint b)
    {
        if (a == b) return;
        if (!a.neighbors.Contains(b)) a.neighbors.Add(b);
        if (!b.neighbors.Contains(a)) b.neighbors.Add(a);
    }

    private static GameObject FindChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName) return child.gameObject;
        }
        return null;
    }

    #endregion

    #region PURCHASE

    // Clique na placa ou no chão do terreno.
    public void OnLotClicked(BuildLot lot)
    {
        if (lot == null || !lot.IsFree || GameManager.InputBlocked) return;

        if (credits[lot.Kind] > 0)
        {
            credits[lot.Kind]--;
            StartBuild(lot);
            RefreshPlacing();
            return;
        }

        UpgradeData upgrade = GetUpgrade(lot.Kind);
        if (upgrade == null || UpgradesManager.Instance == null) return;

        // Mesmo caminho da aba: TryBuy cobra, dispara UpgradePurchased (som e brilho da compra) ou ActionDenied.
        pendingLot = lot;
        if (!UpgradesManager.Instance.TryBuy(upgrade, lot.SignWorldPosition)) pendingLot = null;
    }

    private void OnUpgradePurchased(UpgradeData upgrade, int newLevel)
    {
        LotKind? kind = KindOf(upgrade);
        if (!kind.HasValue) return;

        BuildLot lot = pendingLot != null && pendingLot.Kind == kind.Value && pendingLot.IsFree ? pendingLot : null;
        pendingLot = null;
        if (lot != null)
        {
            StartBuild(lot);
            return;
        }

        // Comprado pela aba: a obra fica paga e o jogador escolhe o terreno.
        if (FreeLots(kind.Value) == 0)
        {
            Debug.LogWarning($"Comprou {upgrade.id} sem terreno livre de {KindName(kind.Value)}: maxLevel do upgrade maior que o número de terrenos no ExpansionData.");
            return;
        }
        credits[kind.Value]++;
        if (upgradeTab != null) upgradeTab.SetActive(false);
        RefreshPlacing();
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Won || state == GameState.Lost) SetPlacing(null);
    }

    private void RefreshPlacing()
    {
        if (credits[LotKind.Base] > 0) SetPlacing(LotKind.Base);
        else if (credits[LotKind.Depot] > 0) SetPlacing(LotKind.Depot);
        else SetPlacing(null);
    }

    private void SetPlacing(LotKind? kind)
    {
        if (Placing == kind) return;
        Placing = kind;
        if (kind.HasValue) FeedbacksManager.ShowText(kind == LotKind.Base ? chooseBaseText : chooseDepotText);
        else FeedbacksManager.HideText();
        ExpansionEvents.RaisePlacementChanged(kind);
    }

    public bool HasCredit(LotKind kind) => credits[kind] > 0;

    #endregion

    #region BUILD

    private void StartBuild(BuildLot lot)
    {
        lot.BeginBuild();
        ExpansionEvents.RaiseBuildStarted(lot);
        StartCoroutine(BuildRoutine(lot));
    }

    private IEnumerator BuildRoutine(BuildLot lot)
    {
        float duration = Mathf.Max(0f, Data.buildTime);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            lot.SetProgress(t / duration);
            yield return null;
        }

        Location building = Spawn(lot);
        lot.FinishBuild(building);
        if (LocationsManager.Instance != null) LocationsManager.Instance.Refresh();
        ExpansionEvents.RaiseBuildFinished(lot);
        if (building != null) ExpansionEvents.RaiseLocationBuilt(building);
    }

    private Location Spawn(BuildLot lot)
    {
        if (!templates.TryGetValue(lot.Kind, out Location template)) return null;

        Vector3 position = lot.Entry.transform.position + buildingOffsets[lot.Kind];
        GameObject go = Instantiate(template.gameObject, position, template.transform.rotation, housesParent);
        go.name = $"[Expansão] {(lot.Kind == LotKind.Base ? "Guard" : "Warehouse")} {lot.Slot.label}";
        Location location = go.GetComponent<Location>();
        location.entry = lot.Entry;
        return location;
    }

    #endregion

    #region QUERIES

    public UpgradeData GetUpgrade(LotKind kind) =>
        UpgradesManager.Instance != null ? UpgradesManager.Instance.GetUpgrade(kind == LotKind.Base ? BaseUpgradeId : DepotUpgradeId) : null;

    public static LotKind? KindOf(UpgradeData upgrade)
    {
        if (upgrade == null) return null;
        if (upgrade.id == BaseUpgradeId) return LotKind.Base;
        if (upgrade.id == DepotUpgradeId) return LotKind.Depot;
        return null;
    }

    public int FreeLots(LotKind kind)
    {
        int free = 0;
        foreach (BuildLot lot in lots)
        {
            if (lot.Kind == kind && lot.IsFree) free++;
        }
        return free;
    }

    // Casas que esta base nova atenderia (mais perto dela que de qualquer base de agora), as que estão com problema
    // e as distâncias delas até o terreno (para o anel de alcance).
    public int ServedHouses(BuildLot lot, List<float> distances, out int troubled)
    {
        troubled = 0;
        LocationsManager locations = LocationsManager.Instance;
        if (locations == null) return 0;

        basePoints.Clear();
        foreach (CityGuard guard in locations.Guards) basePoints.Add(guard.transform.position);
        foreach (BuildLot other in lots)
        {
            if (other != lot && other.Kind == LotKind.Base && other.State == LotState.Building) basePoints.Add(other.SitePosition);
        }

        housePoints.Clear();
        foreach (House house in locations.Houses) housePoints.Add(house.transform.position);
        int served = ExpansionMath.CountServed(lot.SitePosition, basePoints, housePoints, distances);

        Vector2 site = lot.SitePosition;
        foreach (House house in locations.Houses)
        {
            if (house.currentState == State.Intact) continue;
            if (IsServed(site, house.transform.position)) troubled++;
        }
        return served;
    }

    private bool IsServed(Vector2 site, Vector2 house)
    {
        float mine = Vector2.Distance(house, site);
        foreach (Vector2 other in basePoints)
        {
            if (Vector2.Distance(house, other) <= mine) return false;
        }
        return true;
    }

    // Caminho pela rua do terreno até a base mais perto (pontos em mundo) e o tempo de caminhada.
    public float WalkToNearestBase(BuildLot lot, List<Vector2> path)
    {
        path.Clear();
        LocationsManager locations = LocationsManager.Instance;
        if (locations == null || lot.Entry == null) return 0f;

        float best = float.MaxValue;
        List<Waypoint> bestPath = null;
        foreach (CityGuard guard in locations.Guards)
        {
            if (guard.entry == null) continue;
            List<Waypoint> route = Pathfinder.FindPath(guard.entry, lot.Entry);
            if (route == null || route.Count == 0) continue;
            float length = 0f;
            for (int i = 1; i < route.Count; i++) length += Vector2.Distance(route[i - 1].transform.position, route[i].transform.position);
            if (length >= best) continue;
            best = length;
            bestPath = route;
        }
        if (bestPath == null) return 0f;

        foreach (Waypoint waypoint in bestPath) path.Add(waypoint.transform.position);
        return best / Mathf.Max(0.01f, Stats.WorkerSpeed);
    }

    public static string KindName(LotKind kind) => kind == LotKind.Base ? "base" : "armazém";

    #endregion
}
