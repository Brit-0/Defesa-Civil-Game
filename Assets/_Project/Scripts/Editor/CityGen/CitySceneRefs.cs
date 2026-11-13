using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefesaCivil.CityGen
{
    /// <summary>Pais e componentes da cena que o gerador usa, localizados por caminho fixo (CLAUDE.md).</summary>
    public class CitySceneRefs
    {
        public Scene scene;
        public Transform houses, trees, waypoints, uiContainer, polygonLayer;
        public Camera mainCamera, polygonCamera;
        public RectTransform polygonsDisplay;
        public SpriteRenderer limiter;
        public GPS gps;

        public const string CgPrefix = "[CG] ";

        static Transform Root(Scene s, string name) => s.GetRootGameObjects().FirstOrDefault(g => g.name == name)?.transform;

        public static CitySceneRefs Find(Scene s, out string error)
        {
            error = null;
            var r = new CitySceneRefs { scene = s };
            var ui = Root(s, "UI"); var ess = Root(s, "Essentials"); var scripts = Root(s, "Scripts");
            r.uiContainer = ui ? ui.Find("Canvas/UIContainer") : null;
            r.houses = r.uiContainer ? r.uiContainer.Find("Houses") : null;
            r.trees = r.uiContainer ? r.uiContainer.Find("Trees") : null;
            r.waypoints = r.uiContainer ? r.uiContainer.Find("Waypoints") : null;
            r.polygonsDisplay = r.uiContainer ? r.uiContainer.Find("PolygonsDisplay") as RectTransform : null;
            var mainCam = ess ? ess.Find("Main Camera") : null;
            r.mainCamera = mainCam ? mainCam.GetComponent<Camera>() : null;
            r.polygonLayer = mainCam ? mainCam.Find("PolygonLayer") : null;
            var pc = mainCam ? mainCam.Find("PolygonCamera") : null;
            r.polygonCamera = pc ? pc.GetComponent<Camera>() : null;
            var lim = ess ? ess.Find("Limiter") : null;
            r.limiter = lim ? lim.GetComponent<SpriteRenderer>() : null;
            var gps = scripts ? scripts.Find("GPS") : null;
            r.gps = gps ? gps.GetComponent<GPS>() : null;

            var missing = new System.Collections.Generic.List<string>();
            if (!r.houses) missing.Add("UI/Canvas/UIContainer/Houses");
            if (!r.trees) missing.Add("UI/Canvas/UIContainer/Trees");
            if (!r.waypoints) missing.Add("UI/Canvas/UIContainer/Waypoints");
            if (!r.polygonsDisplay) missing.Add("UI/Canvas/UIContainer/PolygonsDisplay");
            if (!r.mainCamera) missing.Add("Essentials/Main Camera");
            if (!r.polygonLayer) missing.Add("Essentials/Main Camera/PolygonLayer");
            if (!r.polygonCamera) missing.Add("Essentials/Main Camera/PolygonCamera");
            if (!r.limiter) missing.Add("Essentials/Limiter");
            if (!r.gps) missing.Add("Scripts/GPS");
            if (missing.Count > 0) { error = "Não encontrado na cena: " + string.Join(", ", missing); return null; }
            return r;
        }

        public static bool IsGenerated(Transform t) => t.name.StartsWith(CgPrefix);
        public static string Name(string type, string id) => CgPrefix + type + " " + id;
    }
}
