using System.Collections.Generic;
using UnityEngine;

// Contas da expansão sem cena (testadas no EditMode): ligar a entrada na rua, quem a base nova atende e a borda da tela.
public static class ExpansionMath
{
    // Distância de p até o segmento ab; closest é o ponto do segmento mais perto de p.
    public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b, out Vector2 closest)
    {
        Vector2 ab = b - a;
        float length2 = ab.sqrMagnitude;
        float t = length2 > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / length2) : 0f;
        closest = a + ab * t;
        return Vector2.Distance(p, closest);
    }

    // A ligação da malha de waypoints (positions[i] com cada índice de neighbors[i]) que passa mais perto de p.
    public static bool NearestEdge(IReadOnlyList<Vector2> positions, IReadOnlyList<List<int>> neighbors, Vector2 p,
        out int a, out int b, out float distance)
    {
        a = b = -1;
        distance = float.MaxValue;
        for (int i = 0; i < positions.Count; i++)
        {
            foreach (int j in neighbors[i])
            {
                if (j <= i || j >= positions.Count) continue;
                float d = DistanceToSegment(p, positions[i], positions[j], out _);
                if (d >= distance) continue;
                distance = d;
                a = i;
                b = j;
            }
        }
        return a >= 0;
    }

    // Casas que passariam a ter candidate como base mais perto (a mesma regra em linha reta do GetNearestGuard).
    // distances recebe a distância de cada uma delas até candidate.
    public static int CountServed(Vector2 candidate, IReadOnlyList<Vector2> bases, IReadOnlyList<Vector2> houses,
        List<float> distances = null)
    {
        distances?.Clear();
        int served = 0;
        foreach (Vector2 house in houses)
        {
            float mine = Vector2.Distance(house, candidate);
            bool closer = true;
            foreach (Vector2 other in bases)
            {
                if (Vector2.Distance(house, other) <= mine)
                {
                    closer = false;
                    break;
                }
            }
            if (!closer) continue;
            served++;
            distances?.Add(mine);
        }
        return served;
    }

    // Raio que deixa a fração percentile das distâncias dentro do anel (0 sem casas).
    public static float Percentile(List<float> values, float percentile)
    {
        if (values == null || values.Count == 0) return 0f;
        values.Sort();
        int index = Mathf.Clamp(Mathf.CeilToInt(percentile * values.Count) - 1, 0, values.Count - 1);
        return values[index];
    }

    // Quantos pontos cabem num anel de raio radius com o espaço spacing (no mínimo 12, para o anel ler como anel).
    public static int RingDots(float radius, float spacing)
    {
        if (radius <= 0f || spacing <= 0f) return 0;
        return Mathf.Max(12, Mathf.RoundToInt(2f * Mathf.PI * radius / spacing));
    }

    // Comprimento de um caminho de pontos (a caminhada pela rua).
    public static float PathLength(IReadOnlyList<Vector2> points)
    {
        float length = 0f;
        for (int i = 1; i < points.Count; i++) length += Vector2.Distance(points[i - 1], points[i]);
        return length;
    }

    // Ponto da placa na tela: dentro de safe fica onde está; fora, vai para a borda de safe na direção de target
    // vista do centro, e angle diz para onde a seta aponta (graus, 0 = para cima, anti-horário).
    public static Vector2 ClampToEdge(Vector2 target, Rect safe, out float angle, out bool clamped)
    {
        clamped = !safe.Contains(target);
        angle = 0f;
        if (!clamped) return target;

        Vector2 center = safe.center;
        Vector2 direction = target - center;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;

        float scaleX = Mathf.Abs(direction.x) > 0.0001f ? safe.width * 0.5f / Mathf.Abs(direction.x) : float.MaxValue;
        float scaleY = Mathf.Abs(direction.y) > 0.0001f ? safe.height * 0.5f / Mathf.Abs(direction.y) : float.MaxValue;
        Vector2 point = center + direction * Mathf.Min(scaleX, scaleY);
        angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        return new Vector2(Mathf.Clamp(point.x, safe.xMin, safe.xMax), Mathf.Clamp(point.y, safe.yMin, safe.yMax));
    }
}
