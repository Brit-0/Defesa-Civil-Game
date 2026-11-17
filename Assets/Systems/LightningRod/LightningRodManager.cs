using System.Collections;
using System.Collections.Generic;
using DefesaCivil.CityGen;
using TMPro;
using UnityEngine;

// Para-raio por quarteirão (PARARAIO.md): proteção permanente contra os raios da tempestade num quarteirão escolhido.
// - A compra é a das melhorias, como a expansão: UpgradesManager.TryBuy do upgrade "rod" (preço e crescimento no
//   Rod.asset; o máximo é um por quarteirão com casa, SetLevelCap, v5A). Comprado pela aba, a obra fica paga, o jogador entra no modo de escolha e cada quarteirão livre na
//   tela ganha um marcador; o mouse em cima mostra a área protegida e o clique começa a obra ali.
// - Pronto, o para-raio fica no telhado mais alto do quarteirão. O raio que ia cair numa casa dele é absorvido: nenhum
//   dano nem fogo. As casas protegidas pesam LightningRodData.attraction no sorteio do raio (o para-raio puxa o raio).
// - O DisasterManager pergunta por PickStrikeTarget e TryAbsorb; sem esta manager, os dois fazem o sorteio de sempre.
// Nasce sozinho (LightningRodBootstrap).
public class LightningRodManager : MonoBehaviour
{
    public static LightningRodManager Instance;

    public const string UpgradeId = "rod";

    [Header("TEXTS")]
    [SerializeField] private string chooseText = "Para-raio pago: clique num quarteirão para proteger";

    public LightningRodData Data { get; private set; }
    public IReadOnlyList<RodBlock> Blocks => blocks;
    public bool Placing { get; private set; }
    public int Credits { get; private set; }
    public TMP_FontAsset Font { get; private set; }
    public RectTransform SignLayer { get; private set; }
    public Material WorldMaterial { get; private set; }

    private readonly List<RodBlock> blocks = new();
    private readonly Dictionary<House, RodBlock> blockOf = new();
    private readonly List<bool> flags = new();
    private RodBlock pendingBlock;
    private GameObject upgradeTab;

    private void Awake()
    {
        Instance = this;
        Data = LightningRodData.Load();
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
        // v5A (pedido do Pietro): sem limite fixo de compras, um para-raio por quarteirão com casa. Desligado, nenhum.
        if (UpgradesManager.Instance != null) UpgradesManager.Instance.SetLevelCap(UpgradeId, ready ? blocks.Count : 0);
        if (!ready) enabled = false;
    }

    #region SETUP

    private bool Setup()
    {
        LocationsManager locations = LocationsManager.Instance;
        UpgradesManager upgrades = UpgradesManager.Instance;
        if (locations == null || upgrades == null || locations.Houses.Count == 0)
        {
            Debug.LogWarning("Para-raio desligado: falta LocationsManager, UpgradesManager ou casas na cena.");
            return false;
        }
        if (upgrades.GetUpgrade(UpgradeId) == null)
        {
            Debug.LogWarning("Para-raio desligado: sem o UpgradeData rod em Resources/Upgrades. Rode Tools/Jam/Para-raio/Criar dados.");
            return false;
        }
        if (Data.blocks.Count == 0)
        {
            Debug.LogWarning("Para-raio desligado: LightningRodData sem quarteirões. Rode Tools/Jam/Para-raio/Criar dados.");
            return false;
        }
        if (!MakeSignLayer()) return false;

        Transform world = locations.Houses[0].transform.parent.parent;
        WorldMaterial = FindWorldMaterial(world);

        var members = new List<House>[Data.blocks.Count];
        foreach (House house in locations.Houses)
        {
            int index = LightningRodMath.BlockOf(Data.blocks, BaseOf(house), Data.houseMargin);
            if (index < 0) continue;
            (members[index] ??= new List<House>()).Add(house);
        }

        for (int i = 0; i < Data.blocks.Count; i++)
        {
            if (members[i] == null || members[i].Count == 0) continue;
            var go = new GameObject($"[Para-raio] Quarteirão {Data.blocks[i].id}", typeof(RectTransform));
            RodBlock block = go.AddComponent<RodBlock>();
            block.Init(this, Data.blocks[i], members[i], world);
            blocks.Add(block);
            foreach (House house in members[i]) blockOf[house] = block;
        }
        return blocks.Count > 0;
    }

    // Base visível da casa (o pé do desenho), que é o que fica dentro do quarteirão no city_layout.json.
    public static Vector2 BaseOf(House house)
    {
        YSortCanvas sort = house.GetComponent<YSortCanvas>();
        return new Vector2(house.transform.position.x, sort != null ? sort.BaseY() : house.transform.position.y);
    }

