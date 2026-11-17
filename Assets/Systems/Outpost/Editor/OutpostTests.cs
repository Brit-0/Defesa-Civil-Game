using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Testes de editor da guarita (GUARITAS.md): as contas da OutpostMath, os .asset do commit e a Automação sem o nível 4.
// Sem cena: rodam no Test Runner (EditMode).
public class OutpostTests
{
    private const string UpgradePath = "Assets/Resources/Upgrades/Outpost.asset";
    private const string AutomationPath = "Assets/Resources/Upgrades/Automation.asset";
    private const string RodDataPath = "Assets/Systems/LightningRod/Resources/LightningRod/LightningRodData.asset";
    private const string BoothPath = "Assets/Systems/Outpost/Resources/Outpost/Outpost_Booth.png";
    private const string RadioPath = "Assets/Systems/Outpost/Resources/Outpost/Outpost_Radio.wav";

    private static readonly Rect Block = Rect.MinMaxRect(0f, 0f, 8f, 8f);
    private static readonly Vector2 Booth = new Vector2(1.5f, 2f);

    [Test]
    public void PickCorner_WithoutObstacles_StaysInFrontOnTheLeft()
    {
        Vector2 foot = OutpostMath.PickCorner(Block, new List<Rect>(), Booth, 0.9f);
        Assert.Less(Vector2.Distance(new Vector2(0.9f, 0.9f), foot), 1e-4f);
    }

    [Test]
    public void PickCorner_AvoidsTheHouseDrawings()
    {
        // casas ocupando a faixa de baixo inteira e o canto de cima à esquerda: sobra o de cima à direita
        var houses = new List<Rect> { Rect.MinMaxRect(0f, 0f, 8f, 3.5f), Rect.MinMaxRect(0f, 5f, 4f, 8f) };
        Vector2 foot = OutpostMath.PickCorner(Block, houses, new Vector2(1.5f, 1.2f), 0.9f);
        Assert.Less(Vector2.Distance(new Vector2(7.1f, 7.1f), foot), 1e-4f);

        // nenhum canto livre: fica o que menos cobre
        var crowded = new List<Rect> { Rect.MinMaxRect(0f, 0f, 3f, 8f), Rect.MinMaxRect(6.5f, 0f, 8f, 8f) };
        Vector2 least = OutpostMath.PickCorner(Block, crowded, Booth, 0.9f);
        Assert.Greater(least.x, 4f, "o lado direito cobre menos casa");
    }

    [Test]
    public void PickCorner_KeepsTheFootInsideTinyBlocks()
    {
        Vector2 foot = OutpostMath.PickCorner(Rect.MinMaxRect(0f, 0f, 1f, 1f), null, Booth, 0.9f);
        Assert.IsTrue(Rect.MinMaxRect(0f, 0f, 1f, 1f).Contains(foot));
    }

    [Test]
    public void OverlapAndGap_MeasureTheRectangles()
    {
        Assert.AreEqual(1f, OutpostMath.OverlapArea(Rect.MinMaxRect(0f, 0f, 2f, 2f), Rect.MinMaxRect(1f, 1f, 3f, 3f)), 1e-4f);
        Assert.AreEqual(0f, OutpostMath.OverlapArea(Rect.MinMaxRect(0f, 0f, 1f, 1f), Rect.MinMaxRect(2f, 2f, 3f, 3f)));
        Assert.AreEqual(5f, OutpostMath.Gap(Rect.MinMaxRect(0f, 0f, 1f, 1f), Rect.MinMaxRect(4f, 5f, 6f, 6f)), 1e-4f);
        Assert.AreEqual(0f, OutpostMath.Gap(Rect.MinMaxRect(0f, 0f, 2f, 2f), Rect.MinMaxRect(1f, 1f, 3f, 3f)));
    }

    [Test]
    public void Ready_WaitsTheReactionDelay()
    {
        Assert.IsFalse(OutpostMath.Ready(10f, 11f, 1.5f));
        Assert.IsTrue(OutpostMath.Ready(10f, 11.5f, 1.5f));
        Assert.IsTrue(OutpostMath.Ready(10f, 10f, 0f));
    }

    // Pego no Play da v5A: casa intacta da cena tem um currentCall vazio (Call é [Serializable]) e a guarita mandava
    // essa casa ao GuardManager (NullReference no Stats.GetSuggestedTeam). Só casa com problema espera equipe.
    [Test]
    public void WaitsForTeam_OnlyHousesWithARealProblem()
    {
        GameObject go = new("HouseTest");
        try
        {
            House house = go.AddComponent<House>();
            house.currentState = State.Intact;
            house.currentCall = new Call();
            Assert.IsFalse(GuardManager.WaitsForTeam(house), "casa intacta com chamado vazio");

            house.currentState = State.Damaged;
            Assert.IsFalse(GuardManager.WaitsForTeam(house), "chamado sem a casa");

            house.currentCall = new Call { house = house, type = CallType.Problem };
            Assert.IsTrue(GuardManager.WaitsForTeam(house));
            Assert.IsFalse(GuardManager.WaitsForTeam(null));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void OutpostUpgrade_IsInTheAutomationTab_WithIcon_AndOnePerBlock()
    {
        var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(UpgradePath);
        Assert.IsNotNull(upgrade, "rode Tools/Jam/Guarita/Criar dados");
        Assert.AreEqual(OutpostManager.UpgradeId, upgrade.id);
        Assert.AreEqual(UpgradeCategory.Automation, upgrade.category);
        Assert.IsNotNull(upgrade.icon, "ícone Outpost_Icon");
        Assert.AreEqual(64, upgrade.icon.texture.width);
        Assert.Greater(upgrade.baseCost, 0);
        Assert.GreaterOrEqual(upgrade.costGrowth, 1f);

        var blocks = AssetDatabase.LoadAssetAtPath<LightningRodData>(RodDataPath);
        Assert.IsNotNull(blocks);
        Assert.GreaterOrEqual(upgrade.maxLevel, blocks.blocks.Count, "sem teto fixo: o jogo corta em um por quarteirão com casa");

        var data = AssetDatabase.LoadAssetAtPath<OutpostData>(OutpostDataMenu.DataPath);
        Assert.IsNotNull(data, "rode Tools/Jam/Guarita/Criar dados");
        Assert.Greater(data.dispatchInterval, 0f);
        Assert.Greater(data.buildTime, 0f);
    }

    [Test]
    public void Automation_EndsAtLevel3_TheDispatchIsTheOutposts()
    {
        var automation = AssetDatabase.LoadAssetAtPath<UpgradeData>(AutomationPath);
        Assert.IsNotNull(automation);
        Assert.AreEqual(3, automation.maxLevel, "o nível 4 (despacho automático na cidade inteira) virou as guaritas");
        Assert.AreEqual(3, AutomationLevels.AutoCollect);
        StringAssert.DoesNotContain("despacho autom", automation.description.ToLowerInvariant());
    }

    [Test]
    public void BoothArt_IsPixelArtLikeTheHouses_AndHasItsSound()
    {
        var booth = AssetDatabase.LoadAssetAtPath<Sprite>(BoothPath);
        Assert.IsNotNull(booth, "sprite da guarita");
        Assert.AreEqual(32f, booth.pixelsPerUnit, "mesma densidade de pixel das casas");
        Assert.AreEqual(FilterMode.Point, booth.texture.filterMode);
        Assert.LessOrEqual(booth.rect.height, 64f, "do tamanho de um poste, menor que as casas");
        Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AudioClip>(RadioPath), "chiado do rádio no despacho");
    }
}
