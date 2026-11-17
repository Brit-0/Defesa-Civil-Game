using UnityEngine;

// A luz de um momento do ciclo, já misturada entre duas fases.
public struct DayNightSample
{
    public Color ambient;
    public float ambientIntensity;
    public Color sun;
    public float sunIntensity;
    public float night;
}

// Contas do ciclo de dia e noite (docs/jam/ENERGIA_DIA_NOITE.md), sem cena: testadas no DayNightTests.
public static class DayNightModel
{
    // Fase (0 a 1) depois de tantos segundos de partida.
    public static float PhaseAt(float startPhase, float elapsed, float cycleSeconds)
    {
        if (cycleSeconds <= 0f) return Mathf.Repeat(startPhase, 1f);
        return Mathf.Repeat(startPhase + elapsed / cycleSeconds, 1f);
    }

    // Luz na fase dada. As fases vêm em ordem; da última volta para a primeira (a noite emenda no amanhecer).
    // Entre duas fases a passagem é suave (smoothstep), sem degrau no começo nem no fim.
    public static DayNightSample Evaluate(DayNightKey[] keys, float phase)
    {
        if (keys == null || keys.Length == 0) return new DayNightSample { ambient = Color.white, ambientIntensity = 1f, sun = Color.white };
        if (keys.Length == 1) return Sample(keys[0]);

        phase = Mathf.Repeat(phase, 1f);
        int from = keys.Length - 1;
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i].phase <= phase) from = i;
        }
        int to = (from + 1) % keys.Length;

        float start = keys[from].phase;
        float end = keys[to].phase;
        if (end <= start) end += 1f;
        float at = phase < start ? phase + 1f : phase;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, at));
        return Lerp(keys[from], keys[to], t);
    }

    // Quanto uma luz está acesa (0 a 1): cada luz acende no seu momento dentro do pedaço "spread" do anoitecer,
    // e é a última a acender a primeira a apagar de manhã, para não piscar tudo junto.
    public static float LightOn(float night, float slot, float spread)
    {
        float start = Mathf.Clamp01(slot) * Mathf.Clamp01(spread);
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, start + .12f, night));
    }

    // Lugar da luz na fila de acender (0 a 1), fixo para a mesma posição do mundo.
    public static float Slot(Vector2 position)
    {
        float hash = Mathf.Sin(position.x * 12.9898f + position.y * 78.233f) * 43758.5453f;
        return hash - Mathf.Floor(hash);
    }

    // Fase da chave com esse nome (ou -1): para pular direto para a noite no teste e nos prints.
    public static float PhaseOf(DayNightKey[] keys, string name)
    {
        if (keys == null) return -1f;
        foreach (DayNightKey key in keys)
        {
            if (string.Equals(key.name, name, System.StringComparison.OrdinalIgnoreCase)) return key.phase;
        }
        return -1f;
    }

    private static DayNightSample Sample(DayNightKey key) => new DayNightSample
    {
        ambient = key.ambient,
        ambientIntensity = key.ambientIntensity,
        sun = key.sun,
        sunIntensity = key.sunIntensity,
        night = key.night,
    };

    private static DayNightSample Lerp(DayNightKey a, DayNightKey b, float t) => new DayNightSample
    {
        ambient = Color.Lerp(a.ambient, b.ambient, t),
        ambientIntensity = Mathf.Lerp(a.ambientIntensity, b.ambientIntensity, t),
        sun = Color.Lerp(a.sun, b.sun, t),
        sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t),
        night = Mathf.Lerp(a.night, b.night, t),
    };
}
