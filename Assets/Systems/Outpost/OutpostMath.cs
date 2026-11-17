using System.Collections.Generic;
using UnityEngine;

// Contas da guarita sem cena (GUARITAS.md): em que canto do quarteirão ela fica e quando um chamado já pode sair.
// Testadas em Editor/OutpostTests.
public static class OutpostMath
{
    // Pé da guarita: um dos quatro cantos do quarteirão, recuado 'inset' para dentro, onde o desenho dela (size, com o
    // pé no meio da borda de baixo) menos encosta nos obstáculos (desenhos das casas e das árvores). Menor área por
    // cima primeiro; depois a maior folga; empate: os cantos de baixo (a guarita fica na frente), o da esquerda antes.
    public static Vector2 PickCorner(Rect area, IReadOnlyList<Rect> obstacles, Vector2 size, float inset)
    {
        float ix = Mathf.Min(inset, area.width * 0.5f), iy = Mathf.Min(inset, area.height * 0.5f);
        var corners = new[]
        {
            new Vector2(area.xMin + ix, area.yMin + iy), new Vector2(area.xMax - ix, area.yMin + iy),
            new Vector2(area.xMin + ix, area.yMax - iy), new Vector2(area.xMax - ix, area.yMax - iy),
        };

        int best = 0;
        float bestOverlap = float.MaxValue, bestGap = float.MinValue;
        for (int i = 0; i < corners.Length; i++)
        {
            Rect booth = BoothRect(corners[i], size);
            float overlap = 0f, gap = float.MaxValue;
            if (obstacles != null)
            {
                foreach (Rect obstacle in obstacles)
                {
                    overlap += OverlapArea(booth, obstacle);
                    gap = Mathf.Min(gap, Gap(booth, obstacle));
                }
            }
            bool less = overlap < bestOverlap - 1e-3f;
            bool same = Mathf.Abs(overlap - bestOverlap) <= 1e-3f;
            if (!less && !(same && gap > bestGap + 1e-3f)) continue;
            best = i;
            bestOverlap = overlap;
            bestGap = gap;
        }
        return corners[best];
    }

    // Retângulo do desenho com o pé no meio da borda de baixo.
    public static Rect BoothRect(Vector2 foot, Vector2 size) => new Rect(foot.x - size.x * 0.5f, foot.y, size.x, size.y);

    public static float OverlapArea(Rect a, Rect b)
    {
        float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
        float h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
        return w > 0f && h > 0f ? w * h : 0f;
    }

    // Distância entre as bordas de dois retângulos (0 se encostam ou se cruzam).
    public static float Gap(Rect a, Rect b)
    {
        float dx = Mathf.Max(0f, Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax));
        float dy = Mathf.Max(0f, Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    // O chamado já esperou o tempo de a guarita reagir (o jogador vê o pino aparecer antes de a equipe sair).
    public static bool Ready(float waitingSince, float now, float reactionDelay) => now - waitingSince >= reactionDelay;
}
