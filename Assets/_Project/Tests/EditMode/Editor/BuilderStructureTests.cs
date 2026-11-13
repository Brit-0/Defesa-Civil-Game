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
            Assert.IsNotNull(ts.refs.waypoints.Find("Waypoint"), "Waypoint antigo mantido (R15)");
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
                Assert.AreEqual(s.bottomCenter.y, c[0].y, 0.01f, "borda inferior");
                Assert.AreEqual(s.bottomCenter.x, (c[0].x + c[2].x) / 2f, 0.01f, "centro inferior");
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
            Assert.AreEqual(layout.structures.Count, so.FindProperty("allWaypoints").arraySize);
            Assert.AreEqual(3, so.FindProperty("allLocations").arraySize);
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
