using UnityEditor;
using UnityEngine;

//Monta os prefabs do Agente C pelo Editor, porque foram escritos sem o Unity aberto
//(mesmo padrão do Tools/Jam/Create Default Data, seção 6.4 do ARCHITECTURE.md)
public static class FeelPrefabsMenu
{
    private const string AudioFolder = "Assets/Systems/Audio";

    [MenuItem("Tools/Jam/Create Feel Prefabs")]
    public static void CreateMissing()
    {
        CreateAll(false);
    }

    [MenuItem("Tools/Jam/Recreate Feel Prefabs (sobrescreve)")]
    public static void Recreate()
    {
        if (!EditorUtility.DisplayDialog("Recriar prefabs do Feel", "Sobrescreve os prefabs do Agente C e perde ajustes feitos neles pelo Inspector.", "Recriar", "Cancelar")) return;

        CreateAll(true);
    }

    private static void CreateAll(bool overwrite)
    {
        CreateAudioManager(overwrite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateAudioManager(bool overwrite)
    {
        string path = AudioFolder + "/AudioManager.prefab";
        if (Skip(path, overwrite)) return;

        GameObject root = new GameObject("AudioManager");
        root.AddComponent<AudioManager>().AutoFillClips();
        SavePrefab(root, path);
    }

    private static bool Skip(string path, bool overwrite)
    {
        if (overwrite || AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return false;

        Debug.Log("Feel: já existe, mantido: " + path);
        return true;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("Feel: prefab criado em " + path);
    }
}
