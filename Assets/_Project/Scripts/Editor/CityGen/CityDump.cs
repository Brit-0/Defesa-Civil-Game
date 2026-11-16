using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Dump canônico do que o gerador controla na cena (objetos [CG], Limiter, enquadramento).
    /// Usado para medir idempotência (R29) sem depender do arquivo .unity.
    /// </summary>
    public static class CityDump
    {
        static string F(float v) => v.ToString("0.0000", CultureInfo.InvariantCulture);
        static string V(Vector2 v) => F(v.x) + "," + F(v.y);

        public static string Canonical(CitySceneRefs r)
        {
            var lines = new List<string>();
            void Children(Transform parent, string tag)
            {
                int i = 0;
                foreach (Transform c in parent)
                {
                    int idx = i++;
                    if (!CitySceneRefs.IsGenerated(c)) continue;
                    var sb = new StringBuilder(tag + "|" + idx + "|" + c.name + "|pos=" + V(c.position));
                    if (c is RectTransform rt) sb.Append("|size=" + V(rt.sizeDelta));
                    var img = c.Find("Visual") ? c.Find("Visual").GetComponent<Image>() : c.GetComponent<Image>();
                    if (img) sb.Append("|sprite=" + (img.sprite ? img.sprite.name : "null") + "|ray=" + img.raycastTarget);
                    var box = c.GetComponent<BoxCollider2D>();
                    if (box) sb.Append("|box=" + V(box.size) + "/" + V(box.offset));
                    var poly = c.GetComponent<PolygonCollider2D>();
                    if (poly) sb.Append("|path=" + string.Join(";", poly.GetPath(0).Select(V)));
                    var house = c.GetComponent<House>();
                    if (house) sb.Append("|entry=" + (house.entry ? house.entry.name : "null"));
                    var ysc = c.GetComponent<YSortCanvas>();
                    if (ysc) sb.Append("|order=" + c.GetComponent<Canvas>().sortingOrder);
                    var wp = c.GetComponent<Waypoint>();
                    if (wp) sb.Append("|nb=" + string.Join(",", wp.neighbors.Select(n => n ? n.name : "null")));
                    lines.Add(sb.ToString());
                }
            }
            Children(r.polygonLayer, "poly");
            Children(r.houses, "house");
            Children(r.trees, "tree");
            Children(r.waypoints, "wp");
            var lt = r.limiter.transform;
            lines.Add("limiter|" + V(lt.position) + "|" + V(lt.localScale));
            lines.Add("polycam|" + F(r.polygonCamera.orthographicSize));
            lines.Add("display|" + V(r.polygonsDisplay.position) + "|" + V(r.polygonsDisplay.sizeDelta));
            lines.Add("gps.waypoints|" + string.Join(",", Names(r.gps, "allWaypoints")));
            lines.Add("gps.locations|" + string.Join(",", Names(r.gps, "allLocations")));
            return string.Join("\n", lines);
        }

        static IEnumerable<string> Names(GPS gps, string field)
        {
            var so = new UnityEditor.SerializedObject(gps);
            var p = so.FindProperty(field);
            for (int i = 0; i < p.arraySize; i++)
            {
                var o = p.GetArrayElementAtIndex(i).objectReferenceValue;
                yield return o ? o.name : "null";
            }
        }
    }
}
