using System;
using System.Collections.Generic;
using UnityEngine;

// Configurações vivas do jogo (v6D): lidas dos PlayerPrefs na primeira vez, mudadas pelo painel e gravadas.
// Quem reage (mixer, tremor, FPS, HUD) escuta o Changed ou lê o Current na hora; a regra pura fica no GameSettings.
public static class SettingsStore
{
    public static event Action<GameSettings> Changed;

    private static GameSettings current;
    private static ISettingsPrefs prefs;
    private static bool dirty;

    public static GameSettings Current
    {
        get
        {
            if (current == null) Load();
            return current;
        }
    }

    // PlayerPrefs no jogo; os testes trocam por um MemoryPrefs (e devolvem null no fim)
    public static ISettingsPrefs Prefs
    {
        get => prefs ??= new UnityPrefs();
        set
        {
            prefs = value;
            current = null;
            dirty = false;
        }
    }

    // As chaves de "já visto" de todo tutorial e dica de contexto do jogo
    public static IReadOnlyList<string> TipKeys
    {
        get
        {
            List<string> keys = new List<string> { Tutorial.DoneKey, Tutorial.FireTipKey, Tutorial.StrongTipKey, EnergyTutorial.DoneKey };
            keys.AddRange(BlockDisasterTips.Keys);
            return keys;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Changed = null;
        current = null;
        prefs = null;
        dirty = false;
    }

    public static void Load()
    {
        current = GameSettings.Read(Prefs);
        //Dicas desligadas numa versão com dica nova: a nova também fica marcada como vista
        if (!current.tips) TipsSwitch.Disable(Prefs, TipKeys);
    }

    // Muda e avisa na hora. save false = arrastando um controle (grava no Save, ao soltar ou ao fechar o painel):
    // no WebGL cada PlayerPrefs.Save escreve no IndexedDB do navegador.
    public static void Change(Action<GameSettings> edit, bool save = true)
    {
        GameSettings s = Current;
        bool tipsBefore = s.tips;
        edit?.Invoke(s);
        s.Clamp();

        if (tipsBefore != s.tips)
        {
            if (s.tips) TipsSwitch.Enable(Prefs, TipKeys);
            else TipsSwitch.Disable(Prefs, TipKeys);
        }

        s.Write(Prefs);
        dirty = true;
        if (save) Save();
        Changed?.Invoke(s);
    }

    public static void Save()
    {
        if (!dirty) return;

        dirty = false;
        Prefs.Save();
    }

    // Volta tudo ao padrão (as dicas religam, com o "já visto" de antes)
    public static void RestoreDefaults()
    {
        GameSettings defaults = new GameSettings();
        Change(s =>
        {
            s.master = defaults.master;
            s.music = defaults.music;
            s.sfx = defaults.sfx;
            s.ambience = defaults.ambience;
            s.masterMuted = defaults.masterMuted;
            s.musicMuted = defaults.musicMuted;
            s.sfxMuted = defaults.sfxMuted;
            s.ambienceMuted = defaults.ambienceMuted;
            s.fullParticles = defaults.fullParticles;
            s.showFps = defaults.showFps;
            s.uiSize = defaults.uiSize;
            s.colorblind = defaults.colorblind;
            s.shake = defaults.shake;
            s.shakeIntensity = defaults.shakeIntensity;
            s.vibration = defaults.vibration;
            s.tips = defaults.tips;
        });
    }

    // Rever tutoriais: apaga todo "já visto" e liga as dicas
    public static void ReviewTutorials()
    {
        TipsSwitch.ResetAll(Prefs, TipKeys);
        if (Current.tips)
        {
            Prefs.Save();
            Changed?.Invoke(Current);
            return;
        }

        //Religar sem devolver o retrato: o ResetAll já apagou o que ele devolveria
        Change(s => s.tips = true);
    }
}

public class UnityPrefs : ISettingsPrefs
{
    public bool HasKey(string key) => PlayerPrefs.HasKey(key);
    public float GetFloat(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);
    public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);
    public string GetString(string key, string fallback) => PlayerPrefs.GetString(key, fallback);
    public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
    public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
    public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
    public void Save() => PlayerPrefs.Save();
}
