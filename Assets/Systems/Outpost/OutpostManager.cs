using System.Collections;
using System.Collections.Generic;
using DefesaCivil.CityGen;
using TMPro;
using UnityEngine;

// Guarita por quarteirão (GUARITAS.md, v5A): no lugar da Automação 4, que despachava a cidade inteira sozinha e tirava a
// graça do jogo (pedido do Pietro), cada guarita despacha sozinha só os chamados do quarteirão dela.
// - A compra é como a do para-raio: UpgradesManager.TryBuy do upgrade "outpost" (preço e crescimento no Outpost.asset;
//   o máximo é um por quarteirão com casa, SetLevelCap). Comprada pela aba, a obra fica paga, o jogador entra no modo
//   de escolha e cada quarteirão livre na tela ganha um marcador; o mouse em cima mostra a área e a guarita meio apagada
//   no canto dela, e o clique começa a obra ali.
// - Pronta, a cada OutpostData.dispatchInterval ela manda a equipe sugerida da base com energia mais perto para o chamado
//   mais urgente do quarteirão que já esperou o reactionDelay (GuardManager.TryAutoDispatch). Sem gente, material ou
//   energia, o chamado só espera. Os quarteirões são os do para-raio (LightningRodData.blocks).
// - Pausa (Esc): o despacho e a obra andam no GameClock (para na pausa) e só em GameState.Playing.
// Nasce sozinha (OutpostBootstrap).
public class OutpostManager : MonoBehaviour
{
    public static OutpostManager Instance;

    public const string UpgradeId = "outpost";

    [Header("TEXTS")]
    [SerializeField] private string chooseText = "Guarita paga: clique num quarteirão para ela cuidar";

    public OutpostData Data { get; private set; }
    public LightningRodData Blocks { get; private set; }
    public IReadOnlyList<OutpostBlock> Sites => sites;
    public bool Placing { get; private set; }
    public int Credits { get; private set; }
    public TMP_FontAsset Font { get; private set; }
    public RectTransform SignLayer { get; private set; }
    public Transform World { get; private set; }
    public Transform Shadows { get; private set; }

    private readonly List<OutpostBlock> sites = new();
    private readonly Dictionary<House, OutpostBlock> siteOf = new();
    private readonly Dictionary<House, float> waitingSince = new();
    private readonly List<House> candidates = new();
    private OutpostBlock pendingSite;
    private GameObject upgradeTab;

    private void Awake()
    {
        Instance = this;
        Data = OutpostData.Load();
        Blocks = LightningRodData.Load();
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

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        bool ready = Setup();
        if (UpgradesManager.Instance != null) UpgradesManager.Instance.SetLevelCap(UpgradeId, ready ? sites.Count : 0);
        if (!ready) enabled = false;
    }

    #region SETUP

    private bool Setup()
    {
        LocationsManager locations = LocationsManager.Instance;
        UpgradesManager upgrades = UpgradesManager.Instance;
        if (locations == null || upgrades == null || locations.Houses.Count == 0)
        {
            Debug.LogWarning("Guarita desligada: falta LocationsManager, UpgradesManager ou casas na cena.");
            return false;
        }
        if (upgrades.GetUpgrade(UpgradeId) == null)
        {
            Debug.LogWarning("Guarita desligada: sem o UpgradeData outpost em Resources/Upgrades. Rode Tools/Jam/Guarita/Criar dados.");
            return false;
        }
        if (Blocks.blocks.Count == 0)
        {
            Debug.LogWarning("Guarita desligada: LightningRodData sem quarteirões. Rode Tools/Jam/Para-raio/Criar dados.");
            return false;
        }
        if (!MakeSignLayer()) return false;

        Transform houses = locations.Houses[0].transform.parent;
        World = houses.parent;
        Shadows = MakeShadowLayer(World);

        var members = new List<House>[Blocks.blocks.Count];
        foreach (House house in locations.Houses)
        {
            int index = LightningRodMath.BlockOf(Blocks.blocks, LightningRodManager.BaseOf(house), Blocks.houseMargin);
            if (index < 0) continue;
            (members[index] ??= new List<House>()).Add(house);
        }

        List<Rect> obstacles = Obstacles(locations, World);
        for (int i = 0; i < Blocks.blocks.Count; i++)
        {
            if (members[i] == null || members[i].Count == 0) continue;
            var go = new GameObject($"[Guarita] Quarteirão {Blocks.blocks[i].id}", typeof(RectTransform));
            OutpostBlock site = go.AddComponent<OutpostBlock>();
            site.Init(this, Blocks.blocks[i], members[i], World, houses, obstacles);
            sites.Add(site);
            foreach (House house in members[i]) siteOf[house] = site;
        }
        return sites.Count > 0;
    }

