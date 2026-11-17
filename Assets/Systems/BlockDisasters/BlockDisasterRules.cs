using System.Collections.Generic;
using UnityEngine;

// Estado da válvula do bueiro enquanto a equipe está na enchente (ValveMinigame lê e desenha).
public class ValveState
{
    public float pressure;      // 0 a 1
    public float angle;         // giro acumulado, em graus (só no sentido escolhido)
    public float direction;     // +1 anti-horário, -1 horário, 0 até o primeiro giro
    public bool held;
    public float lastPointerAngle;
    public int slips;
}

// Contas da enchente e do meteoro sem cena (docs/jam/ENCHENTE_METEORO.md). Testadas em Editor/BlockDisasterTests
// e iguais às do simulador (docs/jam/sim/quarteirao.py).
public static class BlockDisasterRules
{
    // Menor diferença entre dois ângulos, em graus, de -180 a 180.
    public static float AngleDelta(float from, float to)
    {
        return Mathf.DeltaAngle(from, to);
    }

    // Um quadro da válvula. turnedDegrees = quanto o ponteiro girou em volta dela neste quadro (0 sem botão).
    // Devolve a água que escoou neste quadro (fração do quarteirão) e diz se houve a falha leve (soltou com pressão).
    public static float ValveStep(ValveState state, BlockDisasterData data, bool held, float turnedDegrees, float dt, out bool slipped)
    {
        slipped = false;
        if (state.held && !held && state.pressure >= data.valveSlipPressure)
        {
            slipped = true;
            state.pressure = 0f;
            state.slips++;
        }
        if (!held) state.direction = 0f;
        state.held = held;

        if (held && Mathf.Abs(turnedDegrees) > 0f)
        {
            // O primeiro giro escolhe o sentido; voltar para trás não conta (sacudir o mouse não enche a pressão).
            if (state.direction == 0f && Mathf.Abs(turnedDegrees) >= 2f) state.direction = Mathf.Sign(turnedDegrees);
            float forward = state.direction != 0f ? Mathf.Max(0f, turnedDegrees * state.direction) : 0f;
            state.angle += forward;
            state.pressure += forward / 360f * data.valvePressurePerTurn;
        }

        state.pressure = Mathf.Clamp01(state.pressure - data.valvePressureDecay * dt);
        return state.pressure * data.valveDrainPerSecond * dt;
    }

    // Aceleração da reconstrução: a obra anda (1 + boost) vezes mais rápido.
    public static float RebuildSpeed(float boost) => 1f + Mathf.Max(0f, boost);

    public static float RebuildHit(float boost, BlockDisasterData data)
    {
        return Mathf.Min(data.rebuildMaxBoost, boost + data.rebuildHitBoost);
    }

    public static float RebuildMiss(float boost) => boost * 0.5f;

    public static float RebuildDecay(float boost, BlockDisasterData data, float dt)
    {
        return Mathf.Max(0f, boost - data.rebuildBoostDecay * dt);
    }

    // Próxima janela do ponto certo, sempre à frente do progresso. roll em [0, 1]. -1 se não cabe mais na obra.
    public static float NextWindow(float progress, float roll, BlockDisasterData data)
    {
        float start = progress + Mathf.Lerp(data.rebuildWindowGapMin, data.rebuildWindowGapMax, Mathf.Clamp01(roll));
        return start + data.rebuildWindowSize <= 1f ? start : -1f;
    }

    // Tempo de obra para uma equipe: o tempo base vale para a equipe mínima; gente a mais divide.
    public static float WorkTime(float baseTime, int minimum, int team)
    {
        int need = Mathf.Max(1, minimum);
        return baseTime * need / Mathf.Max(need, team);
    }

    // Quarteirão sorteado: entre os que têm casas suficientes livres (sem equipe), sem a Base nem o Armazém.
    // candidates[i] = casas livres do quarteirão i; blocked[i] = tem Base ou Armazém. roll em [0, 1). -1 sem nenhum.
    public static int PickBlock(IReadOnlyList<int> candidates, IReadOnlyList<bool> blocked, int minHouses, float roll)
    {
        List<int> ok = new();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] >= Mathf.Max(1, minHouses) && (blocked == null || !blocked[i])) ok.Add(i);
        }
        if (ok.Count == 0)
        {
            // Mapa sem quarteirão ideal: o que tiver mais casas livres, mesmo com a Base.
            int best = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] > 0 && (best < 0 || candidates[i] > candidates[best])) best = i;
            }
            return best;
        }
        return ok[Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(roll) * ok.Count), 0, ok.Count - 1)];
    }
}
