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
    public class CityBuilder
    {
        public const string PolygonPrefabPath = "Assets/Systems/Houses/Polygon.prefab";
        public const float DisplayAspect = 16f / 9f; // MapRender 640×360

        readonly CityLayout layout;
        readonly CitySceneRefs refs;
        readonly SpriteCatalog cat;
        readonly BuildResult res = new BuildResult();

        public CityBuilder(CityLayout layout, CitySceneRefs refs, SpriteCatalog cat)
        {
            this.layout = layout; this.refs = refs; this.cat = cat;
        }

        public BuildResult Run()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("City Build");
            int group = Undo.GetCurrentGroup();
            MigrateOldPolygon();
            BuildPolygons();
            FitLimiter();
            FitPolygonFraming();
            MigrateOldStructures();
            BuildStructures();
            SortByBottom(refs.houses, layout.structures.ToDictionary(s => CitySceneRefs.Name(s.type, s.id), s => (Vector2)s.bottomCenter));
            FillGps();
            BuildDecorations();
            SortByBottom(refs.trees, layout.decorations.ToDictionary(d => CitySceneRefs.Name("Tree", d.id), d => (Vector2)d.bottomCenter));
            Undo.CollapseUndoOperations(group);
            return res;
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
            Undo.DestroyObjectImmediate(go);
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
                    Undo.RegisterCreatedObjectUndo(go, "City Build");
                    go.name = CitySceneRefs.Name("Polygon", p.id);
                    go.transform.localPosition = Vector3.zero;
                    t = go.transform; created = true; res.created++;
                }
                existing.Remove(p.id);
                var col = t.GetComponent<PolygonCollider2D>();
                bool same = col.pathCount == 1 && col.GetPath(0).Length == local.Length && col.GetPath(0).Zip(local, (a, b) => Approx(a, b)).All(x => x);
                if (!same)
                {
                    Undo.RecordObject(col, "City Build");
                    col.pathCount = 1;
                    col.SetPath(0, local);
                }
                if (!created) Changed(!same);
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

        readonly Dictionary<StructureData, Waypoint> entries = new Dictionary<StructureData, Waypoint>();
        readonly List<House> locations = new List<House>();

        void BuildStructures()
        {
            var wpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WaypointPrefabPath);
            float wpLocalZ = wpPrefab.transform.localPosition.z;
            var existing = new Dictionary<string, Transform>();
            foreach (var type in new[] { StructureType.House, StructureType.Guard, StructureType.Warehouse })
                foreach (var kv in GeneratedChildren(refs.houses, type)) existing[type + " " + kv.Key] = kv.Value;
            var existingWp = GeneratedChildren(refs.waypoints, "Waypoint");
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
                    Undo.RegisterCreatedObjectUndo(go, "City Build");
                    go.name = CitySceneRefs.Name(s.type, s.id);
                    t = go.transform; created = true; res.created++;
                }
                existing.Remove(key);

                var rt = (RectTransform)t;
                var size = new Vector2(cs.widthU / scale.x, cs.heightU / scale.y);
                var local = refs.houses.InverseTransformPoint(new Vector3(s.bottomCenter.x, s.bottomCenter.y + cs.heightU / 2f, refs.houses.position.z));
                var anchored = new Vector2(local.x, local.y);
                var half = new Vector2(0.5f, 0.5f);
                var img = t.Find("Visual").GetComponent<UnityEngine.UI.Image>();
                var box = t.GetComponent<BoxCollider2D>();

                bool changed = false;
                if (rt.anchorMin != half || rt.anchorMax != half || rt.pivot != half || !Approx(rt.sizeDelta, size) || !Approx(rt.anchoredPosition, anchored) || !Approx(rt.localPosition.z, 0f))
                {
                    Undo.RecordObject(rt, "City Build");
                    rt.anchorMin = half; rt.anchorMax = half; rt.pivot = half;
                    rt.sizeDelta = size; rt.anchoredPosition = anchored;
                    rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
                    changed = true;
                }
                if (img.sprite != sprite) { Undo.RecordObject(img, "City Build"); img.sprite = sprite; changed = true; }
                if (box && (!Approx(box.size, size) || !Approx(box.offset, Vector2.zero)))
                {
                    Undo.RecordObject(box, "City Build");
                    box.size = size; box.offset = Vector2.zero;
                    changed = true;
                }

                // waypoint de entrada (R14)
                bool wpCreated = false;
                if (!existingWp.TryGetValue(key, out var wp))
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(wpPrefab, refs.waypoints);
                    Undo.RegisterCreatedObjectUndo(go, "City Build");
                    go.name = CitySceneRefs.Name("Waypoint", key);
                    wp = go.transform; wpCreated = true; res.created++;
                }
                existingWp.Remove(key);
                var wl = refs.waypoints.InverseTransformPoint(new Vector3(s.entry.x, s.entry.y, refs.waypoints.position.z));
                var wpLocal = new Vector3(wl.x, wl.y, wpCreated ? wpLocalZ : wp.localPosition.z);
                bool wpChanged = (wp.localPosition - wpLocal).sqrMagnitude > 1e-6f;
                if (wpChanged) { Undo.RecordObject(wp, "City Build"); wp.localPosition = wpLocal; }
                if (!wpCreated) Changed(wpChanged);

                var house = t.GetComponent<House>();
                var waypoint = wp.GetComponent<Waypoint>();
                if (house.entry != waypoint) { Undo.RecordObject(house, "City Build"); house.entry = waypoint; changed = true; }
                entries[s] = waypoint;
                if (house is Location) locations.Add(house);

                if (!created) Changed(changed);
            }
            foreach (var kv in existing) Destroy(kv.Value.gameObject, "id fora do JSON");
            foreach (var kv in existingWp) Destroy(kv.Value.gameObject, "id fora do JSON");
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
                if (ordered[i].GetSiblingIndex() != i) { Undo.SetSiblingIndex(ordered[i], i, "City Build"); changed = true; }
            Changed(changed);
        }

        void FillGps()
        {
            var so = new SerializedObject(refs.gps);
            bool changed = SetList(so.FindProperty("allWaypoints"), layout.structures.Where(entries.ContainsKey).Select(s => (Object)entries[s]).ToList());
            changed |= SetList(so.FindProperty("allLocations"), locations.Cast<Object>().ToList());
            if (changed) { Undo.RecordObject(refs.gps, "City Build"); so.ApplyModifiedProperties(); }
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
                    Undo.RegisterCreatedObjectUndo(go, "City Build");
                    go.layer = refs.trees.gameObject.layer;
                    go.transform.SetParent(refs.trees, false);
                    t = go.transform; created = true; res.created++;
                }
                existing.Remove(d.id);
                var rt = (RectTransform)t;
                var img = t.GetComponent<UnityEngine.UI.Image>();
                var size = new Vector2(cs.widthU / scale.x, cs.heightU / scale.y);
                var local = refs.trees.InverseTransformPoint(new Vector3(d.bottomCenter.x, d.bottomCenter.y + cs.heightU / 2f, refs.trees.position.z));
                var anchored = new Vector2(local.x, local.y);
                bool changed = false;
                if (rt.anchorMin != half || rt.anchorMax != half || rt.pivot != half || !Approx(rt.sizeDelta, size) || !Approx(rt.anchoredPosition, anchored) || !Approx(rt.localPosition.z, 0f))
                {
                    Undo.RecordObject(rt, "City Build");
                    rt.anchorMin = half; rt.anchorMax = half; rt.pivot = half;
                    rt.sizeDelta = size; rt.anchoredPosition = anchored;
                    rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);
                    changed = true;
                }
                if (img.sprite != sprite || img.raycastTarget)
                {
                    Undo.RecordObject(img, "City Build");
                    img.sprite = sprite;
                    img.raycastTarget = false; // decisão: decorações não recebem cliques
                    changed = true;
                }
                if (!created) Changed(changed);
            }
            foreach (var kv in existing) Destroy(kv.Value.gameObject, "id fora do JSON");
        }

        // ---------- Limiter (R27) ----------

        void FitLimiter()
        {
            var bb = PolygonBox();
            var t = refs.limiter.transform;
            var spriteSize = refs.limiter.sprite.bounds.size;
            var parentScale = t.parent ? t.parent.lossyScale : Vector3.one;
            var pos = new Vector3(bb.center.x, bb.center.y, t.position.z);
            var scale = new Vector3(bb.width / spriteSize.x / parentScale.x, bb.height / spriteSize.y / parentScale.y, t.localScale.z);
            bool changed = (t.position - pos).sqrMagnitude > 1e-10f || (t.localScale - scale).sqrMagnitude > 1e-10f;
            if (changed)
            {
                Undo.RecordObject(t, "City Build");
                t.position = pos; t.localScale = scale;
            }
            Changed(changed);
        }

        // ---------- enquadramento dos chãos (R34) ----------

        public static float RequiredOrtho(Rect bb, Vector2 center)
        {
            float halfW = Mathf.Max(Mathf.Abs(bb.xMin - center.x), Mathf.Abs(bb.xMax - center.x));
            float halfH = Mathf.Max(Mathf.Abs(bb.yMin - center.y), Mathf.Abs(bb.yMax - center.y));
            return Mathf.Max(halfH, halfW / DisplayAspect);
        }

        void FitPolygonFraming()
        {
            Vector2 c = refs.mainCamera.transform.position;
            float ortho = RequiredOrtho(PolygonBox(), c);
            bool changed = false;
            if (!Approx(refs.polygonCamera.orthographicSize, ortho))
            {
                Undo.RecordObject(refs.polygonCamera, "City Build");
                refs.polygonCamera.orthographicSize = ortho;
                changed = true;
            }
            var rt = refs.polygonsDisplay;
            var parentScale = rt.parent.lossyScale;
            var size = new Vector2(2f * ortho * DisplayAspect / parentScale.x, 2f * ortho / parentScale.y);
            var half = new Vector2(0.5f, 0.5f);
            var pos = new Vector3(c.x, c.y, rt.position.z);
            if (rt.anchorMin != half || rt.anchorMax != half || rt.pivot != half || !Approx(rt.sizeDelta, size) || ((Vector2)rt.position - c).sqrMagnitude > 1e-8f)
            {
                Undo.RecordObject(rt, "City Build");
                rt.anchorMin = half; rt.anchorMax = half; rt.pivot = half;
                rt.sizeDelta = size;
                rt.position = pos;
                changed = true;
            }
            Changed(changed);
        }
    }
}
