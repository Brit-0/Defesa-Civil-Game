using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DefesaCivil.CityGen.Tests
{
    public class BuilderStructureTests
    {
        static SpriteCatalog cat;
        TestCityScene ts;
        CityLayout layout;

        [OneTimeSetUp] public void Load() => cat = SpriteCatalog.Load();
        [SetUp] public void SetUp()
        {
            ts = TestCityScene.Create();
            layout = CityGenerator.Generate(31337, ts.center, cat, "t");
        }
        [TearDown] public void TearDown() => ts.Close();

        BuildResult Build() => new CityBuilder(layout, ts.refs, cat, undo: false).Run();
        List<Transform> Generated(Transform p) => p.Cast<Transform>().Where(CitySceneRefs.IsGenerated).ToList();
        Transform Of(StructureData s) => ts.refs.houses.Find(CitySceneRefs.Name(s.type, s.id));

        [Test] public void CreatesAllStructuresAndMigratesOldOnes()
        {
            Build();
            Assert.AreEqual(layout.structures.Count, Generated(ts.refs.houses).Count);
            Assert.AreEqual(1, ts.refs.houses.GetComponentsInChildren<CityGuard>(true).Length);
            Assert.AreEqual(2, ts.refs.houses.GetComponentsInChildren<Warehouse>(true).Length);
            foreach (var n in new[] { "House", "Guard", "Warehouse" }) Assert.IsNull(ts.refs.uiContainer.Find(n), n + " antigo removido");
            Assert.IsNull(ts.refs.waypoints.Find("Waypoint"), "Waypoint antigo removido");
        }

        [Test] public void SpriteSizeAndColliderPerInstance()
        {
            Build();
            foreach (var s in layout.structures)
            {
                var t = (RectTransform)Of(s);
                var cs = cat.Get(s.sprite);
                Assert.AreEqual(s.sprite.name, t.Find("Visual").GetComponent<Image>().sprite.name);
                var c = new Vector3[4]; t.GetWorldCorners(c);
                Assert.AreEqual(cs.widthU, c[2].x - c[0].x, 0.01f, "largura nativa");
                Assert.AreEqual(cs.heightU, c[2].y - c[0].y, 0.01f, "altura nativa");
                Vector2 rc = cs.RectCenterFor(s.bottomCenter);
                Assert.AreEqual(rc.y, (c[0].y + c[2].y) / 2f, 0.01f, "centro Y do retângulo");
                Assert.AreEqual(rc.x, (c[0].x + c[2].x) / 2f, 0.01f, "centro X do retângulo");
                Assert.AreEqual(s.bottomCenter.y, c[0].y + cs.padBottomU, 0.01f, "base visível");
                var box = t.GetComponent<BoxCollider2D>();
                Assert.AreEqual(t.sizeDelta.x, box.size.x, 1e-3f); Assert.AreEqual(t.sizeDelta.y, box.size.y, 1e-3f);
            }
            var prefabSprite = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Systems/Houses/House.prefab").transform.Find("Visual").GetComponent<Image>().sprite;
            Assert.AreEqual("Rocha_Alta_J_NE", prefabSprite.name, "prefab mantém o PLACEHOLDER");
        }

        [Test] public void EntriesAndGps()
        {
            Build();
            foreach (var s in layout.structures)
            {
                var h = Of(s).GetComponent<House>();
                Assert.IsNotNull(h.entry);
                Assert.AreEqual(ts.refs.waypoints, h.entry.transform.parent);
                Assert.That(Vector2.Distance(h.entry.transform.position, s.entry), Is.LessThan(0.01f));
            }
            var so = new SerializedObject(ts.refs.gps);
            Assert.AreEqual(layout.structures.Count + layout.junctions.Count, so.FindProperty("allWaypoints").arraySize);
            Assert.AreEqual(3, so.FindProperty("allLocations").arraySize);
        }

        [Test] public void NeighborsAreSymmetricAndMatchLinks()
        {
            Build();
            var wps = ts.refs.waypoints.Cast<Transform>().Where(CitySceneRefs.IsGenerated).Select(t => t.GetComponent<Waypoint>()).ToList();
            Assert.AreEqual(layout.structures.Count + layout.junctions.Count, wps.Count);
            int links = 0;
            foreach (var w in wps)
            {
                CollectionAssert.DoesNotContain(w.neighbors, w, w.name + " não pode estar na própria lista");
                CollectionAssert.AllItemsAreUnique(w.neighbors);
                foreach (var n in w.neighbors) CollectionAssert.Contains(n.neighbors, w, w.name + " ↔ " + n.name);
                links += w.neighbors.Count;
            }
            Assert.AreEqual(layout.links.Count * 2, links);
            Assert.IsTrue(wps.All(w => w.neighbors.Count > 0), "todo waypoint gerado tem ao menos um vizinho");
        }

        [Test] public void DrawOrderFollowsYAcrossHousesTreesAndWorkers()
        {
            Build();
            var items = new List<(float y, int order, int kind)>();
            foreach (Transform c in ts.refs.houses) { var ys = c.GetComponent<YSortCanvas>(); Assert.IsNotNull(ys, c.name); Assert.IsTrue(c.GetComponent<Canvas>().overrideSorting); Assert.IsNotNull(c.GetComponent<UnityEngine.UI.GraphicRaycaster>(), "estrutura clicável"); items.Add((ys.BaseY(), c.GetComponent<Canvas>().sortingOrder, 0)); }
            foreach (Transform c in ts.refs.trees) { var ys = c.GetComponent<YSortCanvas>(); Assert.IsNotNull(ys, c.name); items.Add((ys.BaseY(), c.GetComponent<Canvas>().sortingOrder, 1)); }
            // worker simulado: mais ao sul que tudo → na frente de tudo; mais ao norte → atrás de tudo
            var w = new GameObject("W", typeof(RectTransform)); w.transform.SetParent(ts.uiRoot, false);
            var wc = w.AddComponent<Canvas>(); var wy = w.AddComponent<YSortCanvas>(); wy.kind = YSortCanvas.Kind_Worker; wy.dynamic = true;
            float minY = items.Min(i => i.y), maxY = items.Max(i => i.y);
            w.transform.position = new Vector3(0, minY - 3f, 0); wy.Apply();
            Assert.Greater(wc.sortingOrder, items.Max(i => i.order), "ao sul de tudo: na frente");
            w.transform.position = new Vector3(0, maxY + 3f, 0); wy.Apply();
            Assert.Less(wc.sortingOrder, items.Min(i => i.order), "ao norte de tudo: atrás");
            // ordem estritamente monotônica no y (itens com y maior têm ordem menor)
            foreach (var a in items) foreach (var b in items.Take(40))
                if (a.y > b.y + 0.05f) Assert.Less(a.order, b.order);
            Assert.Less(items.Max(i => i.order), YSortCanvas.OverlayOrder);
        }

        [Test] public void YSortedSiblings()
        {
            Build();
            var ys = Generated(ts.refs.houses).Select(t => layout.structures.First(s => CitySceneRefs.Name(s.type, s.id) == t.name).bottomCenter.y).ToList();
            for (int i = 1; i < ys.Count; i++) Assert.That(ys[i], Is.LessThanOrEqualTo(ys[i - 1] + 1e-5f));
        }

        [Test] public void SecondBuildIsIdempotent()
        {
            Build();
            var dump = CityDump.Canonical(ts.refs);
            var r = Build();
            Assert.AreEqual(0, r.created + r.updated + r.deleted, r.ToString() + "\n" + string.Join("\n", r.log));
            Assert.AreEqual(dump, CityDump.Canonical(ts.refs));
        }

        [Test] public void ChangingOneSpriteTouchesOnlyThatObject()
        {
            Build();
            var before = Generated(ts.refs.houses).ToList();
            var s = layout.structures.First(x => x.type == StructureType.House);
            var other = cat.HousePool(s.district).First(c => c.name != s.sprite.name);
            s.sprite = other.ToRef();
            var r = Build();
            Assert.AreEqual(1, r.updated, r.ToString());
            Assert.AreEqual(0, r.created + r.deleted);
            CollectionAssert.AreEquivalent(before, Generated(ts.refs.houses));
            Assert.AreEqual(other.name, Of(s).Find("Visual").GetComponent<Image>().sprite.name);
        }

        [Test] public void RemovingStructureRemovesItsWaypoint()
        {
            Build();
            var s = layout.structures.Last(x => x.type == StructureType.House);
            layout.structures.Remove(s);
            Build();
            Assert.IsNull(Of(s));
            Assert.IsNull(ts.refs.waypoints.Find(CitySceneRefs.Name("Waypoint", s.type + " " + s.id)));
        }
    }
}
