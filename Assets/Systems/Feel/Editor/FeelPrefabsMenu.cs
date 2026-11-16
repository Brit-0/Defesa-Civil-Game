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
    private const string FeedbacksFolder = "Assets/Systems/Feedbacks";

    private const string ThemePath = UIFolder + "/Resources/" + FeelUITheme.ResourcePath + ".asset";

    [MenuItem("Tools/Jam/Create Feel Prefabs")]
    public static void CreateAll()
    {
        //Sprites de UI do jogo para os painéis do C: numa pasta Resources, para o FeelUI carregar sem cena
        EnsureTheme();

        //Som: solto em Scripts/. Atravessa o reinício da cena.
        EnsurePrefab(AudioFolder + "/AudioManager.prefab", "AudioManager",
            root => root.GetComponent<AudioManager>().AutoFillClips(),
            typeof(AudioManager), typeof(GameAudio));

        //Juice que só escuta eventos e acha o HUD pelo nome: solto em Scripts/
        EnsurePrefab(FeelFolder + "/Feel.prefab", "Feel", null,
            typeof(HudJuice), typeof(MoneyPopups), typeof(CameraShaker), typeof(WorkerFeel), typeof(ActionFeedback),
            typeof(WeatherFx), typeof(ChaosVignette), typeof(FeelParticles));

        //HUD: soltos dentro do UICanvas (raiz com RectTransform). Cada um monta a própria UI no Awake.
        EnsurePrefab(UIFolder + "/ChaosBar.prefab", "ChaosBar", null, typeof(RectTransform), typeof(ChaosBar));
        EnsurePrefab(UIFolder + "/DisasterBanner.prefab", "DisasterBanner", null, typeof(RectTransform), typeof(DisasterBanner));
        EnsurePrefab(FeedbacksFolder + "/TutorialTips.prefab", "TutorialTips", null, typeof(RectTransform), typeof(TutorialTips));
        EnsurePrefab(UIFolder + "/PauseMenu.prefab", "PauseMenu", null, typeof(RectTransform), typeof(PauseMenu));
        EnsurePrefab(UIFolder + "/EndScreen.prefab", "EndScreen", null, typeof(RectTransform), typeof(EndScreen));
        EnsurePrefab(UIFolder + "/MainMenu.prefab", "MainMenu", null, typeof(RectTransform), typeof(MainMenu));

        //Transição de tela: solta em Scripts/ (opcional; sem ela o jogo cria uma com os valores padrão)
        EnsurePrefab(UIFolder + "/ScreenTransition.prefab", "ScreenTransition", null, typeof(RectTransform), typeof(ScreenTransition));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void EnsureTheme()
    {
        FeelUITheme theme = AssetDatabase.LoadAssetAtPath<FeelUITheme>(ThemePath);
        bool exists = theme != null;

        if (!exists)
        {
            if (!AssetDatabase.IsValidFolder(UIFolder + "/Resources")) AssetDatabase.CreateFolder(UIFolder, "Resources");

            theme = ScriptableObject.CreateInstance<FeelUITheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
        }

        theme.AutoFillSprites();

        Debug.Log("Feel: tema de UI " + (exists ? "completado: " : "criado: ") + ThemePath);
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
