using UnityEditor;
using UnityEditor.Presets;

public static class CreateSpritePreset
{
    [MenuItem("Tools/Create Preset From Selected Texture")]
    static void Create()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        var importer = AssetImporter.GetAtPath(path);
        var preset = new Preset(importer);
        string presetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Sprite_PixelArt.preset");
        AssetDatabase.CreateAsset(preset, presetPath);
        AssetDatabase.SaveAssets();
    }
}