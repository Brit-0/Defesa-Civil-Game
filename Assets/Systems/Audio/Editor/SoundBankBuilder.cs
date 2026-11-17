using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// Monta o SoundBank da v5 a partir de Assets/Audio/V5 (arquivos <id>_NN.wav/ogg feitos por Tools/Audio/sons_v5.py e
// musica_v5.py), aplica a importação certa (efeitos descomprimidos ao carregar, loops e música comprimidos) e liga os
// clipes antigos aos sons novos. Rodar de novo só troca clipes e importação: os números ajustados no Inspector ficam.
public static class SoundBankBuilder
{
    public const string BankPath = "Assets/Resources/Audio/SoundBank.asset";
    public const string AudioRoot = "Assets/Audio/V5";
    public const string MixerPath = "Assets/Audio/V5/MixerV5.mixer";

    // id, categoria, volume, variação de tom (semitons), vozes, intervalo, loop, abaixa a música, escada (semitons)
    private struct Defaults
    {
        public SoundCategory category;
        public float volume, pitch, cooldown, ladder;
        public int voices;
        public bool loop, ducks;
    }

    private static Defaults D(SoundCategory category, float volume, float pitch, int voices, float cooldown,
                              bool loop = false, bool ducks = false, float ladder = 0f)
    {
        return new Defaults { category = category, volume = volume, pitch = pitch, voices = voices, cooldown = cooldown, loop = loop, ducks = ducks, ladder = ladder };
    }

    private static readonly Dictionary<string, Defaults> Table = new Dictionary<string, Defaults>
    {
        ["ui_click"] = D(SoundCategory.Interface, .8f, .5f, 3, .04f),
        ["ui_tab"] = D(SoundCategory.Interface, .8f, .4f, 2, .06f),
        ["ui_popup"] = D(SoundCategory.Interface, .8f, .6f, 2, .08f),
        ["ui_buy"] = D(SoundCategory.Interface, 1f, .3f, 2, .1f),
        ["ui_denied"] = D(SoundCategory.Interface, .9f, .3f, 1, .2f),
        ["ui_coin_tick"] = D(SoundCategory.Interface, .7f, .15f, 4, .03f, ladder: 1f),
        ["ui_hover"] = D(SoundCategory.Interface, .6f, .8f, 2, .05f),
        ["ui_whoosh"] = D(SoundCategory.Interface, .9f, .4f, 1, .1f),
        ["ui_confetti"] = D(SoundCategory.Gameplay, 1f, .3f, 1, .2f),
        ["call_new_fire"] = D(SoundCategory.Gameplay, .85f, .5f, 2, .15f),
        ["call_new_storm"] = D(SoundCategory.Gameplay, .85f, .5f, 2, .15f),
        ["call_new_quake"] = D(SoundCategory.Gameplay, .85f, .5f, 2, .15f),
        ["call_new_power"] = D(SoundCategory.Gameplay, .85f, .5f, 2, .15f),
        ["call_urgent"] = D(SoundCategory.Gameplay, .8f, 0f, 1, .5f),
        ["team_depart"] = D(SoundCategory.Gameplay, .8f, .8f, 2, .2f),
        ["team_arrive"] = D(SoundCategory.Gameplay, .7f, .8f, 2, .25f),
        ["repair_tool"] = D(SoundCategory.Gameplay, .6f, 1f, 4, .06f),
        ["repair_done"] = D(SoundCategory.Gameplay, .9f, .2f, 2, .15f),
        ["call_collect"] = D(SoundCategory.Gameplay, 1f, .2f, 2, .08f),
        ["fire_ignite"] = D(SoundCategory.Disaster, .85f, .8f, 3, .1f),
        ["fire_jet_start"] = D(SoundCategory.Disaster, .9f, .5f, 2, .1f),
        ["fire_jet_loop"] = D(SoundCategory.Disaster, .8f, 0f, 1, 0f, loop: true),
        ["fire_out"] = D(SoundCategory.Disaster, .9f, .6f, 3, .1f),
        ["fire_loop"] = D(SoundCategory.Ambience, .8f, 0f, 1, 0f, loop: true),
        ["warning_fire"] = D(SoundCategory.Disaster, 1f, 0f, 1, .5f, ducks: true),
        ["warning_storm"] = D(SoundCategory.Disaster, 1f, 0f, 1, .5f, ducks: true),
        ["warning_quake"] = D(SoundCategory.Disaster, 1f, 0f, 1, .5f, ducks: true),
        ["siren"] = D(SoundCategory.Disaster, .8f, 0f, 1, .5f, ducks: true),
        ["rain_light_loop"] = D(SoundCategory.Ambience, 1f, 0f, 1, 0f, loop: true),
        ["rain_heavy_loop"] = D(SoundCategory.Ambience, 1f, 0f, 1, 0f, loop: true),
        ["wind_loop"] = D(SoundCategory.Ambience, 1f, 0f, 1, 0f, loop: true),
        ["thunder_near"] = D(SoundCategory.Disaster, 1f, .8f, 3, .08f, ducks: true),
        ["thunder_far"] = D(SoundCategory.Disaster, .8f, 1.2f, 2, 1.5f),
        ["quake_rumble"] = D(SoundCategory.Disaster, 1f, .3f, 1, .5f, ducks: true),
        ["quake_hit"] = D(SoundCategory.Disaster, .9f, 1.2f, 3, .15f),
        ["rod_absorb"] = D(SoundCategory.Disaster, .9f, .6f, 2, .1f, ducks: true),
        ["build_start"] = D(SoundCategory.Gameplay, .8f, .5f, 2, .2f),
        ["build_done"] = D(SoundCategory.Gameplay, .9f, .4f, 2, .2f),
        ["amb_day"] = D(SoundCategory.Ambience, 1f, 0f, 1, 0f, loop: true),
        ["amb_night"] = D(SoundCategory.Ambience, 1f, 0f, 1, 0f, loop: true),
        ["amb_cricket"] = D(SoundCategory.Ambience, .7f, 1.5f, 3, .3f),
        ["music_layer_0"] = D(SoundCategory.Music, 1f, 0f, 1, 0f, loop: true),
        ["music_layer_1"] = D(SoundCategory.Music, 1f, 0f, 1, 0f, loop: true),
        ["music_layer_2"] = D(SoundCategory.Music, 1f, 0f, 1, 0f, loop: true),
        ["music_layer_3"] = D(SoundCategory.Music, 1f, 0f, 1, 0f, loop: true),
        ["jingle_win"] = D(SoundCategory.Gameplay, 1f, 0f, 1, 1f, ducks: true),
        ["jingle_lose"] = D(SoundCategory.Gameplay, 1f, 0f, 1, 1f, ducks: true),
    };

