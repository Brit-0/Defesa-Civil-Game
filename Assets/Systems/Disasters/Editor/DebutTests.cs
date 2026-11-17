using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Estreia garantida (docs/jam/RITMO_V3.md): o primeiro sorteio depois de liberar um tipo é esse tipo. EditMode, sem cena.
public class DebutTests
{
    private BalanceData balance;

    [SetUp]
    public void SetUp()
    {
        balance = ScriptableObject.CreateInstance<BalanceData>();
        balance.fireUnlockTime = 90f;
        balance.stormUnlockTime = 170f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(balance);
    }

    [Test]
    public void FreshlyUnlockedTypeDebutsOnce()
    {
        var debuted = new HashSet<DisasterType> { DisasterType.Earthquake };

        Assert.IsFalse(DisasterManager.TryGetDebut(balance, 89f, debuted, out _), "antes de liberar, sorteio normal");

        Assert.IsTrue(DisasterManager.TryGetDebut(balance, 95f, debuted, out DisasterType debut));
        Assert.AreEqual(DisasterType.Fire, debut);

        debuted.Add(DisasterType.Fire);
        Assert.IsFalse(DisasterManager.TryGetDebut(balance, 120f, debuted, out _), "depois da estreia, sorteio normal");

        Assert.IsTrue(DisasterManager.TryGetDebut(balance, 175f, debuted, out debut));
        Assert.AreEqual(DisasterType.Storm, debut);
    }

    [Test]
    public void OldestUnlockDebutsFirstAndZeroWeightNever()
    {
        var debuted = new HashSet<DisasterType> { DisasterType.Earthquake };

        Assert.IsTrue(DisasterManager.TryGetDebut(balance, 200f, debuted, out DisasterType debut));
        Assert.AreEqual(DisasterType.Fire, debut, "os dois liberados sem estrear: vem o que liberou antes");

        balance.fireWeight = 0f;
        Assert.IsTrue(DisasterManager.TryGetDebut(balance, 200f, debuted, out debut));
        Assert.AreEqual(DisasterType.Storm, debut, "peso 0 tira o tipo do jogo, sem estreia");
    }
}
