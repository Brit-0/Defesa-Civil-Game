using UnityEngine;

// Dificuldade adaptativa (docs/jam/ADAPTATIVA_V6B.md): quem joga bem (caos médio baixo, fila de chamados curta) e já
// cresceu (níveis de upgrade ou dinheiro guardado) recebe desastres mais frequentes e maiores. O ritmo vai de 0 a 1:
// sobe devagar, desce rápido quando o jogador começa a apanhar e os dois efeitos têm teto. Quem apanha ou ainda não
// cresceu fica em 0, com a curva de sempre. Regra pura (DisasterPaceTests), as mesmas contas do docs/jam/sim/sim.py.
public static class DisasterPace
{
    // Média que esquece devagar, com constante de tempo em segundos (0 = só a amostra). Não depende do tamanho do quadro.
    public static float Smooth(float average, float sample, float deltaTime, float timeConstant)
    {
        if (timeConstant <= 0f) return sample;

        return average + (sample - average) * (1f - Mathf.Exp(-Mathf.Max(0f, deltaTime) / timeConstant));
    }

    // Ritmo que a eficiência pede agora: calma x fila curta x poder (o maior entre níveis e dinheiro).
    public static float GetTarget(BalanceData balance, float chaosAverage, float queueAverage, int money, int levels)
    {
        float calm = Mathf.InverseLerp(balance.paceChaosHigh, balance.paceChaosLow, chaosAverage);
        float shortQueue = Mathf.InverseLerp(balance.paceQueueHigh, balance.paceQueueLow, queueAverage);
        float power = Mathf.Max(Mathf.InverseLerp(balance.paceLevelsStart, balance.paceLevelsFull, levels),
                                Mathf.InverseLerp(0f, balance.paceMoneyFull, money));
        return calm * shortQueue * power;
    }

    // Subida suave (paceRisePerMinute) e descida rápida (paceFallPerMinute) até o alvo.
    public static float Step(BalanceData balance, float pace, float target, float deltaTime)
    {
        if (target > pace) return Mathf.Min(target, pace + balance.paceRisePerMinute * deltaTime / 60f);

        return Mathf.Max(target, pace - balance.paceFallPerMinute * deltaTime / 60f);
    }

    // Intervalo entre desastres com o ritmo: até paceIntervalCut mais curto, nunca abaixo do piso (nem o sobe).
    public static float ApplyInterval(BalanceData balance, float interval, float pace)
    {
        float floor = Mathf.Min(interval, balance.paceIntervalFloor);
        return Mathf.Max(floor, interval * (1f - balance.paceIntervalCut * Mathf.Clamp01(pace)));
    }

    // Casas por desastre com o ritmo, antes de arredondar: até paceHouseBoost a mais, nunca acima do teto (nem o desce).
    public static float ApplyHouses(BalanceData balance, float houses, float pace)
    {
        float max = Mathf.Max(houses, balance.paceHousesMax);
        return Mathf.Min(max, houses * (1f + balance.paceHouseBoost * Mathf.Clamp01(pace)));
    }
}
