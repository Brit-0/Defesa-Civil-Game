using System;
using UnityEditor;
using UnityEngine;

//Monta os prefabs do Agente C pelo Editor, porque foram escritos sem o Unity aberto
//(mesmo padrão do Tools/Jam/Create Default Data, seção 6.4 do ARCHITECTURE.md).
//Pode rodar de novo a cada entrega: cria o que falta e completa o que existe, sem apagar ajustes.
public static class FeelPrefabsMenu
{
    private const string AudioFolder = "Assets/Systems/Audio";
    private const string FeelFolder = "Assets/Systems/Feel";
    private const string UIFolder = "Assets/Systems/UI";

    [MenuItem("Tools/Jam/Create Feel Prefabs")]
    public static void CreateAll()
    {
        //Som: solto em Scripts/. Atravessa o reinício da cena.
        EnsurePrefab(AudioFolder + "/AudioManager.prefab", "AudioManager",
            root => root.GetComponent<AudioManager>().AutoFillClips(),
            typeof(AudioManager), typeof(GameAudio));

        //Juice que só escuta eventos e acha o HUD pelo nome: solto em Scripts/
        EnsurePrefab(FeelFolder + "/Feel.prefab", "Feel", null,
            typeof(HudJuice), typeof(MoneyPopups), typeof(CameraShaker), typeof(WorkerFeel));

        //HUD: soltos dentro do UICanvas (raiz com RectTransform). Cada um monta a própria UI no Awake.
        EnsurePrefab(UIFolder + "/ChaosBar.prefab", "ChaosBar", null, typeof(RectTransform), typeof(ChaosBar));
        EnsurePrefab(UIFolder + "/DisasterBanner.prefab", "DisasterBanner", null, typeof(RectTransform), typeof(DisasterBanner));
        EnsurePrefab(UIFolder + "/EndScreen.prefab", "EndScreen", null, typeof(RectTransform), typeof(EndScreen));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsurePrefab(string path, string rootName, Action<GameObject> setup, params Type[] components)
    {
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(rootName);

        foreach (Type component in components)
        {
            if (root.GetComponent(component) == null) root.AddComponent(component);
        }

        setup?.Invoke(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);

        if (exists) PrefabUtility.UnloadPrefabContents(root);
        else UnityEngine.Object.DestroyImmediate(root);

        Debug.Log("Feel: prefab " + (exists ? "completado: " : "criado: ") + path);
    }
}