    // Camada dos marcadores no HUD, por baixo dos contadores e menus (como as placas da expansão).
    private bool MakeSignLayer()
    {
        Canvas ui = null;
        foreach (Canvas c in FindObjectsByType<Canvas>())
            if (c.isRootCanvas && c.renderMode != RenderMode.WorldSpace && c.name == "UICanvas") ui = c;
        if (ui == null)
        {
            Debug.LogWarning("Para-raio desligado: sem UICanvas na cena para os marcadores dos quarteirões.");
            return false;
        }

        var go = new GameObject("LightningRodSigns", typeof(RectTransform));
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

    // Material das árvores: o para-raio no telhado recebe a mesma luz 2D que as casas.
    private static Material FindWorldMaterial(Transform world)
    {
        Transform trees = world != null ? world.Find("Trees") : null;
        var image = trees != null ? trees.GetComponentInChildren<UnityEngine.UI.Image>() : null;
        return image != null ? image.material : null;
    }

    #endregion

    #region PURCHASE

    // Clique no marcador ou no chão do quarteirão (modo de escolha).
    public void OnBlockClicked(RodBlock block)
    {
        if (block == null || !block.IsFree || GameManager.InputBlocked) return;

        if (Credits > 0)
        {
            Credits--;
            StartBuild(block);
            RefreshPlacing();
            return;
        }

        UpgradeData upgrade = GetUpgrade();
        if (upgrade == null || UpgradesManager.Instance == null) return;

        // Mesmo caminho da aba: TryBuy cobra, dispara UpgradePurchased (som e brilho da compra) ou ActionDenied.
        pendingBlock = block;
        if (!UpgradesManager.Instance.TryBuy(upgrade, block.SignWorldPosition)) pendingBlock = null;
    }

    private void OnUpgradePurchased(UpgradeData upgrade, int newLevel)
    {
        if (upgrade == null || upgrade.id != UpgradeId) return;

        RodBlock block = pendingBlock != null && pendingBlock.IsFree ? pendingBlock : null;
        pendingBlock = null;
        if (block != null)
        {
            StartBuild(block);
            return;
        }

        // Comprado pela aba: a obra fica paga e o jogador escolhe o quarteirão.
        if (FreeBlocks() == 0)
        {
            Debug.LogWarning("Comprou um para-raio sem quarteirão livre: o teto do rod (SetLevelCap) não segurou a compra.");
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

    private void RefreshPlacing() => SetPlacing(Credits > 0 && FreeBlocks() > 0);

    private void SetPlacing(bool placing)
    {
        if (Placing == placing) return;
        Placing = placing;
        if (placing) FeedbacksManager.ShowText(chooseText);
        else FeedbacksManager.HideText();
        LightningRodEvents.RaisePlacementChanged(placing);
    }

    #endregion

    #region BUILD

    private void StartBuild(RodBlock block)
    {
        block.BeginBuild();
        LightningRodEvents.RaiseBuildStarted(block);
        StartCoroutine(BuildRoutine(block));
    }

    private IEnumerator BuildRoutine(RodBlock block)
    {
        float duration = Mathf.Max(0f, Data.buildTime);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            block.SetProgress(t / duration);
            yield return null;
        }

        block.FinishBuild();
        LightningRodEvents.RaiseBuildFinished(block);
    }

    #endregion

    #region STRIKES (DisasterManager)

    // Casa onde o raio cai: sorteio de sempre sem para-raio pronto; com ele, as casas protegidas pesam 'attraction'.
    public static House PickStrikeTarget(List<House> candidates)
    {
        LightningRodManager manager = Instance;
        if (manager == null || !manager.enabled || !manager.AnyBuilt()) return candidates[Random.Range(0, candidates.Count)];

        manager.flags.Clear();
        foreach (House house in candidates) manager.flags.Add(manager.ProtectorOf(house) != null);
        int index = LightningRodMath.PickIndex(manager.flags, manager.Data.attraction, Random.value);
        return candidates[Mathf.Clamp(index, 0, candidates.Count - 1)];
    }

    // Raio numa casa de quarteirão protegido: vai para a ponta do para-raio (o mesmo clarão, raio e trovão de sempre,
    // GameEvents.LightningStruck) e não faz dano. Devolve false se a casa não está protegida.
    public static bool TryAbsorb(House house)
    {
        LightningRodManager manager = Instance;
        RodBlock block = manager != null && manager.enabled ? manager.ProtectorOf(house) : null;
        if (block == null) return false;

        block.CountStrike();
        GameEvents.RaiseLightningStruck(block.RodTip);
        LightningRodEvents.RaiseStrikeAbsorbed(block, house.transform.position);
        return true;
    }

    #endregion

    #region QUERIES

    public UpgradeData GetUpgrade() => UpgradesManager.Instance != null ? UpgradesManager.Instance.GetUpgrade(UpgradeId) : null;

    public RodBlock ProtectorOf(House house)
    {
        if (house == null || !blockOf.TryGetValue(house, out RodBlock block)) return null;
        return block.State == LotState.Built ? block : null;
    }

    public RodBlock BlockOfHouse(House house) => house != null && blockOf.TryGetValue(house, out RodBlock block) ? block : null;

    public bool AnyBuilt()
    {
        foreach (RodBlock block in blocks)
        {
            if (block.State == LotState.Built) return true;
        }
        return false;
    }

    public int FreeBlocks()
    {
        int free = 0;
        foreach (RodBlock block in blocks)
        {
            if (block.IsFree) free++;
        }
        return free;
    }

    // Parte dos raios que cairia neste quarteirão com um para-raio nele (os já prontos contam junto), pelas casas intactas.
    public float ShareIfBuilt(RodBlock candidate)
    {
        LocationsManager locations = LocationsManager.Instance;
        if (locations == null) return 0f;

        float weight = Mathf.Max(1f, Data.attraction), mine = 0f, total = 0f;
        foreach (House house in locations.Houses)
        {
            if (house.currentState != State.Intact) continue;
            RodBlock block = BlockOfHouse(house);
            float w = block == candidate || ProtectorOf(house) != null ? weight : 1f;
            total += w;
            if (block == candidate) mine += w;
        }
        return total > 0f ? mine / total : 0f;
    }

    #endregion
}
