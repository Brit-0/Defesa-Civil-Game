using NUnit.Framework;
using UnityEngine;

// Regras do quadro de força, o minigame do RELIGAR (docs/jam/ENERGIA_DIA_NOITE.md). EditMode, sem cena.
public class FuseBoxTests
{
    [Test]
    public void NumbersAreShuffledButNeverLeftToRight()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            int[] numbers = FuseBox.Shuffled(4, new System.Random(seed));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, numbers, "uma chave para cada número");
            CollectionAssert.AreNotEqual(new[] { 1, 2, 3, 4 }, numbers, "em fila seria só clicar em sequência");
        }
        CollectionAssert.AreEqual(new[] { 1 }, FuseBox.Shuffled(1, new System.Random(1)));
    }

    [Test]
    public void PressingInOrderRestores()
    {
        FuseBox fuse = new FuseBox(4, .6f, new System.Random(7));

        for (int number = 1; number <= 4; number++)
        {
            Assert.AreEqual(fuse.IndexOf(number), fuse.NextIndex, "a seta do tutorial vai na próxima chave");
            FusePress result = fuse.Press(fuse.IndexOf(number));
            Assert.AreEqual(number < 4 ? FusePress.Correct : FusePress.Done, result);
            Assert.IsTrue(fuse.IsOn(fuse.IndexOf(number)));
        }

        Assert.IsTrue(fuse.Done);
        Assert.AreEqual(-1, fuse.NextIndex);
        Assert.AreEqual(0, fuse.Mistakes);
        Assert.AreEqual(FusePress.Ignored, fuse.Press(0), "quadro pronto não responde mais");
    }

    [Test]
    public void WrongSwitchDropsAllAndLocksBriefly()
    {
        FuseBox fuse = new FuseBox(4, .6f, new System.Random(3));
        Assert.AreEqual(FusePress.Correct, fuse.Press(fuse.IndexOf(1)));

        Assert.AreEqual(FusePress.Wrong, fuse.Press(fuse.IndexOf(3)));
        Assert.AreEqual(0, fuse.Lit, "as que subiram descem");
        for (int i = 0; i < fuse.Count; i++) Assert.IsFalse(fuse.IsOn(i));
        Assert.AreEqual(1, fuse.Mistakes);
        Assert.IsTrue(fuse.Locked);
        Assert.AreEqual(FusePress.Ignored, fuse.Press(fuse.IndexOf(1)), "travado não aceita clique");

        fuse.Tick(.3f);
        Assert.IsTrue(fuse.Locked);
        fuse.Tick(.31f);
        Assert.IsFalse(fuse.Locked, "falha leve: menos de um segundo");
        Assert.AreEqual(FusePress.Correct, fuse.Press(fuse.IndexOf(1)), "recomeça do 1");
    }

    [Test]
    public void SameSwitchTwiceIsIgnored()
    {
        FuseBox fuse = new FuseBox(3, .5f, new System.Random(11));
        int first = fuse.IndexOf(1);

        Assert.AreEqual(FusePress.Correct, fuse.Press(first));
        Assert.AreEqual(FusePress.Ignored, fuse.Press(first), "chave já ligada não conta como erro");
        Assert.AreEqual(0, fuse.Mistakes);
        Assert.AreEqual(FusePress.Ignored, fuse.Press(-1));
        Assert.AreEqual(FusePress.Ignored, fuse.Press(3));
    }

    [Test]
    public void AtLeastOneSwitch()
    {
        FuseBox fuse = new FuseBox(0, -1f, null);

        Assert.AreEqual(1, fuse.Count);
        Assert.AreEqual(FusePress.Done, fuse.Press(0));
    }

    [Test]
    public void DataAssetIsShortAndForgiving()
    {
        FuseBoxData data = Resources.Load<FuseBoxData>(FuseBoxData.ResourcePath);

        Assert.IsNotNull(data, "rode Tools/Jam/Create Fire And Energy Data");
        Assert.That(data.switches, Is.InRange(3, 5), "poucos segundos");
        Assert.That(data.wrongLock, Is.InRange(.2f, 1f), "falha leve");
        Assert.That(data.restoreDelay, Is.InRange(0f, .8f));
    }
}
