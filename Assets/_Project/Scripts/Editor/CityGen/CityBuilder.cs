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
