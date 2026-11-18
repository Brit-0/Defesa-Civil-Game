using System;
using System.Collections.Generic;
using System.Globalization;

public enum UiSize { Small, Medium, Large }

// Configurações do jogador (v6D, docs/jam/CONFIG_V6D.md): padrões, limites e a gravação chave a chave.
// Regra pura (sem UnityEngine): o SettingsStore grava nos PlayerPrefs e os testes EditMode num dicionário, com a mesma conta.
[Serializable]
public class GameSettings
{
    public const string Prefix = "DefesaCivil.Settings.";
    public const int FormatVersion = 1;
    public const float MinDecibels = -80f;

    [Serializable]
    public struct Range
    {
        public float min, max;
        public Range(float min, float max) { this.min = min; this.max = max; }
        public float Clamp(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
    }

    public static readonly Range VolumeRange = new Range(0f, 1f);
    public static readonly Range ShakeRange = new Range(0f, 1f);

    // SOM: 1 = o mix da v5D (0 dB no grupo do mixer)
    public float master = 1f;
    public float music = 1f;
    public float sfx = 1f;
    public float ambience = 1f;
    public bool masterMuted;
    public bool musicMuted;
    public bool sfxMuted;
    public bool ambienceMuted;

    // TELA
    public bool fullParticles = true;
    public bool showFps;
    public UiSize uiSize = UiSize.Medium;
    public bool colorblind;

    // JOGO
    public bool shake = true;
    public float shakeIntensity = 1f;
    public bool vibration = true;
    public bool tips = true;

    public GameSettings Clone() => (GameSettings)MemberwiseClone();

    // Valor fora do limite (prefs mexidos à mão, versão antiga) volta para dentro; NaN volta ao padrão
    public void Clamp()
    {
        GameSettings defaults = new GameSettings();
        master = VolumeRange.Clamp(master, defaults.master);
        music = VolumeRange.Clamp(music, defaults.music);
        sfx = VolumeRange.Clamp(sfx, defaults.sfx);
        ambience = VolumeRange.Clamp(ambience, defaults.ambience);
        shakeIntensity = ShakeRange.Clamp(shakeIntensity, defaults.shakeIntensity);
        if (!Enum.IsDefined(typeof(UiSize), uiSize)) uiSize = defaults.uiSize;
    }

    public bool SameAs(GameSettings other)
    {
        if (other == null) return false;
        Dictionary<string, string> a = Snapshot(), b = other.Snapshot();
        foreach (KeyValuePair<string, string> pair in a)
        {
            if (!b.TryGetValue(pair.Key, out string value) || value != pair.Value) return false;
        }
        return true;
    }

    #region SAVE AND LOAD

    // Sem a chave de versão (primeira partida) tudo fica no padrão
    public static GameSettings Read(ISettingsPrefs prefs)
    {
        GameSettings s = new GameSettings();
        if (prefs == null || !prefs.HasKey(Prefix + "Version")) return s;

        s.master = prefs.GetFloat(Prefix + "Master", s.master);
        s.music = prefs.GetFloat(Prefix + "Music", s.music);
        s.sfx = prefs.GetFloat(Prefix + "Sfx", s.sfx);
        s.ambience = prefs.GetFloat(Prefix + "Ambience", s.ambience);
        s.masterMuted = ReadBool(prefs, "MasterMuted", s.masterMuted);
        s.musicMuted = ReadBool(prefs, "MusicMuted", s.musicMuted);
        s.sfxMuted = ReadBool(prefs, "SfxMuted", s.sfxMuted);
        s.ambienceMuted = ReadBool(prefs, "AmbienceMuted", s.ambienceMuted);
        s.fullParticles = ReadBool(prefs, "FullParticles", s.fullParticles);
        s.showFps = ReadBool(prefs, "ShowFps", s.showFps);
        s.uiSize = (UiSize)prefs.GetInt(Prefix + "UiSize", (int)s.uiSize);
        s.colorblind = ReadBool(prefs, "Colorblind", s.colorblind);
        s.shake = ReadBool(prefs, "Shake", s.shake);
        s.shakeIntensity = prefs.GetFloat(Prefix + "ShakeIntensity", s.shakeIntensity);
        s.vibration = ReadBool(prefs, "Vibration", s.vibration);
        s.tips = ReadBool(prefs, "Tips", s.tips);
        s.Clamp();
        return s;
    }

    public void Write(ISettingsPrefs prefs)
    {
        if (prefs == null) return;

        prefs.SetInt(Prefix + "Version", FormatVersion);
        prefs.SetFloat(Prefix + "Master", master);
        prefs.SetFloat(Prefix + "Music", music);
        prefs.SetFloat(Prefix + "Sfx", sfx);
        prefs.SetFloat(Prefix + "Ambience", ambience);
        WriteBool(prefs, "MasterMuted", masterMuted);
        WriteBool(prefs, "MusicMuted", musicMuted);
        WriteBool(prefs, "SfxMuted", sfxMuted);
        WriteBool(prefs, "AmbienceMuted", ambienceMuted);
        WriteBool(prefs, "FullParticles", fullParticles);
        WriteBool(prefs, "ShowFps", showFps);
        prefs.SetInt(Prefix + "UiSize", (int)uiSize);
        WriteBool(prefs, "Colorblind", colorblind);
        WriteBool(prefs, "Shake", shake);
        prefs.SetFloat(Prefix + "ShakeIntensity", shakeIntensity);
        WriteBool(prefs, "Vibration", vibration);
        WriteBool(prefs, "Tips", tips);
    }

