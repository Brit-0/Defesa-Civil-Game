using NUnit.Framework;
using UnityEngine;

// Dificuldades do menu inicial (docs/jam/CONSERTOS_V7B1.md): Normal é o jogo como está; Fácil e Difícil mudam um pouco
// o intervalo entre desastres, a rampa e o custo do Centro de Operações. EditMode, sem cena, com o asset de Resources.
public class DifficultyTests
{
    private const int GoalCost = 33000;

    private DifficultyData data;
    private BalanceData balance;

    [SetUp]
    public void SetUp()
    {
        data = Resources.Load<DifficultyData>(DifficultyData.ResourcePath);
        Assert.IsNotNull(data, "DifficultyData em Resources/" + DifficultyData.ResourcePath);

        // A curva do BALANCEAMENTO_V5 (DebutTests.SetRamp), sem o ritmo adaptativo
        balance = ScriptableObject.CreateInstance<BalanceData>();
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
        balance.paceIntervalCut = 0.45f;
        balance.paceIntervalFloor = 12f;
        balance.paceHouseBoost = 0.6f;
        balance.paceHousesMax = 9;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(balance);
        Difficulty.Set(DifficultyLevel.Normal);
    }

    private float Interval(DifficultyLevel level, float time, float pace = 0f)
    {
        float ramp = Difficulty.RampTime(data, level, time);
        return Difficulty.Interval(data, level, DisasterManager.GetInterval(balance, ramp, pace));
    }

    private int Houses(DifficultyLevel level, float time)
    {
        return DisasterManager.GetHouseCount(balance, Difficulty.RampTime(data, level, time), 0f);
    }

    [Test]
    public void Normal_IsTheGameAsItIs()
    {
        DifficultySettings normal = data.normal;
        Assert.AreEqual(1f, normal.intervalMultiplier, 1e-5f);
        Assert.AreEqual(1f, normal.rampSpeed, 1e-5f);
        Assert.AreEqual(1f, normal.goalCostMultiplier, 1e-5f);

        for (float t = 0f; t <= 1800f; t += 30f)
        {
            Assert.AreEqual(DisasterManager.GetInterval(balance, t, 0.5f), Interval(DifficultyLevel.Normal, t, 0.5f), 1e-4f, $"intervalo aos {t} s");
            Assert.AreEqual(DisasterManager.GetHouseCount(balance, t, 0f), Houses(DifficultyLevel.Normal, t), $"casas aos {t} s");
        }
        Assert.AreEqual(GoalCost, Difficulty.GoalCost(data, DifficultyLevel.Normal, GoalCost));
    }

    [Test]
    public void Easy_HasFewerDisastersAndACheaperGoal()
    {
        for (float t = 0f; t <= 1800f; t += 30f)
        {
            Assert.Greater(Interval(DifficultyLevel.Easy, t), Interval(DifficultyLevel.Normal, t), $"intervalo aos {t} s");
            Assert.LessOrEqual(Houses(DifficultyLevel.Easy, t), Houses(DifficultyLevel.Normal, t), $"casas aos {t} s");
        }
        Assert.Less(Difficulty.GoalCost(data, DifficultyLevel.Easy, GoalCost), GoalCost);
    }

    [Test]
    public void Hard_HasMoreDisastersAFasterRampAndAPricierGoal()
    {
        bool moreHouses = false;
        for (float t = 0f; t <= 1800f; t += 30f)
        {
            Assert.Less(Interval(DifficultyLevel.Hard, t), Interval(DifficultyLevel.Normal, t), $"intervalo aos {t} s");
            Assert.GreaterOrEqual(Houses(DifficultyLevel.Hard, t), Houses(DifficultyLevel.Normal, t), $"casas aos {t} s");
            moreHouses |= Houses(DifficultyLevel.Hard, t) > Houses(DifficultyLevel.Normal, t);
        }
        Assert.IsTrue(moreHouses, "a rampa mais rápida põe mais casas por desastre em algum momento");

        // O teto da rampa (9 min no Normal) chega antes
        float ceiling = DisasterManager.GetCeilingTime(balance);
        Assert.Greater(Difficulty.RampTime(data, DifficultyLevel.Hard, ceiling * 0.85f), ceiling);
        Assert.Greater(Difficulty.GoalCost(data, DifficultyLevel.Hard, GoalCost), GoalCost);
    }

    // "Um pouco": nenhuma das duas muda o jogo por mais de um quarto
    [Test]
    public void EasyAndHard_AreOnlyALittleDifferent()
    {
        foreach (DifficultyLevel level in new[] { DifficultyLevel.Easy, DifficultyLevel.Hard })
        {
            DifficultySettings settings = data.Get(level);
            Assert.That(settings.intervalMultiplier, Is.InRange(0.75f, 1.25f), $"{level}: intervalo");
            Assert.That(settings.rampSpeed, Is.InRange(1f, 1.35f), $"{level}: rampa");
            Assert.That(settings.goalCostMultiplier, Is.InRange(0.75f, 1.25f), $"{level}: Centro");
        }
    }

    [Test]
    public void IntervalFloor_HoldsButNeverRaisesAShorterCurve()
    {
        Assert.AreEqual(data.intervalFloor, Difficulty.Interval(data, DifficultyLevel.Hard, data.intervalFloor + 0.5f), 1e-4f, "o multiplicador para no piso");
        Assert.AreEqual(data.intervalFloor - 2f, Difficulty.Interval(data, DifficultyLevel.Hard, data.intervalFloor - 2f), 1e-4f, "abaixo do piso fica como veio");
        Assert.Greater(Difficulty.Interval(data, DifficultyLevel.Easy, data.intervalFloor - 2f), data.intervalFloor - 2f, "o fácil sempre alarga");
    }

    [Test]
    public void Current_ChoiceAppliesToTheGoalCost()
    {
        Difficulty.Set(DifficultyLevel.Easy);
        Assert.AreEqual(DifficultyLevel.Easy, Difficulty.Current);
        Assert.AreEqual(Difficulty.GoalCost(data, DifficultyLevel.Easy, GoalCost), Difficulty.GoalCost(GoalCost));

        Difficulty.Set(DifficultyLevel.Hard);
        Assert.AreEqual(Difficulty.GoalCost(data, DifficultyLevel.Hard, GoalCost), Difficulty.GoalCost(GoalCost));
        Assert.AreEqual(Difficulty.Interval(data, DifficultyLevel.Hard, 30f), Difficulty.Interval(30f), 1e-5f);

        Difficulty.Set(DifficultyLevel.Normal);
        Assert.AreEqual(GoalCost, Difficulty.GoalCost(GoalCost));
    }
}
