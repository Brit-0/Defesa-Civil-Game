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
                var hard = r.violations.Where(v => v.ruleId != "R09").ToList(); // proporção dos bairros varia com o sorteio
                Assert.AreEqual(0, hard.Count, $"semente {seed}: " + string.Join(" | ", hard.Take(5)));
            }
        }

        [Test] public void CountsWithinRanges()
        {
            var l = CityGenerator.Generate(777, Center, cat, "t");
            int houses = l.structures.Count(s => s.type == StructureType.House);
            Assert.That(houses, Is.InRange(LayoutValidator.MinHouses, LayoutValidator.MaxHouses));
            Assert.AreEqual(1, l.structures.Count(s => s.type == StructureType.Guard));
            Assert.AreEqual(2, l.structures.Count(s => s.type == StructureType.Warehouse));
            Assert.That(l.decorations.Count, Is.GreaterThanOrEqualTo(2 * houses));
            Assert.AreEqual(6, l.polygons.Count(p => p.kind == PolygonKind.Plaza), "2 parques + 4 bosques");
            Assert.AreEqual(CityGenerator.Street, l.limiterMargin, "rua de contorno");
            Assert.AreEqual(777, l.seed);
            Assert.IsTrue(l.structures.Any(s => s.facing == Facing.North), "fileira de cima");
            Assert.IsTrue(l.structures.Where(s => s.facing == Facing.North).All(s => s.sprite.name.EndsWith("_N")));
            Assert.IsFalse(l.decorations.Any(d => cat.Get(d.sprite).category == "Carros"), "nenhum carro");
            Assert.Greater(l.junctions.Count, 0);
        }

        [Test] public void JsonRoundTripOfGeneratedLayout()
        {
            var l = CityGenerator.Generate(99, Center, cat, "t");
            Assert.AreEqual(l.ToJson(), CityLayout.FromJson(l.ToJson()).ToJson());
        }
    }
}
