using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// Testes de editor do áudio v5 (SFX_V5.md): as contas da SoundMath, o SoundBank montado, o mixer, a importação dos
// arquivos e as camadas da música. Sem cena: rodam no Test Runner (EditMode).
public class SoundBankTests
{
    private static SoundBank Bank => AssetDatabase.LoadAssetAtPath<SoundBank>(SoundBankBuilder.BankPath);

    [Test]
    public void PickIndex_NeverRepeatsTheLastAndStaysInRange()
    {
        for (int count = 2; count <= 6; count++)
        {
            for (int last = 0; last < count; last++)
            {
                for (float r = 0f; r < 1f; r += .01f)
                {
                    int pick = SoundMath.PickIndex(count, last, r);
                    Assert.That(pick, Is.InRange(0, count - 1));
                    Assert.AreNotEqual(last, pick, $"{count} clipes, último {last}, sorteio {r}");
                }
            }
        }
        Assert.AreEqual(0, SoundMath.PickIndex(1, 0, .7f));
    }

    [Test]
    public void Ladder_ClimbsWhileFastAndResetsWhenSlow()
    {
        Assert.AreEqual(1, SoundMath.NextLadderStep(0, .1f, .6f, 12));
        Assert.AreEqual(12, SoundMath.NextLadderStep(12, .1f, .6f, 12), "para no teto");
        Assert.AreEqual(0, SoundMath.NextLadderStep(5, 1f, .6f, 12), "pausa longa volta ao começo");
        Assert.AreEqual(2f, SoundMath.SemitonesToPitch(12f), 1e-4f);
    }

    [Test]
    public void Pan_And_Offscreen_FollowTheCamera()
    {
        Assert.AreEqual(0f, SoundMath.Pan(5f, 5f, 8f, .55f), 1e-5f);
        Assert.AreEqual(.55f, SoundMath.Pan(50f, 5f, 8f, .55f), 1e-5f, "longe à direita: lado cheio, limitado pela abertura");
        Assert.AreEqual(-.275f, SoundMath.Pan(1f, 5f, 8f, .55f), 1e-5f);
        Assert.AreEqual(1f, SoundMath.OffscreenGain(3f, 2f, Vector2.zero, new Vector2(8f, 4.5f), -7f), 1e-5f, "na tela não cai");
        Assert.AreEqual(SoundMath.DbToLinear(-7f), SoundMath.OffscreenGain(40f, 0f, Vector2.zero, new Vector2(8f, 4.5f), -7f), 1e-5f);
    }

    [Test]
    public void Layers_EnterWithActivityTensionAndDanger()
    {
        float[] t = new float[4];
        SoundMath.LayerTargets(0f, 0f, 0f, t);
        CollectionAssert.AreEqual(new[] { 1f, 0f, 0f, 0f }, t, "calmo: só a base");
        SoundMath.LayerTargets(1f, 0f, 0f, t);
        CollectionAssert.AreEqual(new[] { 1f, 1f, 0f, 0f }, t, "desastre no ar: entra o ritmo");
        SoundMath.LayerTargets(0f, .5f, 0f, t);
        CollectionAssert.AreEqual(new[] { 1f, 1f, .5f, 0f }, t);
        SoundMath.LayerTargets(0f, 1f, 1f, t);
        CollectionAssert.AreEqual(new[] { 1f, 1f, 1f, 1f }, t, "caos no máximo: as quatro");
    }

    [Test]
    public void PauseCutoff_GoesFromOpenToClosedByOctaves()
    {
        Assert.AreEqual(22000f, SoundMath.CutoffAt(0f, 22000f, 800f), .5f);
        Assert.AreEqual(800f, SoundMath.CutoffAt(1f, 22000f, 800f), .5f);
        Assert.AreEqual(Mathf.Sqrt(22000f * 800f), SoundMath.CutoffAt(.5f, 22000f, 800f), 1f, "meio do caminho em oitavas");
    }

    [Test]
    public void Bank_HasEverySoundTheGameUses_WithClips()
    {
        SoundBank bank = Bank;
        Assert.IsNotNull(bank, "rodar Tools > Audio > Montar SoundBank v5");
        Assert.IsNotNull(Resources.Load<SoundBank>(SoundBank.ResourcePath), "o AudioManager carrega pelo Resources");

        foreach (string id in GameAudio.UsedIds)
        {
            SoundEntry entry = bank.Find(id);
            Assert.IsNotNull(entry, "som " + id + " (GameAudio) fora do banco");
        }
        foreach (SoundEntry entry in bank.entries)
        {
            Assert.IsNotEmpty(entry.clips, entry.id);
            Assert.IsFalse(entry.clips.Any(c => c == null), entry.id + " com clipe vazio");
            Assert.That(entry.maxVoices, Is.GreaterThanOrEqualTo(1), entry.id);
        }
        Assert.AreEqual(bank.entries.Count, bank.entries.Select(e => e.id).Distinct().Count(), "nome repetido no banco");
    }

