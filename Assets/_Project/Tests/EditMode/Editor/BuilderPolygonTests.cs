using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DefesaCivil.CityGen.Tests
{
    public class BuilderPolygonTests
    {
        static SpriteCatalog cat;
        TestCityScene ts;
        CityLayout layout;

        [OneTimeSetUp] public void Load() => cat = SpriteCatalog.Load();
        [SetUp] public void SetUp()
        {
            ts = TestCityScene.Create();
            layout = CityGenerator.Generate(424242, ts.center, cat, "t");
        }
        [TearDown] public void TearDown() => ts.Close();

        BuildResult Build() => new CityBuilder(layout, ts.refs, cat, undo: false).Run();
        List<Transform> Polys() => ts.refs.polygonLayer.Cast<Transform>().Where(CitySceneRefs.IsGenerated).ToList();

        [Test] public void CreatesOnePolygonPerEntryAndRemovesOldPolygon()
        {
            Build();
            Assert.AreEqual(layout.polygons.Count, Polys().Count);
            Assert.IsNull(ts.refs.polygonLayer.Find("Polygon"), "Polygon antigo removido");
            Assert.IsNotNull(ts.bgCanvas, "Canvas/BG intocado");
            Assert.AreEqual(ts.refs.polygonLayer, ts.bgCanvas.transform.parent);
        }

        [Test] public void PathMatchesWorldPoints()
        {
            Build();
            var p = layout.polygons[0];
            var t = ts.refs.polygonLayer.Find(CitySceneRefs.Name("Polygon", p.id));
            var path = t.GetComponent<PolygonCollider2D>().GetPath(0);
            for (int i = 0; i < path.Length; i++)
            {
                var w = (Vector2)ts.refs.polygonLayer.TransformPoint(path[i]);
                Assert.That(Vector2.Distance(w, p.points[i]), Is.LessThan(1e-4f));
            }
            Assert.IsNotNull(t.GetComponent<MeshFilter>().sharedMesh, "mesh gerada");
        }

        [Test] public void LimiterEqualsPolygonBox()
        {
            Build();
            var bb = CityGeometry.BoundingBox(layout.polygons.Select(p => (IList<Vector2>)CityGeometry.ToVectors(p.points)));
            float m = layout.limiterMargin;
            bb = Rect.MinMaxRect(bb.xMin - m, bb.yMin - m, bb.xMax + m, bb.yMax + m);
            var b = ts.refs.limiter.bounds;
            Assert.That(Mathf.Abs(b.min.x - bb.xMin) + Mathf.Abs(b.min.y - bb.yMin) + Mathf.Abs(b.max.x - bb.xMax) + Mathf.Abs(b.max.y - bb.yMax), Is.LessThan(0.01f));
        }

        [Test] public void FramingCoversCity()
        {
            Build();
            var bb = CityGeometry.BoundingBox(layout.polygons.Select(p => (IList<Vector2>)CityGeometry.ToVectors(p.points)));
            float ortho = ts.refs.polygonCamera.orthographicSize;
            Assert.That(ortho * CityBuilder.DisplayAspect * 2f, Is.GreaterThanOrEqualTo(bb.width - 1e-3f));
            Assert.That(ortho * 2f, Is.GreaterThanOrEqualTo(bb.height - 1e-3f));
            var c = new Vector3[4]; ts.refs.polygonsDisplay.GetWorldCorners(c);
            Assert.AreEqual(ortho * 2f * CityBuilder.DisplayAspect, c[2].x - c[0].x, 0.01f);
            Assert.AreEqual(ortho * 2f, c[2].y - c[0].y, 0.01f);
            Assert.AreEqual(ts.center.x, (c[0].x + c[2].x) / 2f, 0.01f);
            Assert.AreEqual(ts.center.y, (c[0].y + c[2].y) / 2f, 0.01f);
        }

        [Test] public void SecondBuildChangesNothing()
        {
            Build();
            var dump = CityDump.Canonical(ts.refs);
            var ids = Polys().ToList();
            var r = Build();
            Assert.AreEqual(0, r.created + r.updated + r.deleted, r.ToString());
            Assert.AreEqual(dump, CityDump.Canonical(ts.refs));
            CollectionAssert.AreEqual(ids, Polys().ToList());
        }

        [Test] public void RemovingOneIdDeletesOnlyIt()
        {
            Build();
            var keep = Polys().Where(t => t.name != CitySceneRefs.Name("Polygon", layout.polygons[0].id)).ToList();
            layout.polygons.RemoveAt(0);
            var r = Build();
            Assert.AreEqual(1, r.deleted);
            CollectionAssert.AreEqual(keep, Polys().ToList());
        }
    }
}