    // Camada dos marcadores no HUD, por baixo dos contadores e menus (como as placas do para-raio).
    private bool MakeSignLayer()
    {
        Canvas ui = null;
        foreach (Canvas c in FindObjectsByType<Canvas>())
            if (c.isRootCanvas && c.renderMode != RenderMode.WorldSpace && c.name == "UICanvas") ui = c;
        if (ui == null)
        {
            Debug.LogWarning("Guarita desligada: sem UICanvas na cena para os marcadores dos quarteirões.");
            return false;
        }

        var go = new GameObject("OutpostSigns", typeof(RectTransform));
        SignLayer = (RectTransform)go.transform;
        SignLayer.SetParent(ui.transform, false);
        SignLayer.anchorMin = Vector2.zero;
        SignLayer.anchorMax = Vector2.one;
        SignLayer.offsetMin = SignLayer.offsetMax = Vector2.zero;
        SignLayer.SetSiblingIndex(0);

        TMP_Text sample = ui.GetComponentInChildren<TMP_Text>(true);
        Font = sample != null ? sample.font : TMP_Settings.defaultFontAsset;
        foreach (Transform child in ui.GetComponentsInChildren<Transform>(true))
            if (child.name == "UpgradeTab") upgradeTab = child.gameObject;
        return true;
    }

    // Sombra da guarita: logo acima do chão assado ([CG] Ground), por baixo de casas, árvores e trabalhadores.
    private static Transform MakeShadowLayer(Transform world)
    {
        Transform ground = world != null ? world.Find("[CG] Ground") : null;
        if (ground == null) return null;

        var layer = (RectTransform)new GameObject("OutpostShadows", typeof(RectTransform)).transform;
        layer.SetParent(world, false);
        layer.anchorMin = layer.anchorMax = layer.pivot = new Vector2(0.5f, 0.5f);
        layer.anchoredPosition = Vector2.zero;
        layer.sizeDelta = Vector2.zero;
        layer.SetSiblingIndex(ground.GetSiblingIndex() + 1);
        return layer;
    }

