using UnityEngine;

// Dificuldade adaptativa (docs/jam/ADAPTATIVA_V6B.md): quem joga bem (caos médio baixo, fila de chamados curta) e já
// cresceu (níveis de upgrade ou dinheiro guardado) recebe desastres mais frequentes e maiores. O ritmo vai de 0 a 1:
// sobe devagar, desce rápido quando o jogador começa a apanhar e os dois efeitos têm teto. Quem apanha ou ainda não
// cresceu fica em 0, com a curva de sempre. Regra pura (DisasterPaceTests), as mesmas contas do docs/jam/sim/sim.py.
// Teto macio (CONSERTOS_V7A, pedido do Pietro): ao chegar ao teto já difícil, a curva e o ritmo continuam apertando,
// bem mais devagar, em vez de parar.
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

    // Intervalo entre desastres com o ritmo: até paceIntervalCut mais curto; abaixo do piso, só paceSoftShare do resto
    // (nunca sobe o intervalo de entrada).
    public static float ApplyInterval(BalanceData balance, float interval, float pace)
    {
        float floor = Mathf.Min(interval, balance.paceIntervalFloor);
        return SoftFloor(interval * (1f - balance.paceIntervalCut * Mathf.Clamp01(pace)), floor, balance.paceSoftShare);
    }

    // Casas por desastre com o ritmo, antes de arredondar: até paceHouseBoost a mais; acima do teto, só paceSoftShare
    // do resto (nunca desce as casas de entrada).
    public static float ApplyHouses(BalanceData balance, float houses, float pace)
    {
        float max = Mathf.Max(houses, balance.paceHousesMax);
        return SoftCeiling(houses * (1f + balance.paceHouseBoost * Mathf.Clamp01(pace)), max, balance.paceSoftShare);
    }

    // Intervalo depois do fim da rampa: cai disasterLateIntervalPerMinute por minuto até disasterIntervalFloor e dali
    // encolhe disasterIntervalSoftPerMinute por minuto, sem parar e sem chegar a 0 (0 = piso duro, como antes).
    public static float LateInterval(BalanceData balance, float interval, float lateMinutes)
    {
        float floor = Mathf.Min(interval, balance.disasterIntervalFloor);
        float rate = balance.disasterLateIntervalPerMinute;
        float linear = interval - rate * Mathf.Max(0f, lateMinutes);
        if (linear >= floor || rate <= 0f) return Mathf.Max(floor, linear);

        float minutesPastFloor = (floor - linear) / rate;
        return floor * Mathf.Pow(1f - Mathf.Clamp(balance.disasterIntervalSoftPerMinute, 0f, .5f), minutesPastFloor);
    }

    // Casas depois do fim da rampa: sobem disasterLateHousesPerMinute por minuto até disasterHousesMax e dali
    // disasterHousesSoftPerMinute por minuto, sem parar (0 = teto duro, como antes).
    public static float LateHouses(BalanceData balance, float houses, float lateMinutes)
    {
        float max = Mathf.Max(houses, balance.disasterHousesMax);
        float rate = balance.disasterLateHousesPerMinute;
        float linear = houses + rate * Mathf.Max(0f, lateMinutes);
        if (linear <= max || rate <= 0f) return Mathf.Min(max, linear);

        float minutesPastMax = (linear - max) / rate;
        return max + Mathf.Max(0f, balance.disasterHousesSoftPerMinute) * minutesPastMax;
    }

    // Piso macio: até o piso o valor passa igual; o que passaria dele ainda vale 'share' (0 = piso duro).
    public static float SoftFloor(float value, float floor, float share)
    {
        return value >= floor ? value : floor - (floor - value) * Mathf.Clamp01(share);
    }

    // Teto macio: até o teto o valor passa igual; o que passaria dele ainda vale 'share' (0 = teto duro).
    public static float SoftCeiling(float value, float ceiling, float share)
    {
        return value <= ceiling ? value : ceiling + (value - ceiling) * Mathf.Clamp01(share);
    }
}
