using UnityEditor;
using UnityEngine;

// Cria (ou volta aos padrões) os dados da guarita (GUARITAS.md): o upgrade "outpost" na aba Automação e o OutpostData.
// Os .asset já vêm no commit. Preço escolhido no simulador (docs/jam/sim/guarita.py, 30 sementes).
public static class OutpostDataMenu
{
    private const string UpgradePath = "Assets/Resources/Upgrades/Outpost.asset";
    public const string DataPath = "Assets/Systems/Outpost/Resources/Outpost/OutpostData.asset";
    public const string IconPath = "Assets/Systems/Outpost/Resources/Outpost/Outpost_Icon.png";

    // Sem teto fixo: o jogo corta em um por quarteirão com casa (UpgradesManager.SetLevelCap).
    public const int NoFixedCap = 99;

    [MenuItem("Tools/Jam/Guarita/Criar dados")]
    public static void CreateData()
    {
        UpgradeData upgrade = Upgrade();
        var data = AssetDatabase.LoadAssetAtPath<OutpostData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<OutpostData>();
            AssetDatabase.CreateAsset(data, DataPath);
        }
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log($"Guarita: {upgrade.name} ({upgrade.baseCost}, ×{upgrade.costGrowth}) e {data.name} prontos.");
    }

    private static UpgradeData Upgrade()
    {
        var asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(UpgradePath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<UpgradeData>();
            AssetDatabase.CreateAsset(asset, UpgradePath);
        }
        asset.id = OutpostManager.UpgradeId;
        asset.displayName = "Guarita";
        asset.description = "Constrói uma guarita no quarteirão que você escolher: os chamados de lá saem sozinhos, da base mais perto, com quem estiver livre.";
        asset.icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
        asset.order = 6;
        asset.category = UpgradeCategory.Automation;
        asset.maxLevel = NoFixedCap;
        asset.baseCost = 90;
        asset.costGrowth = 1.25f;
        asset.effectPerLevel = 1f;
        EditorUtility.SetDirty(asset);
        return asset;
    }
}