    // O que a guarita não deve cobrir: o desenho das casas, da Base, dos Armazéns e das árvores (em unidades do mundo).
    private static List<Rect> Obstacles(LocationsManager locations, Transform world)
    {
        var rects = new List<Rect>();
        var corners = new Vector3[4];
        void Add(RectTransform rect)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy) return;
            rect.GetWorldCorners(corners);
            rects.Add(Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y));
        }

        foreach (House house in locations.Houses) Add(house.transform.Find("Visual") as RectTransform);
        foreach (Warehouse warehouse in locations.Warehouses) Add(warehouse.transform.Find("Visual") as RectTransform);
        foreach (CityGuard guard in FindObjectsByType<CityGuard>()) Add(guard.transform.Find("Visual") as RectTransform);
        Transform trees = world != null ? world.Find("Trees") : null;
        if (trees != null)
            foreach (Transform tree in trees) Add(tree as RectTransform);
        return rects;
    }

    #endregion

    #region PURCHASE

    // Clique no marcador ou no chão do quarteirão (modo de escolha).
    public void OnSiteClicked(OutpostBlock site)
    {
        if (site == null || !site.IsFree || GameManager.InputBlocked) return;

        if (Credits > 0)
        {
            Credits--;
            StartBuild(site);
            RefreshPlacing();
            return;
        }

        UpgradeData upgrade = GetUpgrade();
        if (upgrade == null || UpgradesManager.Instance == null) return;

        // Mesmo caminho da aba: TryBuy cobra, dispara UpgradePurchased (som e brilho da compra) ou ActionDenied.
        pendingSite = site;
        if (!UpgradesManager.Instance.TryBuy(upgrade, site.SignWorldPosition)) pendingSite = null;
    }

    private void OnUpgradePurchased(UpgradeData upgrade, int newLevel)
    {
        if (upgrade == null || upgrade.id != UpgradeId) return;

        OutpostBlock site = pendingSite != null && pendingSite.IsFree ? pendingSite : null;
        pendingSite = null;
        if (site != null)
        {
            StartBuild(site);
            return;
        }

        // Comprada pela aba: a obra fica paga e o jogador escolhe o quarteirão.
        if (FreeSites() == 0)
        {
            Debug.LogWarning("Comprou uma guarita sem quarteirão livre: o teto do outpost (SetLevelCap) não segurou a compra.");
            return;
        }
        Credits++;
        if (upgradeTab != null) upgradeTab.SetActive(false);
        RefreshPlacing();
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Won || state == GameState.Lost) SetPlacing(false);
    }

    private void RefreshPlacing() => SetPlacing(Credits > 0 && FreeSites() > 0);

    private void SetPlacing(bool placing)
    {
        if (Placing == placing) return;
        Placing = placing;
        if (placing) FeedbacksManager.ShowText(chooseText);
        else FeedbacksManager.HideText();
        OutpostEvents.RaisePlacementChanged(placing);
    }

    #endregion

    #region BUILD

    private void StartBuild(OutpostBlock site)
    {
        site.BeginBuild();
        OutpostEvents.RaiseBuildStarted(site);
        StartCoroutine(BuildRoutine(site));
    }

    private IEnumerator BuildRoutine(OutpostBlock site)
    {
        float duration = Mathf.Max(0f, Data.buildTime);
        for (float t = 0f; t < duration; t += GameClock.DeltaTime)
        {
            site.SetProgress(t / duration);
            yield return null;
        }

        site.FinishBuild();
        OutpostEvents.RaiseBuildFinished(site);
    }

    #endregion

    #region AUTO DISPATCH

    private void Update()
    {
        GameManager game = GameManager.Instance;
        if (game == null || game.State != GameState.Playing || GuardManager.Instance == null) return;

        foreach (OutpostBlock site in sites)
        {
            if (site.State != LotState.Built) continue;
            site.Cooldown -= GameClock.DeltaTime;
            if (site.Cooldown > 0f) continue;
            if (!Dispatch(site)) continue;
            site.Cooldown = Data.dispatchInterval;
        }
    }

    // Chamados do quarteirão que já esperaram o tempo de reação, do mais urgente ao menos (GuardManager.CompareUrgency).
    // Devolve true se tentou algum (com ou sem gente para mandar): a guarita volta a olhar depois do intervalo.
    private bool Dispatch(OutpostBlock site)
    {
        float now = GameClock.Time;
        candidates.Clear();
        foreach (House house in site.Houses)
        {
            if (!GuardManager.WaitsForTeam(house))
            {
                if (house != null) waitingSince.Remove(house);
                continue;
            }
            if (!waitingSince.TryGetValue(house, out float since)) waitingSince[house] = since = now;
            if (OutpostMath.Ready(since, now, Data.reactionDelay)) candidates.Add(house);
        }
        if (candidates.Count == 0) return false;

        candidates.Sort(GuardManager.CompareUrgency);
        foreach (House house in candidates)
        {
            if (!GuardManager.Instance.TryAutoDispatch(house)) continue;
            waitingSince.Remove(house);
            site.CountDispatch();
            OutpostEvents.RaiseAutoDispatched(site, house);
            return true;
        }
        site.MarkWaiting();
        return true;
    }

    #endregion

    #region QUERIES

    public UpgradeData GetUpgrade() => UpgradesManager.Instance != null ? UpgradesManager.Instance.GetUpgrade(UpgradeId) : null;

    public OutpostBlock SiteOfHouse(House house) => house != null && siteOf.TryGetValue(house, out OutpostBlock site) ? site : null;

    // Guarita pronta que cuida desta casa, ou null.
    public OutpostBlock GuardOf(House house)
    {
        OutpostBlock site = SiteOfHouse(house);
        return site != null && site.State == LotState.Built ? site : null;
    }

    public int FreeSites()
    {
        int free = 0;
        foreach (OutpostBlock site in sites)
        {
            if (site.IsFree) free++;
        }
        return free;
    }

    public int BuiltSites()
    {
        int built = 0;
        foreach (OutpostBlock site in sites)
        {
            if (site.State == LotState.Built) built++;
        }
        return built;
    }

    #endregion
}
