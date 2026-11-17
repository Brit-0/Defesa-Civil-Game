using NUnit.Framework;

// Casa destelhada pela tempestade (docs/jam/TEMPESTADE.md): a chuva sobe o dano esperando equipe. EditMode, sem cena.
public class StormTests
{
    [Test]
    public void LeakRaisesOneDamagePerIntervalUpToTheMax()
    {
        float timer = 0f;
        int damage = 1;

        damage = StormLeak.Tick(damage, ref timer, 24.9f, 25f, 3);
        Assert.AreEqual(1, damage, "antes do intervalo nada muda");

        damage = StormLeak.Tick(damage, ref timer, .2f, 25f, 3);
        Assert.AreEqual(2, damage);
        Assert.AreEqual(.1f, timer, 1e-4f, "a sobra conta para a próxima subida");

        damage = StormLeak.Tick(damage, ref timer, 60f, 25f, 3);
        Assert.AreEqual(3, damage, "para no máximo");
        Assert.AreEqual(0f, timer);

        damage = StormLeak.Tick(damage, ref timer, 100f, 25f, 3);
        Assert.AreEqual(3, damage);
    }

    [Test]
    public void LeakOffWithZeroIntervalAndNeverLowersDamage()
    {
        float timer = 0f;

        Assert.AreEqual(2, StormLeak.Tick(2, ref timer, 999f, 0f, 3), "intervalo 0 desliga");
        Assert.AreEqual(3, StormLeak.Tick(3, ref timer, 999f, 25f, 2), "dano acima do teto não cai");
        Assert.AreEqual(1, StormLeak.Tick(1, ref timer, -5f, 25f, 3), "tempo negativo não conta");
        Assert.AreEqual(0f, timer);
    }
}
