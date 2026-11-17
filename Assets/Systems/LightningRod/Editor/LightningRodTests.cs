using System.Collections.Generic;
using System.IO;
using DefesaCivil.CityGen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Testes de editor do para-raio (PARARAIO.md): as contas da LightningRodMath, os quarteirões lidos do city_layout.json
// e os .asset do commit. Sem cena: rodam no Test Runner (EditMode).
public class LightningRodTests
{
    private const string DataPath = "Assets/Systems/LightningRod/Resources/LightningRod/LightningRodData.asset";
    private const string RodPath = "Assets/Resources/Upgrades/Rod.asset";

    private static readonly List<RodBlockSlot> TwoBlocks = new()
    {
        new RodBlockSlot { id = "A", min = new Vector2(0f, 0f), max = new Vector2(8f, 8f) },
        new RodBlockSlot { id = "B", min = new Vector2(9.2f, 0f), max = new Vector2(17.2f, 8f) },
    };

    [Test]
    public void BlockOf_TakesTheBlockAndItsMargin_NotTheStreet()
    {
        Assert.AreEqual(0, LightningRodMath.BlockOf(TwoBlocks, new Vector2(4f, 4f), 0.4f));
        Assert.AreEqual(1, LightningRodMath.BlockOf(TwoBlocks, new Vector2(9.0f, 0.1f), 0.4f), "casa com o pé na folga do B");
        Assert.AreEqual(0, LightningRodMath.BlockOf(TwoBlocks, new Vector2(4f, -0.3f), 0.4f), "pé um pouco abaixo da borda");
        Assert.AreEqual(-1, LightningRodMath.BlockOf(TwoBlocks, new Vector2(8.6f, 4f), 0.4f), "meio da rua entre os dois");
        Assert.AreEqual(-1, LightningRodMath.BlockOf(TwoBlocks, new Vector2(4f, 20f), 0.4f));
        // folga grande alcança os dois: fica o de centro mais perto
        Assert.AreEqual(1, LightningRodMath.BlockOf(TwoBlocks, new Vector2(8.7f, 4f), 2f));
    }

    [Test]
    public void PickIndex_WithoutAttraction_IsUniform()
    {
        var flags = new List<bool> { false, true, false, false };
        Assert.AreEqual(0, LightningRodMath.PickIndex(flags, 1f, 0f));
        Assert.AreEqual(1, LightningRodMath.PickIndex(flags, 1f, 0.26f));
        Assert.AreEqual(3, LightningRodMath.PickIndex(flags, 1f, 0.999f));
        Assert.AreEqual(3, LightningRodMath.PickIndex(flags, 1f, 1f), "roll 1 não passa do fim");
        Assert.AreEqual(-1, LightningRodMath.PickIndex(new List<bool>(), 6f, 0.5f));
    }

    [Test]
    public void PickIndex_ProtectedHousesWeighTheAttraction()
    {
        // 6 casas protegidas entre 150: com atração 6 elas levam 36 / (36 + 144) = 20% dos raios
        var flags = new List<bool>();
        for (int i = 0; i < 150; i++) flags.Add(i < 6);
        int hits = 0, rolls = 10000;
        for (int i = 0; i < rolls; i++)
        {
            int index = LightningRodMath.PickIndex(flags, 6f, (i + 0.5f) / rolls);
            if (flags[index]) hits++;
        }
        Assert.AreEqual(0.20f, hits / (float)rolls, 0.002f);
        Assert.AreEqual(0, LightningRodMath.PickIndex(flags, 0.2f, 0.005f), "atração abaixo de 1 vale 1");
    }

    [Test]
    public void OutlineDashes_HaveTheCornersAndFollowTheSpacing()
    {
        var points = new List<Vector2>();
        int count = LightningRodMath.OutlineDashes(Rect.MinMaxRect(0f, 0f, 7f, 3.5f), 0.7f, points);
        Assert.AreEqual(2 * 10 + 2 * 5, count);
        CollectionAssert.Contains(points, new Vector2(0f, 0f));
        CollectionAssert.Contains(points, new Vector2(7f, 0f));
        CollectionAssert.Contains(points, new Vector2(7f, 3.5f));
        CollectionAssert.Contains(points, new Vector2(0f, 3.5f));
        Assert.AreEqual(0, LightningRodMath.OutlineDashes(Rect.MinMaxRect(0f, 0f, 7f, 3.5f), 0f, points));
    }

