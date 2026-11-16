using UnityEditor;
using UnityEngine;

//Ícones do Pixel Lab que chegam em Assets/Arts/UI/Pixel/Icons: importa como sprite pixel art
//(Filter Point, sem compressão, sem mipmap) e liga no FeelUITheme pelo nome do arquivo, sem ninguém abrir o Inspector.
public class PixelIconImporter : AssetPostprocessor
{
    private const string ThemePath = "Assets/Systems/UI/Resources/" + FeelUITheme.ResourcePath + ".asset";

    private static bool IsLabIcon(string path)
    {
        return path.StartsWith(FeelUITheme.LabIconsFolder) && path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase);
    }

    private void OnPreprocessTexture()
    {
        if (!IsLabIcon(assetPath)) return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
    }

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (!Touches(imported) && !Touches(deleted) && !Touches(moved) && !Touches(movedFrom)) return;

        FeelUITheme theme = AssetDatabase.LoadAssetAtPath<FeelUITheme>(ThemePath);
        if (theme == null) return;

        if (theme.FillLabIcons())
        {
            AssetDatabase.SaveAssetIfDirty(theme);
            Debug.Log("FeelUITheme: ícones do Pixel Lab atualizados (" + theme.labIcons.Length + " em " + FeelUITheme.LabIconsFolder + ")");
        }
    }

    private static bool Touches(string[] paths)
    {
        foreach (string path in paths)
        {
            if (IsLabIcon(path)) return true;
        }

        return false;
    }
}
