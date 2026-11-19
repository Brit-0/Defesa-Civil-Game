using UnityEngine;

public enum DifficultyLevel { Easy, Normal, Hard }

// Dificuldade escolhida no menu inicial (docs/jam/CONSERTOS_V7B1.md). Normal = o jogo como está; Fácil e Difícil
// multiplicam por cima (DifficultyData): o intervalo entre desastres, a velocidade da rampa e o custo do Centro de
// Operações. O ritmo adaptativo (DisasterPace) continua agindo em cima. Regra pura (DifficultyTests).
public static class Difficulty
{
    // Vale até o fim do Play: o recomeço da partida mantém a escolha.
    public static DifficultyLevel Current { get; private set; } = DifficultyLevel.Normal;

    public static void Set(DifficultyLevel level)
    {
        Current = level;
    }

    // O Editor entra no Play sem recarregar o domínio: cada Play começa no Normal.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Current = DifficultyLevel.Normal;
    }

    public static float Interval(float interval) => Interval(DifficultyData.Instance, Current, interval);
    public static float RampTime(float rampTime) => RampTime(DifficultyData.Instance, Current, rampTime);
    public static int GoalCost(int cost) => GoalCost(DifficultyData.Instance, Current, cost);

    // Intervalo entre desastres × intervalMultiplier, sem passar do piso (uma curva que já vinha abaixo dele fica igual).
    public static float Interval(DifficultyData data, DifficultyLevel level, float interval)
    {
        float scaled = interval * Mathf.Max(0f, data.Get(level).intervalMultiplier);
        return Mathf.Max(Mathf.Min(interval, data.intervalFloor), scaled);
    }

    // Tempo de partida que a rampa enxerga: rampSpeed > 1 chega antes ao teto e segue subindo mais rápido depois dele.
    public static float RampTime(DifficultyData data, DifficultyLevel level, float rampTime)
    {
        return rampTime * Mathf.Max(0f, data.Get(level).rampSpeed);
    }

    public static int GoalCost(DifficultyData data, DifficultyLevel level, int cost)
    {
        return Mathf.RoundToInt(cost * Mathf.Max(0f, data.Get(level).goalCostMultiplier));
    }
}
