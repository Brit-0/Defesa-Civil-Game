using System.Collections.Generic;
using UnityEngine;

// Contas do para-raio sem cena (PARARAIO.md): de que quarteirão é uma casa, o sorteio do raio com a atração dos
// quarteirões protegidos e os traços da borda da prévia. Testadas em Editor/LightningRodTests (e no simulador).
public static class LightningRodMath
{
    // Índice do quarteirão que contém o ponto (com folga em volta) ou -1. Se a folga alcançar dois, o de centro mais perto.
    public static int BlockOf(IReadOnlyList<RodBlockSlot> blocks, Vector2 point, float margin)
    {
        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] == null) continue;
            Rect area = blocks[i].Area;
            Rect reach = Rect.MinMaxRect(area.xMin - margin, area.yMin - margin, area.xMax + margin, area.yMax + margin);
            if (!reach.Contains(point)) continue;
            float distance = (area.center - point).sqrMagnitude;
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = i;
        }
        return best;
    }

    // Sorteio do raio entre as casas intactas: cada uma pesa 1, ou 'attraction' se o quarteirão dela tem para-raio.
    // 'roll' em [0, 1). Igual ao alvo_do_raio do simulador (docs/jam/sim/sim.py).
    public static int PickIndex(IReadOnlyList<bool> protectedFlags, float attraction, float roll)
    {
        int count = protectedFlags.Count;
        if (count == 0) return -1;

        float weight = Mathf.Max(1f, attraction);
        float total = 0f;
        foreach (bool flag in protectedFlags) total += flag ? weight : 1f;

        float x = Mathf.Clamp01(roll) * total;
        for (int i = 0; i < count; i++)
        {
            x -= protectedFlags[i] ? weight : 1f;
            if (x < 0f) return i;
        }
        return count - 1;
    }

    // Centros dos traços da borda de um retângulo, mais ou menos de 'spacing' em 'spacing', com os quatro cantos.
    public static int OutlineDashes(Rect area, float spacing, List<Vector2> points)
    {
        points.Clear();
        if (spacing <= 0f || area.width <= 0f || area.height <= 0f) return 0;

        var corners = new[] { new Vector2(area.xMin, area.yMin), new Vector2(area.xMax, area.yMin), new Vector2(area.xMax, area.yMax), new Vector2(area.xMin, area.yMax) };
        for (int edge = 0; edge < 4; edge++)
        {
            Vector2 a = corners[edge], b = corners[(edge + 1) % 4];
            int steps = Mathf.Max(1, Mathf.RoundToInt(Vector2.Distance(a, b) / spacing));
            for (int k = 0; k < steps; k++) points.Add(Vector2.Lerp(a, b, (float)k / steps));
        }
        return points.Count;
    }
}
