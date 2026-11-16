using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Referência para R28 (câmeras/MapRender/CameraController intocados) e R15 (3 Waypoints antigos).
    /// Capturada uma vez, com a cena no estado de ac210a1 para esses objetos.
    /// </summary>
    [Serializable]
    public class SceneBaseline
    {
        public const string Path = "Assets/_Project/Data/Cities/scene_baseline.json";
        public const string CameraControllerPath = "Assets/Systems/Camera/CameraController.cs";
        public const string MapRenderPath = "Assets/Arts/MapRender.renderTexture";

        public string capturedAt = "";
        public List<string> mainCamera = new List<string>();
        public List<string> polygonCamera = new List<string>();
        public List<string> polygonsDisplayImage = new List<string>();
        public string cameraControllerFileHash = "";
        public string mapRenderFileHash = "";
        public List<string> legacyWaypoints = new List<string>();

        public static SceneBaseline Capture(CitySceneRefs r)
        {
            var b = new SceneBaseline { capturedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") };
            b.Fill(r);
            foreach (Transform c in r.waypoints)
                if (!CitySceneRefs.IsGenerated(c)) b.legacyWaypoints.Add(GlobalObjectId.GetGlobalObjectIdSlow(c.gameObject).ToString());
            return b;
        }

        void Fill(CitySceneRefs r)
        {
            mainCamera = Props(r.mainCamera.GetComponents<Component>().Where(c => c != null), null);
            polygonCamera = Props(r.polygonCamera.GetComponents<Component>().Where(c => c != null), new HashSet<string> { "orthographic size" });
            polygonsDisplayImage = Props(r.polygonsDisplay.GetComponents<Component>().Where(c => c != null && !(c is RectTransform)), null);
            cameraControllerFileHash = FileHash(CameraControllerPath);
            mapRenderFileHash = FileHash(MapRenderPath);
        }

        /// <summary>Compara o estado atual com a referência. Devolve as diferenças (vazio = ok).</summary>
        public List<string> Diff(CitySceneRefs r)
        {
            var now = new SceneBaseline(); now.Fill(r);
            var d = new List<string>();
            Compare(d, "Main Camera (exceto o zoom do CameraController)", WithoutZoom(mainCamera), WithoutZoom(now.mainCamera));
            Compare(d, "PolygonCamera (exceto orthographicSize)", polygonCamera, now.polygonCamera);
            Compare(d, "PolygonsDisplay (exceto retângulo)", polygonsDisplayImage, now.polygonsDisplayImage);
            if (now.cameraControllerFileHash != cameraControllerFileHash) d.Add("CameraController.cs mudou");
            if (now.mapRenderFileHash != mapRenderFileHash) d.Add("MapRender.renderTexture mudou");
            return d;
        }

        // v6 (pedido do Pietro, 16/11/2026): o zoom serializado do CameraController na cena acompanha o mapa maior; o código não muda
        static readonly string[] ZoomFields = { "CameraController.zoom=", "CameraController.zoomMult=", "CameraController.minZoom=", "CameraController.maxZoom=" };
        static List<string> WithoutZoom(List<string> lines) => lines.Where(x => !ZoomFields.Any(x.StartsWith)).ToList();

        static void Compare(List<string> d, string what, List<string> before, List<string> now)
        {
            var a = new HashSet<string>(before); var b = new HashSet<string>(now);
            var changed = now.Where(x => !a.Contains(x)).Concat(before.Where(x => !b.Contains(x)).Select(x => "(antes) " + x)).Take(4).ToList();
            if (changed.Count > 0) d.Add(what + " mudou: " + string.Join(" | ", changed));
        }

        static List<string> Props(IEnumerable<Component> comps, HashSet<string> skipNames)
        {
            var lines = new List<string>();
            foreach (var c in comps)
            {
                string type = c.GetType().Name;
                var so = new SerializedObject(c);
                var it = so.GetIterator();
                bool enter = true;
                while (it.Next(enter))
                {
                    enter = true;
                    if (skipNames != null && skipNames.Contains(it.name)) { enter = false; continue; }
                    if (it.propertyPath == "m_Children" || it.propertyPath.StartsWith("m_Children.")) { enter = false; continue; }
                    // referências: compara por GlobalObjectId e não entra em m_FileID/m_PathID (mudam a cada carga)
                    if (it.propertyType == SerializedPropertyType.ObjectReference) enter = false;
                    if (it.hasChildren && it.propertyType == SerializedPropertyType.Generic) continue;
                    lines.Add(type + "." + it.propertyPath + "=" + Value(it));
                }
            }
            return lines;
        }

        static string Value(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer: return p.longValue.ToString();
                case SerializedPropertyType.Boolean: return p.boolValue.ToString();
                case SerializedPropertyType.Float: return p.doubleValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                case SerializedPropertyType.String: return p.stringValue;
                case SerializedPropertyType.Enum: return p.intValue.ToString();
                case SerializedPropertyType.ObjectReference:
                    return p.objectReferenceValue ? GlobalObjectId.GetGlobalObjectIdSlow(p.objectReferenceValue).ToString() : "null";
                default: return "";
            }
        }

        static string FileHash(string assetPath) => File.Exists(assetPath) ? Sha(File.ReadAllBytes(assetPath)) : "missing";

        static string Sha(byte[] data)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
        }

        public void Save()
        {
            File.WriteAllText(System.IO.Path.GetFullPath(Path), JsonUtility.ToJson(this, true));
            AssetDatabase.ImportAsset(Path);
        }

        public static SceneBaseline Load()
        {
            var full = System.IO.Path.GetFullPath(Path);
            return File.Exists(full) ? JsonUtility.FromJson<SceneBaseline>(File.ReadAllText(full)) : null;
        }
    }
}
