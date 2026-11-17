using NUnit.Framework;
using UnityEngine;

// Testes de editor da estimativa da aba de chamado (#531): chegada pela distância e velocidade, conserto pela fórmula
// real do Stats para a equipe escolhida, o texto do tempo e o lado do aro do tutorial. Sem cena: UpgradesManager e
// GuardManager ausentes, upgrades em 0.
public class CallEstimateTests
{
    private GameObject houseObject;

    [TearDown]
    public void TearDown()
    {
        if (houseObject != null) Object.DestroyImmediate(houseObject);
    }

    private Call NewCall(int damage, State state, int needed = 1)
    {
        houseObject = new GameObject("HouseTest");
        House house = houseObject.AddComponent<House>();
        house.damageAmount = damage;
        house.currentState = state;
        return new Call { house = house, workersNeeded = needed, type = CallType.Problem };
    }

    [Test]
    public void TravelTime_IsWalkOfTheLastWorkerPlusDispatchGaps()
    {
        float speed = Stats.WorkerSpeed;
        float gap = Stats.DispatchInterval;

        Assert.AreEqual(0f, Stats.GetTravelTime(10f, 0), 1e-4f);
        Assert.AreEqual(10f / speed, Stats.GetTravelTime(10f, 1), 1e-4f);
        Assert.AreEqual(2f * gap + 10f / speed, Stats.GetTravelTime(10f, 3), 1e-4f);
        Assert.Greater(Stats.GetTravelTime(20f, 2), Stats.GetTravelTime(10f, 2));
    }

    [Test]
    public void WorkTime_ForATeamSize_SplitsTheDamageWork()
    {
        BalanceData balance = Stats.Balance;
        Call call = NewCall(4, State.Damaged);

        float alone = Mathf.Max(balance.minWorkTime, balance.workTimePerDamage * 4f);
        Assert.AreEqual(alone, Stats.GetWorkTime(call, 1), 1e-4f);
        Assert.AreEqual(Mathf.Max(balance.minWorkTime, balance.workTimePerDamage * 4f / 2f), Stats.GetWorkTime(call, 2), 1e-4f);
        Assert.LessOrEqual(Stats.GetWorkTime(call, 4), Stats.GetWorkTime(call, 2));

        // O tempo do chamado de verdade é o mesmo da estimativa com a equipe enviada
        call.workersSent = 2;
        Assert.AreEqual(Stats.GetWorkTime(call, 2), Stats.GetWorkTime(call), 1e-4f);
    }

    [Test]
    public void WorkTime_NeverUsesFewerThanTheNeededWorkers()
    {
        Call call = NewCall(4, State.Damaged, 2);

        Assert.AreEqual(Stats.GetWorkTime(call, 2), Stats.GetWorkTime(call, 0), 1e-4f);
        Assert.AreEqual(Stats.GetWorkTime(call, 2), Stats.GetWorkTime(call, 1), 1e-4f);
    }

    [Test]
    public void WorkTime_OnFireUsesTheFireTime()
    {
        Call call = NewCall(1, State.OnFire);

        Assert.AreEqual(Mathf.Max(Stats.Balance.minWorkTime, Stats.Balance.fireWorkTime), Stats.GetWorkTime(call, 1), 1e-4f);
    }

    [Test]
    public void FormatTime_SecondsRoundUpAndMinutesUseColon()
    {
        Assert.AreEqual("0 s", CallEstimate.FormatTime(0f));
        Assert.AreEqual("4 s", CallEstimate.FormatTime(3.2f));
        Assert.AreEqual("59 s", CallEstimate.FormatTime(59f));
        Assert.AreEqual("1:00", CallEstimate.FormatTime(60f));
        Assert.AreEqual("1:05", CallEstimate.FormatTime(64.1f));
    }

    [Test]
    public void RingSide_IsSquareStepOf32BetweenTheLimits()
    {
        Assert.AreEqual(FeelUI.RingMin, FeelUI.RingSide(new Vector2(20f, 20f)), 1e-4f);
        Assert.AreEqual(128f, FeelUI.RingSide(new Vector2(100f, 40f)), 1e-4f);
        Assert.AreEqual(FeelUI.RingMax, FeelUI.RingSide(new Vector2(900f, 300f)), 1e-4f);

        for (float w = 0f; w < 400f; w += 7f)
        {
            float side = FeelUI.RingSide(new Vector2(w, w * .5f));
            Assert.AreEqual(0f, side % FeelUI.RingStep, 1e-4f, "lado " + side + " fora do passo de 32");
            Assert.That(side, Is.InRange(FeelUI.RingMin, FeelUI.RingMax));
        }

        Assert.IsTrue(FeelUI.RingFits(new Vector2(144f, 60f)));
        Assert.IsFalse(FeelUI.RingFits(new Vector2(332f, 312f)));
    }
}
