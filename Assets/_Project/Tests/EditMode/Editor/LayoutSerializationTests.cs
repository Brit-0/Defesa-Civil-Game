using System;
using NUnit.Framework;

namespace DefesaCivil.CityGen.Tests
{
    public class LayoutSerializationTests
    {
        static CityLayout Sample()
        {
            var l = new CityLayout { seed = 42, generatedAt = "2026-11-13T00:00:00Z", center = new Vec2(-3.44f, -0.15f) };
            var p = new PolygonData { id = "001", kind = PolygonKind.Block, district = District.Suburban };
            p.points.Add(new Vec2(0, 0)); p.points.Add(new Vec2(4, 0)); p.points.Add(new Vec2(4, 3)); p.points.Add(new Vec2(0, 3));
            l.polygons.Add(p);
            l.structures.Add(new StructureData
            {
                id = "001", type = StructureType.Guard, district = District.Industrial, polygonId = "001",
                sprite = new SpriteRef { guid = "abc", name = "Fabrica_A_S", path = "x.png" },
                bottomCenter = new Vec2(2, 0.3f), entry = new Vec2(2, -0.4f)
            });
            l.decorations.Add(new DecorationData { id = "001", polygonId = "001", sprite = new SpriteRef { guid = "def", name = "Pinheiro_S" }, bottomCenter = new Vec2(1, 2.8f) });
            return l;
        }

        [Test]
        public void RoundTripKeepsAllFields()
        {
            var a = Sample();
            var b = CityLayout.FromJson(a.ToJson());
            Assert.AreEqual(a.ToJson(), b.ToJson());
            Assert.AreEqual(42, b.seed);
            Assert.AreEqual(4, b.polygons[0].points.Count);
            Assert.AreEqual(StructureType.Guard, b.structures[0].type);
            Assert.AreEqual(-0.4f, b.structures[0].entry.y, 1e-6f);
            Assert.AreEqual("Pinheiro_S", b.decorations[0].sprite.name);
            Assert.AreEqual(-3.44f, b.center.x, 1e-6f);
        }

        [Test]
        public void MissingSeedIsRejected()
        {
            Assert.Throws<ArgumentException>(() => CityLayout.FromJson("{\"version\":2}"));
        }

        [Test]
        public void WrongVersionIsRejected()
        {
            Assert.Throws<ArgumentException>(() => CityLayout.FromJson("{\"version\":99,\"seed\":1}"));
        }

        [Test]
        public void EmptyJsonIsRejected()
        {
            Assert.Throws<ArgumentException>(() => CityLayout.FromJson(" "));
        }
    }
}
