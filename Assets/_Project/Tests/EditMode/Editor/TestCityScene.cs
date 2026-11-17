using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefesaCivil.CityGen.Tests
{
    /// <summary>Cena temporária (aditiva) com a hierarquia mínima que o gerador espera.</summary>
    public class TestCityScene
    {
        public Scene scene;
        public Transform uiRoot;
        bool reused;
        public CitySceneRefs refs;
        public GameObject bgCanvas;
        public Vector2 center = new Vector2(1.5f, -0.5f);

        // O CityBuilder assa o chão da cidade de teste no mesmo city_ground.png da Gameplay: sem guardar e devolver,
        // rodar os testes deixava o chão de outra cidade esticado no retângulo da real, com sombras e detalhes fora
        // do lugar (foi para o commit e32072c; lista Para Arrumar, item 18).
        byte[] groundBackup;
        static string GroundFile => System.IO.Path.GetFullPath(CityGround.TexturePath);

        public static TestCityScene Create(bool withOldObjects = true)
        {
            var t = new TestCityScene();
            if (System.IO.File.Exists(GroundFile)) t.groundBackup = System.IO.File.ReadAllBytes(GroundFile);
            var active = SceneManager.GetActiveScene();
            // O Test Runner roda numa cena sem título; usa-a (nunca a Gameplay).
            if (string.IsNullOrEmpty(active.path)) { t.scene = active; t.reused = true; }
            else t.scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var prev = active;
            SceneManager.SetActiveScene(t.scene);

            var ess = new GameObject("Essentials");
            var cam = new GameObject("Main Camera", typeof(Camera));
            cam.transform.SetParent(ess.transform); cam.transform.position = new Vector3(t.center.x, t.center.y, -10f);
            var pl = new GameObject("PolygonLayer"); pl.transform.SetParent(cam.transform, false);
            t.bgCanvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)); t.bgCanvas.transform.SetParent(pl.transform, false);
            var pc = new GameObject("PolygonCamera", typeof(Camera)); pc.transform.SetParent(cam.transform, false);
            pc.GetComponent<Camera>().orthographic = true; pc.GetComponent<Camera>().orthographicSize = 5f;
            var lim = new GameObject("Limiter", typeof(SpriteRenderer)); lim.transform.SetParent(ess.transform);
            var tex = new Texture2D(4, 4);
            lim.GetComponent<SpriteRenderer>().sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);

            var ui = new GameObject("UI");
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(ui.transform);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var crt = (RectTransform)canvas.transform;
            crt.sizeDelta = new Vector2(1920, 1080); crt.localScale = Vector3.one * 0.01f; crt.position = new Vector3(t.center.x, t.center.y, 90f);
            var cont = NewRect("UIContainer", canvas.transform);
            t.uiRoot = cont;
            NewRect("PolygonsDisplay", cont).gameObject.AddComponent<RawImage>();
            NewRect("Houses", cont); NewRect("Trees", cont); NewRect("Waypoints", cont);
            var oldWp = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Systems/Navigation/Waypoint.prefab"), cont.Find("Waypoints"));
            oldWp.name = "Waypoint";

            var scripts = new GameObject("Scripts");
            var gps = new GameObject("GPS"); gps.transform.SetParent(scripts.transform); gps.AddComponent<GPS>();

            if (withOldObjects)
            {
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CityBuilder.PolygonPrefabPath), pl.transform);
                foreach (var n in new[] { "House", "Guard", "Warehouse" })
                    PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Systems/Houses/{n}.prefab"), cont);
            }
            SceneManager.SetActiveScene(prev);
            t.refs = CitySceneRefs.Find(t.scene, out var err);
            if (t.refs == null) throw new System.Exception(err);
            return t;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(100, 100);
            return rt;
        }

        public void Close()
        {
            RestoreGround();
            if (!scene.IsValid()) return;
            if (reused) { foreach (var g in scene.GetRootGameObjects()) Object.DestroyImmediate(g); }
            else EditorSceneManager.CloseScene(scene, true);
        }

        void RestoreGround()
        {
            if (groundBackup == null) return;
            byte[] now = System.IO.File.Exists(GroundFile) ? System.IO.File.ReadAllBytes(GroundFile) : null;
            if (now != null && System.Linq.Enumerable.SequenceEqual(now, groundBackup)) return;
            System.IO.File.WriteAllBytes(GroundFile, groundBackup);
            AssetDatabase.ImportAsset(CityGround.TexturePath, ImportAssetOptions.ForceUpdate);
        }
    }
}
