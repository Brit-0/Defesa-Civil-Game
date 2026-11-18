using NUnit.Framework;
using UnityEngine;

// Dificuldade adaptativa (docs/jam/ADAPTATIVA_V6B.md): a regra pura do DisasterPace, as mesmas contas do docs/jam/sim/sim.py
// (inverso, media_movel, eficiencia_alvo, passo_eficiencia, intervalo_adaptado, casas_adaptadas). EditMode, sem cena.
public class DisasterPaceTests
{
    private BalanceData balance;

    [SetUp]
    public void SetUp()
    {
        balance = ScriptableObject.CreateInstance<BalanceData>();
        balance.paceAverageTime = 45f;
        balance.paceChaosLow = 0.08f;
        balance.paceChaosHigh = 0.25f;
        balance.paceQueueLow = 0.5f;
        balance.paceQueueHigh = 3f;
        balance.paceLevelsStart = 16;
        balance.paceLevelsFull = 30;
        balance.paceMoneyFull = 4000;
        balance.paceRisePerMinute = 0.35f;
        balance.paceFallPerMinute = 1f;
        balance.paceIntervalCut = 0.45f;
        balance.paceIntervalFloor = 12f;
        balance.paceHouseBoost = 0.6f;
        balance.paceHousesMax = 9;

        // A curva do BALANCEAMENTO_V5 (DebutTests.SetRamp)
        balance.disasterIntervalStart = 50f;
        balance.disasterIntervalEnd = 21f;
        balance.disasterRampDelay = 60f;
        balance.disasterRampDuration = 480f;
        balance.disasterHousesStart = 2;
        balance.disasterHousesEnd = 6;
        balance.disasterLateIntervalPerMinute = 0.4f;
        balance.disasterIntervalFloor = 15f;
        balance.disasterLateHousesPerMinute = 0.15f;
        balance.disasterHousesMax = 9;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(balance);
    }

    [Test]
    public void BeginnerAndStrugglingPlayerGetNoExtraPace()
    {
        Assert.AreEqual(0f, DisasterPace.GetTarget(balance, 0.02f, 0f, 0, 12), 1e-5f, "calmo, mas ainda não cresceu");
        Assert.AreEqual(0f, DisasterPace.GetTarget(balance, 0.25f, 0f, 9000, 37), 1e-5f, "caos médio alto");
        Assert.AreEqual(0f, DisasterPace.GetTarget(balance, 0.02f, 3f, 9000, 37), 1e-5f, "fila longa");
        Assert.AreEqual(0f, DisasterPace.GetTarget(balance, 0.45f, 7f, 30000, 37), 1e-5f, "o ruim no fim: rico, mas apanhando");
    }

    [Test]
    public void EfficientPlayerReachesFullPace()
    {
        Assert.AreEqual(1f, DisasterPace.GetTarget(balance, 0.05f, 0.2f, 0, 30), 1e-5f, "todos os níveis");
        Assert.AreEqual(1f, DisasterPace.GetTarget(balance, 0.05f, 0.2f, 4000, 0), 1e-5f, "dinheiro guardado também é poder");
        Assert.AreEqual(1f, DisasterPace.GetTarget(balance, 0.05f, 0.2f, 4000, 30), 1e-5f, "o maior dos dois, sem somar");

        // Meio do caminho em cada parte: 0,5 x 0,5 x 0,5
        Assert.AreEqual(0.125f, DisasterPace.GetTarget(balance, 0.165f, 1.75f, 0, 23), 1e-4f);
    }

    [Test]
    public void PaceRisesSlowlyAndFallsFast()
    {
        float pace = DisasterPace.Step(balance, 0f, 1f, 60f);
        Assert.AreEqual(0.35f, pace, 1e-5f, "um minuto de jogo perfeito: 0,35");
        Assert.AreEqual(0.6f, DisasterPace.Step(balance, 0.5f, 0.6f, 60f), 1e-5f, "não passa do alvo");

        Assert.AreEqual(0.5f, DisasterPace.Step(balance, 1f, 0f, 30f), 1e-5f, "apanhando: cai 1 por minuto");
        Assert.AreEqual(0.2f, DisasterPace.Step(balance, 1f, 0.2f, 120f), 1e-5f, "não desce abaixo do alvo");

        // Subida suave: 0 a 1 em 1/0,35 min, quadro a quadro
        pace = 0f;
        for (int frame = 0; frame < 60 * 120; frame++) pace = DisasterPace.Step(balance, pace, 1f, 1f / 60f);
        Assert.AreEqual(0.7f, pace, 1e-3f, "2 min: 0,7");
    }

