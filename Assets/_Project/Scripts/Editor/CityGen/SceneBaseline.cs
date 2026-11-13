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
        public string mainCameraHash = "";
        public string polygonCameraHash = "";
        public string polygonsDisplayImageHash = "";
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
            mainCameraHash = Hash(r.mainCamera.GetComponents<Component>().Where(c => c != null), null);
            polygonCameraHash = Hash(r.polygonCamera.GetComponents<Component>().Where(c => c != null), new HashSet<string> { "orthographic size" });
            polygonsDisplayImageHash = Hash(r.polygonsDisplay.GetComponents<Component>().Where(c => c != null && !(c is RectTransform)), null);
            cameraControllerFileHash = FileHash(CameraControllerPath);
            mapRenderFileHash = FileHash(MapRenderPath);
        }

        /// <summary>Compara o estado atual com a referência. Devolve as diferenças (vazio = ok).</summary>
        public List<string> Diff(CitySceneRefs r)
        {
            var now = new SceneBaseline(); now.Fill(r);
            var d = new List<string>();
            if (now.mainCameraHash != mainCameraHash) d.Add("componentes da Main Camera mudaram");
            if (now.polygonCameraHash != polygonCameraHash) d.Add("PolygonCamera mudou além do orthographicSize");
            if (now.polygonsDisplayImageHash != polygonsDisplayImageHash) d.Add("PolygonsDisplay mudou além do retângulo");
            if (now.cameraControllerFileHash != cameraControllerFileHash) d.Add("CameraController.cs mudou");
            if (now.mapRenderFileHash != mapRenderFileHash) d.Add("MapRender.renderTexture mudou");
            return d;
        }

        static string Hash(IEnumerable<Component> comps, HashSet<string> skipNames)
        {
            var sb = new StringBuilder();
            foreach (var c in comps)
            {
                sb.Append("#").Append(c.GetType().FullName).Append('\n');
                var so = new SerializedObject(c);
                var it = so.GetIterator();
                bool enter = true;
                while (it.Next(enter))
                {
                    enter = true;
                    if (skipNames != null && skipNames.Contains(it.name)) { enter = false; continue; }
                    if (it.propertyPath == "m_Children" || it.propertyPath.StartsWith("m_Children.")) { enter = false; continue; }
                    sb.Append(it.propertyPath).Append('=').Append(Value(it)).Append('\n');
                }
            }
            return Sha(Encoding.UTF8.GetBytes(sb.ToString()));
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