    // Sons antigos que viram entrada do banco como estão (sem arquivo novo): id -> caminho
    private static readonly Dictionary<string, string> LegacyEntries = new Dictionary<string, string>
    {
        ["ui_hover"] = "Assets/Audio/Sfx_Hover.wav",
        ["ui_whoosh"] = "Assets/Audio/Sfx_Whoosh.wav",
        ["ui_confetti"] = "Assets/Audio/Sfx_Confetti.wav",
        ["siren"] = "Assets/Audio/Sfx_Siren.wav",
        ["fire_loop"] = "Assets/Audio/Amb_Fire.ogg",
    };

    // Clipe antigo -> som novo (quem ainda toca o clipe antigo ganha o novo, com variações e escada)
    private static readonly (string path, string id, float volume)[] LegacyLinks =
    {
        ("Assets/Audio/Sfx_Click.wav", "ui_click", 1f),
        ("Assets/Audio/Sfx_Hover.wav", "ui_hover", 1f),
        ("Assets/Audio/Sfx_Buy.wav", "ui_buy", 1f),
        ("Assets/Audio/Sfx_Error.wav", "ui_denied", 1f),
        ("Assets/Audio/Sfx_Whoosh.wav", "ui_whoosh", 1f),
        ("Assets/Audio/Sfx_Popup.wav", "ui_popup", 1f),
        ("Assets/Audio/Sfx_Dispatch.wav", "team_depart", 1f),
        ("Assets/Audio/Sfx_Hammer.wav", "repair_tool", 1f),
        ("Assets/Audio/Sfx_Repaired.wav", "repair_done", 1f),
        ("Assets/Audio/Sfx_Coin.wav", "ui_coin_tick", 1f),
        ("Assets/Audio/Sfx_Siren.wav", "siren", 1f),
        ("Assets/Audio/Sfx_Earthquake.wav", "quake_rumble", 1f),
        ("Assets/Audio/Sfx_Thunder.wav", "thunder_near", 1f),
        ("Assets/Audio/Sfx_Extinguish.wav", "fire_out", 1f),
        ("Assets/Audio/Sfx_Victory.wav", "jingle_win", 1f),
        ("Assets/Audio/Sfx_Defeat.wav", "jingle_lose", 1f),
        ("Assets/Audio/Sfx_Confetti.wav", "ui_confetti", 1f),
        ("Assets/Systems/LightningRod/Resources/LightningRod/Rod_Zap.wav", "rod_absorb", 1f),
    };

