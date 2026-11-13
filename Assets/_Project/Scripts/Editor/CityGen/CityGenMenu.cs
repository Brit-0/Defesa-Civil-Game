using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        /// <summary>Valida o JSON e, se a Gameplay estiver aberta, a cena. Grava o relatório de cena (ou de layout).</summary>
        [MenuItem("Tools/City/Validate")]
        public static void Validate() => ValidateAll();

        public static ValidationReport ValidateAll()
        {
            CityLayout layout;
            try { layout = CityGenPaths.LoadLayout(); }
            catch (System.Exception e) { Debug.LogError("[CityGen] Validate: " + e.Message); return null; }
            var cat = SpriteCatalog.Load();
            var report = LayoutValidator.Validate(layout, cat);
            var refs = CitySceneRefs.Find(SceneManager.GetActiveScene(), out _);
            if (refs != null)
            {
                report.LogToConsole();
                report = SceneValidator.Validate(refs, layout, cat, SceneBaseline.Load());
            }
            report.Write();
            report.LogToConsole();
            return report;
        }

        [MenuItem("Tools/City/Build")]
        public static void Build() => BuildActiveScene(true);

        /// <summary>
        /// Valida o layout, aplica na cena ativa, roda o Build 2× para medir idempotência (R29), valida a cena e
        /// salva só se houver 0 erros.
        /// </summary>
        public static ValidationReport BuildActiveScene(bool save)
        {
            CityLayout layout;
            try { layout = CityGenPaths.LoadLayout(); }
            catch (System.Exception e) { Debug.LogError("[CityGen] Build: " + e.Message); return null; }
            var cat = SpriteCatalog.Load();
            var pre = LayoutValidator.Validate(layout, cat);
            if (pre.errors > 0) { pre.Write(); pre.LogToConsole(); Debug.LogError("[CityGen] Build cancelado: layout com erros."); return pre; }

            var scene = SceneManager.GetActiveScene();
            var refs = CitySceneRefs.Find(scene, out var err);
            if (refs == null) { Debug.LogError("[CityGen] Build: " + err); return null; }

            var first = new CityBuilder(layout, refs, cat).Run();
            Debug.Log("[CityGen] Build: " + first);
            var dump = CityDump.Canonical(refs);
            var second = new CityBuilder(layout, refs, cat).Run();

            var report = SceneValidator.Validate(refs, layout, cat, SceneBaseline.Load());
            if (second.created + second.updated + second.deleted > 0 || dump != CityDump.Canonical(refs))
            {
                report.Add("R29", "Build", Vector2.zero, "2º Build alterou a cena: " + second);
                report.Finish();
            }
            report.notes.Add("Build: " + first + ". 2º Build: " + second + ".");
            report.Write();
            report.LogToConsole();
            EditorSceneManager.MarkSceneDirty(scene);
            if (report.errors == 0 && save)
            {
                bool ok = EditorSceneManager.SaveScene(scene);
                Debug.Log("[CityGen] Build: cena " + (ok ? "salva." : "NÃO salva (falha ao salvar)."));
            }
            else if (report.errors > 0) Debug.LogWarning("[CityGen] Build: cena NÃO salva (há erros). Corrija e rode de novo, ou desfaça (Undo).");
            return report;
        }

        [MenuItem("Tools/City/Capture Baseline")]
        public static void CaptureBaseline()
        {
            var refs = CitySceneRefs.Find(SceneManager.GetActiveScene(), out var err);
            if (refs == null) { Debug.LogError("[CityGen] Baseline: " + err); return; }
            SceneBaseline.Capture(refs).Save();
            Debug.Log("[CityGen] Baseline gravado em " + SceneBaseline.Path);
        }
    }
}
