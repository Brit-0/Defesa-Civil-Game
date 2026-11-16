using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DefesaCivil.CityGen.Tests
{
    public class LayoutValidatorTests
    {
        static SpriteCatalog cat;
        [OneTimeSetUp] public void Load() => cat = SpriteCatalog.Load();

        static PolygonData Poly(string id, float x0, float y0, float x1, float y1, string kind = PolygonKind.Block)
        {
            var p = new PolygonData { id = id, kind = kind };
            foreach (var v in GeometryTests.Rect(x0, y0, x1, y1)) p.points.Add(v);
            return p;
        }

        /// <summary>Dois quarteirões (topo 5–10 e base 0–4,2), centro registrado no centro da caixa.</summary>
        static CityLayout Base()
        {
            var l = new CityLayout { seed = 1, center = new Vec2(10, 5) };
            l.polygons.Add(Poly("001", 0, 5, 20, 10));
            l.polygons.Add(Poly("002", 0, 0, 20, 4.2f));
            return l;
        }

        StructureData S(CityLayout l, string id, string type, CatalogSprite cs, float x, float y)
        {
            var s = new StructureData { id = id, type = type, district = cs.category, sprite = cs.ToRef(), bottomCenter = new Vec2(x, y) };
            var g = LayoutValidator.BuildGeo(l);
            var e = LayoutValidator.ExpectedEntry(g, new Vector2(x, y));
            s.entry = e.HasValue ? (Vec2)e.Value : new Vec2(x, y - 0.4f);
            l.structures.Add(s);
            return s;
        }

        DecorationData D(CityLayout l, string id, CatalogSprite cs, float x, float y)
        {
            var d = new DecorationData { id = id, sprite = cs.ToRef(), bottomCenter = new Vec2(x, y) };
            l.decorations.Add(d);
            return d;
        }

        ValidationReport V(CityLayout l) => LayoutValidator.Validate(l, cat);
        CatalogSprite Sub => cat.Suburban[0];
        CatalogSprite Com => cat.Commercial[0];
        CatalogSprite Ind => cat.Industrial[0];
        CatalogSprite Deco => cat.Decoration.First(d => d.widthU <= 2f);

        [Test] public void R01_HouseCount()
        {
            var l = Base(); S(l, "001", StructureType.House, Sub, 3, 5.3f);
            Assert.IsTrue(V(l).Has("R01"));
            for (int i = 2; i <= 50; i++) S(l, i.ToString("000"), StructureType.House, Sub, 3, 5.3f);
            Assert.IsFalse(V(l).Has("R01"));
        }

        [Test] public void R02_R03_GuardAndWarehouseCounts()
        {
            var l = Base();
            Assert.IsTrue(V(l).Has("R02")); Assert.IsTrue(V(l).Has("R03"));
            S(l, "001", StructureType.Guard, Ind, 3, 5.3f);
            S(l, "001", StructureType.Warehouse, Ind, 8, 5.3f); S(l, "002", StructureType.Warehouse, Ind, 14, 5.3f);
            Assert.IsFalse(V(l).Has("R02")); Assert.IsFalse(V(l).Has("R03"));
        }

        [Test] public void R05_SuffixS()
        {
            var l = Base(); var s = S(l, "001", StructureType.House, Sub, 3, 5.3f);
            Assert.IsFalse(V(l).Has("R05"));
            s.sprite.name = s.sprite.name.Replace("_S", "_N");
            Assert.IsTrue(V(l).Has("R05"));
        }

        [Test] public void R05_NorthRowUsesNSprites()
        {
            var l = Base(); var s = S(l, "001", StructureType.House, Sub, 3, 6.5f);
            s.facing = Facing.North;
            Assert.IsTrue(V(l).Has("R05"), "fileira N com sprite _S");
            s.sprite = cat.North(Sub).ToRef();
            Assert.IsFalse(V(l).Has("R05"));
        }

        [Test] public void R14_NorthEntryIsOnStreetAbove()
        {
            var l = Base();
            var g = LayoutValidator.BuildGeo(l);
            var e = LayoutValidator.ExpectedEntry(g, new Vector2(3, 2f), Facing.North, 0.8f);
            Assert.IsTrue(e.HasValue);
            Assert.AreEqual(4.6f, e.Value.y, 1e-4f, "rua entre 4,2 e 5,0");
            Assert.IsNull(LayoutValidator.ExpectedEntry(g, new Vector2(3, 7f), Facing.North, 0.8f), "fileira de cima da cidade sem rua de contorno");
        }

        [Test] public void R38_WaypointGraph()
        {
            var l = new CityLayout { seed = 1, center = new Vec2(10.4f, 4.6f), limiterMargin = 0.8f };
            l.polygons.Add(Poly("001", 0, 0, 10, 4)); l.polygons.Add(Poly("002", 10.8f, 0, 20.8f, 4));
            l.polygons.Add(Poly("003", 0, 4.8f, 20.8f, 9.2f));
            S(l, "001", StructureType.House, Sub, 3, 0.2f); S(l, "002", StructureType.House, Sub, 14, 0.2f);
            var n = S(l, "003", StructureType.House, cat.North(Sub), 6, 1.5f); n.facing = Facing.North;
            var g = LayoutValidator.BuildGeo(l);
            foreach (var s in l.structures) s.entry = LayoutValidator.ExpectedEntry(g, s.bottomCenter, s.facing, 0.8f).Value;
            StreetGraph.Fill(l);
            Assert.IsFalse(V(l).Has("R38"), string.Join(" | ", V(l).violations.Where(v => v.ruleId == "R38").Select(v => v.ToString())));
            Assert.Greater(l.junctions.Count, 0);
            Assert.IsTrue(l.links.All(k => k.a != k.b));
            // House 003 (fileira N do quarteirão de baixo) fica na mesma rua que nenhuma outra entrada: liga-se a cruzamentos
            Assert.IsTrue(l.links.Any(k => k.a == "House 003" || k.b == "House 003"));
            var removed = l.links[0]; l.links.RemoveAt(0);
            Assert.IsTrue(V(l).Has("R38"), "falta ligação");
            l.links.Add(removed);
            l.links.Add(new WaypointLink { a = "House 001", b = "House 001" });
            Assert.IsTrue(V(l).Has("R38"), "ligado a si mesmo");
        }

        [Test] public void R06_HousePool()
        {
            var l = Base(); S(l, "001", StructureType.House, Sub, 3, 5.3f);
            Assert.IsFalse(V(l).Has("R06"));
            S(l, "002", StructureType.House, Deco, 8, 5.3f);
            Assert.IsTrue(V(l).Has("R06"));
        }

        [Test] public void R07_GuardWarehousePool()
        {
            var l = Base(); S(l, "001", StructureType.Guard, Ind, 3, 5.3f);
            Assert.IsFalse(V(l).Has("R07"));
            S(l, "001", StructureType.Warehouse, Sub, 8, 5.3f);
            Assert.IsTrue(V(l).Has("R07"));
        }

        [Test] public void R09_ProportionIsWarning()
        {
            var l = Base();
            for (int i = 1; i <= 10; i++) S(l, i.ToString("000"), StructureType.House, Sub, 1 + i, 5.3f);
            var r = V(l);
            Assert.IsTrue(r.Has("R09"));
            Assert.AreEqual(Severity.Warning, r.violations.First(v => v.ruleId == "R09").severity);
            l = Base();
            for (int i = 1; i <= 12; i++) S(l, i.ToString("000"), StructureType.House, Sub, 1 + i, 5.3f);
            for (int i = 13; i <= 17; i++) S(l, i.ToString("000"), StructureType.House, Com, i - 11, 0.3f + 5f);
            for (int i = 18; i <= 20; i++) S(l, i.ToString("000"), StructureType.House, Ind, i - 16, 5.3f);
            Assert.IsFalse(V(l).Has("R09"));
        }

        [Test] public void R10_MajorityPerBlock()
        {
            var l = Base();
            for (int i = 1; i <= 5; i++) S(l, i.ToString("000"), StructureType.House, Sub, 1 + i, 5.3f);
            for (int i = 6; i <= 10; i++) S(l, i.ToString("000"), StructureType.House, Com, 1 + i, 5.3f);
            Assert.IsTrue(V(l).Has("R10"));
            l = Base();
            for (int i = 1; i <= 8; i++) S(l, i.ToString("000"), StructureType.House, Sub, 1 + i, 5.3f);
            for (int i = 9; i <= 10; i++) S(l, i.ToString("000"), StructureType.House, Com, 1 + i, 5.3f);
            Assert.IsFalse(V(l).Has("R10"));
        }

        [Test] public void R12_BottomCornersInsideBlock()
        {
            var l = Base(); S(l, "001", StructureType.House, Sub, 3, 5.3f);
            Assert.IsFalse(V(l).Has("R12"));
            S(l, "002", StructureType.House, Sub, 0.5f, 5.3f); // canto esquerdo para fora
            Assert.IsTrue(V(l).Has("R12"));
        }

        [Test] public void R13_SouthStreet()
        {
            var l = Base(); S(l, "001", StructureType.House, Sub, 3, 5.3f);
            Assert.IsFalse(V(l).Has("R13"));
            S(l, "002", StructureType.House, Sub, 3, 0.3f); // fileira mais ao sul
            Assert.IsTrue(V(l).Has("R13"));
        }

        [Test] public void R14_Entry()
        {
            var l = Base(); var s = S(l, "001", StructureType.House, Sub, 3, 5.3f);
            Assert.AreEqual(4.6f, s.entry.y, 1e-4f);
            Assert.IsFalse(V(l).Has("R14"));
            s.entry = new Vec2(3, 4.0f);
            Assert.IsTrue(V(l).Has("R14"));
        }

        [Test] public void R17_DecorationPool()
        {
            var l = Base(); D(l, "001", Deco, 5, 9.8f);
            Assert.IsFalse(V(l).Has("R17"));
            D(l, "002", Sub, 9, 9.8f);
            Assert.IsTrue(V(l).Has("R17"));
        }

        [Test] public void R19_DecorationCount()
        {
            var l = Base(); S(l, "001", StructureType.House, Sub, 3, 5.3f);
            Assert.IsTrue(V(l).Has("R19"));
            D(l, "001", Deco, 5, 9.8f); D(l, "002", Deco, 9, 9.8f);
            Assert.IsFalse(V(l).Has("R19"));
        }

        [Test] public void R20_DecorationInsidePolygon()
        {
            var l = Base(); D(l, "001", Deco, 5, 9.8f);
            Assert.IsFalse(V(l).Has("R20"));
            D(l, "002", Deco, 5, 4.6f); // na rua
            Assert.IsTrue(V(l).Has("R20"));
        }

        [Test] public void R35_UniformStreets()
        {
            var l = Base();
            Assert.IsFalse(V(l).Has("R35"));
            l.polygons[1] = Poly("002", 0, 0, 20, 3.5f); // rua de 1,5 u
            Assert.IsTrue(V(l).Has("R35"));
            // malha 2×2 com rua de contorno: cruzamento e contorno são aceitos
            l = new CityLayout { seed = 1, center = new Vec2(10.4f, 5.4f), limiterMargin = 0.8f };
            l.polygons.Add(Poly("001", 0, 0, 10, 5)); l.polygons.Add(Poly("002", 10.8f, 0, 20.8f, 5));
            l.polygons.Add(Poly("003", 0, 5.8f, 7, 10.8f)); l.polygons.Add(Poly("004", 7.8f, 5.8f, 20.8f, 10.8f));
            Assert.IsFalse(V(l).Has("R35"));
            l.limiterMargin = 1.5f;
            Assert.IsTrue(V(l).Has("R35"));
        }

        [Test] public void R36_DecorationBehindMustNotCoverStructure()
        {
            var l = Base(); S(l, "001", StructureType.House, Sub, 5, 5.3f);
            D(l, "001", Deco, 5, 5.2f); // à frente: pode cobrir
            Assert.IsFalse(V(l).Has("R36"));
            D(l, "002", Deco, 5, 6.5f); // atrás e por cima
            var r = V(l);
            Assert.IsTrue(r.Has("R36"));
            Assert.AreEqual(Severity.Warning, r.violations.First(v => v.ruleId == "R36").severity);
        }

        [Test] public void R37_StructuresDoNotOverlap()
        {
            var l = Base(); S(l, "001", StructureType.House, Sub, 5, 5.3f); S(l, "002", StructureType.House, Sub, 10, 5.3f);
            Assert.IsFalse(V(l).Has("R37"));
            S(l, "003", StructureType.House, Sub, 6, 6f);
            Assert.IsTrue(V(l).Has("R37"));
        }

        [Test] public void R23_BlockMustBeAxisAlignedRectangle()
        {
            var l = Base(); S(l, "001", StructureType.House, Sub, 3, 5.3f);
            Assert.IsFalse(V(l).Has("R23"));
            var tri = l.polygons[0]; tri.points = new List<Vec2> { new Vec2(0, 5), new Vec2(20, 5), new Vec2(0, 25) };
            Assert.IsTrue(V(l).Has("R23"));
        }

        [Test] public void R24_PlazaNeedsThreeVertices()
        {
            var l = Base();
            var p = new PolygonData { id = "003", kind = PolygonKind.Plaza };
            p.points.Add(new Vec2(30, 0)); p.points.Add(new Vec2(32, 0));
            l.polygons.Add(p);
            Assert.IsTrue(V(l).Has("R24"));
        }

        [Test] public void R25_StreetWidth()
        {
            var l = Base();
            Assert.IsFalse(V(l).Has("R25"));
            l.polygons.Add(Poly("003", 20.5f, 0, 25, 4));
            Assert.IsTrue(V(l).Has("R25"));
        }

        [Test] public void R29_DuplicateIds()
        {
            var l = Base(); l.polygons.Add(Poly("001", 30, 0, 32, 2));
            Assert.IsTrue(V(l).Has("R29"));
        }

        CityLayout FourBlocks()
        {
            var l = new CityLayout { seed = 1, center = new Vec2(10.4f, 5f) };
            l.polygons.Add(Poly("001", 0, 5.4f, 10, 10)); // NW
            l.polygons.Add(Poly("002", 10.8f, 5.4f, 20.8f, 10)); // NE
            l.polygons.Add(Poly("003", 0, 0.8f, 10, 4.6f)); // SW
            l.polygons.Add(Poly("004", 10.8f, 0.8f, 20.8f, 4.6f)); // SE
            // todos os 4 são quarteirões (têm estruturas)
            S(l, "001", StructureType.House, Ind, 6, 6f); S(l, "002", StructureType.House, Ind, 16, 6.5f);
            S(l, "003", StructureType.House, Ind, 6, 1.2f); S(l, "004", StructureType.House, Ind, 16, 1.5f);
            return l;
        }

        [Test] public void R32_WarehousesOnBothSidesOfGuard()
        {
            var l = FourBlocks();
            float hw = Ind.VisibleRect(Vector2.zero).width / 2f;
            S(l, "001", StructureType.Guard, Ind, 10f - hw - 0.1f, 5.6f);
            S(l, "001", StructureType.Warehouse, Ind, hw + 0.1f, 6.2f); // oeste
            S(l, "002", StructureType.Warehouse, Ind, 20.7f - hw, 6.2f); // leste
            Assert.IsFalse(V(l).Has("R32"));
            l.structures[6].bottomCenter = new Vec2(hw + 0.1f, 6.9f); // os dois a oeste
            Assert.IsTrue(V(l).Has("R32"));
        }

        [Test] public void R33_GuardNearMapCenter()
        {
            var l = FourBlocks();
            float hw = Ind.VisibleRect(Vector2.zero).width / 2f;
            S(l, "001", StructureType.Guard, Ind, 10f - hw - 0.1f, 5.6f);
            Assert.IsFalse(V(l).Has("R33"));
            l.structures[4].bottomCenter = new Vec2(hw + 0.1f, 5.6f);
            Assert.IsTrue(V(l).Has("R33"));
        }

        [Test] public void R34_CenterMatches()
        {
            var l = Base();
            Assert.IsFalse(V(l).Has("R34"));
            l.center = new Vec2(0, 0);
            Assert.IsTrue(V(l).Has("R34"));
        }

        [Test] public void R34_CenterOnGuardBlock()
        {
            var l = Base();
            S(l, "001", StructureType.Guard, Ind, 9, 5.6f);
            Assert.IsTrue(V(l).Has("R34"));
            l.center = new Vec2(10, 7.5f + LayoutValidator.FocusLift);
            Assert.IsFalse(V(l).Has("R34"));
        }

        [Test] public void ReportWritesFileAndCounts()
        {
            var l = Base();
            var r = V(l);
            Assert.AreEqual(r.violations.Count(v => v.severity == Severity.Error), r.errors);
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "citygen_report_test.json");
            r.Write(path);
            StringAssert.Contains("\"ruleId\"", System.IO.File.ReadAllText(path));
        }
    }
}
