using NUnit.Framework;
using UnityEngine;

// Testes de editor da Coleta de Impostos (BALANCEAMENTO_V6F): piso por casa com o caixa vazio, a fração do caixa até
// o teto por casa intacta (o mesmo em todo nível) e o corte do caos pelo expoente. Números próprios, sem o asset.
public class PassiveIncomeTests
{
    private BalanceData balance;

    [SetUp]
    public void SetUp()
    {
        balance = ScriptableObject.CreateInstance<BalanceData>();
        balance.incomeFloorPerHouse = 0.3f;
        balance.incomeMoneyShare = 0.15f;
        balance.incomePerHouse = 14f;
        balance.incomeChaosPower = 3f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(balance);
    }

    [Test]
    public void EmptyCash_PaysOnlyTheFloorPerLevel()
    {
        Assert.AreEqual(30, Stats.PassiveIncome(balance, 1f, 100, 0, 0f));
        Assert.AreEqual(90, Stats.PassiveIncome(balance, 3f, 100, 0, 0f));
        Assert.AreEqual(0, Stats.PassiveIncome(balance, 0f, 100, 5000, 0f));
    }

    [Test]
    public void Cash_AddsAShareUntilTheCapPerIntactHouse()
    {
        // 1 x (0,3 x 100 + 0,15 x 400) = 90
        Assert.AreEqual(90, Stats.PassiveIncome(balance, 1f, 100, 400, 0f));
        Assert.Greater(Stats.PassiveIncome(balance, 1f, 100, 800, 0f), Stats.PassiveIncome(balance, 1f, 100, 400, 0f));
        // teto 14 x 100, igual no nível 1 e no 5
        Assert.AreEqual(1400, Stats.PassiveIncome(balance, 1f, 100, 1000000, 0f));
        Assert.AreEqual(1400, Stats.PassiveIncome(balance, 5f, 100, 1000000, 0f));
        Assert.AreEqual(0, Stats.PassiveIncome(balance, 5f, 0, 1000000, 0f));
    }

    [Test]
    public void Chaos_CutsByThePower()
    {
        // metade do caos com expoente 3 = 1/8
        Assert.AreEqual(175, Stats.PassiveIncome(balance, 5f, 100, 1000000, 0.5f));
        Assert.AreEqual(0, Stats.PassiveIncome(balance, 5f, 100, 1000000, 1f));
        balance.incomeChaosPower = 1f;
        Assert.AreEqual(700, Stats.PassiveIncome(balance, 5f, 100, 1000000, 0.5f));
    }

    [Test]
    public void EarlyGame_PaysFarLessThanTheOldRule()
    {
        // A regra antiga (1,2 x nível x casas intactas) pagava 184 por vez no nível 1 com 153 casas, com ou sem caixa.
        int old = Mathf.RoundToInt(1.2f * 1f * 153);
        Assert.Less(Stats.PassiveIncome(balance, 1f, 153, 150, 0f), old / 2);
        Assert.Less(Stats.PassiveIncome(balance, 3f, 153, 100, 0.1f), Mathf.RoundToInt(1.2f * 3f * 153 * 0.9f) / 2);
    }
}
