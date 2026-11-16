using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

// Testes de editor da expansão (EXPANSAO.md): as contas da ExpansionMath e os terrenos padrão do mapa.
// Só contas e o city_layout.json, sem cena: rodam no Test Runner (EditMode) e fora do Unity.
public class ExpansionTests
{
    private const string LayoutPath = "Assets/_Project/Data/Cities/city_layout.json";

    [Test]
    public void DistanceToSegment_ProjectsInsideAndClampsAtTheEnds()
    {
        Assert.AreEqual(2f, ExpansionMath.DistanceToSegment(new Vector2(1f, 2f), Vector2.zero, new Vector2(4f, 0f), out Vector2 inside), 1e-4f);
        Assert.AreEqual(new Vector2(1f, 0f), inside);
        Assert.AreEqual(5f, ExpansionMath.DistanceToSegment(new Vector2(7f, 4f), Vector2.zero, new Vector2(4f, 0f), out Vector2 end), 1e-4f);
        Assert.AreEqual(new Vector2(4f, 0f), end);
        Assert.AreEqual(1f, ExpansionMath.DistanceToSegment(new Vector2(0f, 1f), Vector2.zero, Vector2.zero, out _), 1e-4f);
    }

    [Test]
    public void NearestEdge_FindsTheStreetUnderTheLot()
    {
        // quadrado 0-1-2-3 com uma diagonal 0-2
        var positions = new List<Vector2> { new(0f, 0f), new(10f, 0f), new(10f, 10f), new(0f, 10f) };
        var neighbors = new List<List<int>> { new() { 1, 3, 2 }, new() { 0, 2 }, new() { 1, 3, 0 }, new() { 2, 0 } };

        Assert.IsTrue(ExpansionMath.NearestEdge(positions, neighbors, new Vector2(4f, -0.5f), out int a, out int b, out float d));
        Assert.AreEqual((0, 1), (a, b));
        Assert.AreEqual(0.5f, d, 1e-4f);
        Assert.IsTrue(ExpansionMath.NearestEdge(positions, neighbors, new Vector2(6f, 5f), out a, out b, out _));
        Assert.AreEqual((0, 2), (a, b));
        Assert.IsFalse(ExpansionMath.NearestEdge(new List<Vector2> { Vector2.zero }, new List<List<int>> { new() }, Vector2.one, out _, out _, out _));
    }

    [Test]
    public void CountServed_OnlyHousesStrictlyCloserThanEveryBase()
    {
        var bases = new List<Vector2> { new(0f, 0f) };
        var houses = new List<Vector2> { new(9f, 0f), new(5f, 0f), new(1f, 0f), new(12f, 0f) };
        var distances = new List<float>();

        int served = ExpansionMath.CountServed(new Vector2(10f, 0f), bases, houses, distances);

        Assert.AreEqual(2, served); // 9 e 12; a casa em 5 empata e fica com a base antiga
        CollectionAssert.AreEquivalent(new[] { 1f, 2f }, distances);
        Assert.AreEqual(0, ExpansionMath.CountServed(new Vector2(10f, 0f), new List<Vector2> { new(10f, 0f) }, houses));
    }

    [Test]
    public void Percentile_IsTheRingThatHoldsThatShareOfHouses()
    {
        var values = new List<float> { 10f, 1f, 9f, 2f, 8f, 3f, 7f, 4f, 6f, 5f };
        Assert.AreEqual(8f, ExpansionMath.Percentile(values, 0.8f));
        Assert.AreEqual(10f, ExpansionMath.Percentile(values, 1f));
        Assert.AreEqual(1f, ExpansionMath.Percentile(values, 0.01f));
        Assert.AreEqual(0f, ExpansionMath.Percentile(new List<float>(), 0.8f));
    }

    [Test]
    public void RingDots_HasAMinimumAndFollowsTheSpacing()
    {
        Assert.AreEqual(0, ExpansionMath.RingDots(0f, 0.7f));
        Assert.AreEqual(12, ExpansionMath.RingDots(0.5f, 0.7f));
        Assert.AreEqual(Mathf.RoundToInt(2f * Mathf.PI * 10f / 0.7f), ExpansionMath.RingDots(10f, 0.7f));
    }

    [Test]
    public void PathLength_SumsTheLegs()
    {
        Assert.AreEqual(0f, ExpansionMath.PathLength(new List<Vector2> { Vector2.one }));
        Assert.AreEqual(7f, ExpansionMath.PathLength(new List<Vector2> { new(0f, 0f), new(3f, 0f), new(3f, 4f) }), 1e-4f);
    }

