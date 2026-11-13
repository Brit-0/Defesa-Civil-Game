using System.IO;
using UnityEditor;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    public static class CityGenPaths
    {
        public const string Layout = "Assets/_Project/Data/Cities/city_layout.json";
        public static string LayoutFullPath => Path.GetFullPath(Layout);

        public static CityLayout LoadLayout()
        {
            if (!File.Exists(LayoutFullPath)) throw new FileNotFoundException("Layout não encontrado: " + Layout);
            return CityLayout.FromJson(File.ReadAllText(LayoutFullPath));
        }
    }

    /// <summary>Menus Tools/City/*.</summary>
    public static class CityGenMenu
    {
        [MenuItem("Tools/City/Generate")]
        public static void Generate() => Debug.Log("[CityGen] Generate: ainda não implementado.");

        [MenuItem("Tools/City/Validate")]
        public static void Validate()
        {
            ValidationReport report;
            try { report = LayoutValidator.Validate(CityGenPaths.LoadLayout(), SpriteCatalog.Load()); }
            catch (System.Exception e) { Debug.LogError("[CityGen] Validate: " + e.Message); return; }
            report.Write();
            report.LogToConsole();
        }

        [MenuItem("Tools/City/Build")]
        public static void Build() => Debug.Log("[CityGen] Build: ainda não implementado.");
    }
}
