using UnityEditor;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Cores do chão por zona (v4): cada bairro tem o seu tom, para a cidade ser lida de longe. A prévia (CityPreview)
    /// e o Build (CityBuilder) usam a mesma tabela; no Build cada cor vira um material, cópia do PolygonMAT.
    /// </summary>
    public static class CityPalette
    {
        public const string BaseMaterialPath = "Assets/Shaders & Materials/PolygonMAT.mat";
        public const string MaterialFolder = "Assets/Shaders & Materials/Ground";

        public static readonly Color32 Street = new Color32(38, 38, 48, 255);
        static readonly Color32 Park = new Color32(98, 140, 78, 255);
        static readonly Color32 Suburban = new Color32(140, 150, 92, 255);
        static readonly Color32 Commercial = new Color32(158, 148, 134, 255);
        static readonly Color32 Industrial = new Color32(108, 114, 128, 255);

        /// <summary>Chave da zona do polígono: Park (praça ou bosque), Suburban, Commercial ou Industrial.</summary>
        public static string Key(PolygonData p)
        {
            if (p.kind == PolygonKind.Plaza) return "Park";
            return p.district == District.Commercial || p.district == District.Industrial ? p.district : District.Suburban;
        }

        public static Color32 Ground(PolygonData p) => Key(p) switch
        {
            "Park" => Park, District.Commercial => Commercial, District.Industrial => Industrial, _ => Suburban
        };

        /// <summary>Material da zona: criado na primeira vez como cópia do PolygonMAT, com a cor atualizada pela tabela.</summary>
        public static Material GroundMaterial(PolygonData p)
        {
            string path = MaterialFolder + "/Ground_" + Key(p) + ".mat";
            Color color = Ground(p);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/Shaders & Materials", "Ground");
                mat = new Material(AssetDatabase.LoadAssetAtPath<Material>(BaseMaterialPath)) { color = color };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.color != color)
            {
                mat.color = color;
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }
    }
}