    [Test]
    public void ClampToEdge_KeepsInsideAndPointsOutside()
    {
        var safe = Rect.MinMaxRect(-100f, -50f, 100f, 50f);

        Vector2 inside = ExpansionMath.ClampToEdge(new Vector2(10f, 10f), safe, out float angle, out bool clamped);
        Assert.IsFalse(clamped);
        Assert.AreEqual(new Vector2(10f, 10f), inside);

        Vector2 right = ExpansionMath.ClampToEdge(new Vector2(500f, 0f), safe, out angle, out clamped);
        Assert.IsTrue(clamped);
        Assert.AreEqual(100f, right.x, 1e-3f);
        Assert.AreEqual(0f, right.y, 1e-3f);
        Assert.AreEqual(-90f, angle, 1e-3f); // seta para a direita

        Vector2 below = ExpansionMath.ClampToEdge(new Vector2(40f, -400f), safe, out angle, out clamped);
        Assert.IsTrue(clamped);
        Assert.AreEqual(-50f, below.y, 1e-3f);
        Assert.AreEqual(5f, below.x, 1e-3f); // na reta do centro até o alvo
        Assert.AreEqual(180f, Mathf.Abs(Mathf.DeltaAngle(0f, angle)), 8f); // seta para baixo
        Vector2 corner = ExpansionMath.ClampToEdge(new Vector2(-900f, 900f), safe, out _, out _);
        Assert.IsTrue(corner.x >= safe.xMin && corner.x <= safe.xMax && corner.y >= safe.yMin && corner.y <= safe.yMax);
    }

    [Test]
    public void DefaultLots_TwoOfEachKind_FarFromEachOtherAndFromTheGuard()
    {
        List<LotSlot> lots = ExpansionData.DefaultLots();
        List<LotSlot> bases = lots.FindAll(l => l.kind == LotKind.Base);
        Assert.AreEqual(2, bases.Count);
        Assert.AreEqual(2, lots.FindAll(l => l.kind == LotKind.Depot).Count);
        Assert.AreNotEqual(bases[0].label, bases[1].label);
        Assert.Greater(Vector2.Distance(bases[0].entry, bases[1].entry), 30f, "bases novas longe uma da outra");

        Vector2 guard = Layout("Guard", "entry")[0];
        foreach (LotSlot lot in bases)
            Assert.Greater(Vector2.Distance(lot.entry, guard), 20f, $"base {lot.label} longe da Guarda");

        // o armazém de cada lado fica colado na base do mesmo lado
        foreach (LotSlot depot in lots.FindAll(l => l.kind == LotKind.Depot))
        {
            LotSlot partner = bases.Find(b => b.label == depot.label);
            Assert.IsNotNull(partner, $"armazém {depot.label} sem base do mesmo lado");
            Assert.Less(Vector2.Distance(depot.entry, partner.entry), 6f);
        }
    }

    [Test]
    public void DefaultLots_AreEmptyGround()
    {
        // Prédio do mapa com a base visível do lado do terreno (acima da rua) e perto dele = terreno ocupado.
        // Casas do outro lado da rua (viradas para o norte) dividem a linha central, e isso pode.
        var buildings = new List<Vector2>();
        foreach (string type in new[] { "House", "Guard", "Warehouse" }) buildings.AddRange(Layout(type, "bottomCenter"));
        Assert.Greater(buildings.Count, 100);

        foreach (LotSlot lot in ExpansionData.DefaultLots())
        foreach (Vector2 b in buildings)
        {
            bool sameSide = b.y > lot.entry.y && b.y - lot.entry.y < 8f;
            Assert.IsFalse(sameSide && Mathf.Abs(b.x - lot.entry.x) < 3.5f, $"terreno {lot.label} {lot.kind} ocupado por um prédio em {b}");
        }
    }

    // Um ponto ("entry" ou "bottomCenter") de cada estrutura do tipo no city_layout.json ("type" vem antes deles).
    private static List<Vector2> Layout(string type, string field)
    {
        Assert.IsTrue(File.Exists(LayoutPath), "rode os testes com a pasta do projeto como diretório atual");
        string json = File.ReadAllText(LayoutPath);
        var pattern = new Regex("\"type\"\\s*:\\s*\"" + type + "\"[^\\]]*?\"" + field + "\"\\s*:\\s*\\{\\s*\"x\"\\s*:\\s*(-?[0-9.eE+-]+)\\s*,\\s*\"y\"\\s*:\\s*(-?[0-9.eE+-]+)",
            RegexOptions.Singleline);
        var entries = new List<Vector2>();
        foreach (Match m in pattern.Matches(json))
            entries.Add(new Vector2(float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        Assert.Greater(entries.Count, 0, $"nenhum {type} no city_layout.json");
        return entries;
    }
}
