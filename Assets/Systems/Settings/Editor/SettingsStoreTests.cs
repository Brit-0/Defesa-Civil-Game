using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// Configurações v6D (docs/jam/CONFIG_V6D.md): a regra pura do GameSettings e do SettingsStore (padrões, limites,
// salvar e carregar, dicas desligadas) num MemoryPrefs, e uma ida e volta nos PlayerPrefs de verdade. EditMode, sem cena.
public class SettingsStoreTests
{
    private static readonly string[] Tips = { "Teste.TutorialDone", "Teste.TipFire", "Teste.FloodTip" };

    private MemoryPrefs memory;

    [SetUp]
    public void SetUp()
    {
        memory = new MemoryPrefs();
        SettingsStore.Prefs = memory;
    }

    [TearDown]
    public void TearDown()
    {
        SettingsStore.Prefs = null;
    }

    [Test]
    public void DefaultsKeepTheV5MixAndEverythingOn()
    {
        GameSettings s = new GameSettings();

        Assert.AreEqual(1f, s.master);
        Assert.AreEqual(1f, s.music);
        Assert.AreEqual(1f, s.sfx);
        Assert.AreEqual(1f, s.ambience);
        Assert.IsFalse(s.masterMuted || s.musicMuted || s.sfxMuted || s.ambienceMuted, "nada começa mudo");
        Assert.IsTrue(s.shake);
        Assert.AreEqual(1f, s.shakeIntensity);
        Assert.IsTrue(s.fullParticles);
        Assert.IsFalse(s.showFps);
        Assert.AreEqual(UiSize.Medium, s.uiSize);
        Assert.IsFalse(s.colorblind);
        Assert.IsTrue(s.vibration);
        Assert.IsTrue(s.tips);
        Assert.AreEqual(0f, GameSettings.ToDecibels(s.master, false), 1e-4f, "volume 1 = 0 dB, o mix da v5D");
    }

    [Test]
    public void FirstRunReadsDefaults()
    {
        Assert.IsTrue(GameSettings.Read(memory).SameAs(new GameSettings()));
        Assert.IsTrue(SettingsStore.Current.SameAs(new GameSettings()));
    }

    [Test]
    public void ClampKeepsValuesInsideTheLimits()
    {
        GameSettings s = new GameSettings { master = 3f, music = -1f, sfx = float.NaN, ambience = .4f, shakeIntensity = 9f, uiSize = (UiSize)7 };
        s.Clamp();

        Assert.AreEqual(1f, s.master);
        Assert.AreEqual(0f, s.music);
        Assert.AreEqual(1f, s.sfx, "NaN volta ao padrão");
        Assert.AreEqual(.4f, s.ambience, 1e-6f);
        Assert.AreEqual(1f, s.shakeIntensity);
        Assert.AreEqual(UiSize.Medium, s.uiSize);
    }

    [Test]
    public void ReadClampsValuesWrittenOutsideTheLimits()
    {
        new GameSettings().Write(memory);
        memory.SetFloat(GameSettings.Prefix + "Music", 7f);
        memory.SetFloat(GameSettings.Prefix + "ShakeIntensity", -2f);
        memory.SetInt(GameSettings.Prefix + "UiSize", -5);

        GameSettings s = GameSettings.Read(memory);

        Assert.AreEqual(1f, s.music);
        Assert.AreEqual(0f, s.shakeIntensity);
        Assert.AreEqual(UiSize.Medium, s.uiSize);
    }

    [Test]
    public void SaveAndLoadRoundTrip()
    {
        GameSettings s = new GameSettings
        {
            master = .7f, music = .25f, sfx = .5f, ambience = 0f, musicMuted = true, ambienceMuted = true,
            fullParticles = false, showFps = true, uiSize = UiSize.Large, colorblind = true,
            shake = false, shakeIntensity = .35f, vibration = false
        };
        s.Write(memory);

        GameSettings loaded = GameSettings.Read(memory);

        Assert.IsTrue(loaded.SameAs(s), "tudo que foi gravado volta igual");
        foreach (string key in GameSettings.Keys) Assert.IsTrue(memory.HasKey(GameSettings.Prefix + key), "falta gravar " + key);
    }