    [MenuItem("Tools/Audio/Montar SoundBank v5")]
    public static void BuildMenu()
    {
        SoundBank bank = Build();
        Debug.Log("SoundBank v5: " + bank.entries.Count + " sons, " + bank.legacy.Count + " clipes antigos ligados");
        Selection.activeObject = bank;
    }

    public static SoundBank Build()
    {
        ApplyImportSettings();

        SoundBank bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
        if (bank == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(BankPath));
            bank = ScriptableObject.CreateInstance<SoundBank>();
            AssetDatabase.CreateAsset(bank, BankPath);
        }

        bank.mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

        Dictionary<string, List<AudioClip>> found = new Dictionary<string, List<AudioClip>>();
        foreach (string path in ClipPaths())
        {
            string id = IdOf(path);
            if (!found.TryGetValue(id, out List<AudioClip> list)) found[id] = list = new List<AudioClip>();
            list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
        }
        foreach (KeyValuePair<string, string> legacy in LegacyEntries)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(legacy.Value);
            if (clip != null && !found.ContainsKey(legacy.Key)) found[legacy.Key] = new List<AudioClip> { clip };
        }

        foreach (KeyValuePair<string, List<AudioClip>> pair in found.OrderBy(p => p.Key))
        {
            SoundEntry entry = bank.entries.Find(e => e.id == pair.Key);
            if (entry == null)
            {
                entry = NewEntry(pair.Key);
                bank.entries.Add(entry);
            }
            entry.clips = pair.Value.Where(c => c != null).ToArray();
        }
        bank.entries.RemoveAll(e => !found.ContainsKey(e.id));
        bank.entries.Sort((a, b) => string.CompareOrdinal(a.id, b.id));

        bank.legacy.Clear();
        foreach ((string path, string id, float volume) in LegacyLinks)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null) bank.legacy.Add(new LegacySound { clip = clip, id = id, volume = volume });
        }

        EditorUtility.SetDirty(bank);
        AssetDatabase.SaveAssets();
        return bank;
    }

    private static SoundEntry NewEntry(string id)
    {
        Defaults d = Table.TryGetValue(id, out Defaults found) ? found : D(SoundCategory.Gameplay, .9f, .5f, 3, .05f);
        bool steady = d.loop || d.category == SoundCategory.Music || d.ducks;
        return new SoundEntry
        {
            id = id, category = d.category, volume = d.volume, pitchJitter = d.pitch, maxVoices = d.voices, cooldown = d.cooldown,
            loop = d.loop, ducks = d.ducks, ladderStep = d.ladder, volumeJitterDb = steady ? 0f : 1.5f
        };
    }

    public static IEnumerable<string> ClipPaths()
    {
        return AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p);
    }

    // music_layer_2_01.ogg -> music_layer_2; thunder_near_03.wav -> thunder_near
    public static string IdOf(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        int cut = name.LastIndexOf('_');
        return cut > 0 && int.TryParse(name.Substring(cut + 1), out _) ? name.Substring(0, cut) : name;
    }

    // Efeito curto: descomprimido ao carregar (toca sem custo de CPU). Loop, ambiente e música: comprimidos na memória.
    public static AudioClipLoadType ExpectedLoadType(string path)
    {
        string id = IdOf(path);
        bool longOne = id.EndsWith("_loop") || id.StartsWith("amb_day") || id.StartsWith("amb_night") || id.StartsWith("music_") || id.StartsWith("jingle_");
        return longOne ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
    }

    public static void ApplyImportSettings()
    {
        foreach (string path in ClipPaths())
        {
            AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            AudioClipLoadType loadType = ExpectedLoadType(path);
            bool longOne = loadType == AudioClipLoadType.CompressedInMemory;
            float quality = longOne ? .5f : .7f;
            if (settings.loadType == loadType && settings.compressionFormat == AudioCompressionFormat.Vorbis
                && Mathf.Approximately(settings.quality, quality) && settings.preloadAudioData && importer.loadInBackground == longOne) continue;

            settings.loadType = loadType;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = quality;
            settings.preloadAudioData = true;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = longOne;
            importer.SaveAndReimport();
        }
    }
}
