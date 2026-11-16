using System.Collections.Generic;
using UnityEngine;

// Ícones (Pixel Lab, 64x64) e sons do fogo e da energia, em Assets/Systems/Energy/Resources/FireEnergy.
// Arquivo que falta devolve null: o HUD cai no ícone do kit pixel ou fica sem som, sem erro.
public static class FireEnergyArt
{
    public const string Folder = "FireEnergy/";

    public const string Flame = "fire_flame";
    public const string Bolt = "energy_bolt";
    public const string Off = "energy_off";
    public const string Generator = "upgrade_generator";

    public const string SfxBlackout = "sfx_blackout";
    public const string SfxRestore = "sfx_restore";
    public const string SfxOverload = "sfx_overload";
    public const string SfxUnlock = "sfx_energy_on";
    public const string SfxDenied = "sfx_no_energy";

    private static readonly Dictionary<string, Object> cache = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cache.Clear();
    }

    public static Sprite Load(string key) => Get<Sprite>(key);
    public static AudioClip Clip(string key) => Get<AudioClip>(key);

    private static T Get<T>(string key) where T : Object
    {
        if (cache.TryGetValue(key, out Object found)) return found as T;

        T loaded = Resources.Load<T>(Folder + key);
        cache[key] = loaded;
        return loaded;
    }
}
