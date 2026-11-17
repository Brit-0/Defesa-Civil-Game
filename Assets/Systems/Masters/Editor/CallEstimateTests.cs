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
    public void RingSide_IsSquareStepOfWholeKitPixelsBetweenTheLimits()
    {
        Assert.AreEqual(FeelUI.RingMin, FeelUI.RingSide(new Vector2(20f, 20f)), 1e-4f);
        Assert.AreEqual(144f, FeelUI.RingSide(new Vector2(100f, 40f)), 1e-4f);
        Assert.AreEqual(FeelUI.RingMax, FeelUI.RingSide(new Vector2(900f, 300f)), 1e-4f);
        Assert.AreEqual(0f, FeelUI.RingStep % 3f, 1e-4f, "passo em pixels inteiros do kit (3 unidades)");

        for (float w = 0f; w < 400f; w += 7f)
        {
            float side = FeelUI.RingSide(new Vector2(w, w * .5f));
            Assert.AreEqual(0f, side % FeelUI.RingStep, 1e-4f, "lado " + side + " fora do passo");
            Assert.That(side, Is.InRange(FeelUI.RingMin, FeelUI.RingMax));
        }

        Assert.IsTrue(FeelUI.RingFits(new Vector2(144f, 60f)));
        Assert.IsFalse(FeelUI.RingFits(new Vector2(332f, 312f)));
    }

    // v5A: o aro é um traço de 1 pixel, fechado (sem buraco nas diagonais) e meio transparente
    [Test]
    public void ThinRing_IsOnePixelClosedAndTranslucent()
    {
        foreach (int diameter in new[] { 32, 40, 56, 80 })
        {
            Assert.AreEqual(diameter, PixelFx.ThinRing(diameter, FeelUI.RingAlpha).texture.width);
            var pixels = new Color32[diameter * diameter];
            PixelFx.PaintThinRing(pixels, diameter, FeelUI.RingAlpha);
            int lit = 0;
            foreach (Color32 p in pixels)
            {
                if (p.a == 0) continue;
                lit++;
                Assert.AreEqual(FeelUI.RingAlpha, p.a);
            }
            // um anel de 1 pixel tem por volta de 2,8 × diâmetro pixels acesos; traço de 2 passaria de 5 ×
            Assert.That(lit, Is.InRange(diameter * 2.5f, diameter * 3.6f), "diâmetro " + diameter);

            // fechado: andando em volta do centro, todo ângulo cruza o traço
            float c = (diameter - 1) * .5f, r = diameter * .5f - .5f;
            for (int k = 0; k < 360; k++)
            {
                float a = k * Mathf.Deg2Rad;
                int x = Mathf.Clamp(Mathf.RoundToInt(c + Mathf.Cos(a) * r), 0, diameter - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt(c + Mathf.Sin(a) * r), 0, diameter - 1);
                bool near = false;
                for (int dy = -1; dy <= 1 && !near; dy++)
                    for (int dx = -1; dx <= 1 && !near; dx++)
                    {
                        int px = x + dx, py = y + dy;
                        near = px >= 0 && py >= 0 && px < diameter && py < diameter && pixels[py * diameter + px].a > 0;
                    }
                Assert.IsTrue(near, $"buraco no aro de {diameter} no ângulo {k}");
            }
        }
    }
}
