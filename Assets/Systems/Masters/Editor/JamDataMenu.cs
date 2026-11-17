using UnityEditor;
using UnityEngine;

// Cria os dados padrão da jam (GDD, seção 13). Quem roda é o MEGABRAIN, que depois commita os .asset.
// Create Default Data: cria só o que falta; nunca mexe em asset que já existe (não apaga ajuste feito à mão).
//   Rodar duas vezes não duplica: upgrade e desastre são reconhecidos pelo id, mesmo com o arquivo renomeado.
// Reset Data To Defaults: reescreve os campos que o menu conhece com os valores do GDD, depois de confirmar.
//   Campos que o menu não conhece (os ícones) nunca são tocados.
public static class JamDataMenu
{
    private const string AssetsFolder = "Assets";
    private const string ResourcesName = "Resources";
    private const string ResourcesFolder = AssetsFolder + "/" + ResourcesName;
    private const string BalanceFolder = "Balance";
    private const string BalanceAsset = "BalanceData";
    private const string DisastersFolder = "Disasters";
    private const string UpgradesFolder = "Upgrades";

    [MenuItem("Tools/Jam/Create Default Data")]
    public static void CreateDefaultData()
    {
        Run(false);
    }

    [MenuItem("Tools/Jam/Reset Data To Defaults")]
    public static void ResetDataToDefaults()
    {
        if (!EditorUtility.DisplayDialog("Dados da jam", "Reescrever os dados da jam com os valores padrão do GDD? Ajustes feitos à mão nos campos de balanceamento serão perdidos.", "Reescrever", "Cancelar")) return;

        Run(true);
    }

    private static void Run(bool overwrite)
    {
        int created = 0;
        int updated = 0;

        CreateBalance(overwrite, ref created, ref updated);

        CreateDisaster("Earthquake", "earthquake", DisasterType.Earthquake, "Terremoto", overwrite, ref created, ref updated);
        CreateDisaster("Storm", "storm", DisasterType.Storm, "Tempestade", overwrite, ref created, ref updated);
        CreateDisaster("Fire", "fire", DisasterType.Fire, "Incêndio", overwrite, ref created, ref updated);

        // Tabela da seção 7 do GDD: asset, id, nome, descrição, ordem, níveis, custo base, crescimento, efeito por nível.
        // Calibrados no T25 pelo simulador docs/jam/sim (GDD: Cart 60, Automation 150 e 2,6, Income 200, Goal 5.000).
        CreateUpgrade("Workers", UpgradeIds.Workers, "Contratar trabalhador", "+1 trabalhador", 0, 12, 30, 1.45f, 1f, overwrite, ref created, ref updated);
        CreateUpgrade("Speed", UpgradeIds.Speed, "Botas de corrida", "+12% de velocidade de caminhada", 1, 8, 25, 1.5f, 0.12f, overwrite, ref created, ref updated);
        CreateUpgrade("Tools", UpgradeIds.Tools, "Ferramentas melhores", "-10% de tempo de trabalho", 2, 8, 40, 1.5f, 0.1f, overwrite, ref created, ref updated);
        CreateUpgrade("Cart", UpgradeIds.Cart, "Carrinho de mão", "+1 material por trabalhador no Armazém", 3, 4, 25, 1.8f, 1f, overwrite, ref created, ref updated);
        CreateUpgrade("Reward", UpgradeIds.Reward, "Seguro municipal", "+25% de dinheiro por conserto", 4, 10, 50, 1.55f, 0.25f, overwrite, ref created, ref updated);
        CreateUpgrade("Automation", UpgradeIds.Automation, "Automação", "Cada nível tira um passo do despacho: base automática, equipe automática e coleta automática. O despacho sozinho é das guaritas.", 5, 3, 80, 2f, 1f, overwrite, ref created, ref updated);
        CreateUpgrade("Income", UpgradeIds.Income, "Repasse da Prefeitura", "A Prefeitura paga por casa intacta de tempos em tempos. Paga menos quando o caos está alto.", 6, 5, 150, 2f, 1f, overwrite, ref created, ref updated);
        CreateUpgrade("Goal", UpgradeIds.Goal, "Centro de Operações", "Objetivo final: a cidade ganha uma central de Defesa Civil. Comprar é vencer.", 8, 1, 2500, 1f, 0f, overwrite, ref created, ref updated);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Dados da jam: {created} asset(s) criado(s), {updated} reescrito(s).");
    }

