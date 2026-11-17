using UnityEngine;

// Contas do áudio v5 sem estado (testadas no SoundBankTests): variação sem repetir, escada de tom, lado do som na tela,
// camadas da música pela intensidade e o corte do abafado da pausa.
public static class SoundMath
{
    // Variação sorteada sem repetir a última (com 2 ou mais clipes)
    public static int PickIndex(int count, int last, float random01)
    {
        if (count <= 1) return 0;

        int pick = Mathf.Min(count - 2, Mathf.FloorToInt(random01 * (count - 1)));
        return last >= 0 && pick >= last ? pick + 1 : pick;
    }

    public static float SemitonesToPitch(float semitones)
    {
        return Mathf.Pow(2f, semitones / 12f);
    }

    public static float DbToLinear(float db)
    {
        return Mathf.Pow(10f, db / 20f);
    }

    // Degrau da escada: sobe se o toque anterior foi há menos de `reset`, senão volta a zero
    public static int NextLadderStep(int step, float sinceLast, float reset, int max)
    {
        return sinceLast <= reset ? Mathf.Min(step + 1, max) : 0;
    }

    // Lado do som (-1 esquerda, 1 direita) pela posição na tela; `width` limita a abertura
    public static float Pan(float worldX, float cameraX, float halfWidth, float width)
    {
        if (halfWidth <= 0f) return 0f;

        return Mathf.Clamp((worldX - cameraX) / halfWidth, -1f, 1f) * width;
    }

    // Fora da tela o som cai até `offscreenDb` (a uma tela de distância)
    public static float OffscreenGain(float worldX, float worldY, Vector2 cameraPos, Vector2 halfSize, float offscreenDb)
    {
        if (halfSize.x <= 0f || halfSize.y <= 0f) return 1f;

        float outX = Mathf.Max(0f, Mathf.Abs(worldX - cameraPos.x) - halfSize.x) / halfSize.x;
        float outY = Mathf.Max(0f, Mathf.Abs(worldY - cameraPos.y) - halfSize.y) / halfSize.y;
        return DbToLinear(offscreenDb * Mathf.Clamp01(Mathf.Max(outX, outY)));
    }

    // Camadas da música: 0 base (sempre), 1 ritmo (algo acontecendo), 2 tensão (caos médio), 3 perigo (caos alto)
    public static void LayerTargets(float activity, float tension, float danger, float[] targets)
    {
        targets[0] = 1f;
        targets[1] = Mathf.Clamp01(Mathf.Max(activity, tension * 2f));
        targets[2] = Mathf.Clamp01(tension);
        targets[3] = Mathf.Clamp01(danger);
    }

    // Corte do passa-baixa da pausa, andando em escala de oitavas (o ouvido escuta frequência assim)
    public static float CutoffAt(float muffle01, float openHz, float closedHz)
    {
        float octaves = Mathf.Log(openHz / closedHz, 2f);
        return openHz / Mathf.Pow(2f, octaves * Mathf.Clamp01(muffle01));
    }
}
