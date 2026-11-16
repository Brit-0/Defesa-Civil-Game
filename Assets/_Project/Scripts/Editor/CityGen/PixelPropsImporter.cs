using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Importa os props de rua desenhados por Tools/PixelProps/props_kit.py: fatia cada folha em quadros
    /// (_S e os de brilho _G), põe o pivô na base visível e liga o normal map como textura secundária.
    /// </summary>
    public static class PixelPropsImporter
    {
        public const string GlowSuffix = "_G";
        /// <summary>Px vazios embaixo de cada quadro (PAD do props_kit.py).</summary>
        const int Pad = 4;

        class Prop { public string name; public int frameW; public string[] frames; }

        static readonly Prop[] Props =
        {
            new Prop { name = "Poste_Novo", frameW = 40, frames = new[] { "S", "G" } },
            new Prop { name = "Poste_Novo_Duplo", frameW = 64, frames = new[] { "S", "G" } },
            new Prop { name = "Semaforo_Novo", frameW = 32, frames = new[] { "S", "G0", "G1", "G2" } },
            new Prop { name = "Placa_Pare_Nova", frameW = 32, frames = new[] { "S" } },
            new Prop { name = "Placa_Rua_Nova", frameW = 32, frames = new[] { "S" } },
        };

        /// <summary>Prédios de referência: folha recolorida de uma fábrica do pack (mesmos quadros, nomes trocados).</summary>
        static readonly (string name, string source)[] Clones =
        {
            ("Fabrica_Guarda", "Fabrica_A"), ("Fabrica_Armazem_A", "Fabrica_S"), ("Fabrica_Armazem_B", "Fabrica_K"),
        };

        static void ImportClones()
        {
            foreach (var (name, source) in Clones)
            {
                string dir = $"{SpriteCatalog.Root}/Industrial/{name}/{name}", src = $"{SpriteCatalog.Root}/Industrial/{source}/{source}.png";
                if (!File.Exists(dir + ".png")) { Debug.LogWarning("[CityGen] Props: falta " + dir + ".png (rode Tools/PixelProps/props_kit.py)."); continue; }
                AssetDatabase.ImportAsset(dir + "_n.png", ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(dir + ".png", ImportAssetOptions.ForceUpdate);
                var from = (TextureImporter)AssetImporter.GetAtPath(src);
                var n = (TextureImporter)AssetImporter.GetAtPath(dir + "_n.png");
                n.textureType = TextureImporterType.Default;
                n.sRGBTexture = false;
                n.alphaIsTransparency = false;
                Common(n);
                n.SaveAndReimport();

                var t = (TextureImporter)AssetImporter.GetAtPath(dir + ".png");
                var settings = new TextureImporterSettings();
                from.ReadTextureSettings(settings);
                t.SetTextureSettings(settings);
                Common(t);
#pragma warning disable 618
                var sheet = from.spritesheet;
                for (int i = 0; i < sheet.Length; i++) sheet[i].name = name + sheet[i].name.Substring(source.Length);
                t.spritesheet = sheet;
#pragma warning restore 618
                t.secondarySpriteTextures = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "_n.png") } };
                t.SaveAndReimport();
            }
        }

        [MenuItem("Tools/City/Importar Props Pixel")]
        public static void Import()
        {
            ImportClones();
            int done = 0;
            foreach (var p in Props)
            {
                string path = $"{SpriteCatalog.Root}/Roads/{p.name}/{p.name}.png", normalPath = $"{SpriteCatalog.Root}/Roads/{p.name}/{p.name}_n.png";
                if (!File.Exists(path)) { Debug.LogWarning("[CityGen] Props: falta " + path + " (rode Tools/PixelProps/props_kit.py)."); continue; }
                AssetDatabase.ImportAsset(normalPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                var n = (TextureImporter)AssetImporter.GetAtPath(normalPath);
                n.textureType = TextureImporterType.Default;
                n.sRGBTexture = false;
                n.alphaIsTransparency = false;
                Common(n);
                n.SaveAndReimport();

                var t = (TextureImporter)AssetImporter.GetAtPath(path);
                t.textureType = TextureImporterType.Sprite;
                t.spriteImportMode = SpriteImportMode.Multiple;
                t.spritePixelsPerUnit = SpriteCatalog.PixelsPerUnit;
                t.sRGBTexture = true;
                t.alphaIsTransparency = true;
                Common(t);
                var settings = new TextureImporterSettings();
                t.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                t.SetTextureSettings(settings);
                t.GetSourceTextureWidthAndHeight(out int w, out int h);
                var sheet = new List<SpriteMetaData>();
                for (int i = 0; i < p.frames.Length; i++)
                    sheet.Add(new SpriteMetaData
                    {
                        name = p.name + "_" + p.frames[i], rect = new Rect(i * p.frameW, 0, p.frameW, h),
                        alignment = (int)SpriteAlignment.Custom, pivot = new Vector2(0.5f, Pad / (float)h)
                    });
#pragma warning disable 618 // spritesheet: a API nova pede o pacote 2D Sprite; esta ainda grava o .meta certo
                t.spritesheet = sheet.ToArray();
#pragma warning restore 618
                t.secondarySpriteTextures = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath) } };
                t.SaveAndReimport();
                done++;
            }
            SpriteCatalog.Load(true);
            Debug.Log($"[CityGen] Props: {done} de {Props.Length} folhas importadas.");
        }

        static void Common(TextureImporter t)
        {
            t.filterMode = FilterMode.Point;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.mipmapEnabled = false;
            t.npotScale = TextureImporterNPOTScale.None;
            t.wrapMode = TextureWrapMode.Clamp;
            t.maxTextureSize = 8192;
        }

        /// <summary>Quadros de brilho (_G, _G0...) da mesma folha do sprite, em ordem de nome.</summary>
        public static List<Sprite> GlowFrames(string path, string texture)
        {
            var list = new List<Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s && s.name.StartsWith(texture + GlowSuffix)) list.Add(s);
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list;
        }
    }
}
