using NUnit.Framework;
using UnityEngine;

// Fogo que pula quando o jato falha e a dica do jato que espera o jogador (docs/jam/CONSERTOS_V7B3.md). EditMode, sem cena.
public class FireSpreadTests
{
    [Test]
    public void SpreadRollsOnlyBelowChance()
    {
        Assert.IsTrue(FireSpread.Rolls(0f, .35f));
        Assert.IsTrue(FireSpread.Rolls(.349f, .35f));
        Assert.IsFalse(FireSpread.Rolls(.35f, .35f));
        Assert.IsFalse(FireSpread.Rolls(.9f, .35f));
        Assert.IsFalse(FireSpread.Rolls(0f, 0f), "chance 0 desliga");
        Assert.IsTrue(FireSpread.Rolls(.999f, 1f));
    }

    [Test]
    public void SpreadHappensAboutAsOftenAsTheChance()
    {
        System.Random random = new System.Random(7);
        int spread = 0;
        const int Tries = 20000;
        for (int i = 0; i < Tries; i++)
            if (FireSpread.Rolls((float)random.NextDouble(), .35f)) spread++;

        Assert.AreEqual(.35f, spread / (float)Tries, .015f);
    }

    [Test]
    public void SpreadPicksNearestHouseThatCanCatch()
    {
        // a mais perto (2) já está em chamas ou quebrada: vai para a de 3
        float[] distances = { 4f, 2f, 3f, 4.5f };
        bool[] canCatch = { true, false, true, true };

        Assert.AreEqual(2, FireSpread.PickNearest(distances, canCatch, 5f));
    }

    [Test]
    public void SpreadNeedsAHouseInRange()
    {
        float[] distances = { 6f, 2f, 7f };
        bool[] canCatch = { true, false, true };

        Assert.AreEqual(-1, FireSpread.PickNearest(distances, canCatch, 5f));
        Assert.AreEqual(-1, FireSpread.PickNearest(new float[0], new bool[0], 5f), "sem casas");
        Assert.AreEqual(0, FireSpread.PickNearest(new[] { 5f }, new[] { true }, 5f), "no limite do alcance pega");
    }

    [Test]
    public void SpreadTieKeepsFirstHouse()
    {
        Assert.AreEqual(1, FireSpread.PickNearest(new[] { 4f, 3f, 3f }, new[] { true, true, true }, 5f));
    }

    // Chance moderada e alcance que cobre a vizinha mais perto de toda casa do mapa (2,2 a 4,2 em 153 casas).
    [Test]
    public void SpreadDataIsModerate()
    {
        BalanceData balance = Resources.Load<BalanceData>("Balance/BalanceData");
        Assert.IsNotNull(balance);
        Assert.That(balance.fireSpreadChance, Is.InRange(.2f, .5f));
        Assert.That(balance.fireSpreadRadius, Is.InRange(4.2f, 6f));
        Assert.That(balance.fireSpreadDelay, Is.InRange(.3f, 1.5f), "o pulo vem logo depois do O FOGO CRESCEU!");
    }

    [Test]
    public void FireTipEndsOnlyWhenJetPutsFireOut()
    {
        Assert.IsTrue(Tutorial.FireTipEnds(SkillResult.FireOut));
        Assert.IsFalse(Tutorial.FireTipEnds(SkillResult.FireDouse), "uma chama só não basta");
        Assert.IsFalse(Tutorial.FireTipEnds(SkillResult.FireGrew));
        Assert.IsFalse(Tutorial.FireTipEnds(SkillResult.FireSpread));
        Assert.IsFalse(Tutorial.FireTipEnds(SkillResult.Critical));
    }
}