    // Todas as chaves que o Write grava (Restaurar padrões e os testes apagam por esta lista)
    public static readonly string[] Keys =
    {
        "Version", "Master", "Music", "Sfx", "Ambience", "MasterMuted", "MusicMuted", "SfxMuted", "AmbienceMuted",
        "FullParticles", "ShowFps", "UiSize", "Colorblind", "Shake", "ShakeIntensity", "Vibration", "Tips"
    };

    private static bool ReadBool(ISettingsPrefs prefs, string key, bool fallback) => prefs.GetInt(Prefix + key, fallback ? 1 : 0) != 0;
    private static void WriteBool(ISettingsPrefs prefs, string key, bool value) => prefs.SetInt(Prefix + key, value ? 1 : 0);

    private Dictionary<string, string> Snapshot()
    {
        MemoryPrefs memory = new MemoryPrefs();
        Write(memory);
        return memory.Dump();
    }

    #endregion

    #region RULES

    // Volume do grupo no mixer: 20·log10 do controle (1 = 0 dB); mudo ou zero = -80 dB
    public static float ToDecibels(float linear, bool muted)
    {
        if (muted || float.IsNaN(linear) || linear <= .0001f) return MinDecibels;
        return Math.Max(MinDecibels, 20f * (float)Math.Log10(Math.Min(1f, linear)));
    }

    // Escala do HUD por tamanho: a resolução de referência do CanvasScaler é dividida por ela
    public static float UiScale(UiSize size)
    {
        switch (size)
        {
            case UiSize.Small: return .85f;
            case UiSize.Large: return 1.15f;
            default: return 1f;
        }
    }

    // Quanto do tremor de tela passa (0 = desligado)
    public float ShakeMultiplier => shake ? shakeIntensity : 0f;

    public static string Percent(float linear) => Math.Round(Math.Max(0f, linear) * 100f).ToString(CultureInfo.InvariantCulture) + "%";

    #endregion
}

// Onde as configurações moram: PlayerPrefs no jogo (UnityPrefs), dicionário nos testes (MemoryPrefs)
public interface ISettingsPrefs
{
    bool HasKey(string key);
    float GetFloat(string key, float fallback);
    int GetInt(string key, int fallback);
    string GetString(string key, string fallback);
    void SetFloat(string key, float value);
    void SetInt(string key, int value);
    void SetString(string key, string value);
    void DeleteKey(string key);
    void Save();
}

public class MemoryPrefs : ISettingsPrefs
{
    private readonly Dictionary<string, string> values = new Dictionary<string, string>();

    public int SaveCount { get; private set; }

    public bool HasKey(string key) => values.ContainsKey(key);
    public float GetFloat(string key, float fallback) => values.TryGetValue(key, out string v) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : fallback;
    public int GetInt(string key, int fallback) => values.TryGetValue(key, out string v) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? i : fallback;
    public string GetString(string key, string fallback) => values.TryGetValue(key, out string v) ? v : fallback;
    public void SetFloat(string key, float value) => values[key] = value.ToString("R", CultureInfo.InvariantCulture);
    public void SetInt(string key, int value) => values[key] = value.ToString(CultureInfo.InvariantCulture);
    public void SetString(string key, string value) => values[key] = value ?? "";
    public void DeleteKey(string key) => values.Remove(key);
    public void Save() => SaveCount++;

    public Dictionary<string, string> Dump() => new Dictionary<string, string>(values);
}

// Dicas e tutoriais desligados: cada "já visto" é guardado e marcado como visto (os tutoriais pulam sozinhos, sem
// mexer neles); religar devolve o que era. Rever apaga tudo: as dicas voltam do começo.
public static class TipsSwitch
{
    public const string SnapshotKey = GameSettings.Prefix + "TipsSnapshot";
    private const string Missing = "-";

    public static bool Disabled(ISettingsPrefs prefs) => prefs.HasKey(SnapshotKey);

    // Pode rodar de novo: chave nova (de uma versão mais nova) entra no retrato sem perder as que já estavam nele
    public static void Disable(ISettingsPrefs prefs, IReadOnlyList<string> keys)
    {
        Dictionary<string, string> saved = Parse(prefs.GetString(SnapshotKey, ""));
        foreach (string key in keys)
        {
            if (!saved.ContainsKey(key)) saved[key] = prefs.HasKey(key) ? prefs.GetInt(key, 0).ToString(CultureInfo.InvariantCulture) : Missing;
            prefs.SetInt(key, 1);
        }
        prefs.SetString(SnapshotKey, Join(saved));
    }

    public static void Enable(ISettingsPrefs prefs, IReadOnlyList<string> keys)
    {
        if (!prefs.HasKey(SnapshotKey)) return;

        Dictionary<string, string> saved = Parse(prefs.GetString(SnapshotKey, ""));
        foreach (string key in keys)
        {
            if (saved.TryGetValue(key, out string value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seen)) prefs.SetInt(key, seen);
            else prefs.DeleteKey(key);
        }
        prefs.DeleteKey(SnapshotKey);
    }

    public static void ResetAll(ISettingsPrefs prefs, IReadOnlyList<string> keys)
    {
        foreach (string key in keys) prefs.DeleteKey(key);
        prefs.DeleteKey(SnapshotKey);
    }

    private static Dictionary<string, string> Parse(string text)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(text)) return result;

        foreach (string part in text.Split(';'))
        {
            int cut = part.LastIndexOf('=');
            if (cut > 0) result[part.Substring(0, cut)] = part.Substring(cut + 1);
        }
        return result;
    }

    private static string Join(Dictionary<string, string> values)
    {
        List<string> parts = new List<string>();
        foreach (KeyValuePair<string, string> pair in values) parts.Add(pair.Key + "=" + pair.Value);
        return string.Join(";", parts);
    }
}
