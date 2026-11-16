using UnityEditor;
using UnityEngine;

// Cria os dados padrão da jam (GDD, seção 13). Quem roda é o MEGABRAIN, que depois commita os .asset.
// Create Default Data: cria só o que falta; nunca mexe em asset que já existe (não apaga ajuste feito à mão).
// Reset Data To Defaults: reescreve os campos que o menu conhece com os valores do GDD, depois de confirmar.
public static class JamDataMenu
{
    private const string ResourcesFolder = "Assets/Resources";
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

        // Tabela da seção 7 do GDD: asset, id, nome, descrição, ordem, níveis, custo base, crescimento, efeito por nível.
        CreateUpgrade("Workers", UpgradeIds.Workers, "Contratar trabalhador", "+1 trabalhador", 0, 12, 30, 1.45f, 1f, overwrite, ref created, ref updated);
        CreateUpgrade("Speed", UpgradeIds.Speed, "Botas de corrida", "+12% de velocidade de caminhada", 1, 8, 25, 1.5f, 0.12f, overwrite, ref created, ref updated);
        CreateUpgrade("Tools", UpgradeIds.Tools, "Ferramentas melhores", "-10% de tempo de trabalho", 2, 8, 40, 1.5f, 0.1f, overwrite, ref created, ref updated);
        CreateUpgrade("Cart", UpgradeIds.Cart, "Carrinho de mão", "+1 material por trabalhador no Armazém", 3, 4, 60, 1.8f, 1f, overwrite, ref created, ref updated);
        CreateUpgrade("Reward", UpgradeIds.Reward, "Seguro municipal", "+25% de dinheiro por conserto", 4, 10, 50, 1.55f, 0.25f, overwrite, ref created, ref updated);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Dados da jam: {created} asset(s) criado(s), {updated} reescrito(s).");
    }

    #region BALANCE

    private static void CreateBalance(bool overwrite, ref int created, ref int updated)
    {
        BalanceData asset = LoadOrCreate<BalanceData>(BalanceFolder, BalanceAsset, out bool isNew);
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
        DisasterData asset = LoadOrCreate<DisasterData>(DisastersFolder, assetName, out bool isNew);
        if (!isNew && !overwrite) return;

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
    private static void CreateUpgrade(string assetName, string id, string displayName, string description, int order, int maxLevel, int baseCost, float costGrowth, float effectPerLevel, bool overwrite, ref int created, ref int updated)
    {
        UpgradeData asset = LoadOrCreate<UpgradeData>(UpgradesFolder, assetName, out bool isNew);
        if (!isNew && !overwrite) return;

        asset.id = id;
        asset.displayName = displayName;
        asset.description = description;
        asset.order = order;
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

    private static T LoadOrCreate<T>(string folder, string assetName, out bool isNew) where T : ScriptableObject
    {
        string folderPath = ResourcesFolder + "/" + folder;
        string path = folderPath + "/" + assetName + ".asset";

        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        isNew = asset == null;
        if (isNew)
        {
            if (!AssetDatabase.IsValidFolder(folderPath)) AssetDatabase.CreateFolder(ResourcesFolder, folder);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
        }
        return asset;
    }

    #endregion
}
