using System.Collections.Generic;
using System.IO;
using DefesaCivil.CityGen;
using UnityEditor;
using UnityEngine;

// Cria (ou atualiza) os dados do para-raio: o upgrade "rod" e o LightningRodData com os quarteirões do city_layout.json.
// Os .asset já vêm no commit; este menu serve para regerar os quarteirões depois de um mapa novo ou voltar aos padrões.
// Preço e atração escolhidos no simulador (docs/jam/sim/pararaio.py, 30 sementes): ver docs/jam/PARARAIO.md.
public static class LightningRodDataMenu
{
    public const string LayoutPath = "Assets/_Project/Data/Cities/city_layout.json";
    private const string UpgradesFolder = "Assets/Resources/Upgrades";
    private const string DataPath = "Assets/Systems/LightningRod/Resources/LightningRod/LightningRodData.asset";
    private const string IconPath = "Assets/Systems/LightningRod/Resources/LightningRod/Rod_Icon.png";

    [MenuItem("Tools/Jam/Para-raio/Criar dados")]
    public static void CreateData()
    {
        UpgradeData rod = Upgrade();

        var data = AssetDatabase.LoadAssetAtPath<LightningRodData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<LightningRodData>();
            AssetDatabase.CreateAsset(data, DataPath);
        }
        data.blocks = BlocksFromLayout(File.ReadAllText(LayoutPath));
        EditorUtility.SetDirty(data);

        AssetDatabase.SaveAssets();
        Debug.Log($"Para-raio: {rod.name} ({rod.baseCost}, até {rod.maxLevel}) e {data.blocks.Count} quarteirões prontos.");
    }

    // Polígonos "Block" do mapa como retângulos (as praças ficam de fora: não têm casa).
    public static List<RodBlockSlot> BlocksFromLayout(string json)
    {
        CityLayout layout = CityLayout.FromJson(json);
        var blocks = new List<RodBlockSlot>();
        foreach (PolygonData polygon in layout.polygons)
        {
            if (polygon.kind != PolygonKind.Block || polygon.points.Count == 0) continue;
            Vector2 min = polygon.points[0], max = polygon.points[0];
            foreach (Vec2 point in polygon.points)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            blocks.Add(new RodBlockSlot { id = polygon.id, district = polygon.district, min = min, max = max });
        }
        return blocks;
    }

    private static UpgradeData Upgrade()
    {
        string path = $"{UpgradesFolder}/Rod.asset";
        var asset = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<UpgradeData>();
            AssetDatabase.CreateAsset(asset, path);
        }
        asset.id = LightningRodManager.UpgradeId;
        asset.displayName = "Para-raio";
        asset.description = "Constrói um para-raio no quarteirão que você escolher: ele puxa os raios da tempestade e protege as casas de lá para sempre.";
        asset.icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
        asset.order = 8;
        asset.category = UpgradeCategory.Expansion;
        asset.maxLevel = 3;
        asset.baseCost = 80;
        asset.costGrowth = 1.5f;
        asset.effectPerLevel = 1f;
        EditorUtility.SetDirty(asset);
        return asset;
    }
}
