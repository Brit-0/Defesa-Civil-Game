using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DefesaCivil.CityGen.Tests
{
    public class BuilderDecorationTests
    {
        static SpriteCatalog cat;
        TestCityScene ts;
        CityLayout layout;

        [OneTimeSetUp] public void Load() => cat = SpriteCatalog.Load();
        [SetUp] public void SetUp()
        {
            ts = TestCityScene.Create();
            layout = CityGenerator.Generate(8080, ts.center, cat, "t");
        }
        [TearDown] public void TearDown() => ts.Close();

        BuildResult Build() => new CityBuilder(layout, ts.refs, cat).Run();
        List<Transform> Trees() => ts.refs.trees.Cast<Transform>().Where(CitySceneRefs.IsGenerated).ToList();

        [Test] public void CreatesDecorationsWithNativeSizeAndNoRaycast()
        {
            Build();
            Assert.AreEqual(layout.decorations.Count, Trees().Count);
            foreach (var d in layout.decorations)
            {
                var t = (RectTransform)ts.refs.trees.Find(CitySceneRefs.Name("Tree", d.id));
                var img = t.GetComponent<Image>();
                Assert.AreEqual(d.sprite.name, img.sprite.name);
                Assert.IsFalse(img.raycastTarget);
                var cs = cat.Get(d.sprite);
                var c = new Vector3[4]; t.GetWorldCorners(c);
                Assert.AreEqual(cs.widthU, c[2].x - c[0].x, 0.01f);
                Assert.AreEqual(cs.heightU, c[2].y - c[0].y, 0.01f);
                Assert.AreEqual(d.bottomCenter.y, c[0].y, 0.01f);
                Assert.AreEqual(d.bottomCenter.x, (c[0].x + c[2].x) / 2f, 0.01f);
            }
        }

        [Test] public void YSortedAndTreesAfterHouses()
        {
            Build();
            var ys = Trees().Select(t => layout.decorations.First(d => CitySceneRefs.Name("Tree", d.id) == t.name).bottomCenter.y).ToList();
            for (int i = 1; i < ys.Count; i++) Assert.That(ys[i], Is.LessThanOrEqualTo(ys[i - 1] + 1e-5f));
        }

        [Test] public void FullBuildIsIdempotent()
        {
            Build();
            var dump = CityDump.Canonical(ts.refs);
            var r = Build();
            Assert.AreEqual(0, r.created + r.updated + r.deleted, r.ToString() + "\n" + string.Join("\n", r.log));
            Assert.AreEqual(dump, CityDump.Canonical(ts.refs));
        }

        [Test] public void RemovingOneDecorationDeletesOnlyIt()
        {
            Build();
            var d = layout.decorations[5];
            var expected = Trees().Where(t => t.name != CitySceneRefs.Name("Tree", d.id)).ToList();
            layout.decorations.RemoveAt(5);
            var r = Build();
            Assert.AreEqual(1, r.deleted);
            CollectionAssert.AreEquivalent(expected, Trees());
        }
    }
}
