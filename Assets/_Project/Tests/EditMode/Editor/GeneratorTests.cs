using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DefesaCivil.CityGen.Tests
{
    public class GeneratorTests
    {
        static SpriteCatalog cat;
        static readonly Vector2 Center = new Vector2(-3.44f, -0.15f);
        [OneTimeSetUp] public void Load() => cat = SpriteCatalog.Load();

        [Test] public void SameSeedSameLayout()
        {
            var a = CityGenerator.Generate(12345, Center, cat, "t");
            var b = CityGenerator.Generate(12345, Center, cat, "t");
            Assert.AreEqual(a.ToJson(), b.ToJson());
            var c = CityGenerator.Generate(54321, Center, cat, "t");
            Assert.AreNotEqual(a.ToJson(), c.ToJson());
        }

        [Test] public void TwentySeedsHaveNoErrors()
        {
            var rng = new System.Random(2026);
            for (int i = 0; i < 20; i++)
            {
                int seed = rng.Next(1, int.MaxValue);
                var l = CityGenerator.Generate(seed, Center, cat, "t");
                var r = LayoutValidator.Validate(l, cat);
                Assert.AreEqual(0, r.errors, $"semente {seed}: " + string.Join(" | ", r.violations.Where(v => v.severity == Severity.Error).Take(5)));
                Assert.AreEqual(0, r.warnings, $"semente {seed}: " + string.Join(" | ", r.violations.Take(5)));
            }
        }

        [Test] public void CountsWithinRanges()
        {
            var l = CityGenerator.Generate(777, Center, cat, "t");
            int houses = l.structures.Count(s => s.type == StructureType.House);
            Assert.That(houses, Is.InRange(50, 80));
            Assert.AreEqual(1, l.structures.Count(s => s.type == StructureType.Guard));
            Assert.AreEqual(2, l.structures.Count(s => s.type == StructureType.Warehouse));
            Assert.That(l.decorations.Count, Is.InRange(2 * houses, 3 * houses));
            Assert.That(l.polygons.Count(p => p.kind == PolygonKind.Plaza), Is.InRange(2, 4));
            Assert.AreEqual(777, l.seed);
        }

        [Test] public void JsonRoundTripOfGeneratedLayout()
        {
            var l = CityGenerator.Generate(99, Center, cat, "t");
            Assert.AreEqual(l.ToJson(), CityLayout.FromJson(l.ToJson()).ToJson());
        }
    }
}
