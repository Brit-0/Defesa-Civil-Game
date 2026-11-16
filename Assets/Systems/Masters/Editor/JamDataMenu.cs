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
