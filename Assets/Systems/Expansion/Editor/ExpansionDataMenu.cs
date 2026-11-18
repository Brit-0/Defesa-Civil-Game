using UnityEditor;
using UnityEngine;

// Cria (ou atualiza) os dados da expansão: os upgrades base2 e depot2 e o ExpansionData com os terrenos.
// Os .asset já vêm no commit; este menu é para o caso de a importação falhar ou de querer voltar aos padrões.
// Preços escolhidos no simulador (docs/jam/sim/expansao.py, 30 sementes): 100% de vitória para o bom e o médio,
// a vitória atrasa no máximo 1 min com as quatro obras.
public static class ExpansionDataMenu
{
    private const string UpgradesFolder = "Assets/Resources/Upgrades";
    private const string DataFolder = "Assets/Systems/Expansion/Resources/Expansion";
    private const string SpriteFolder = "Assets/Systems/Expansion/Resources/Expansion/";

    [MenuItem("Tools/Jam/Expansão/Criar dados")]
    public static void CreateData()
    {
        UpgradeData base2 = Upgrade("Base2", ExpansionManager.BaseUpgradeId, "Nova base",
            "Constrói uma base da Guarda num terreno vazio. As casas mais perto dela passam a ser atendidas de lá: a equipe anda menos.",
            "Lot_IconBase", 7, 2, 150, 1.6f);
        UpgradeData depot2 = Upgrade("Depot2", ExpansionManager.DepotUpgradeId, "Novo armazém",
            "Constrói um armazém num terreno vazio, ao lado de uma base nova: o material fica mais perto de quem conserta.",
            "Lot_IconDepot", 7, 2, 300, 1.6f); // BALANCEAMENTO_V5: 90 (muito bom pelo preço)

        string dataPath = DataFolder + "/ExpansionData.asset";
        var data = AssetDatabase.LoadAssetAtPath<ExpansionData>(dataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ExpansionData>();
            AssetDatabase.CreateAsset(data, dataPath);
        }
        else
        {
            data.lots = ExpansionData.DefaultLots();
            EditorUtility.SetDirty(data);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Expansão: {base2.name} ({base2.baseCost}), {depot2.name} ({depot2.baseCost}) e {data.lots.Count} terrenos prontos.");
    }

    private static UpgradeData Upgrade(string assetName, string id, string displayName, string description, string icon,
        int order, int maxLevel, int baseCost, float costGrowth)
    {
        string path = $"{UpgradesFolder}/{assetName}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<UpgradeData>();
            AssetDatabase.CreateAsset(asset, path);
        }
        asset.id = id;
        asset.displayName = displayName;
        asset.description = description;
        asset.icon = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + icon + ".png");
        asset.order = order;
        asset.category = UpgradeCategory.Expansion;
        asset.maxLevel = maxLevel;
        asset.baseCost = baseCost;
        asset.costGrowth = costGrowth;
        asset.effectPerLevel = 1f;
        EditorUtility.SetDirty(asset);
        return asset;
    }
}
