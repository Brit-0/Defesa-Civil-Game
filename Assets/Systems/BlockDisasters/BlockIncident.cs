using System.Collections.Generic;
using UnityEngine;

public enum IncidentKind { Flood, Meteor }

// Trabalho com regra própria no lugar do círculo comum (docs/jam/ENCHENTE_METEORO.md). O House e o Stats perguntam
// aqui quando o chamado tem regra (Call.rule); sem regra, o trabalho de sempre.
public interface IWorkRule
{
    float WorkTime(int team);           // segundos de obra sem mini game (0 = só pelo mini game)
    int MaterialCost { get; }
    int Reward { get; }                 // antes do efeito dos upgrades e do bônus do chamado
    bool HasCritical { get; }           // ponto certo vale nesta obra
    float CritWindowSize { get; }
    void Begin(House house);            // a equipe chegou
    float Tick(House house, float dt, float duration); // quanto a obra anda neste quadro (pode voltar)
    float NextWindow(float progress);   // início da próxima janela do ponto certo; -1 sem mais janela
    float Critical(House house, bool hit); // ponto certo: quanto a obra anda (ou volta); a janela volta à frente
}

// Um quarteirão alagado ou destruído e o chamado único dele. O líder guarda o chamado; as outras casas do quarteirão
// passam o clique para ele (House.IncidentLeader).
public class BlockIncident : IWorkRule
{
    public IncidentKind kind;
    public DisasterData disaster;
    public Rect area;
    public House leader;
    public readonly List<House> houses = new();
    public Call call;
    public float startTime;     // GameClock.Time do começo (a água sobe a partir daqui)
    public bool resolved;

    // Enchente: a válvula do bueiro, aberta quando a equipe chega.
    public readonly ValveState valve = new();
    public bool ValveOpen { get; private set; }
    private bool pointerHeld;
    private float pendingDegrees;

    // Meteoro: aceleração da obra, acertos e erros em sequência.
    public float Boost { get; private set; }
    public int Hits { get; private set; }
    public int Misses { get; private set; }

    private static BlockDisasterData Data => BlockDisasterData.Instance;

    public Vector3 Center => new(area.center.x, area.center.y, leader != null ? leader.transform.position.z : 0f);

    // Água de 0 a 1: sobe no começo; com a equipe trabalhando desce junto com a obra.
    public float WaterLevel
    {
        get
        {
            if (kind != IncidentKind.Flood || resolved || leader == null) return 0f;
            if (leader.IsComplete) return 0f;
            if (leader.IsWorking) return 1f - leader.WorkProgress01;
            float rise = Data.floodRiseTime;
            return rise > 0f ? Mathf.Clamp01((GameClock.Time - startTime) / rise) : 1f;
        }
    }

    #region WORK RULE

    public float WorkTime(int team)
    {
        if (kind == IncidentKind.Flood)
        {
            return Data.floodAutoDrainTime > 0f ? BlockDisasterRules.WorkTime(Data.floodAutoDrainTime, Data.floodWorkers, team) : 0f;
        }
        return BlockDisasterRules.WorkTime(Data.meteorWorkTime, Data.meteorWorkers, team);
    }

    public int MaterialCost => kind == IncidentKind.Meteor ? Data.meteorMaterial : 0;

    public int Reward => houses.Count * (kind == IncidentKind.Flood ? Data.floodRewardPerHouse : Data.meteorRewardPerHouse);

    public bool HasCritical => kind == IncidentKind.Meteor;

    public float CritWindowSize => Data.rebuildWindowSize;

    public void Begin(House house)
    {
        if (kind == IncidentKind.Flood)
        {
            ValveOpen = true;
            pendingDegrees = 0f;
            GameEvents.RaiseValveOpened(this);
        }
        else
        {
            Boost = 0f;
        }
    }

    public float Tick(House house, float dt, float duration)
    {
        float auto = duration > 0f ? dt / duration : 0f;
        if (kind == IncidentKind.Meteor)
        {
            Boost = BlockDisasterRules.RebuildDecay(Boost, Data, dt);
            return auto * BlockDisasterRules.RebuildSpeed(Boost);
        }

        float turned = pendingDegrees;
        pendingDegrees = 0f;
        float drained = BlockDisasterRules.ValveStep(valve, Data, pointerHeld, turned, dt, out bool slipped);
        if (slipped)
        {
            GameEvents.RaiseSkillCheck(house, SkillResult.ValveSlip);
            return auto + drained - Data.valveSlipPenalty;
        }
        return auto + drained;
    }

    public float NextWindow(float progress) => BlockDisasterRules.NextWindow(progress, Random.value, Data);

    public float Critical(House house, bool hit)
    {
        if (hit)
        {
            Hits++;
            Boost = BlockDisasterRules.RebuildHit(Boost, Data);
            return Data.rebuildHitProgress;
        }
        Misses++;
        Boost = BlockDisasterRules.RebuildMiss(Boost);
        return -Data.rebuildMissPenalty;
    }

    #endregion

    // Chamado pelo ValveMinigame a cada quadro com a válvula aberta: o giro entra no próximo Tick da obra.
    public void ValveInput(bool held, float turnedDegrees)
    {
        if (!ValveOpen) return;

        pointerHeld = held;
        if (held) pendingDegrees += turnedDegrees;
    }

    public void CloseValve()
    {
        ValveOpen = false;
        pointerHeld = false;
        pendingDegrees = 0f;
    }
}
