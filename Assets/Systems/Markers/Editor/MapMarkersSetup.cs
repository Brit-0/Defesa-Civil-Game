using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Importa os sprites de Tools/PixelProps/markers_kit.py e liga o MapMarkers na cena aberta (objeto "[Markers]").
// Pode rodar de novo: só reimporta e preenche as referências.
public static class MapMarkersSetup
{
    private const string Folder = "Assets/Arts/UI/Markers";
    private const string FontPath = "Assets/Fonts/ModernDOS.asset";
    private const string ObjectName = "[Markers]";

    private static readonly (string field, string sprite)[] Sprites =
    {
        ("discSpr", "Mk_Disc"), ("fillSpr", "Mk_Fill"), ("ringSpr", "Mk_Ring"), ("arrowSpr", "Mk_Arrow"),
        ("badgeSpr", "Mk_Badge"), ("plateSpr", "Mk_Plate"), ("fireIcon", "Mk_Icon_Fire"), ("damageIcon", "Mk_Icon_Damage"),
        ("stormIcon", "Mk_Icon_Storm"), ("doneIcon", "Mk_Icon_Done"), ("guardIcon", "Mk_Icon_Guard"), ("depotIcon", "Mk_Icon_Depot"),
    };

    [MenuItem("Tools/City/Ligar Marcadores")]
    public static void Setup()
    {
        AssetDatabase.Refresh();
        foreach (var (_, sprite) in Sprites) Import($"{Folder}/{sprite}.png", sprite == "Mk_Plate" ? 4 : 0);

        MapMarkers markers = Object.FindAnyObjectByType<MapMarkers>(FindObjectsInactive.Include);
        if (markers == null) markers = new GameObject(ObjectName).AddComponent<MapMarkers>();

        var so = new SerializedObject(markers);
        int missing = 0;
        foreach (var (field, sprite) in Sprites)
        {
            var asset = AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{sprite}.png");
            if (asset == null) missing++;
            so.FindProperty(field).objectReferenceValue = asset;
        }
        so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(markers.gameObject.scene);
        EditorSceneManager.SaveScene(markers.gameObject.scene);
        Debug.Log($"[Markers] MapMarkers ligado em \"{markers.name}\": {Sprites.Length - missing} de {Sprites.Length} sprites" +
                  (missing > 0 ? " (rode Tools/PixelProps/markers_kit.py)." : "."));
    }

    private static void Import(string path, int border)
    {
        var t = AssetImporter.GetAtPath(path) as TextureImporter;
        if (t == null) return;
        t.textureType = TextureImporterType.Sprite;
        t.spriteImportMode = SpriteImportMode.Single;
        t.spritePixelsPerUnit = 100f;
        t.filterMode = FilterMode.Point;
        t.textureCompression = TextureImporterCompression.Uncompressed;
        t.mipmapEnabled = false;
        t.alphaIsTransparency = true;
        t.wrapMode = TextureWrapMode.Clamp;
        t.npotScale = TextureImporterNPOTScale.None;
        t.spriteBorder = Vector4.one * border;
        var settings = new TextureImporterSettings();
        t.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        t.SetTextureSettings(settings);
        t.SaveAndReimport();
    }
}