    [Test]
    public void Bank_LegacyClipsPointToRealSounds()
    {
        SoundBank bank = Bank;
        Assert.That(bank.legacy.Count, Is.GreaterThanOrEqualTo(15));
        foreach (LegacySound link in bank.legacy)
        {
            Assert.IsNotNull(link.clip);
            Assert.IsNotNull(bank.Find(link.id), link.clip.name + " -> " + link.id);
            Assert.IsTrue(bank.TryLegacy(link.clip, out _), link.clip.name);
        }
        Assert.IsTrue(bank.legacy.Exists(l => l.clip.name == "Sfx_Coin" && bank.Find(l.id).ladderStep > 0f), "moedas com escada de tom");
        Assert.IsTrue(bank.legacy.Exists(l => l.clip.name == "Rod_Zap" && l.id == "rod_absorb"), "raio absorvido do para-raio");
    }

    [Test]
    public void Bank_VariationsAndLoopsAreSet()
    {
        SoundBank bank = Bank;
        foreach (string id in new[] { "ui_click", "ui_tab", "repair_tool", "team_depart", "thunder_near", "quake_hit", "repair_done" })
        {
            Assert.That(bank.Find(id).clips.Length, Is.GreaterThanOrEqualTo(2), id + " precisa de variações");
        }
        foreach (string id in new[] { "fire_jet_loop", "rain_light_loop", "rain_heavy_loop", "wind_loop", "amb_day", "amb_night", "fire_loop" })
        {
            Assert.IsTrue(bank.Find(id).loop, id + " é loop");
            Assert.AreEqual(SoundCategory.Ambience == bank.Find(id).category || id == "fire_jet_loop", true, id);
        }
        Assert.IsTrue(bank.Find("thunder_near").ducks && bank.Find("quake_rumble").ducks && bank.Find("jingle_win").ducks, "sons grandes abaixam a música");
    }

    [Test]
    public void Mixer_HasTheFourGroupsAndExposedParameters()
    {
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(SoundBankBuilder.MixerPath);
        Assert.IsNotNull(mixer);
        Assert.AreSame(mixer, Bank.mixer, "o banco leva o mixer para o build");
        foreach (string group in new[] { "Master", "Musica", "SFX", "Ambiente", "UI" })
        {
            Assert.IsNotEmpty(mixer.FindMatchingGroups(group), "grupo " + group);
        }

        SerializedProperty exposed = new SerializedObject(mixer).FindProperty("m_ExposedParameters");
        HashSet<string> names = new HashSet<string>();
        for (int i = 0; i < exposed.arraySize; i++) names.Add(exposed.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue);
        foreach (string parameter in new[] { "MasterVolume", "MusicVolume", "SfxVolume", "AmbienceVolume", "UiVolume", "MusicCutoff" })
        {
            Assert.IsTrue(names.Contains(parameter), "parâmetro exposto " + parameter);
        }
    }

    [Test]
    public void Import_ShortSoundsDecompressAndLongOnesStayCompressed()
    {
        int count = 0;
        foreach (string path in SoundBankBuilder.ClipPaths())
        {
            AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            Assert.AreEqual(SoundBankBuilder.ExpectedLoadType(path), settings.loadType, path);
            Assert.AreEqual(AudioCompressionFormat.Vorbis, settings.compressionFormat, path);
            count++;
        }
        Assert.That(count, Is.GreaterThanOrEqualTo(70));
    }

    [Test]
    public void Music_LayersHaveTheSameLengthToLoopTogether()
    {
        int[] samples = Enumerable.Range(0, 4).Select(i => Bank.Find("music_layer_" + i).clips[0].samples).ToArray();
        Assert.AreEqual(1, samples.Distinct().Count(), "camadas com tamanhos " + string.Join(", ", samples));
        Assert.AreEqual(38.4f, Bank.Find("music_layer_0").clips[0].length, .01f, "16 compassos a 100 bpm");
    }

    [Test]
    public void AudioManagerPrefab_NoLongerCarriesTheOldMusic()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Systems/Audio/AudioManager.prefab");
        SerializedObject manager = new SerializedObject(prefab.GetComponent<AudioManager>());
        foreach (string field in new[] { "calmMusic", "tenseMusic", "dangerMusic" })
        {
            Assert.IsNull(manager.FindProperty(field).objectReferenceValue, field + " pesaria no build do WebGL");
        }
    }
}
