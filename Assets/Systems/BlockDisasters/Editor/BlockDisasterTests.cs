using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Contas da enchente e do meteoro (docs/jam/ENCHENTE_METEORO.md): a válvula do bueiro, a reconstrução com acertos em
// sequência, o quarteirão sorteado e os dados de entrada. Sem cena.
public class BlockDisasterTests
{
    private BlockDisasterData data;

    [SetUp]
    public void SetUp() => data = ScriptableObject.CreateInstance<BlockDisasterData>();

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(data);

    // Gira a válvula a 'turnsPerSecond' voltas por segundo por 'seconds' e devolve a água escoada.
    private float Spin(ValveState valve, float turnsPerSecond, float seconds, float dt = 1f / 60f)
    {
        float drained = 0f;
        for (float t = 0f; t < seconds; t += dt)
        {
            drained += BlockDisasterRules.ValveStep(valve, data, true, turnsPerSecond * 360f * dt, dt, out _);
        }
        return drained;
    }

    [Test]
    public void Valve_GirarSobePressaoEEscoa()
    {
        ValveState valve = new();
        float drained = Spin(valve, 1.5f, 1f);
        Assert.Greater(valve.pressure, .3f, "uma volta e meia por segundo enche a pressão");
        Assert.Greater(drained, 0f);
        Assert.Greater(valve.angle, 500f);
    }

    [Test]
    public void Valve_SacudirParaTrasNaoConta()
    {
        ValveState valve = new();
        float dt = 1f / 60f;
        for (int i = 0; i < 120; i++)
        {
            // vai e volta o mesmo tanto: só a ida conta
            float turn = i % 2 == 0 ? 20f : -20f;
            BlockDisasterRules.ValveStep(valve, data, true, turn, dt, out _);
        }
        ValveState steady = new();
        Spin(steady, 1.5f, 2f);
        Assert.Less(valve.pressure, steady.pressure, "sacudir rende menos que girar em círculo");
    }

    [Test]
    public void Valve_SoltarComPressaoEFalhaLeve()
    {
        ValveState valve = new();
        Spin(valve, 1.5f, 1f);
        Assert.GreaterOrEqual(valve.pressure, data.valveSlipPressure);

        BlockDisasterRules.ValveStep(valve, data, false, 0f, 1f / 60f, out bool slipped);
        Assert.IsTrue(slipped);
        Assert.AreEqual(0f, valve.pressure);
        Assert.AreEqual(1, valve.slips);
    }

    [Test]
    public void Valve_SoltarSemPressaoNaoFalha()
    {
        ValveState valve = new();
        BlockDisasterRules.ValveStep(valve, data, true, 5f, 1f / 60f, out _);
        BlockDisasterRules.ValveStep(valve, data, false, 0f, 1f / 60f, out bool slipped);
        Assert.IsFalse(slipped);
    }

    [Test]
    public void Valve_PressaoVazaParada()
    {
        ValveState valve = new();
        Spin(valve, 1.5f, 1f);
        float before = valve.pressure;
        for (int i = 0; i < 60; i++) BlockDisasterRules.ValveStep(valve, data, true, 0f, 1f / 60f, out _);
        Assert.Less(valve.pressure, before);
    }

    [Test]
    public void Valve_MinigameRapido()
    {
        // Girando sem parar (1,5 volta/s), com o escoamento automático da equipe mínima: menos de 8 s.
        ValveState valve = new();
        float auto = 1f / data.floodAutoDrainTime;
        float water = 1f, t = 0f, dt = 1f / 60f;
        while (water > 0f && t < 60f)
        {
            water -= BlockDisasterRules.ValveStep(valve, data, true, 1.5f * 360f * dt, dt, out _) + auto * dt;
            t += dt;
        }
        Assert.Less(t, 8f, $"bueiro desentupido em {t:0.0} s");
        Assert.Greater(t, 2f, "nem instantâneo");
    }

    [Test]
    public void Rebuild_AcertosAceleramComTeto()
    {
        float boost = 0f;
        for (int i = 0; i < 10; i++) boost = BlockDisasterRules.RebuildHit(boost, data);
        Assert.AreEqual(data.rebuildMaxBoost, boost, 1e-4f);
        Assert.AreEqual(1f + data.rebuildMaxBoost, BlockDisasterRules.RebuildSpeed(boost), 1e-4f);

        Assert.AreEqual(boost * .5f, BlockDisasterRules.RebuildMiss(boost), 1e-4f);
        Assert.Less(BlockDisasterRules.RebuildDecay(boost, data, 1f), boost);
        Assert.AreEqual(0f, BlockDisasterRules.RebuildDecay(.01f, data, 10f));
    }

