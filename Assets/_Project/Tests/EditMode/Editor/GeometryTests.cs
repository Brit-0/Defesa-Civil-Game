using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DefesaCivil.CityGen.Tests
{
    public class GeometryTests
    {
        internal static List<Vector2> Rect(float x0, float y0, float x1, float y1) =>
            new List<Vector2> { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1) };

        [Test]
        public void PointInPolygon_InsideBorderOutside()
        {
            var r = Rect(0, 0, 4, 2);
            Assert.IsTrue(CityGeometry.PointInPolygon(new Vector2(1, 1), r));
            Assert.IsTrue(CityGeometry.PointInPolygon(new Vector2(4, 1), r));
            Assert.IsFalse(CityGeometry.PointInPolygon(new Vector2(4.1f, 1), r));
            var tri = new List<Vector2> { new Vector2(0, 0), new Vector2(4, 0), new Vector2(0, 4) };
            Assert.IsTrue(CityGeometry.PointInPolygon(new Vector2(1, 1), tri));
            Assert.IsFalse(CityGeometry.PointInPolygon(new Vector2(3, 3), tri));
        }

        [Test]
        public void DistanceToBoundary()
        {
            Assert.AreEqual(0.5f, CityGeometry.DistanceToBoundary(new Vector2(2, 0.5f), Rect(0, 0, 4, 2)), 1e-5f);
        }

        [Test]
        public void PolygonDistance_GapOverlapContain()
        {
            Assert.AreEqual(0.8f, CityGeometry.PolygonDistance(Rect(0, 0, 2, 2), Rect(2.8f, 0, 5, 2)), 1e-5f);
            Assert.AreEqual(0f, CityGeometry.PolygonDistance(Rect(0, 0, 2, 2), Rect(1, 1, 3, 3)));
            Assert.AreEqual(0f, CityGeometry.PolygonDistance(Rect(0, 0, 4, 4), Rect(1, 1, 2, 2)));
            Assert.IsTrue(CityGeometry.PolygonsOverlap(Rect(0, 0, 2, 2), Rect(2, 0, 3, 2)));
            Assert.AreEqual(1f, CityGeometry.PolygonDistance(Rect(0, 0, 1, 1), Rect(0, 2, 1, 3)), 1e-5f);
        }

        [Test]
        public void AxisAlignedRectangle()
        {
            Assert.IsTrue(CityGeometry.IsAxisAlignedRectangle(Rect(0, 0, 3, 1)));
            var rotated = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 2), new Vector2(-1, 1) };
            Assert.IsFalse(CityGeometry.IsAxisAlignedRectangle(rotated));
            var tri = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1) };
            Assert.IsFalse(CityGeometry.IsAxisAlignedRectangle(tri));
        }

        [Test]
        public void BoundingBox()
        {
            var bb = CityGeometry.BoundingBox(new[] { (IList<Vector2>)Rect(0, 0, 1, 1), Rect(3, -2, 4, 5) });
            Assert.AreEqual(0f, bb.xMin); Assert.AreEqual(-2f, bb.yMin);
            Assert.AreEqual(4f, bb.xMax); Assert.AreEqual(5f, bb.yMax);
        }

        [Test]
        public void SouthStreet_BetweenBlocks()
        {
            var polys = new List<IList<Vector2>> { Rect(0, 5, 4, 9), Rect(0, 0, 4, 4.2f) };
            Assert.IsTrue(CityGeometry.SouthStreetSegment(new Vector2(2, 5.3f), polys, 0f, out var top, out var bottom));
            Assert.AreEqual(5f, top, 1e-4f); Assert.AreEqual(4.2f, bottom, 1e-4f);
        }

        [Test]
        public void SouthStreet_BottomRowHasNone()
        {
            var polys = new List<IList<Vector2>> { Rect(0, 5, 4, 9), Rect(0, 0, 4, 4.2f) };
            Assert.IsFalse(CityGeometry.SouthStreetSegment(new Vector2(2, 0.3f), polys, 0f, out _, out _));
        }

        [Test]
        public void SouthStreet_OpenToLimiter()
        {
            var polys = new List<IList<Vector2>> { Rect(0, 5, 4, 9), Rect(5, 0, 9, 4) };
            Assert.IsTrue(CityGeometry.SouthStreetSegment(new Vector2(2, 6f), polys, 0f, out var top, out var bottom));
            Assert.AreEqual(5f, top, 1e-4f); Assert.AreEqual(0f, bottom, 1e-4f);
        }

        [Test]
        public void SouthStreet_StartOutsideFails()
        {
            var polys = new List<IList<Vector2>> { Rect(0, 5, 4, 9) };
            Assert.IsFalse(CityGeometry.SouthStreetSegment(new Vector2(2, 4.5f), polys, 0f, out _, out _));
        }
    }
}
