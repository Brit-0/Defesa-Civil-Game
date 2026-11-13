using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Sprite _S de Assets/Arts/Houses, com tamanho nativo em u (px / 32) e o retângulo VISÍVEL
    /// (sem a margem transparente), usado para posicionar pela base real do desenho.
    /// </summary>
    public class CatalogSprite
    {
        public string guid, name, path, category, texture;
        public float widthU, heightU;
        /// <summary>Margem transparente à esquerda e embaixo, e tamanho da parte visível (u).</summary>
        public float padLeftU, padBottomU, visWU, visHU;
        /// <summary>Pixels do sprite (linhas de baixo para cima), para a prévia em PNG.</summary>
        public Color32[] pixels; public int pxW, pxH;

        public SpriteRef ToRef() => new SpriteRef { guid = guid, name = name, path = path };

        /// <summary>Deslocamento X do centro visível em relação ao centro do retângulo do sprite.</summary>
        public float VisCenterOffsetXU => padLeftU + visWU / 2f - widthU / 2f;

        /// <summary>Centro do retângulo do sprite (mundo) para uma base visível em 'baseCenter'.</summary>
        public Vector2 RectCenterFor(Vector2 baseCenter) =>
            new Vector2(baseCenter.x - VisCenterOffsetXU, baseCenter.y - padBottomU + heightU / 2f);

        /// <summary>Centro da base visível (mundo) para um retângulo do sprite com canto inferior esquerdo 'rectMin'.</summary>
        public Vector2 BaseCenterFor(Vector2 rectMin) =>
            new Vector2(rectMin.x + padLeftU + visWU / 2f, rectMin.y + padBottomU);

        /// <summary>Retângulo visível no mundo para a base em 'baseCenter'.</summary>
        public Rect VisibleRect(Vector2 baseCenter) => new Rect(baseCenter.x - visWU / 2f, baseCenter.y, visWU, visHU);
    }

    /// <summary>Catálogo dos sprites _S: pools de estruturas (R06, R07) e de decoração (R17).</summary>
    public class SpriteCatalog
    {
        public const string Root = "Assets/Arts/Houses";
        public const float PixelsPerUnit = 32f;

        /// <summary>Exclusões pedidas pelo usuário para a decoração: acampamento/objetos, pontes, horta/plantação.</summary>
        static readonly Regex NatureExcluded = new Regex(
            "^(Barraca.*|Saco_Dormir|Canoa|Remo|Fogueira.*|Lenha|Placa_Madeira|Ponte_.*|Horta_.*|Milho_.*|Trigo_.*|Abobora|Cenoura|Melancia|Nabo|Vitoria_Regia.*)$");

        public readonly List<CatalogSprite> Suburban = new List<CatalogSprite>();
        public readonly List<CatalogSprite> Commercial = new List<CatalogSprite>();
        public readonly List<CatalogSprite> Industrial = new List<CatalogSprite>();
        public readonly List<CatalogSprite> Decoration = new List<CatalogSprite>();
        readonly Dictionary<string, CatalogSprite> byKey = new Dictionary<string, CatalogSprite>();
        readonly Dictionary<string, CatalogSprite> byTexture = new Dictionary<string, CatalogSprite>();

        /// <summary>R07: Guard e Warehouse usam Industrial/Fabrica_*_S.</summary>
        public List<CatalogSprite> GuardWarehouse => Industrial;

        public List<CatalogSprite> HousePool(string district) => district switch
        {
            District.Suburban => Suburban,
            District.Commercial => Commercial,
            District.Industrial => Industrial,
            _ => new List<CatalogSprite>()
        };

        static string Key(string guid, string name) => guid + "|" + name;

        public static bool IsHouseTexture(string category, string texture) => category switch
        {
            District.Suburban => texture.StartsWith("Casa_"),
            District.Commercial => texture.StartsWith("Predio_") || texture.StartsWith("Arranha_Ceu_") || texture.StartsWith("Fundo_"),
            District.Industrial => texture.StartsWith("Fabrica_"),
            _ => false
        };

        /// <summary>Decoração: qualquer sprite que não seja de estrutura nem das exclusões de Nature.</summary>
        public static bool IsDecorationTexture(string category, string texture) =>
            !IsHouseTexture(category, texture) && !(category == "Nature" && NatureExcluded.IsMatch(texture));

        static SpriteCatalog cached;

        /// <summary>Catálogo em cache (a medição dos pixels é feita uma vez por recarga de domínio).</summary>
        public static SpriteCatalog Load(bool force = false)
        {
            if (cached != null && !force) return cached;
            var c = new SpriteCatalog();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png") || path.EndsWith("_n.png")) continue;
                var parts = path.Substring(Root.Length + 1).Split('/');
                if (parts.Length < 3) continue;
                string category = parts[0], texture = parts[1];
                var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == texture + "_S");
                if (sprite == null) continue;
                var cs = new CatalogSprite
                {
                    guid = guid, name = sprite.name, path = path, category = category, texture = texture,
                    widthU = sprite.rect.width / PixelsPerUnit, heightU = sprite.rect.height / PixelsPerUnit
                };
                Measure(sprite, cs);
                c.byKey[Key(guid, sprite.name)] = cs;
                c.byTexture[texture] = cs;
                if (IsHouseTexture(category, texture)) c.HousePool(category).Add(cs);
                else if (IsDecorationTexture(category, texture)) c.Decoration.Add(cs);
            }
            foreach (var l in new[] { c.Suburban, c.Commercial, c.Industrial, c.Decoration })
                l.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            cached = c;
            return c;
        }

        /// <summary>Mede o retângulo visível (alfa > 8%) do sprite dentro do seu retângulo.</summary>
        static void Measure(Sprite s, CatalogSprite cs)
        {
            var tex = s.texture;
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var r = s.rect;
            int w = (int)r.width, h = (int)r.height;
            var read = new Texture2D(w, h, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(r.x, r.y, w, h), 0, 0);
            read.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            var px = read.GetPixels32();
            Object.DestroyImmediate(read);
            int x0 = w, x1 = -1, y0 = h, y1 = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a > 20)
                    {
                        if (x < x0) x0 = x; if (x > x1) x1 = x;
                        if (y < y0) y0 = y; if (y > y1) y1 = y;
                    }
            if (x1 < 0) { x0 = 0; x1 = w - 1; y0 = 0; y1 = h - 1; }
            cs.pixels = px; cs.pxW = w; cs.pxH = h;
            cs.padLeftU = x0 / PixelsPerUnit;
            cs.padBottomU = y0 / PixelsPerUnit;
            cs.visWU = (x1 - x0 + 1) / PixelsPerUnit;
            cs.visHU = (y1 - y0 + 1) / PixelsPerUnit;
        }

        /// <summary>Entrada do catálogo para a referência, ou null.</summary>
        public CatalogSprite Get(SpriteRef r) => r != null && byKey.TryGetValue(Key(r.guid, r.name), out var cs) ? cs : null;

        /// <summary>Entrada pelo nome da textura (ex.: "Casa_D"), ou null.</summary>
        public CatalogSprite ByTexture(string texture) => byTexture.TryGetValue(texture, out var cs) ? cs : null;

        public IEnumerable<CatalogSprite> ByPrefix(string category, string prefix) =>
            byTexture.Values.Where(c => c.category == category && c.texture.StartsWith(prefix)).OrderBy(c => c.name, System.StringComparer.Ordinal);

        public static Sprite LoadSprite(SpriteRef r)
        {
            if (r == null) return null;
            var path = AssetDatabase.GUIDToAssetPath(r.guid);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == r.name);
        }

        public bool InHousePool(SpriteRef r, out string district)
        {
            district = null;
            var cs = Get(r);
            if (cs == null || !IsHouseTexture(cs.category, cs.texture)) return false;
            district = cs.category;
            return true;
        }

        public bool InGuardWarehousePool(SpriteRef r) { var cs = Get(r); return cs != null && cs.category == District.Industrial && IsHouseTexture(cs.category, cs.texture); }
        public bool InDecorationPool(SpriteRef r) { var cs = Get(r); return cs != null && IsDecorationTexture(cs.category, cs.texture); }
    }
}
