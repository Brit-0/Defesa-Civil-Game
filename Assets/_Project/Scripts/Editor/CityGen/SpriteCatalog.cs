using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>Sprite candidato de um pool, com tamanho nativo em u (px / 32).</summary>
    public class CatalogSprite
    {
        public string guid, name, path, category, texture;
        public float widthU, heightU;
        public SpriteRef ToRef() => new SpriteRef { guid = guid, name = name, path = path };
    }

    /// <summary>Pools de sprites definidos por R06, R07 e R17 (só sprites _S).</summary>
    public class SpriteCatalog
    {
        public const string Root = "Assets/Arts/Houses";
        public const float PixelsPerUnit = 32f;

        static readonly Regex NatureExcluded = new Regex(
            "^(Barraca.*|Saco_Dormir|Canoa|Remo|Fogueira.*|Lenha|Placa_Madeira|Ponte_.*|Horta_.*|Milho_.*|Trigo_.*|Abobora|Cenoura|Melancia|Nabo|Vitoria_Regia.*)$");

        public readonly List<CatalogSprite> Suburban = new List<CatalogSprite>();
        public readonly List<CatalogSprite> Commercial = new List<CatalogSprite>();
        public readonly List<CatalogSprite> Industrial = new List<CatalogSprite>();
        public readonly List<CatalogSprite> Decoration = new List<CatalogSprite>();
        readonly Dictionary<string, CatalogSprite> byKey = new Dictionary<string, CatalogSprite>();

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

        public static bool IsDecorationTexture(string category, string texture) =>
            category == "Nature" && !NatureExcluded.IsMatch(texture);

        public static SpriteCatalog Load()
        {
            var c = new SpriteCatalog();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png") || path.EndsWith("_n.png")) continue;
                var parts = path.Substring(Root.Length + 1).Split('/');
                if (parts.Length < 3) continue;
                string category = parts[0], texture = parts[1];
                foreach (var s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                {
                    if (s.name != texture + "_S") continue;
                    var cs = new CatalogSprite
                    {
                        guid = guid, name = s.name, path = path, category = category, texture = texture,
                        widthU = s.rect.width / PixelsPerUnit, heightU = s.rect.height / PixelsPerUnit
                    };
                    c.byKey[Key(guid, s.name)] = cs;
                    if (IsHouseTexture(category, texture)) c.HousePool(category).Add(cs);
                    else if (IsDecorationTexture(category, texture)) c.Decoration.Add(cs);
                }
            }
            foreach (var l in new[] { c.Suburban, c.Commercial, c.Industrial, c.Decoration })
                l.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return c;
        }

        /// <summary>Entrada do catálogo (qualquer pool) para a referência, ou null.</summary>
        public CatalogSprite Get(SpriteRef r) => r != null && byKey.TryGetValue(Key(r.guid, r.name), out var cs) ? cs : null;

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
