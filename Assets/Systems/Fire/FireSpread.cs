using System.Collections.Generic;

// Fogo que escapa do jato (docs/jam/CONSERTOS_V7B3.md), sem nada do Unity para dar para testar fora dele.
// Sobrou chama quando a janela do jato fechou: com a chance do BalanceData, o fogo pula para a casa intacta mais perto,
// dentro do alcance. Nenhuma no alcance = não pula.
public static class FireSpread
{
    // roll de 0 (inclusive) a 1 (exclusive), como o Random.value: pula se cair abaixo da chance.
    public static bool Rolls(float roll, float chance)
    {
        return chance > 0f && roll < chance;
    }

    // Índice da casa mais perto que pode pegar fogo, com distância até o alcance; -1 se nenhuma.
    // Empate fica com a primeira da lista.
    public static int PickNearest(IReadOnlyList<float> distances, IReadOnlyList<bool> canCatch, float radius)
    {
        int best = -1;
        for (int i = 0; i < distances.Count && i < canCatch.Count; i++)
        {
            if (!canCatch[i] || distances[i] > radius) continue;
            if (best < 0 || distances[i] < distances[best]) best = i;
        }
        return best;
    }
}
