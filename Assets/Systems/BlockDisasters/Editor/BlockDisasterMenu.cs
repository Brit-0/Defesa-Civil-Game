using System.IO;
using UnityEditor;
using UnityEngine;

// Cria (ou atualiza) os dados da enchente e do meteoro (docs/jam/ENCHENTE_METEORO.md): o BlockDisasterData com os
// quarteirões do city_layout.json e os ícones dos chamados, e os dois DisasterData do sorteio (Flood e Meteor).
// Os .asset já vêm no commit; o menu serve para regerar os quarteirões depois de um mapa novo. Números já salvos no
// BlockDisasterData ficam como estão.
public static class BlockDisasterMenu
{
    private const string DataPath = "Assets/Systems/BlockDisasters/Resources/BlockDisasters/BlockDisasterData.asset";
    private const string DisastersFolder = "Assets/Resources/Disasters";
    private const string IconsFolder = "Assets/Arts/UI/Chamados/";

    [MenuItem("Tools/Jam/Enchente e meteoro/Criar dados")]
    public static void CreateData()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataPath));
        var data = AssetDatabase.LoadAssetAtPath<BlockDisasterData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BlockDisasterData>();
            AssetDatabase.CreateAsset(data, DataPath);
        }
        data.blocks = LightningRodDataMenu.BlocksFromLayout(File.ReadAllText(LightningRodDataMenu.LayoutPath));
        data.callFloodIcon = Icon("call_flood.png");
        data.callRebuildIcon = Icon("call_rebuild.png");
        EditorUtility.SetDirty(data);

        DisasterData flood = Disaster("Flood", "flood", DisasterType.Flood, "Enchente", Icon("disaster_flood.png"));
        DisasterData meteor = Disaster("Meteor", "meteor", DisasterType.Meteor, "Meteoro", Icon("disaster_meteor.png"));

        AssetDatabase.SaveAssets();
        Debug.Log($"Enchente e meteoro: {data.blocks.Count} quarteirões, {flood.displayName} e {meteor.displayName} no sorteio.");
    }

    [MenuItem("Tools/Jam/Enchente e meteoro/Mostrar as dicas de novo")]
    public static void ResetTips()
    {
        foreach (string key in BlockDisasterTips.Keys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        Debug.Log("Enchente e meteoro: as 4 dicas aparecem de novo na próxima vez.");
    }

    private static DisasterData Disaster(string fileName, string id, DisasterType type, string displayName, Sprite icon)
    {
        string path = $"{DisastersFolder}/{fileName}.asset";
        var disaster = AssetDatabase.LoadAssetAtPath<DisasterData>(path);
        if (disaster == null)
        {
            disaster = ScriptableObject.CreateInstance<DisasterData>();
            AssetDatabase.CreateAsset(disaster, path);
        }
        disaster.id = id;
        disaster.type = type;
        disaster.displayName = displayName;
        if (icon != null) disaster.icon = icon;
        EditorUtility.SetDirty(disaster);
        return disaster;
    }

    private static Sprite Icon(string file)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconsFolder + file);
        if (sprite == null) Debug.LogWarning($"Ícone {IconsFolder}{file} não encontrado (importado como Sprite?).");
        return sprite;
    }
}
