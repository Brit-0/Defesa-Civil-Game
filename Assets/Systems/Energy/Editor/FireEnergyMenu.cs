using UnityEditor;
using UnityEngine;

// Fogo e energia (docs/jam/ENERGIA_FOGO.md): cria o que falta, sem mexer no que já existe.
// Todos já vêm commitados; o menu é para refazer um que foi apagado.
//   Resources/Balance/FireEnergyData.asset — números do jato e da energia
//   Resources/Balance/FuseBoxData.asset    — números do quadro de força (minigame do RELIGAR)
//   Resources/Upgrades/Generator.asset     — o upgrade do gerador
//   Systems/Energy/Resources/FireEnergy/FireEnergy.prefab — componente de ligação com os 5 sistemas
public static class FireEnergyMenu
{
    private const string DataPath = "Assets/Resources/Balance/FireEnergyData.asset";
    private const string FuseDataPath = "Assets/Resources/Balance/FuseBoxData.asset";
    private const string GeneratorPath = "Assets/Resources/Upgrades/Generator.asset";
    private const string PrefabFolder = "Assets/Systems/Energy/Resources/FireEnergy";
    private const string PrefabPath = PrefabFolder + "/FireEnergy.prefab";

    [MenuItem("Tools/Jam/Create Fire And Energy Data")]
    public static void Create()
    {
        int created = 0;

        if (AssetDatabase.LoadAssetAtPath<FireEnergyData>(DataPath) == null)
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<FireEnergyData>(), DataPath);
            created++;
        }

        if (AssetDatabase.LoadAssetAtPath<FuseBoxData>(FuseDataPath) == null)
        {
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<FuseBoxData>(), FuseDataPath);
            created++;
        }

        if (AssetDatabase.LoadAssetAtPath<UpgradeData>(GeneratorPath) == null)
        {
            UpgradeData generator = ScriptableObject.CreateInstance<UpgradeData>();
            generator.id = EnergyIds.Generator;
            generator.displayName = "Gerador";
            generator.description = "Cada nível dá mais energia para bases, armazéns e automação.";
            generator.order = 7;
            generator.category = UpgradeCategory.Energy;
            generator.maxLevel = 4;
            generator.baseCost = 60;
            generator.costGrowth = 1.8f;
            generator.effectPerLevel = 3f;
            generator.icon = AssetDatabase.LoadAssetAtPath<Sprite>(PrefabFolder + "/" + FireEnergyArt.Generator + ".png");
            AssetDatabase.CreateAsset(generator, GeneratorPath);
            created++;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets/Systems/Energy/Resources", "FireEnergy");

            GameObject link = new GameObject("FireEnergy");
            link.AddComponent<FireEnergyLink>();
            link.AddComponent<EnergyManager>();
            link.AddComponent<EnergyHud>();
            link.AddComponent<EnergyMarkers>();
            link.AddComponent<FireMinigame>();
            link.AddComponent<EnergyTutorial>();
            PrefabUtility.SaveAsPrefabAsset(link, PrefabPath);
            Object.DestroyImmediate(link);
            created++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[FireEnergyMenu] {created} criado(s); o que já existia ficou como estava.");
    }

    [MenuItem("Tools/Jam/Reset Energy Tutorial")]
    public static void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(EnergyTutorial.DoneKey);
        PlayerPrefs.Save();
        Debug.Log("[FireEnergyMenu] Tutorial da energia volta a aparecer no próximo Play.");
    }
}
