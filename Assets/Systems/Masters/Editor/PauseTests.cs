using NUnit.Framework;
using UnityEngine;

// Testes de editor da v4A (#535): a pausa (Esc ou P) congela o relógio das animações e o som e devolve a câmera lenta
// do tutorial; a aba do Armazém mostra o material que a equipe escolhida traz; o aro do alvo do terremoto cai no passo
// de 32 do sprite (Simple, sem quadricular). Sem cena: GameManager criado à mão, sem Awake.
public class PauseTests
{
    private GameObject managerObject;

    [TearDown]
    public void TearDown()
    {
        if (managerObject != null) Object.DestroyImmediate(managerObject);
        GameClock.Reset();
        Time.timeScale = 1f;
    }

    [Test]
    public void GameClock_StopsWhileFrozenAndKeepsGoingAfter()
    {
        GameClock.Reset();
        Assert.IsFalse(GameClock.Frozen);

        GameClock.Freeze();
        float frozenAt = GameClock.Time;
        Assert.IsTrue(GameClock.Frozen);
        Assert.AreEqual(0f, GameClock.DeltaTime);
        Assert.IsTrue(AudioListener.pause, "os sons do jogo param na pausa");
        Assert.AreEqual(frozenAt, GameClock.Time, 1e-6f);

        GameClock.Thaw();
        Assert.IsFalse(GameClock.Frozen);
        Assert.IsFalse(AudioListener.pause);
        Assert.GreaterOrEqual(GameClock.Time, frozenAt);
        Assert.LessOrEqual(GameClock.Time, Time.unscaledTime + 1e-4f);
    }

    [Test]
    public void PauseAndResume_KeepTheTutorialSlowMotion()
    {
        managerObject = new GameObject("GameManagerTest");
        GameManager game = managerObject.AddComponent<GameManager>();
        game.StartGame();
        Assert.AreEqual(GameState.Playing, game.State);

        Time.timeScale = .3f;
        game.Pause();
        Assert.AreEqual(GameState.Paused, game.State);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(GameClock.Frozen);

        game.Resume();
        Assert.AreEqual(GameState.Playing, game.State);
        Assert.AreEqual(.3f, Time.timeScale, 1e-4f);
        Assert.IsFalse(GameClock.Frozen);
        Assert.IsFalse(AudioListener.pause);
    }

    [Test]
    public void SlowMotionChangedWhilePaused_IsWhatResumeGives()
    {
        managerObject = new GameObject("GameManagerTest");
        GameManager game = managerObject.AddComponent<GameManager>();
        game.StartGame();

        game.SetResumeTimeScale(.3f);
        Assert.AreEqual(1f, Time.timeScale, 1e-4f, "fora da pausa não mexe");

        game.Pause();
        game.SetResumeTimeScale(.3f);
        Assert.AreEqual(0f, Time.timeScale, "a pausa segue parada");
        game.Resume();
        Assert.AreEqual(.3f, Time.timeScale, 1e-4f);
    }

    [Test]
    public void RestockAmount_ForATeamIsWhatTheTripBrings()
    {
        int perWorker = Stats.MaterialPerWorker;
        Assert.AreEqual(0, Stats.GetRestockAmount(0));
        Assert.AreEqual(3 * perWorker, Stats.GetRestockAmount(3));
        Assert.Greater(Stats.GetRestockAmount(4), Stats.GetRestockAmount(3));

        // A ida de verdade (sem o acerto no ponto certo) traz o mesmo que a aba mostrou
        Call call = new Call { type = CallType.Restock, workersSent = 3 };
        Assert.AreEqual(Stats.GetRestockAmount(3), Stats.GetRestockAmount(call));
    }

    [Test]
    public void ShoreTargetRing_IsSquareInTheRingStep()
    {
        for (float w = 0f; w < 400f; w += 9f)
        {
            float side = MiniGameFeel.RingSize(new Vector2(w, w * .7f), 110f);
            Assert.AreEqual(0f, side % FeelUI.RingStep, 1e-4f, "lado " + side + " fora do passo do aro");
            Assert.That(side, Is.InRange(FeelUI.RingMin, FeelUI.RingMax));
        }
    }
}