    [Test]
    public void ChangeSavesNotifiesAndSurvivesReload()
    {
        int notified = 0;
        SettingsStore.Changed += _ => notified++;
        try
        {
            SettingsStore.Change(s => { s.music = .3f; s.uiSize = UiSize.Small; });
            Assert.AreEqual(1, notified);
            Assert.AreEqual(1, memory.SaveCount);

            //Arrastando a barra: muda e avisa, grava só no Save
            SettingsStore.Change(s => s.sfx = .6f, false);
            Assert.AreEqual(2, notified);
            Assert.AreEqual(1, memory.SaveCount);
            SettingsStore.Save();
            Assert.AreEqual(2, memory.SaveCount);
            SettingsStore.Save();
            Assert.AreEqual(2, memory.SaveCount, "sem mudança não grava de novo");

            SettingsStore.Load();
            Assert.AreEqual(.3f, SettingsStore.Current.music, 1e-6f);
            Assert.AreEqual(.6f, SettingsStore.Current.sfx, 1e-6f);
            Assert.AreEqual(UiSize.Small, SettingsStore.Current.uiSize);
        }
        finally
        {
            SettingsStore.Prefs = null;
        }
    }

    [Test]
    public void ChangeClampsWhatThePanelSends()
    {
        SettingsStore.Change(s => { s.master = 1.5f; s.shakeIntensity = -.2f; });

        Assert.AreEqual(1f, SettingsStore.Current.master);
        Assert.AreEqual(0f, SettingsStore.Current.shakeIntensity);
    }

    [Test]
    public void RestoreDefaultsBringsEverythingBack()
    {
        SettingsStore.Change(s => { s.master = .1f; s.sfxMuted = true; s.showFps = true; s.colorblind = true; s.uiSize = UiSize.Large; s.shake = false; });
        SettingsStore.RestoreDefaults();

        Assert.IsTrue(SettingsStore.Current.SameAs(new GameSettings()));
        Assert.IsTrue(GameSettings.Read(memory).SameAs(new GameSettings()), "o padrão também fica gravado");
    }

    [Test]
    public void DecibelsMuteAndUiScale()
    {
        Assert.AreEqual(GameSettings.MinDecibels, GameSettings.ToDecibels(1f, true), "mudo = -80 dB");
        Assert.AreEqual(GameSettings.MinDecibels, GameSettings.ToDecibels(0f, false), "zero = -80 dB");
        Assert.AreEqual(-6.02f, GameSettings.ToDecibels(.5f, false), .01f);
        Assert.AreEqual(0f, GameSettings.ToDecibels(2f, false), 1e-4f, "acima de 1 não amplifica");

        Assert.Less(GameSettings.UiScale(UiSize.Small), 1f);
        Assert.AreEqual(1f, GameSettings.UiScale(UiSize.Medium));
        Assert.Greater(GameSettings.UiScale(UiSize.Large), 1f);
        //O painel (800 de altura) cabe na tela de 1080 com o HUD grande
        Assert.LessOrEqual(800f * GameSettings.UiScale(UiSize.Large), 1080f - 64f);

        Assert.AreEqual("35%", GameSettings.Percent(.349f));
        Assert.AreEqual(0f, new GameSettings { shake = false }.ShakeMultiplier);
        Assert.AreEqual(.5f, new GameSettings { shakeIntensity = .5f }.ShakeMultiplier);
    }

    [Test]
    public void TipsOffMarksSeenAndOnGivesBackWhatWasSeen()
    {
        memory.SetInt(Tips[0], 1);
        memory.SetInt(Tips[1], 0);

        TipsSwitch.Disable(memory, Tips);
        foreach (string key in Tips) Assert.AreEqual(1, memory.GetInt(key, 0), key + " fica como visto");
        TipsSwitch.Disable(memory, Tips);

        TipsSwitch.Enable(memory, Tips);
        Assert.AreEqual(1, memory.GetInt(Tips[0], -1), "o que já tinha visto continua visto");
        Assert.AreEqual(0, memory.GetInt(Tips[1], -1));
        Assert.IsFalse(memory.HasKey(Tips[2]), "o que nunca apareceu volta a não existir");
        Assert.IsFalse(TipsSwitch.Disabled(memory));
    }

