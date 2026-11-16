using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DefesaCivil.CityGen
{
    /// <summary>Resultado de um Build: contagens por operação.</summary>
    public class BuildResult
    {
        public int created, updated, deleted, unchanged;
        public readonly List<string> log = new List<string>();
        public override string ToString() => $"criados {created}, atualizados {updated}, removidos {deleted}, sem mudança {unchanged}";
    }

    /// <summary>
    /// Aplica o city_layout.json nos pais existentes da cena (docs/plan-citygen.md, seção c).
    /// Objetos gerados: nome "[CG] tipo id". Estratégia: atualizar por id.
    /// </summary>
    public partial class CityBuilder
    {
        public const string PolygonPrefabPath = "Assets/Systems/Houses/Polygon.prefab";
        public const float DisplayAspect = 16f / 9f; // MapRender 640×360

        readonly CityLayout layout;
        readonly CitySceneRefs refs;
        readonly SpriteCatalog cat;
        readonly BuildResult res = new BuildResult();
        readonly bool undo;

        /// <param name="undo">false nos testes: o Test Runner desfaz o histórico de Undo ao terminar e recriaria objetos na cena aberta.</param>
        public CityBuilder(CityLayout layout, CitySceneRefs refs, SpriteCatalog cat, bool undo = true)
        {
            this.layout = layout; this.refs = refs; this.cat = cat; this.undo = undo;
        }

        void Record(Object o) { if (undo) Undo.RecordObject(o, "City Build"); }
        void Created(GameObject go) { if (undo) Undo.RegisterCreatedObjectUndo(go, "City Build"); }
        T Add<T>(GameObject g) where T : Component => undo ? Undo.AddComponent<T>(g) : g.AddComponent<T>();

        /// <summary>
        /// Ordem de desenho por Y (Canvas aninhado + YSortCanvas). Estruturas precisam de GraphicRaycaster para receber
        /// cliques, já que o Canvas aninhado tira os Graphics do raycaster do Canvas raiz.
        /// </summary>
        bool EnsureYSort(GameObject go, int kind, float offsetU, bool raycaster)
        {
            bool changed = false;
            var canvas = go.GetComponent<Canvas>();
            if (!canvas) { canvas = Add<Canvas>(go); changed = true; }
            var ys = go.GetComponent<YSortCanvas>();
            if (!ys) { ys = Add<YSortCanvas>(go); changed = true; }
            if (raycaster && !go.GetComponent<UnityEngine.UI.GraphicRaycaster>()) { Add<UnityEngine.UI.GraphicRaycaster>(go); changed = true; }
            if (ys.kind != kind || !Approx(ys.baseOffsetU, offsetU) || ys.dynamic) { Record(ys); ys.kind = kind; ys.baseOffsetU = offsetU; ys.dynamic = false; changed = true; }
            if (!canvas.overrideSorting || canvas.sortingOrder != ys.ComputeOrder()) { Record(canvas); ys.Apply(); changed = true; }
            return changed;
        }

        void SetSibling(Transform t, int i) { if (undo) Undo.SetSiblingIndex(t, i, "City Build"); else t.SetSiblingIndex(i); }

        public BuildResult Run()
        {
            if (undo) { Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("City Build"); }
            int group = Undo.GetCurrentGroup();
            MigrateOldPolygon();
            BuildPolygons();
            FitLimiter();
            FitPolygonFraming();
            BuildGround();
            MigrateOldStructures();
            BuildStructures();
            MigrateOldWaypoints();
            BuildWaypoints();
            SortByBottom(refs.houses, layout.structures.ToDictionary(s => CitySceneRefs.Name(s.type, s.id), s => (Vector2)s.bottomCenter));
            FillGps();
            BuildDecorations();
            SortByBottom(refs.trees, layout.decorations.ToDictionary(d => CitySceneRefs.Name("Tree", d.id), d => (Vector2)d.bottomCenter));
            BuildLighting();
            FlushPrefabOverrides();
            if (undo) Undo.CollapseUndoOperations(group);
            return res;
        }

        /// <summary>
        /// Registra como override de prefab tudo o que o Build mudou nas instâncias (Waypoint, House, Polygon).
        /// Sem isso, alterações feitas e salvas no mesmo frame podem não ir para o arquivo (ex.: neighbors salvo
        /// com o tamanho certo mas com referências nulas).
        /// </summary>
        void FlushPrefabOverrides()
        {
            foreach (var parent in new[] { refs.houses, refs.waypoints, refs.polygonLayer, refs.trees })
                foreach (Transform c in parent)
                {
                    if (!CitySceneRefs.IsGenerated(c)) continue;
                    foreach (var comp in c.GetComponentsInChildren<Component>(true))
                    {
                        if (comp == null) continue;
                        if (PrefabUtility.IsPartOfPrefabInstance(comp)) PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
                        EditorUtility.SetDirty(comp);
                    }
                }
            foreach (var comp in new Component[] { refs.gps, refs.limiter.transform, refs.polygonCamera, refs.polygonsDisplay })
            {
                if (PrefabUtility.IsPartOfPrefabInstance(comp)) PrefabUtility.RecordPrefabInstancePropertyModifications(comp);
                EditorUtility.SetDirty(comp);
            }
        }

        // ---------- utilitários ----------

        static Dictionary<string, Transform> GeneratedChildren(Transform parent, string type)
        {
            var d = new Dictionary<string, Transform>();
            string prefix = CitySceneRefs.CgPrefix + type + " ";
            foreach (Transform c in parent)
                if (c.name.StartsWith(prefix)) d[c.name.Substring(prefix.Length)] = c;
            return d;
        }

        void Destroy(GameObject go, string why)
        {
            res.log.Add("removido " + go.name + " (" + why + ")");
            if (undo) Undo.DestroyObjectImmediate(go); else Object.DestroyImmediate(go);
            res.deleted++;
        }

        bool Changed(bool changed) { if (changed) res.updated++; else res.unchanged++; return changed; }

        static bool Approx(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 1e-10f;
        static bool Approx(float a, float b) => Mathf.Abs(a - b) < 1e-5f;

        // ---------- polígonos ----------

        void MigrateOldPolygon()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PolygonPrefabPath);
            foreach (Transform c in refs.polygonLayer.Cast<Transform>().ToList())
                if (c.name == "Polygon" && PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject) == prefab)
                    Destroy(c.gameObject, "migração: Polygon antigo");
        }

        void BuildPolygons()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PolygonPrefabPath);
            var existing = GeneratedChildren(refs.polygonLayer, "Polygon");
            var layer = refs.polygonLayer;
            foreach (var p in layout.polygons)
            {
                var local = p.points.Select(v => (Vector2)layer.InverseTransformPoint(new Vector3(v.x, v.y, layer.position.z))).ToArray();
                bool created = false;
                if (!existing.TryGetValue(p.id, out var t))
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, layer);
                    Created(go);
                    go.name = CitySceneRefs.Name("Polygon", p.id);
                    go.transform.localPosition = Vector3.zero;
                    t = go.transform; created = true; res.created++;
                }
                existing.Remove(p.id);
                var col = t.GetComponent<PolygonCollider2D>();
                bool same = col.pathCount == 1 && col.GetPath(0).Length == local.Length && col.GetPath(0).Zip(local, (a, b) => Approx(a, b)).All(x => x);
                if (!same)
                {
                    Record(col);
                    col.pathCount = 1;
                    col.SetPath(0, local);
                }
                var mr = t.GetComponent<MeshRenderer>();
                var mat = CityPalette.GroundMaterial(p);
                bool sameMat = mr.sharedMaterial == mat;
                if (!sameMat) { Record(mr); mr.sharedMaterial = mat; }
                if (!created) Changed(!same || !sameMat);
                if (created || !same) RebuildMesh(t.gameObject);
            }
            foreach (var kv in existing) Destroy(kv.Value.gameObject, "id fora do JSON");
        }

        /// <summary>Gera a mesh pelo próprio PolygonMesh2D (Start + OnColliderUpdate), para salvar a cena já com ela.</summary>
        static void RebuildMesh(GameObject go)
        {
            var pm = go.GetComponent<PolygonMesh2D>();
            if (pm == null) return;
            pm.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            pm.OnColliderUpdate();
        }

        Rect PolygonBox() => CityGeometry.BoundingBox(layout.polygons.Select(p => (IList<Vector2>)CityGeometry.ToVectors(p.points)));

        // ---------- estruturas (R01–R07, R11, R14, R30, R31) ----------

        public const string WaypointPrefabPath = "Assets/Systems/Navigation/Waypoint.prefab";
        static string StructurePrefabPath(string type) => $"Assets/Systems/Houses/{type}.prefab";

        void MigrateOldStructures()
        {
            foreach (var type in new[] { StructureType.House, StructureType.Guard, StructureType.Warehouse })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StructurePrefabPath(type));
                foreach (Transform c in refs.uiContainer.Cast<Transform>().ToList())
                    if (c.name == type && PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject) == prefab)
                        Destroy(c.gameObject, "migração: estrutura antiga fora de Houses");
            }
        }

        readonly List<House> locations = new List<House>();

        void BuildStructures()
        {
            var existing = new Dictionary<string, Transform>();
            foreach (var type in new[] { StructureType.House, StructureType.Guard, StructureType.Warehouse })
                foreach (var kv in GeneratedChildren(refs.houses, type)) existing[type + " " + kv.Key] = kv.Value;
            var scale = refs.houses.lossyScale;

            foreach (var s in layout.structures)
            {
                string key = s.type + " " + s.id;
                var cs = cat.Get(s.sprite);
                var sprite = SpriteCatalog.LoadSprite(s.sprite);
                if (cs == null || sprite == null) { res.log.Add("sprite não encontrado para " + key); continue; }

                bool created = false;
                if (!existing.TryGetValue(key, out var t))
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(StructurePrefabPath(s.type)), refs.houses);
                    Created(go);
                    go.name = CitySceneRefs.Name(s.type, s.id);
                    t = go.transform; created = true; res.created++;
                }
                existing.Remove(key);

                var rt = (RectTransform)t;
                var size = new Vector2(cs.widthU / scale.x, cs.heightU / scale.y);
                Vector2 rc = cs.RectCenterFor(s.bottomCenter);
                var local = refs.houses.InverseTransformPoint(new Vector3(rc.x, rc.y, refs.houses.position.z));
                var anchored = new Vector2(local.x, local.y);
                var half = new Vector2(0.5f, 0.5f);
                var img = t.Find("Visual").GetComponent<UnityEngine.UI.Image>();
                var box = t.GetComponent<BoxCollider2D>();

                bool changed = false;
                if (rt.anchorMin != half || rt.anchorMax != half || rt.pivot != half || !Approx(rt.sizeDelta, size) || !Approx(rt.anchoredPosition, anchored) || !Approx(rt.localPosition.z, 0f))
                {
                    Record(rt);
                    rt.anchorMin = half; rt.anchorMax = half; rt.pivot = half;
                    rt.sizeDelta = size; rt.anchoredPosition = anchored;
                    rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
                    changed = true;
                }
                if (img.sprite != sprite) { Record(img); img.sprite = sprite; changed = true; }
                if (box && (!Approx(box.size, size) || !Approx(box.offset, Vector2.zero)))
                {
                    Record(box);
                    box.size = size; box.offset = Vector2.zero;
                    changed = true;
                }

                if (EnsureYSort(t.gameObject, YSortCanvas.Kind_House, cs.padBottomU, true)) changed = true;
                if (LightHouse(t)) changed = true;
                var house = t.GetComponent<House>();
                houses[key] = house;
                if (house is Location) locations.Add(house);

                if (!created) Changed(changed);
            }
            foreach (var kv in existing) Destroy(kv.Value.gameObject, "id fora do JSON");
        }

        /// <summary>Migração (decisão de 2026-11-13): remove os Waypoints antigos (não [CG]); a malha é toda gerada.</summary>
        void MigrateOldWaypoints()
        {
            foreach (Transform c in refs.waypoints.Cast<Transform>().ToList())
                if (!CitySceneRefs.IsGenerated(c) && c.GetComponent<Waypoint>() != null)
                    Destroy(c.gameObject, "migração: Waypoint antigo");
        }

        readonly Dictionary<string, House> houses = new Dictionary<string, House>();
        readonly Dictionary<string, Waypoint> waypointByKey = new Dictionary<string, Waypoint>();

        /// <summary>
        /// Waypoints de entrada (um por estrutura, R14) e de cruzamento (R38), com neighbors iguais às ligações do
        /// JSON: simétricos, sem repetição e nunca o próprio waypoint. Os 3 Waypoints antigos (não [CG]) não são tocados.
        /// </summary>
        void BuildWaypoints()
        {
            var wpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WaypointPrefabPath);
            float wpLocalZ = wpPrefab.transform.localPosition.z;
            var existingWp = GeneratedChildren(refs.waypoints, "Waypoint");
            var wanted = layout.structures.Select(s => (key: StreetGraph.EntryKey(s), p: (Vector2)s.entry))
                .Concat(layout.junctions.Select(j => (key: StreetGraph.JunctionKey(j.id), p: (Vector2)j.position))).ToList();
            foreach (var (key, p) in wanted)
            {
                bool wpCreated = false;
                if (!existingWp.TryGetValue(key, out var wp))
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(wpPrefab, refs.waypoints);
                    Created(go);
                    go.name = CitySceneRefs.Name("Waypoint", key);
                    wp = go.transform; wpCreated = true; res.created++;
                }
                existingWp.Remove(key);
                var wl = refs.waypoints.InverseTransformPoint(new Vector3(p.x, p.y, refs.waypoints.position.z));
                var wpLocal = new Vector3(wl.x, wl.y, wpCreated ? wpLocalZ : wp.localPosition.z);
                bool wpChanged = (wp.localPosition - wpLocal).sqrMagnitude > 1e-6f;
                if (wpChanged) { Record(wp); wp.localPosition = wpLocal; }
                if (!wpCreated) Changed(wpChanged);
                waypointByKey[key] = wp.GetComponent<Waypoint>();
            }
            foreach (var kv in existingWp) Destroy(kv.Value.gameObject, "id fora do JSON");

            // vizinhos
            var adj = waypointByKey.Keys.ToDictionary(k => k, k => new List<string>());
            foreach (var l in layout.links)
            {
                if (l.a == l.b || !adj.ContainsKey(l.a) || !adj.ContainsKey(l.b)) continue;
                if (!adj[l.a].Contains(l.b)) adj[l.a].Add(l.b);
                if (!adj[l.b].Contains(l.a)) adj[l.b].Add(l.a);
            }
            foreach (var kv in adj)
            {
                var wp = waypointByKey[kv.Key];
                var want = kv.Value.OrderBy(k => k, System.StringComparer.Ordinal).Select(k => waypointByKey[k]).ToList();
                bool same = wp.neighbors.Count == want.Count && wp.neighbors.Zip(want, (x, y) => x == y).All(x => x);
                if (!same) { Record(wp); wp.neighbors = want; }
                Changed(!same);
            }

            // entrada de cada estrutura
            foreach (var s in layout.structures)
            {
                string key = StreetGraph.EntryKey(s);
                if (!houses.TryGetValue(key, out var house) || !waypointByKey.TryGetValue(key, out var w)) continue;
                if (house.entry != w) { Record(house); house.entry = w; res.updated++; }
            }
        }

        /// <summary>R30: ordem de irmãos decrescente em y da borda inferior (mais ao sul por último = na frente).</summary>
        void SortByBottom(Transform parent, Dictionary<string, Vector2> bottoms)
        {
            var ordered = parent.Cast<Transform>()
                .Where(c => CitySceneRefs.IsGenerated(c) && bottoms.ContainsKey(c.name))
                .OrderByDescending(c => bottoms[c.name].y).ThenBy(c => bottoms[c.name].x).ThenBy(c => c.name, System.StringComparer.Ordinal)
                .ToList();
            bool changed = false;
            for (int i = 0; i < ordered.Count; i++)
                if (ordered[i].GetSiblingIndex() != i) { SetSibling(ordered[i], i); changed = true; }
            Changed(changed);
        }

        void FillGps()
        {
            var so = new SerializedObject(refs.gps);
            bool changed = SetList(so.FindProperty("allWaypoints"), waypointByKey.OrderBy(k => k.Key, System.StringComparer.Ordinal).Select(k => (Object)k.Value).ToList());
            changed |= SetList(so.FindProperty("allLocations"), locations.Cast<Object>().ToList());
            if (changed) { Record(refs.gps); so.ApplyModifiedProperties(); }
            Changed(changed);
        }

        static bool SetList(SerializedProperty p, List<Object> items)
        {
            bool same = p.arraySize == items.Count;
            for (int i = 0; same && i < items.Count; i++) same = p.GetArrayElementAtIndex(i).objectReferenceValue == items[i];
            if (same) return false;
            p.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            return true;
        }

        // ---------- decorações (R16–R18, R20, R30) ----------

        void BuildDecorations()
        {
            var existing = GeneratedChildren(refs.trees, "Tree");
            var scale = refs.trees.lossyScale;
            var half = new Vector2(0.5f, 0.5f);
            foreach (var d in layout.decorations)
            {
                var cs = cat.Get(d.sprite);
                var sprite = SpriteCatalog.LoadSprite(d.sprite);
                if (cs == null || sprite == null) { res.log.Add("sprite não encontrado para Tree " + d.id); continue; }
                bool created = false;
                if (!existing.TryGetValue(d.id, out var t))
                {
                    var go = new GameObject(CitySceneRefs.Name("Tree", d.id), typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                    Created(go);
                    go.layer = refs.trees.gameObject.layer;
                    go.transform.SetParent(refs.trees, false);
                    t = go.transform; created = true; res.created++;
                }
                existing.Remove(d.id);
                var rt = (RectTransform)t;
                var img = t.GetComponent<UnityEngine.UI.Image>();
                var size = new Vector2(cs.widthU / scale.x, cs.heightU / scale.y);
                Vector2 rc = cs.RectCenterFor(d.bottomCenter);
                var local = refs.trees.InverseTransformPoint(new Vector3(rc.x, rc.y, refs.trees.position.z));
                var anchored = new Vector2(local.x, local.y);
                bool changed = false;
                if (rt.anchorMin != half || rt.anchorMax != half || rt.pivot != half || !Approx(rt.sizeDelta, size) || !Approx(rt.anchoredPosition, anchored) || !Approx(rt.localPosition.z, 0f))
                {
                    Record(rt);
                    rt.anchorMin = half; rt.anchorMax = half; rt.pivot = half;
                    rt.sizeDelta = size; rt.anchoredPosition = anchored;
                    rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
                    changed = true;
                }
                if (img.sprite != sprite || img.raycastTarget)
                {
                    Record(img);
                    img.sprite = sprite;
                    img.raycastTarget = false; // decisão: decorações não recebem cliques
                    changed = true;
                }
                if (EnsureYSort(t.gameObject, YSortCanvas.Kind_Tree, cs.padBottomU, false)) changed = true;
                if (LightDecoration(img)) changed = true;
                if (!created) Changed(changed);
            }
            foreach (var kv in existing) Destroy(kv.Value.gameObject, "id fora do JSON");
        }

        /// <summary>
        /// Assa o chão (CityGround) e mostra a textura num RawImage logo acima do PolygonsDisplay, cobrindo o Limiter.
        /// Os polígonos, a PolygonCamera e o MapRender continuam como estão (R28): o chão novo só fica por cima.
        /// </summary>
        void BuildGround()
        {
            var tex = CityGround.Bake(layout, cat);
            var t = refs.uiContainer.Find(GroundName) as RectTransform;
            bool changed = false;
            if (t == null)
            {
                var go = new GameObject(GroundName, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.RawImage));
                Created(go);
                go.layer = refs.polygonsDisplay.gameObject.layer;
                t = (RectTransform)go.transform;
                t.SetParent(refs.uiContainer, false);
                res.created++;
            }
            int index = refs.polygonsDisplay.GetSiblingIndex() + 1;
            if (t.GetSiblingIndex() != index) { SetSibling(t, index); changed = true; }
            var img = t.GetComponent<UnityEngine.UI.RawImage>();
            if (img.texture != tex || img.raycastTarget || img.color != Color.white)
            {
                Record(img);
                img.texture = tex; img.raycastTarget = false; img.color = Color.white; img.uvRect = new Rect(0, 0, 1, 1);
                changed = true;
            }
            var bb = LayoutValidator.BuildGeo(layout).limiter;
            var parentScale = t.parent.lossyScale;
            var size = new Vector2(bb.width / parentScale.x, bb.height / parentScale.y);
            var half = new Vector2(0.5f, 0.5f);
            var pos = new Vector3(bb.center.x, bb.center.y, refs.polygonsDisplay.position.z);
            if (t.anchorMin != half || t.anchorMax != half || t.pivot != half || !Approx(t.sizeDelta, size) || ((Vector2)t.position - bb.center).sqrMagnitude > 1e-8f)
            {
                Record(t);
                t.anchorMin = half; t.anchorMax = half; t.pivot = half;
                t.sizeDelta = size; t.position = pos;
                changed = true;
            }
            Changed(changed);
        }
    }
}
