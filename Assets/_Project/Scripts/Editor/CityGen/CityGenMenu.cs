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
        public static void Generate() => GenerateAndWrite(true);

        /// <summary>Gera com semente nova, grava o JSON (com confirmação e backup '~' se já existir) e valida.</summary>
        public static ValidationReport GenerateAndWrite(bool confirmOverwrite)
        {
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("[CityGen] Generate: abra a cena Gameplay (Main Camera não encontrada)."); return null; }
            var full = CityGenPaths.LayoutFullPath;
            if (File.Exists(full))
            {
                if (confirmOverwrite && !EditorUtility.DisplayDialog("City Generate",
                        "city_layout.json já existe e pode ter edições manuais. Sobrescrever? (um backup city_layout.json~ será criado)",
                        "Sobrescrever", "Cancelar")) return null;
                File.Copy(full, full + "~", true);
            }
            var layout = CityGenerator.GenerateValid(cam.transform.position, SpriteCatalog.Load(), out var report);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, layout.ToJson());
            AssetDatabase.ImportAsset(CityGenPaths.Layout);
            report.Write();
            report.LogToConsole();
            Debug.Log($"[CityGen] Generate: semente {layout.seed}, {layout.structures.Count} estruturas, {layout.decorations.Count} decorações, {layout.polygons.Count} polígonos.");
            return report;
        }

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
