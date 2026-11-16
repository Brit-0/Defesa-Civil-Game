using System.Collections.Generic;
using NUnit.Framework;

// Regras puras do jato e da rede de energia (docs/jam/ENERGIA_FOGO.md). EditMode, sem cena.
public class FireEnergyTests
{
    #region FIRE JET

    [Test]
    public void JetClosesWhenAllFlamesAreOut()
    {
        FireJet jet = new FireJet(3, 3.5f);

        Assert.IsTrue(jet.Douse());
        Assert.IsTrue(jet.Douse());
        Assert.IsFalse(jet.Closed);
        Assert.IsTrue(jet.Douse());

        Assert.IsTrue(jet.Closed);
        Assert.IsTrue(jet.Succeeded);
        Assert.IsFalse(jet.Douse(), "janela fechada não apaga mais");
        Assert.IsFalse(jet.Tick(10f), "janela fechada não estoura por tempo");
    }

    [Test]
    public void JetFailsOnTimeoutWithFlamesLeft()
    {
        FireJet jet = new FireJet(4, 3.5f);
        jet.Douse();

        Assert.IsFalse(jet.Tick(3f));
        Assert.AreEqual(.5f / 3.5f, jet.TimeLeft01, 1e-4f);
        Assert.IsTrue(jet.Tick(.6f), "fecha no quadro em que o tempo acaba");
        Assert.IsFalse(jet.Succeeded);
        Assert.AreEqual(0f, jet.TimeLeft01);
        Assert.IsFalse(jet.Tick(1f), "só avisa uma vez");
    }

    [Test]
    public void JetHasAtLeastOneFlame()
    {
        Assert.AreEqual(1, new FireJet(0, 1f).Flames);
    }

    [Test]
    public void FireLevelRisesInEqualThirdsOfTheTimer()
    {
        Assert.AreEqual(1, FireRules.LevelFromTimer(1f, 3));
        Assert.AreEqual(1, FireRules.LevelFromTimer(.7f, 3));
        Assert.AreEqual(2, FireRules.LevelFromTimer(.6f, 3));
        Assert.AreEqual(3, FireRules.LevelFromTimer(.3f, 3));
        Assert.AreEqual(3, FireRules.LevelFromTimer(0f, 3), "nunca passa do máximo");
        Assert.AreEqual(3, FireRules.LevelFromTimer(-1f, 3));
        Assert.AreEqual(1, FireRules.LevelFromTimer(.5f, 0));
    }

    [Test]
    public void FlamesGrowWithLevel()
    {
        Assert.AreEqual(3, FireRules.FlamesFor(1, 3, 1));
        Assert.AreEqual(5, FireRules.FlamesFor(3, 3, 1));
        Assert.AreEqual(1, FireRules.FlamesFor(1, 0, 0));
    }

    [Test]
    public void GrowAddsOneChunkKeepingWorkDone()
    {
        // Nível 1, metade feita de 10 s: 5 s trabalhados. Cresce para 2 partes: 20 s, os mesmos 5 s = 25%.
        float progress = .5f, duration = 10f;
        FireRules.Grow(1, ref progress, ref duration);

        Assert.AreEqual(20f, duration, 1e-4f);
        Assert.AreEqual(.25f, progress, 1e-4f);
        Assert.AreEqual(5f, progress * duration, 1e-4f);

        FireRules.Grow(2, ref progress, ref duration);
        Assert.AreEqual(30f, duration, 1e-4f);
        Assert.AreEqual(5f, progress * duration, 1e-4f);
    }

    #endregion

    #region ENERGY GRID

    private static List<EnergyConsumer> City(int bases, int depots, int automation)
    {
        List<EnergyConsumer> list = new();
        for (int i = 0; i < bases; i++) list.Add(new EnergyConsumer { kind = EnergyConsumerKind.Base, demand = 2, order = i });
        for (int i = 0; i < depots; i++) list.Add(new EnergyConsumer { kind = EnergyConsumerKind.Depot, demand = 1, order = i });
        for (int i = 0; i < automation; i++) list.Add(new EnergyConsumer { kind = EnergyConsumerKind.Automation, demand = 1, order = i });
        return list;
    }

    [Test]
    public void EverythingOnWhenCapacityIsEnough()
    {
        List<EnergyConsumer> city = City(1, 2, 2);

        Assert.AreEqual(6, EnergyGrid.TotalDemand(city));
        Assert.AreEqual(6, EnergyGrid.Distribute(city, 8, false));
        Assert.IsTrue(city.TrueForAll(c => c.powered));
        Assert.AreEqual(2, EnergyGrid.PoweredAutomation(city));
    }

    [Test]
    public void OverloadCutsAutomationFirstThenDepots()
    {
        List<EnergyConsumer> city = City(1, 2, 3);

        // 5 de capacidade: base 2 + 2 depósitos 1 = 4, sobra 1 para o primeiro nível de automação.
        Assert.AreEqual(5, EnergyGrid.Distribute(city, 5, false));
        Assert.AreEqual(1, EnergyGrid.PoweredAutomation(city));

        // 3 de capacidade: base 2 + 1 depósito; o segundo depósito e a automação desligam.
        EnergyGrid.Distribute(city, 3, false);
        Assert.AreEqual(0, EnergyGrid.PoweredAutomation(city));
        Assert.AreEqual(1, city.FindAll(c => c.kind == EnergyConsumerKind.Depot && c.powered).Count);
        Assert.IsTrue(city.Find(c => c.kind == EnergyConsumerKind.Base).powered);
    }

    [Test]
    public void NewerBuildingIsCutBeforeOlder()
    {
        List<EnergyConsumer> city = City(2, 0, 0);
        city[0].order = 5; // a base construída depois
        city[1].order = 1;

        EnergyGrid.Distribute(city, 2, false);

        Assert.IsFalse(city.Find(c => c.order == 5).powered);
        Assert.IsTrue(city.Find(c => c.order == 1).powered);
    }

    [Test]
    public void AutomationStaysInSequence()
    {
        // Automação de demanda 2 no nível 1 não cabe em 1: o nível 2 (demanda 1) também fica desligado.
        List<EnergyConsumer> city = new()
        {
            new EnergyConsumer { kind = EnergyConsumerKind.Automation, demand = 2, order = 0 },
            new EnergyConsumer { kind = EnergyConsumerKind.Automation, demand = 1, order = 1 },
        };

        Assert.AreEqual(0, EnergyGrid.Distribute(city, 1, false));
        Assert.AreEqual(0, EnergyGrid.PoweredAutomation(city));
    }

    [Test]
    public void BlackoutTurnsEverythingOff()
    {
        List<EnergyConsumer> city = City(1, 1, 1);

        Assert.AreEqual(0, EnergyGrid.Distribute(city, 99, true));
        Assert.IsTrue(city.TrueForAll(c => !c.powered));
    }

    #endregion
}