    [Test]
    public void Rebuild_SequenciaDeAcertosEncurtaAObra()
    {
        float SimulatedTime(bool hitEveryWindow)
        {
            float progress = 0f, boost = 0f, t = 0f, dt = 1f / 30f;
            float duration = BlockDisasterRules.WorkTime(data.meteorWorkTime, data.meteorWorkers, data.meteorWorkers);
            float window = BlockDisasterRules.NextWindow(0f, .5f, data);
            while (progress < 1f && t < 600f)
            {
                boost = BlockDisasterRules.RebuildDecay(boost, data, dt);
                progress += dt / duration * BlockDisasterRules.RebuildSpeed(boost);
                if (window >= 0f && progress >= window + data.rebuildWindowSize * .5f)
                {
                    if (hitEveryWindow)
                    {
                        boost = BlockDisasterRules.RebuildHit(boost, data);
                        progress += data.rebuildHitProgress;
                    }
                    window = BlockDisasterRules.NextWindow(progress, .5f, data);
                }
                t += dt;
            }
            return t;
        }

        float lazy = SimulatedTime(false);
        float skilled = SimulatedTime(true);
        Assert.AreEqual(data.meteorWorkTime, lazy, 1f, "sem acerto a obra leva o tempo base");
        Assert.Less(skilled, lazy * .5f, $"acertando tudo: {skilled:0} s contra {lazy:0} s");
    }

    [Test]
    public void Rebuild_JanelaSempreAFrente()
    {
        Assert.Greater(BlockDisasterRules.NextWindow(.3f, 0f, data), .3f);
        Assert.AreEqual(.3f + data.rebuildWindowGapMax, BlockDisasterRules.NextWindow(.3f, 1f, data), 1e-4f);
        Assert.AreEqual(-1f, BlockDisasterRules.NextWindow(.97f, 0f, data), "não cabe janela no fim da obra");
    }

    [Test]
    public void WorkTime_EquipeMaiorDivide()
    {
        Assert.AreEqual(90f, BlockDisasterRules.WorkTime(90f, 8, 8), 1e-4f);
        Assert.AreEqual(90f, BlockDisasterRules.WorkTime(90f, 8, 3), 1e-4f, "equipe menor não sai: vale o mínimo");
        Assert.AreEqual(60f, BlockDisasterRules.WorkTime(90f, 8, 12), 1e-4f);
    }

    [Test]
    public void PickBlock_CasasSuficientesSemBase()
    {
        List<int> free = new() { 2, 5, 6, 4 };
        List<bool> landmark = new() { false, true, false, false };
        for (float roll = 0f; roll < 1f; roll += .1f)
        {
            int index = BlockDisasterRules.PickBlock(free, landmark, 3, roll);
            Assert.That(index == 2 || index == 3, $"roll {roll}: {index}");
        }
        Assert.AreEqual(2, BlockDisasterRules.PickBlock(free, new List<bool> { true, true, true, true }, 3, .5f), "sem ideal: o mais cheio");
        Assert.AreEqual(-1, BlockDisasterRules.PickBlock(new List<int> { 0, 0 }, null, 3, .5f));
    }

    [Test]
    public void Valve_AnguloCruzandoOMeio()
    {
        Assert.AreEqual(20f, BlockDisasterRules.AngleDelta(170f, -170f), 1e-4f);
        Assert.AreEqual(-20f, BlockDisasterRules.AngleDelta(-170f, 170f), 1e-4f);
    }

    [Test]
    public void Dados_EntradaTardiaERara()
    {
        Assert.That(data.floodUnlockTime, Is.InRange(200f, 280f), "enchente por volta dos 4 min");
        Assert.That(data.meteorUnlockTime, Is.InRange(420f, 540f), "meteoro por volta dos 8 min");
        Assert.Less(data.meteorWeight, data.floodWeight, "meteoro é raro");
        Assert.GreaterOrEqual(data.meteorWorkers, 6, "reconstrução pede equipe grande");
        Assert.Greater(data.meteorWorkTime, 45f, "reconstrução é longa");
        Assert.Greater(data.meteorCooldown, 0f);
    }

    [Test]
    public void Dados_TiposNovosNoFimDoEnum()
    {
        // Os .asset guardam o tipo como número: os antigos não podem mudar.
        Assert.AreEqual(0, (int)DisasterType.Earthquake);
        Assert.AreEqual(1, (int)DisasterType.Storm);
        Assert.AreEqual(2, (int)DisasterType.Fire);
        Assert.AreEqual(3, (int)DisasterType.Flood);
        Assert.AreEqual(4, (int)DisasterType.Meteor);
    }

    [Test]
    public void Dados_AssetsNoProjeto()
    {
        BlockDisasterData asset = Resources.Load<BlockDisasterData>(BlockDisasterData.ResourcePath);
        Assert.IsNotNull(asset, "rode Tools/Jam/Enchente e meteoro/Criar dados");
        Assert.Greater(asset.blocks.Count, 10, "quarteirões do mapa copiados");
        Assert.IsNotNull(asset.callFloodIcon, "ícone do chamado da enchente");
        Assert.IsNotNull(asset.callRebuildIcon, "ícone do chamado da reconstrução");

        bool flood = false, meteor = false;
        foreach (DisasterData disaster in Resources.LoadAll<DisasterData>("Disasters"))
        {
            flood |= disaster.type == DisasterType.Flood && disaster.icon != null;
            meteor |= disaster.type == DisasterType.Meteor && disaster.icon != null;
        }
        Assert.IsTrue(flood, "Disasters/Flood.asset com ícone");
        Assert.IsTrue(meteor, "Disasters/Meteor.asset com ícone");
    }
}