    #region BALANCE

    private static void CreateBalance(bool overwrite, ref int created, ref int updated)
    {
        BalanceData asset = LoadOrCreate<BalanceData>(BalanceFolder, BalanceAsset, null, out bool isNew);
        if (asset == null) return;
        if (isNew)
        {
            created++;
            return;
        }
        if (!overwrite) return;

        // Os padrões moram na classe: copia os campos de uma instância nova, sem tocar no nome do asset.
        BalanceData defaults = ScriptableObject.CreateInstance<BalanceData>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(defaults), asset);
        Object.DestroyImmediate(defaults);
        EditorUtility.SetDirty(asset);
        updated++;
    }

    #endregion

    #region DISASTERS

    // O menu conhece id, type e displayName. O ícone é ligado à mão e nunca é tocado.
    private static void CreateDisaster(string assetName, string id, DisasterType type, string displayName, bool overwrite, ref int created, ref int updated)
    {
        DisasterData asset = LoadOrCreate<DisasterData>(DisastersFolder, assetName, candidate => candidate.id == id, out bool isNew);
        if (asset == null || (!isNew && !overwrite)) return;

        asset.id = id;
        asset.type = type;
        asset.displayName = displayName;
        EditorUtility.SetDirty(asset);

        if (isNew) created++;
        else updated++;
    }

    #endregion

    #region UPGRADES

    // O menu conhece tudo menos o ícone, que é ligado à mão e nunca é tocado.
    // Aba do painel de melhorias: equipe é o padrão; renda e automação juntas; o objetivo final fica com a expansão
    private static UpgradeCategory CategoryFor(string id)
    {
        switch (id)
        {
            case UpgradeIds.Automation:
            case UpgradeIds.Income:
            case UpgradeIds.Reward:
                return UpgradeCategory.Automation;
            case UpgradeIds.Base2:
            case UpgradeIds.Goal:
                return UpgradeCategory.Expansion;
            default:
                return UpgradeCategory.Workers;
        }
    }

    private static void CreateUpgrade(string assetName, string id, string displayName, string description, int order, int maxLevel, int baseCost, float costGrowth, float effectPerLevel, bool overwrite, ref int created, ref int updated)
    {
        UpgradeData asset = LoadOrCreate<UpgradeData>(UpgradesFolder, assetName, candidate => candidate.id == id, out bool isNew);
        if (asset == null || (!isNew && !overwrite)) return;

        asset.id = id;
        asset.displayName = displayName;
        asset.description = description;
        asset.order = order;
        asset.category = CategoryFor(id);
        asset.maxLevel = maxLevel;
        asset.baseCost = baseCost;
        asset.costGrowth = costGrowth;
        asset.effectPerLevel = effectPerLevel;
        EditorUtility.SetDirty(asset);

        if (isNew) created++;
        else updated++;
    }

    #endregion

    #region HELPERS

    // Devolve o asset que já existe (pelo caminho ou, se foi renomeado, pelo id) ou cria um novo.
    private static T LoadOrCreate<T>(string folder, string assetName, System.Predicate<T> isSame, out bool isNew) where T : ScriptableObject
    {
        string folderPath = ResourcesFolder + "/" + folder;
        string path = folderPath + "/" + assetName + ".asset";
        isNew = false;

        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;

        if (isSame != null && AssetDatabase.IsValidFolder(folderPath))
        {
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folderPath }))
            {
                T candidate = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (candidate != null && isSame(candidate)) return candidate;
            }
        }

        // Já existe outra coisa nesse caminho: não sobrescreve.
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
        {
            Debug.LogError($"Dados da jam: {path} já existe e não é um {typeof(T).Name}. Nada foi alterado nele.");
            return null;
        }

        if (!AssetDatabase.IsValidFolder(ResourcesFolder)) AssetDatabase.CreateFolder(AssetsFolder, ResourcesName);
        if (!AssetDatabase.IsValidFolder(folderPath)) AssetDatabase.CreateFolder(ResourcesFolder, folder);
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"Dados da jam: criado {path}");
        isNew = true;
        return asset;
    }

    #endregion
}
