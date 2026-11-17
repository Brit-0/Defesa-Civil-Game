using NUnit.Framework;
using UnityEngine;

// Contas do ciclo de dia e noite (docs/jam/ENERGIA_DIA_NOITE.md). EditMode, sem cena.
public class DayNightTests
{
    private static DayNightKey[] Keys => DayNightData.DefaultKeys();

    [Test]
    public void PhaseWrapsAroundTheCycle()
    {
        Assert.AreEqual(.1f, DayNightModel.PhaseAt(.1f, 0f, 420f), 1e-5f);
        Assert.AreEqual(.1f, DayNightModel.PhaseAt(.1f, 420f, 420f), 1e-4f);
        Assert.AreEqual(.6f, DayNightModel.PhaseAt(.1f, 210f, 420f), 1e-4f);
        Assert.AreEqual(0f, DayNightModel.PhaseAt(.9f, 42f, 420f), 1e-4f);
        Assert.AreEqual(.3f, DayNightModel.PhaseAt(.3f, 99f, 0f), 1e-5f, "ciclo zerado fica parado");
    }

    [Test]
    public void KeysAreInOrderAndHitExactly()
    {
        DayNightKey[] keys = Keys;
        for (int i = 0; i < keys.Length; i++)
        {
            if (i > 0) Assert.Greater(keys[i].phase, keys[i - 1].phase, keys[i].name);
            DayNightSample sample = DayNightModel.Evaluate(keys, keys[i].phase);
            Assert.AreEqual(keys[i].ambientIntensity, sample.ambientIntensity, 1e-4f, keys[i].name);
            Assert.AreEqual(keys[i].night, sample.night, 1e-4f, keys[i].name);
        }
    }

    [Test]
    public void LightChangesSmoothlyAllAroundTheCycle()
    {
        DayNightKey[] keys = Keys;
        DayNightSample last = DayNightModel.Evaluate(keys, 0f);
        for (int i = 1; i <= 2000; i++)
        {
            DayNightSample sample = DayNightModel.Evaluate(keys, i / 2000f);
            Assert.Less(Mathf.Abs(sample.ambientIntensity - last.ambientIntensity), .01f, $"degrau na fase {i / 2000f}");
            Assert.Less(Mathf.Abs(sample.night - last.night), .02f, $"degrau na fase {i / 2000f}");
            Assert.Less(Mathf.Abs(sample.ambient.r - last.ambient.r) + Mathf.Abs(sample.ambient.b - last.ambient.b), .02f);
            last = sample;
        }
    }

    [Test]
    public void NightIsDarkButReadable()
    {
        DayNightKey[] keys = Keys;
        DayNightSample night = DayNightModel.Evaluate(keys, DayNightModel.PhaseOf(keys, "Noite") + .1f);
        DayNightSample day = DayNightModel.Evaluate(keys, DayNightModel.PhaseOf(keys, "Dia"));
        DayNightSample dusk = DayNightModel.Evaluate(keys, DayNightModel.PhaseOf(keys, "Entardecer"));

        Assert.AreEqual(1f, night.night, 1e-4f);
        Assert.AreEqual(0f, day.night, 1e-4f);
        Assert.That(night.ambientIntensity, Is.InRange(.35f, .5f), "escura, mas a cidade continua legível");
        Assert.Greater(day.ambientIntensity, night.ambientIntensity + .25f);
        Assert.Greater(dusk.ambient.r, dusk.ambient.b, "entardecer quente");
        Assert.Greater(night.ambient.b, night.ambient.r, "noite azulada");
    }

    [Test]
    public void LightsTurnOnOneByOne()
    {
        for (float slot = 0f; slot <= 1f; slot += .1f)
        {
            Assert.AreEqual(0f, DayNightModel.LightOn(0f, slot, .45f), 1e-5f, "de dia tudo apagado");
            Assert.AreEqual(1f, DayNightModel.LightOn(1f, slot, .45f), 1e-5f, "na noite cheia tudo aceso");
        }
        Assert.Greater(DayNightModel.LightOn(.2f, 0f, .45f), DayNightModel.LightOn(.2f, 1f, .45f), "cada luz no seu momento");

        float a = DayNightModel.Slot(new Vector2(3.5f, -2f));
        Assert.AreEqual(a, DayNightModel.Slot(new Vector2(3.5f, -2f)), "mesmo lugar, mesmo momento");
        Assert.That(a, Is.InRange(0f, .99999f));
    }

    [Test]
    public void DataAssetHasAFullDayInSixToEightMinutes()
    {
        DayNightData data = Resources.Load<DayNightData>(DayNightData.ResourcePath);

        Assert.IsNotNull(data, "rode Tools/Jam/Create Day Night Data");
        Assert.That(data.cycleSeconds, Is.InRange(360f, 480f));
        Assert.Less(DayNightModel.Evaluate(data.keys, data.startPhase).night, .05f, "a partida começa de dia");
        Assert.IsNotNull(Resources.Load<Material>("DayNight/WindowGlowMAT"), "material das janelas");
    }
}