    [Test]
    public void TipsSnapshotTakesNewKeysWithoutLosingOldOnes()
    {
        TipsSwitch.Disable(memory, new[] { Tips[0] });
        TipsSwitch.Disable(memory, Tips);

        TipsSwitch.Enable(memory, Tips);
        foreach (string key in Tips) Assert.IsFalse(memory.HasKey(key), key + " nunca tinha aparecido");
    }

    [Test]
    public void ReviewTutorialsClearsSeenAndTurnsTipsOn()
    {
        foreach (string key in SettingsStore.TipKeys) memory.SetInt(key, 1);
        SettingsStore.Change(s => s.tips = false);

        SettingsStore.ReviewTutorials();

        Assert.IsTrue(SettingsStore.Current.tips);
        Assert.IsFalse(TipsSwitch.Disabled(memory));
        foreach (string key in SettingsStore.TipKeys) Assert.IsFalse(memory.HasKey(key), key + " volta a aparecer");
        CollectionAssert.Contains(new List<string>(SettingsStore.TipKeys), Tutorial.DoneKey);
    }

    [Test]
    public void IconsArePixelArtWithOutline()
    {
        Color32[] gear = new Color32[SettingsIcons.Size * SettingsIcons.Size];
        SettingsIcons.PaintGear(gear);
        int solid = 0, ink = 0;
        foreach (Color32 c in gear)
        {
            if (c.a == 0) continue;
            Assert.AreEqual(255, c.a, "pixel art: sem meio-transparente");
            solid++;
            if (c.r == 0x14 && c.g == 0x12 && c.b == 0x18) ink++;
        }
        Assert.Greater(solid, 80, "engrenagem enche o ícone");
        Assert.Greater(ink, 30, "contorno escuro como os Px_Icon do kit");
        Assert.AreEqual(0, gear[8 * SettingsIcons.Size + 8].a, "furo no meio");
    }

    [Test]
    public void ColorblindPinsAvoidRedAgainstGreen()
    {
        Color fire = CallPalette.PinFor("Fire", Color.red);
        Color done = CallPalette.PinFor("Done", Color.green);

        Assert.Greater(done.b, done.g, "pronto deixa de ser verde");
        Assert.Greater(fire.r, fire.b);
        Assert.AreEqual(Color.magenta, CallPalette.PinFor("Guard", Color.magenta), "tipo sem cor própria fica igual");
        Assert.AreEqual(Color.white, CallPalette.Pin("Fire", Color.white), "desligado: cor normal");
    }

    //Ida e volta nos PlayerPrefs reais; devolve o que o jogador tinha
    [Test]
    public void PlayerPrefsRoundTrip()
    {
        UnityPrefs real = new UnityPrefs();
        MemoryPrefs backup = new MemoryPrefs();
        foreach (string key in GameSettings.Keys)
        {
            string full = GameSettings.Prefix + key;
            if (PlayerPrefs.HasKey(full)) backup.SetString(full, key == "Master" || key == "Music" || key == "Sfx" || key == "Ambience" || key == "ShakeIntensity"
                ? PlayerPrefs.GetFloat(full).ToString("R", System.Globalization.CultureInfo.InvariantCulture) : PlayerPrefs.GetInt(full).ToString());
        }

        try
        {
            GameSettings s = new GameSettings { music = .42f, uiSize = UiSize.Large, colorblind = true };
            s.Write(real);
            Assert.IsTrue(GameSettings.Read(real).SameAs(s));
        }
        finally
        {
            foreach (string key in GameSettings.Keys)
            {
                string full = GameSettings.Prefix + key;
                if (!backup.HasKey(full)) PlayerPrefs.DeleteKey(full);
                else if (key == "Master" || key == "Music" || key == "Sfx" || key == "Ambience" || key == "ShakeIntensity") PlayerPrefs.SetFloat(full, backup.GetFloat(full, 1f));
                else PlayerPrefs.SetInt(full, backup.GetInt(full, 0));
            }
            PlayerPrefs.Save();
        }
    }
}
