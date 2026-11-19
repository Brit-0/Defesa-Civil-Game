using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Estreia garantida (docs/jam/RITMO_V3.md): o primeiro sorteio depois de liberar um tipo é esse tipo. EditMode, sem cena.
public class DebutTests
{
    private BalanceData balance;

    [SetUp]
    public void SetUp()
    {
        balance = ScriptableObject.CreateInstance<BalanceData>();
        balance.fireUnlockTime = 90f;
        balance.stormUnlockTime = 170f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(balance);
    }

    [Test]
    public void FreshlyUnlockedTypeDebutsOnce()
    {
        var debuted = new HashSet<DisasterType> { DisasterType.Earthquake };

        Assert.IsFalse(DisasterManager.TryGetDebut(balance, 89f, debuted, out _), "antes de liberar, sorteio normal");

        Assert.IsTrue(DisasterManager.TryGetDebut(balance, 95f, debuted, out DisasterType debut));
        Assert.AreEqual(DisasterType.Fire, debut);

        debuted.Add(DisasterType.Fire);
        Assert.IsFalse(DisasterManager.TryGetDebut(balance, 120f, debuted, out _), "depois da estreia, sorteio normal");

        Assert.IsTrue(DisasterManager.TryGetDebut(balance, 175f, debuted, out debut));
        Assert.AreEqual(DisasterType.Storm, debut);
    }

    [Test]
    public void OldestUnlockDebutsFirstAndZeroWeightNever()
    {
        var debuted = new HashSet<DisasterType> { DisasterType.Earthquake };

        Assert.IsTrue(DisasterManager.TryGetDebut(balance, 200f, debuted, out DisasterType debut));
        Assert.AreEqual(DisasterType.Fire, debut, "os dois liberados sem estrear: vem o que liberou antes");

        balance.fireWeight = 0f;
        Assert.IsTrue(DisasterManager.TryGetDebut(balance, 200f, debuted, out debut));
        Assert.AreEqual(DisasterType.Storm, debut, "peso 0 tira o tipo do jogo, sem estreia");
    }

    // Rampa até o teto e subida lenta depois dele (docs/jam/BALANCEAMENTO_V5.md), as mesmas contas do docs/jam/sim/sim.py.
    [Test]
    public void RampReachesCeilingThenKeepsRisingSlowly()
    {
        SetRamp(lateInterval: 0.4f, lateHouses: 0.15f);

        Assert.AreEqual(50f, DisasterManager.GetInterval(balance, 0f), 1e-4f);
        Assert.AreEqual(2, DisasterManager.GetHouseCount(balance, 0f));
        Assert.AreEqual(50f, DisasterManager.GetInterval(balance, 60f), 1e-4f, "a calma do começo");
        Assert.AreEqual(35.5f, DisasterManager.GetInterval(balance, 300f), 1e-4f, "meio da rampa");
        Assert.AreEqual(4, DisasterManager.GetHouseCount(balance, 300f));
        Assert.AreEqual(540f, DisasterManager.GetCeilingTime(balance), 1e-4f);
        Assert.AreEqual(21f, DisasterManager.GetInterval(balance, 540f), 1e-4f, "o teto");
        Assert.AreEqual(6, DisasterManager.GetHouseCount(balance, 540f));

        Assert.AreEqual(19f, DisasterManager.GetInterval(balance, 540f + 5 * 60f), 1e-4f, "5 min depois: -0,4 s por minuto");
        Assert.AreEqual(7, DisasterManager.GetHouseCount(balance, 540f + 5 * 60f), "5 min depois: 6,75 casas");
        Assert.AreEqual(15f, DisasterManager.GetInterval(balance, 540f + 60 * 60f), 1e-4f, "nunca abaixo do piso");
        Assert.AreEqual(9, DisasterManager.GetHouseCount(balance, 540f + 60 * 60f), "nunca acima do máximo");
    }

    [Test]
    public void LateRiseOffKeepsTheCeiling()
    {
        SetRamp(lateInterval: 0f, lateHouses: 0f);

        Assert.AreEqual(21f, DisasterManager.GetInterval(balance, 540f + 10 * 60f), 1e-4f);
        Assert.AreEqual(6, DisasterManager.GetHouseCount(balance, 540f + 10 * 60f));

        balance.disasterRampDuration = 0f;
        Assert.AreEqual(21f, DisasterManager.GetInterval(balance, 0f), 1e-4f, "sem rampa: o teto desde o começo");
    }

    private void SetRamp(float lateInterval, float lateHouses)
    {
        balance.disasterIntervalStart = 50f;
        balance.disasterIntervalEnd = 21f;
        balance.disasterRampDelay = 60f;
        balance.disasterRampDuration = 480f;
        balance.disasterHousesStart = 2;
        balance.disasterHousesEnd = 6;
        balance.disasterLateIntervalPerMinute = lateInterval;
        balance.disasterIntervalFloor = 15f;
        balance.disasterLateHousesPerMinute = lateHouses;
        balance.disasterHousesMax = 9;
        // Piso e teto duros do BALANCEAMENTO_V5; o teto macio da CONSERTOS_V7A tem os testes no DisasterPaceTests
        balance.disasterIntervalSoftPerMinute = 0f;
        balance.disasterHousesSoftPerMinute = 0f;
    }
}