    [Test]
    public void LayoutBlocks_AreRectangles_AndEveryHouseFallsInItsOwnBlock()
    {
        string json = File.ReadAllText(LightningRodDataMenu.LayoutPath);
        CityLayout layout = CityLayout.FromJson(json);
        List<RodBlockSlot> blocks = LightningRodDataMenu.BlocksFromLayout(json);
        Assert.AreEqual(layout.polygons.FindAll(p => p.kind == PolygonKind.Block).Count, blocks.Count);

        foreach (PolygonData polygon in layout.polygons)
        {
            if (polygon.kind != PolygonKind.Block) continue;
            RodBlockSlot slot = blocks.Find(b => b.id == polygon.id);
            foreach (Vec2 p in polygon.points)
            {
                bool onCornerX = Mathf.Abs(p.x - slot.min.x) < 1e-3f || Mathf.Abs(p.x - slot.max.x) < 1e-3f;
                bool onCornerY = Mathf.Abs(p.y - slot.min.y) < 1e-3f || Mathf.Abs(p.y - slot.max.y) < 1e-3f;
                Assert.IsTrue(onCornerX && onCornerY, $"quarteirão {polygon.id} não é retângulo");
            }
        }

        float margin = ScriptableObject.CreateInstance<LightningRodData>().houseMargin;
        int houses = 0;
        foreach (StructureData s in layout.structures)
        {
            if (s.type != StructureType.House) continue;
            houses++;
            int index = LightningRodMath.BlockOf(blocks, s.bottomCenter, margin);
            Assert.GreaterOrEqual(index, 0, $"casa {s.id} fora de todo quarteirão");
            Assert.AreEqual(s.polygonId, blocks[index].id, $"casa {s.id} caiu no quarteirão errado");
        }
        Assert.Greater(houses, 100);

        // v5A: um para-raio (e uma guarita) por quarteirão com casa vai bem além dos 3 de antes
        var withHouses = new HashSet<string>();
        foreach (StructureData s in layout.structures)
            if (s.type == StructureType.House) withHouses.Add(s.polygonId);
        Assert.Greater(withHouses.Count, 3);
    }

    [Test]
    public void DataAsset_HasTheBlocksOfTheCurrentMap()
    {
        var data = AssetDatabase.LoadAssetAtPath<LightningRodData>(DataPath);
        Assert.IsNotNull(data, "rode Tools/Jam/Para-raio/Criar dados");
        List<RodBlockSlot> expected = LightningRodDataMenu.BlocksFromLayout(File.ReadAllText(LightningRodDataMenu.LayoutPath));
        Assert.AreEqual(expected.Count, data.blocks.Count, "mapa mudou: rode Tools/Jam/Para-raio/Criar dados");
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.AreEqual(expected[i].id, data.blocks[i].id);
            Assert.Less(Vector2.Distance(expected[i].min, data.blocks[i].min) + Vector2.Distance(expected[i].max, data.blocks[i].max), 1e-3f, $"quarteirão {expected[i].id}");
        }
        Assert.GreaterOrEqual(data.attraction, 1f);
        Assert.Greater(data.buildTime, 0f);
    }

    [Test]
    public void RodUpgrade_IsAnExpansionUpgradeWithIconAndRoomOnTheMap()
    {
        var rod = AssetDatabase.LoadAssetAtPath<UpgradeData>(RodPath);
        Assert.IsNotNull(rod);
        Assert.AreEqual(LightningRodManager.UpgradeId, rod.id);
        Assert.AreEqual(UpgradeCategory.Expansion, rod.category);
        Assert.IsNotNull(rod.icon, "ícone Rod_Icon");
        Assert.AreEqual(64, rod.icon.texture.width);
        Assert.Greater(rod.baseCost, 0);
        // v5A: sem limite fixo de compras; o jogo corta em um por quarteirão com casa (UpgradesManager.SetLevelCap)
        var data = AssetDatabase.LoadAssetAtPath<LightningRodData>(DataPath);
        Assert.GreaterOrEqual(rod.maxLevel, data.blocks.Count, "o teto é o mapa, não o asset");

        var pole = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Systems/LightningRod/Resources/LightningRod/Rod_Pole.png");
        Assert.IsNotNull(pole, "sprite do mastro");
        Assert.AreEqual(32f, pole.pixelsPerUnit, "mesma densidade de pixel das casas");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Systems/LightningRod/Resources/LightningRod/Rod_Zap.wav"), "som do raio absorvido");
    }
}
