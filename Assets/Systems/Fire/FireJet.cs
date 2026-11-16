// Minigame do jato (docs/jam/ENERGIA_FOGO.md), sem nada do Unity para dar para testar fora dele.
// A equipe chega no incêndio e abre uma janela curta: o jogador arrasta o mouse apertado por cima das chamas.
// Todas apagadas dentro da janela = fogo apagado na hora e bônus. Sobrou chama quando a janela fecha = o fogo cresce.
public class FireJet
{
    public int Flames { get; }
    public float Duration { get; }
    public int Doused { get; private set; }
    public float TimeLeft { get; private set; }
    public bool Closed { get; private set; }

    public bool Succeeded => Doused >= Flames;
    public float TimeLeft01 => Duration > 0f ? TimeLeft / Duration : 0f;

    public FireJet(int flames, float duration)
    {
        Flames = flames < 1 ? 1 : flames;
        Duration = duration;
        TimeLeft = duration;
    }

    // Apaga uma chama. Devolve false com a janela fechada.
    public bool Douse()
    {
        if (Closed) return false;

        Doused++;
        if (Succeeded) Closed = true;
        return true;
    }

    // Corre o relógio. Devolve true no quadro em que a janela fecha por tempo, com chama sobrando.
    public bool Tick(float deltaTime)
    {
        if (Closed) return false;

        TimeLeft -= deltaTime;
        if (TimeLeft > 0f) return false;

        TimeLeft = 0f;
        Closed = true;
        return true;
    }
}

// Regras do incêndio que crescem: o nível vai de 1 ao máximo com o tempo de espera e com a falha no jato.
public static class FireRules
{
    // Nível pelo tempo que o fogo já queimou sem equipe: 1 quando pega, sobe em partes iguais do temporizador.
    public static int LevelFromTimer(float timeLeft01, int maxLevel)
    {
        if (maxLevel < 1) return 1;

        float burned = 1f - (timeLeft01 < 0f ? 0f : timeLeft01 > 1f ? 1f : timeLeft01);
        int level = 1 + (int)(burned * maxLevel);
        return level > maxLevel ? maxLevel : level;
    }

    public static int FlamesFor(int level, int baseFlames, int flamesPerLevel)
    {
        int flames = baseFlames + (level - 1) * flamesPerLevel;
        return flames < 1 ? 1 : flames;
    }

    // O fogo cresceu de level para level + 1 com o trabalho em andamento: soma mais uma parte de trabalho.
    // Devolve o progresso novo (0 a 1) e a duração nova, sem mudar o tempo que já foi trabalhado.
    public static void Grow(int level, ref float progress, ref float duration)
    {
        if (level < 1) level = 1;

        progress *= level / (float)(level + 1);
        duration *= (level + 1) / (float)level;
    }
}
