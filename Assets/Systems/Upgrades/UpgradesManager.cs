using System.Collections.Generic;
using UnityEngine;

// IDs dos upgrades (GDD, seção 7). O id fica no campo UpgradeData.id do asset.
public static class UpgradeIds
{
    public const string Workers = "workers";
    public const string Speed = "speed";
    public const string Tools = "tools";
    public const string Cart = "cart";
    public const string Reward = "reward";
    public const string Automation = "automation";
    public const string Income = "income";
    public const string Base2 = "base2";
    public const string Goal = "goal";
}

// O que cada nível do upgrade automation liga (GDD, seção 7).
public static class AutomationLevels
{
    public const int AutoBase = 1;      // Central de rádio: base escolhida sozinha
    public const int AutoTeam = 2;      // Protocolo padrão: equipe dimensionada sozinha
    public const int AutoCollect = 3;   // Coleta automática
    public const int AutoDispatch = 4;  // Despacho automático
}

// Níveis, custo e compra dos upgrades. Adicionar um upgrade = criar um asset em Resources/Upgrades.
public class UpgradesManager : MonoBehaviour
{
    public static UpgradesManager Instance;

    private const string UpgradesPath = "Upgrades";

    [Header("TAB")]
    [SerializeField] private UpgradeButton buttonPf;
    [SerializeField] private Transform buttonsParent;
    [SerializeField] private bool clearPlaceholders = true;

    private readonly List<UpgradeData> upgrades = new();
    private readonly Dictionary<string, UpgradeData> upgradesById = new();
    private readonly Dictionary<string, int> levels = new();
    private readonly Dictionary<UpgradeData, UpgradeButton> buttons = new();

    public IReadOnlyList<UpgradeData> Upgrades => upgrades;

    private void Awake()
    {
        Instance = this;
        LoadUpgrades();
    }

    private void Start()
    {
        BuildTab();
    }

    private void LoadUpgrades()
    {
        UpgradeData[] loaded = Resources.LoadAll<UpgradeData>(UpgradesPath);
        if (loaded.Length == 0)
        {
            Debug.LogWarning($"Nenhum UpgradeData em Resources/{UpgradesPath}: aba de upgrades vazia. Rode Tools/Jam/Create Default Data.");
        }

        foreach (UpgradeData upgrade in loaded)
        {
            if (string.IsNullOrEmpty(upgrade.id) || upgradesById.ContainsKey(upgrade.id))
            {
                Debug.LogWarning($"UpgradeData {upgrade.name} sem id ou com id repetido: ignorado.");
                continue;
            }

            upgrades.Add(upgrade);
            upgradesById[upgrade.id] = upgrade;
            levels[upgrade.id] = 0;
        }

        upgrades.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.id, b.id));
    }

    // Preenche a aba em runtime, um botão por asset.
    private void BuildTab()
    {
        if (buttonPf == null || buttonsParent == null)
        {
            Debug.LogWarning("UpgradesManager sem buttonPf ou buttonsParent: os upgrades funcionam, mas a aba não é preenchida.");
            return;
        }

        if (clearPlaceholders)
        {
            foreach (Transform child in buttonsParent)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (UpgradeData upgrade in upgrades)
        {
            UpgradeButton button = Instantiate(buttonPf, buttonsParent);
            button.Set(upgrade);
            buttons[upgrade] = button;
        }
    }

    #region QUERIES

    public UpgradeData GetUpgrade(string id)
    {
        return upgradesById.TryGetValue(id, out UpgradeData upgrade) ? upgrade : null;
    }

    public int GetLevel(string id)
    {
        return levels.TryGetValue(id, out int level) ? level : 0;
    }

    public int GetLevel(UpgradeData upgrade)
    {
        return upgrade != null ? GetLevel(upgrade.id) : 0;
    }

    // Efeito acumulado: effectPerLevel × nível. Upgrade que não existe vale 0.
    public float GetEffect(string id)
    {
        UpgradeData upgrade = GetUpgrade(id);
        return upgrade != null ? upgrade.effectPerLevel * GetLevel(id) : 0f;
    }

    public bool IsMaxLevel(UpgradeData upgrade)
    {
        return GetLevel(upgrade) >= upgrade.maxLevel;
    }

    // Custo do próximo nível: custo base × crescimento^nível, arredondado.
    public int GetCost(UpgradeData upgrade)
    {
        return Mathf.RoundToInt(upgrade.baseCost * Mathf.Pow(upgrade.costGrowth, GetLevel(upgrade)));
    }

    public bool CanBuy(UpgradeData upgrade)
    {
        if (upgrade == null || !upgradesById.ContainsKey(upgrade.id)) return false;

        return !IsMaxLevel(upgrade) && ResourcesManager.Money >= GetCost(upgrade);
    }

    #endregion

    #region PURCHASE

    public bool TryBuy(UpgradeData upgrade)
    {
        if (upgrade == null) return false;

        Vector3 position = buttons.TryGetValue(upgrade, out UpgradeButton button) && button != null ? button.transform.position : transform.position;
        return TryBuy(upgrade, position);
    }

    // Compra negada dispara ActionDenied (NoMoney ou MaxLevel) na posição informada.
    public bool TryBuy(UpgradeData upgrade, Vector3 worldPosition)
    {
        if (upgrade == null || !upgradesById.ContainsKey(upgrade.id)) return false;
        if (GameManager.InputBlocked) return false;

        if (IsMaxLevel(upgrade))
        {
            GameEvents.RaiseActionDenied(DeniedReason.MaxLevel, worldPosition);
            return false;
        }

        int cost = GetCost(upgrade);
        if (ResourcesManager.Money < cost)
        {
            GameEvents.RaiseActionDenied(DeniedReason.NoMoney, worldPosition);
            return false;
        }

        ResourcesManager.Money -= cost;
        int newLevel = GetLevel(upgrade) + 1;
        levels[upgrade.id] = newLevel;

        GameEvents.RaiseUpgradePurchased(upgrade, newLevel);
        ApplyImmediateEffect(upgrade);
        return true;
    }

    // A maioria dos efeitos é lida por Stats na hora do uso. Aqui fica só o que acontece no ato da compra.
    private static void ApplyImmediateEffect(UpgradeData upgrade)
    {
        switch (upgrade.id)
        {
            case UpgradeIds.Workers:
                ResourcesManager.Workers += Mathf.RoundToInt(upgrade.effectPerLevel);
                break;
            // Centro de Operações: comprar o objetivo final é a vitória (GDD, seção 9).
            case UpgradeIds.Goal:
                if (GameManager.Instance != null) GameManager.Instance.Win();
                break;
            default:
                break;
        }
    }

    #endregion
}
