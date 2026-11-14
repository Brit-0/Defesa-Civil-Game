using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DefesaCivil.CityGen
{
    /// <summary>
    /// Validação na cena (regras C do plano). Reconstrói um CityLayout a partir da cena e aplica as regras de
    /// layout sobre ele; depois verifica o que só existe na cena (R04, R11, R14, R15, R16, R18, R22, R26, R28,
    /// R29, R30, R31, R34).
    /// </summary>
    public static class SceneValidator
    {
        public static ValidationReport Validate(CitySceneRefs r, CityLayout json, SpriteCatalog cat, SceneBaseline baseline)
        {
            var scene = FromScene(r, json, cat, out var extra);
            var report = LayoutValidator.Validate(scene, cat);
            report.mode = "cena";
            report.notes.RemoveAll(n => n.StartsWith("R08"));
            report.notes.Add($"R08 (parcial): semente {json.seed} (não verificável na cena).");
            foreach (var v in extra) report.violations.Add(v);

            var polyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CityBuilder.PolygonPrefabPath);
            var jsonPolys = new HashSet<string>(json.polygons.Select(p => p.id));
            var jsonStructs = new HashSet<string>(json.structures.Select(s => s.type + " " + s.id));
            var jsonTrees = new HashSet<string>(json.decorations.Select(d => d.id));

            // R04: toda House (e derivadas) da cena é filha direta de Houses
            foreach (var root in r.scene.GetRootGameObjects())
                foreach (var h in root.GetComponentsInChildren<House>(true))
                    if (h.transform.parent != r.houses) report.Add("R04", h.name, h.transform.position, "estrutura fora de UI/Canvas/UIContainer/Houses");

            // R22 / R26 em PolygonLayer
            foreach (Transform c in r.polygonLayer)
            {
                if (c.name == "Canvas" && !CitySceneRefs.IsGenerated(c)) continue; // exceção fixa (BG)
                if (!CitySceneRefs.IsGenerated(c) || !jsonPolys.Contains(c.name.Substring((CitySceneRefs.CgPrefix + "Polygon ").Length)))
                    report.Add("R26", c.name, c.position, "objeto em PolygonLayer que não consta no JSON");
                if (PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject) != polyPrefab)
                    report.Add("R22", c.name, c.position, "chão não é instância de Polygon.prefab");
            }
            foreach (Transform c in r.houses)
            {
                string key = CitySceneRefs.IsGenerated(c) ? c.name.Substring(CitySceneRefs.CgPrefix.Length) : null;
                if (key == null || !jsonStructs.Contains(key)) report.Add("R26", c.name, c.position, "objeto em Houses que não consta no JSON");
            }
            foreach (Transform c in r.trees)
            {
                string prefix = CitySceneRefs.CgPrefix + "Tree ";
                if (!c.name.StartsWith(prefix) || !jsonTrees.Contains(c.name.Substring(prefix.Length))) report.Add("R26", c.name, c.position, "objeto em Trees que não consta no JSON");
                if (!c.GetComponent<Image>()) report.Add("R16", c.name, c.position, "decoração sem Image");
            }

            // R29 (consistência): tudo do JSON existe na cena
            foreach (var id in jsonPolys) if (!r.polygonLayer.Find(CitySceneRefs.Name("Polygon", id))) report.Add("R29", "Polygon " + id, Vector2.zero, "polígono do JSON ausente na cena");
            foreach (var k in jsonStructs) if (!r.houses.Find(CitySceneRefs.CgPrefix + k)) report.Add("R29", k, Vector2.zero, "estrutura do JSON ausente na cena");
            foreach (var id in jsonTrees) if (!r.trees.Find(CitySceneRefs.Name("Tree", id))) report.Add("R29", "Tree " + id, Vector2.zero, "decoração do JSON ausente na cena");

            // R26 em Waypoints: só waypoints gerados (os antigos foram removidos em 2026-11-13; R15 revogada)
            foreach (Transform c in r.waypoints)
                if (!CitySceneRefs.IsGenerated(c)) report.Add("R26", c.name, c.position, "objeto em Waypoints que não foi gerado pelo Build");
            if (baseline != null)
                foreach (var d in baseline.Diff(r)) report.Add("R28", "câmera", Vector2.zero, d);
            else report.notes.Add("R28 não verificada: sem scene_baseline.json.");

            // R30: ordem de desenho por Y (Canvas aninhado + YSortCanvas) em Houses e Trees
            CheckSort(report, r.houses, YSortCanvas.Kind_House, scene.structures.ToDictionary(s => CitySceneRefs.Name(s.type, s.id), s => (Vector2)s.bottomCenter));
            CheckSort(report, r.trees, YSortCanvas.Kind_Tree, scene.decorations.ToDictionary(d => CitySceneRefs.Name("Tree", d.id), d => (Vector2)d.bottomCenter));

            // R34: enquadramento
            var bb = CityGeometry.BoundingBox(scene.polygons.Select(p => (IList<Vector2>)CityGeometry.ToVectors(p.points)));
            Vector2 cam = r.mainCamera.transform.position;
            float ortho = r.polygonCamera.orthographicSize;
            var view = Rect.MinMaxRect(cam.x - ortho * CityBuilder.DisplayAspect, cam.y - ortho, cam.x + ortho * CityBuilder.DisplayAspect, cam.y + ortho);
            var c4 = new Vector3[4]; r.polygonsDisplay.GetWorldCorners(c4);
            var disp = Rect.MinMaxRect(c4[0].x, c4[0].y, c4[2].x, c4[2].y);
            if (Mathf.Abs(view.xMin - disp.xMin) > 0.01f || Mathf.Abs(view.yMin - disp.yMin) > 0.01f || Mathf.Abs(view.xMax - disp.xMax) > 0.01f || Mathf.Abs(view.yMax - disp.yMax) > 0.01f)
                report.Add("R34", "PolygonsDisplay", disp.center, $"retângulo do Display {disp} difere da vista da PolygonCamera {view}");
            if (bb.xMin < view.xMin - 0.01f || bb.yMin < view.yMin - 0.01f || bb.xMax > view.xMax + 0.01f || bb.yMax > view.yMax + 0.01f)
                report.Add("R34", "PolygonCamera", cam, $"vista {view} não contém a caixa dos polígonos {bb}");

            // R27 na cena: Limiter = caixa dos polígonos
            var lb = r.limiter.bounds;
            float lm = json.limiterMargin;
            var lbox = Rect.MinMaxRect(bb.xMin - lm, bb.yMin - lm, bb.xMax + lm, bb.yMax + lm);
            if (Mathf.Abs(lb.min.x - lbox.xMin) > 0.01f || Mathf.Abs(lb.min.y - lbox.yMin) > 0.01f || Mathf.Abs(lb.max.x - lbox.xMax) > 0.01f || Mathf.Abs(lb.max.y - lbox.yMax) > 0.01f)
                report.Add("R27", "Limiter", lb.center, $"bounds {lb.min}–{lb.max} ≠ caixa + rua de contorno {lbox}");

            report.Finish();
            return report;
        }

        static void CheckSort(ValidationReport rep, Transform parent, int kind, Dictionary<string, Vector2> bottoms)
        {
            foreach (Transform c in parent)
            {
                if (!bottoms.TryGetValue(c.name, out var b)) continue;
                var ys = c.GetComponent<YSortCanvas>(); var cv = c.GetComponent<Canvas>();
                if (ys == null || cv == null || !cv.overrideSorting) { rep.Add("R30", c.name, b, "sem Canvas aninhado com Override Sorting + YSortCanvas"); continue; }
                int expected = YSortCanvas.OrderFor(b.y, kind);
                if (Mathf.Abs(cv.sortingOrder - expected) > 1) rep.Add("R30", c.name, b, $"sortingOrder {cv.sortingOrder} ≠ esperado {expected} para y {b.y:0.##}");
            }
        }

        /// <summary>Reconstrói o layout a partir da cena (posições medidas nos objetos reais).</summary>
        public static CityLayout FromScene(CitySceneRefs r, CityLayout json, SpriteCatalog cat, out List<Violation> extra)
        {
            extra = new List<Violation>();
            var l = new CityLayout { seed = json.seed, center = (Vector2)r.mainCamera.transform.position, streetWidth = json.streetWidth, limiterMargin = json.limiterMargin };
            string polyPrefix = CitySceneRefs.CgPrefix + "Polygon ";
            foreach (Transform c in r.polygonLayer)
            {
                if (!c.name.StartsWith(polyPrefix)) continue;
                var col = c.GetComponent<PolygonCollider2D>();
                if (!col) continue;
                var p = new PolygonData { id = c.name.Substring(polyPrefix.Length) };
                foreach (var v in col.GetPath(0)) p.points.Add((Vector2)c.TransformPoint(v));
                l.polygons.Add(p);
            }
            foreach (Transform c in r.houses)
            {
                if (!CitySceneRefs.IsGenerated(c)) continue;
                var parts = c.name.Substring(CitySceneRefs.CgPrefix.Length).Split(' ');
                if (parts.Length != 2) continue;
                var rt = c as RectTransform;
                var img = c.Find("Visual") ? c.Find("Visual").GetComponent<Image>() : null;
                var corners = new Vector3[4]; rt.GetWorldCorners(corners);
                var s = new StructureData { type = parts[0], id = parts[1], bottomCenter = new Vec2((corners[0].x + corners[3].x) / 2f, corners[0].y) };
                s.sprite = Ref(img ? img.sprite : null);
                var cs = cat.Get(s.sprite);
                if (cs != null) s.bottomCenter = cs.BaseCenterFor(corners[0]);
                s.district = cs != null ? cs.category : District.None;
                var house = c.GetComponent<House>();
                if (house && house.entry)
                {
                    s.entry = (Vector2)house.entry.transform.position;
                    // orientação: a entrada fica na rua da fachada (acima = fileira N)
                    s.facing = s.entry.y > s.bottomCenter.y ? Facing.North : Facing.South;
                    if (house.entry.transform.parent != r.waypoints) extra.Add(V("R14", c.name, s.entry, "entry fora de UI/Canvas/UIContainer/Waypoints"));
                }
                else { s.entry = new Vec2(float.NaN, float.NaN); extra.Add(V("R14", c.name, s.bottomCenter, "entry nulo")); }
                float w = corners[2].x - corners[0].x, h = corners[1].y - corners[0].y;
                if (cs != null && (Mathf.Abs(w - cs.widthU) > LayoutValidator.SizeTolerance || Mathf.Abs(h - cs.heightU) > LayoutValidator.SizeTolerance))
                    extra.Add(V("R11", c.name, s.bottomCenter, $"tamanho {w:0.###}×{h:0.###} u ≠ nativo {cs.widthU}×{cs.heightU}"));
                var box = c.GetComponent<BoxCollider2D>();
                if (!box || Vector2.Distance(box.size, rt.sizeDelta) * rt.lossyScale.x > LayoutValidator.SizeTolerance || box.offset.sqrMagnitude * rt.lossyScale.x > 1e-4f)
                    extra.Add(V("R31", c.name, s.bottomCenter, "BoxCollider2D não cobre o retângulo nativo"));
                l.structures.Add(s);
            }
            // waypoints de cruzamento e ligações (R38)
            string jPrefix = CitySceneRefs.CgPrefix + "Waypoint " + StreetGraph.JunctionType + " ";
            string wPrefix = CitySceneRefs.CgPrefix + "Waypoint ";
            var pairs = new HashSet<(string, string)>();
            foreach (Transform c in r.waypoints)
            {
                var wp = c.GetComponent<Waypoint>();
                if (!wp) continue;
                bool generated = c.name.StartsWith(wPrefix);
                if (c.name.StartsWith(jPrefix)) l.junctions.Add(new WaypointNode { id = c.name.Substring(jPrefix.Length), position = (Vector2)c.position });
                var seenHere = new HashSet<Waypoint>();
                foreach (var n in wp.neighbors)
                {
                    if (!generated) break; // os 3 antigos não fazem parte do grafo gerado
                    if (n == null) { extra.Add(V("R38", c.name, c.position, "vizinho nulo")); continue; }
                    if (n == wp) extra.Add(V("R38", c.name, c.position, "waypoint na própria lista de vizinhos"));
                    if (!seenHere.Add(n)) extra.Add(V("R38", c.name, c.position, "vizinho repetido: " + n.name));
                    if (!n.neighbors.Contains(wp)) extra.Add(V("R38", c.name, c.position, "ligação não é recíproca com " + n.name));
                    if (!n.name.StartsWith(wPrefix)) { extra.Add(V("R38", c.name, c.position, "ligado a waypoint fora do grafo gerado: " + n.name)); continue; }
                    string a = c.name.Substring(wPrefix.Length), b = n.name.Substring(wPrefix.Length);
                    pairs.Add(string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a));
                }
            }
            l.links = pairs.Select(p => new WaypointLink { a = p.Item1, b = p.Item2 }).ToList();

            string treePrefix = CitySceneRefs.CgPrefix + "Tree ";
            foreach (Transform c in r.trees)
            {
                if (!c.name.StartsWith(treePrefix)) continue;
                var rt = c as RectTransform;
                var img = c.GetComponent<Image>();
                var corners = new Vector3[4]; rt.GetWorldCorners(corners);
                var d = new DecorationData { id = c.name.Substring(treePrefix.Length), sprite = Ref(img ? img.sprite : null), bottomCenter = new Vec2((corners[0].x + corners[3].x) / 2f, corners[0].y) };
                var cs = cat.Get(d.sprite);
                if (cs != null) d.bottomCenter = cs.BaseCenterFor(corners[0]);
                float w = corners[2].x - corners[0].x, h = corners[1].y - corners[0].y;
                if (cs != null && (Mathf.Abs(w - cs.widthU) > LayoutValidator.SizeTolerance || Mathf.Abs(h - cs.heightU) > LayoutValidator.SizeTolerance))
                    extra.Add(V("R18", c.name, d.bottomCenter, $"tamanho {w:0.###}×{h:0.###} u ≠ nativo {cs.widthU}×{cs.heightU}"));
                l.decorations.Add(d);
            }
            return l;
        }

        static SpriteRef Ref(Sprite s)
        {
            if (!s) return new SpriteRef();
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s, out string guid, out long _);
            return new SpriteRef { guid = guid, name = s.name, path = AssetDatabase.GetAssetPath(s) };
        }

        static Violation V(string rule, string obj, Vector2 pos, string msg) =>
            new Violation { ruleId = rule, severity = Severity.Of(rule), @object = obj, position = pos, message = msg };
    }
}
