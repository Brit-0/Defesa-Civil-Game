using System.Collections.Generic;
using UnityEngine;

// Modo daltônico (v6D): cores dos pinos de chamado na paleta Okabe-Ito (separa vermelho e verde para protanopia e
// deuteranopia) e o selo de "pronto" azul no lugar do verde. Quem desenha pino ou ícone passa a cor e o sprite por aqui.
public static class CallPalette
{
    // Sprites recoloridos em Resources/SettingsColorblind, com o mesmo nome do original
    public const string ResourceFolder = "SettingsColorblind/";

    private static readonly Dictionary<string, Color> Pins = new Dictionary<string, Color>
    {
        { "Fire", new Color32(0xD5, 0x5E, 0x00, 0xFF) },
        { "Damage", new Color32(0xE6, 0x9F, 0x00, 0xFF) },
        { "Storm", new Color32(0x00, 0x72, 0xB2, 0xFF) },
        { "Flood", new Color32(0x00, 0x72, 0xB2, 0xFF) },
        { "Power", new Color32(0xF0, 0xE4, 0x42, 0xFF) },
        { "Done", new Color32(0x56, 0xB4, 0xE9, 0xFF) },
    };

    private static readonly Dictionary<string, Sprite> alternates = new Dictionary<string, Sprite>();

    public static bool Active => SettingsStore.Current.colorblind;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => alternates.Clear();

    // kind = nome do tipo do pino (Fire, Damage, Storm, Flood, Power, Done); tipo sem cor própria fica com a normal
    public static Color Pin(string kind, Color normal) => Active ? PinFor(kind, normal) : normal;

    public static Color PinFor(string kind, Color normal) => kind != null && Pins.TryGetValue(kind, out Color color) ? color : normal;

    public static Sprite Icon(Sprite normal)
    {
        if (normal == null || !Active) return normal;

        if (!alternates.TryGetValue(normal.name, out Sprite alternate))
        {
            alternate = Resources.Load<Sprite>(ResourceFolder + normal.name);
            alternates[normal.name] = alternate;
        }
        return alternate != null ? alternate : normal;
    }
}