    [Test]
    public void AverageDoesNotDependOnFrameRate()
    {
        float slow = DisasterPace.Smooth(0f, 1f, 1f, 45f);
        float fast = 0f;
        for (int frame = 0; frame < 240; frame++) fast = DisasterPace.Smooth(fast, 1f, 1f / 240f, 45f);
        Assert.AreEqual(slow, fast, 1e-5f);
        Assert.AreEqual(1f - Mathf.Exp(-1f), DisasterPace.Smooth(0f, 1f, 45f, 45f), 1e-5f, "uma constante de tempo: 63%");
        Assert.AreEqual(3f, DisasterPace.Smooth(0.5f, 3f, 0.1f, 0f), 1e-5f, "constante 0: só a amostra");
    }

    [Test]
    public void IntervalAndHousesHaveCeilings()
    {
        Assert.AreEqual(50f, DisasterPace.ApplyInterval(balance, 50f, 0f), 1e-4f, "ritmo 0: a curva de sempre");
        Assert.AreEqual(27.5f, DisasterPace.ApplyInterval(balance, 50f, 1f), 1e-4f, "ritmo 1: 45% mais curto");
        Assert.AreEqual(27.5f, DisasterPace.ApplyInterval(balance, 50f, 3f), 1e-4f, "ritmo preso em 1");
        Assert.AreEqual(12f, DisasterPace.ApplyInterval(balance, 15f, 1f), 1e-4f, "o piso");
        Assert.AreEqual(10f, DisasterPace.ApplyInterval(balance, 10f, 1f), 1e-4f, "o piso não alonga o que já é curto");

        Assert.AreEqual(4f, DisasterPace.ApplyHouses(balance, 4f, 0f), 1e-4f);
        Assert.AreEqual(6.4f, DisasterPace.ApplyHouses(balance, 4f, 1f), 1e-4f, "ritmo 1: 60% a mais");
        Assert.AreEqual(9f, DisasterPace.ApplyHouses(balance, 6f, 1f), 1e-4f, "o teto");
        Assert.AreEqual(10f, DisasterPace.ApplyHouses(balance, 10f, 1f), 1e-4f, "o teto não corta o que já passou");

        balance.paceIntervalCut = 0f;
        balance.paceHouseBoost = 0f;
        Assert.AreEqual(50f, DisasterPace.ApplyInterval(balance, 50f, 1f), 1e-4f, "0 desliga");
        Assert.AreEqual(4f, DisasterPace.ApplyHouses(balance, 4f, 1f), 1e-4f, "0 desliga");
    }

    // A curva do DisasterManager com o ritmo: ritmo 0 é a do BALANCEAMENTO_V5 (DebutTests); com ritmo, mais cedo e maior.
    [Test]
    public void RampWithPaceIsFasterAndBigger()
    {
        Assert.AreEqual(DisasterManager.GetInterval(balance, 300f), DisasterManager.GetInterval(balance, 300f, 0f), 1e-5f);
        Assert.AreEqual(DisasterManager.GetHouseCount(balance, 300f), DisasterManager.GetHouseCount(balance, 300f, 0f));

        Assert.AreEqual(35.5f * 0.55f, DisasterManager.GetInterval(balance, 300f, 1f), 1e-4f, "meio da rampa, ritmo 1");
        Assert.AreEqual(6, DisasterManager.GetHouseCount(balance, 300f, 1f), "4 casas x 1,6 = 6,4");
        Assert.AreEqual(5, DisasterManager.GetHouseCount(balance, 300f, 0.5f), "4 x 1,3 = 5,2");
        Assert.AreEqual(12f, DisasterManager.GetInterval(balance, 540f + 60 * 60f, 1f), 1e-4f, "piso 15 da curva x 0,55: fica no piso 12");
        Assert.AreEqual(9, DisasterManager.GetHouseCount(balance, 540f, 1f), "6 x 1,6 = 9,6: o teto 9");
    }
}
