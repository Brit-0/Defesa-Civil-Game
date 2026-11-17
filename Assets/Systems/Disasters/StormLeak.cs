using UnityEngine;

// Casa quebrada por raio fica destelhada (docs/jam/TEMPESTADE.md): esperando equipe, a chuva entra e o dano sobe 1 a
// cada "interval" segundos, até "maxDamage". Como o fogo que cresce, é o motivo para atender primeiro o pino azul.
// Regra pura (sem cena) para o teste de editor; a House chama a cada quadro.
public static class StormLeak
{
    // Dano depois de mais "delta" segundos de espera. "timer" guarda a espera desde a última subida.
    public static int Tick(int damage, ref float timer, float delta, float interval, int maxDamage)
    {
        if (interval <= 0f || damage >= maxDamage)
        {
            timer = 0f;
            return damage;
        }

        timer += Mathf.Max(0f, delta);
        while (timer >= interval && damage < maxDamage)
        {
            timer -= interval;
            damage++;
        }
        if (damage >= maxDamage) timer = 0f;
        return damage;
    }
}
