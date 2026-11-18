using System.Collections.Generic;
using UnityEngine;

// Ícones do painel de configurações em pixel art desenhados por código (16x16 como os Px_Icon* do kit: creme com
// contorno escuro e sombra lilás), sem arquivo novo. Os testes leem os pixels pelo Paint* (a textura pronta não é legível).
public static class SettingsIcons
{
    public const int Size = 16;

    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    private static readonly Color32 Ink = new Color32(0x14, 0x12, 0x18, 0xFF);
    private static readonly Color32 Cream = new Color32(0xF2, 0xEB, 0xDD, 0xFF);
    private static readonly Color32 Shade = new Color32(0xA8, 0x95, 0xB5, 0xFF);
    private static readonly Color32 Red = new Color32(0xD9, 0x41, 0x2F, 0xFF);
    private static readonly Color32 Gold = new Color32(0xFF, 0xD1, 0x4D, 0xFF);

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // Alto-falante com ondas; mudo troca as ondas por um X vermelho (linhas de cima para baixo)
    private static readonly string[] SpeakerRows =
    {
        "................",
        "................",
        "................",
        "......c.....w...",
        ".....cc..w...w..",
        "....ccc...w..w..",
        ".cccccc...w...w.",
        ".cccccc...w...w.",
        ".cccccc...w...w.",
        ".cccccc...w...w.",
        "....ccc...w..w..",
        ".....cc..w...w..",
        "......c.....w...",
        "................",
        "................",
        "................",
    };

    private static readonly string[] MutedRows =
    {
        "................",
        "................",
        "................",
        "......c.........",
        ".....cc.........",
        "....ccc..r...r..",
        ".cccccc...r.r...",
        ".cccccc....r....",
        ".cccccc...r.r...",
        ".cccccc..r...r..",
        "....ccc.........",
        ".....cc.........",
        "......c.........",
        "................",
        "................",
        "................",
    };

    // Pegador do controle deslizante: 8x12, creme com sombra embaixo e listras de pegar
    private static readonly string[] KnobRows =
    {
        "cccccc",
        "cccccc",
        "cccccc",
        "cssssc",
        "cccccc",
        "cssssc",
        "cccccc",
        "cccccc",
        "llllll",
        "llllll",
    };

    public static Sprite Gear => Cached("Gear", Size, Size, PaintGear);
    public static Sprite Speaker => Cached("Speaker", Size, Size, p => PaintRows(p, Size, Size, SpeakerRows));
    public static Sprite Muted => Cached("Muted", Size, Size, p => PaintRows(p, Size, Size, MutedRows));
    public static Sprite Knob => Cached("Knob", 8, 12, p => PaintRows(p, 8, 12, KnobRows, 1));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => cache.Clear();

    private static Sprite Cached(string key, int width, int height, System.Action<Color32[]> paint)
    {
        if (cache.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;

        Color32[] pixels = new Color32[width * height];
        paint(pixels);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Settings_" + key,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 32f, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        cache[key] = sprite;
        return sprite;
    }

    // Engrenagem: disco com 8 dentes e furo no meio; contorno escuro em volta e sombra lilás embaixo à direita
    public static void PaintGear(Color32[] p)
    {
        const float center = Size / 2f;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx = x + .5f - center, dy = y + .5f - center;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 22.5f, 45f);
                bool tooth = r <= 6.6f && angle > 11f && angle < 34f;
                bool solid = r > 2.2f && (r <= 4.9f || tooth);
                p[y * Size + x] = solid ? Cream : Clear;
            }
        }
        AddShade(p, Size, Size);
        AddOutline(p, Size, Size);
    }

    // c creme, s sombra, l lilás escuro, w onda (creme), r vermelho, g dourado; '.' vazio. margin = borda vazia para o contorno.
    public static void PaintRows(Color32[] p, int width, int height, string[] rows, int margin = 0)
    {
        for (int i = 0; i < p.Length; i++) p[i] = Clear;

        for (int row = 0; row < rows.Length; row++)
        {
            int y = height - 1 - margin - row;
            for (int col = 0; col < rows[row].Length; col++)
            {
                int x = col + margin;
                if (x < 0 || x >= width || y < 0 || y >= height) continue;
                p[y * width + x] = ColorOf(rows[row][col]);
            }
        }
        AddOutline(p, width, height);
    }

    private static Color32 ColorOf(char c)
    {
        switch (c)
        {
            case 'c':
            case 'w': return Cream;
            case 's':
            case 'l': return Shade;
            case 'r': return Red;
            case 'g': return Gold;
            default: return Clear;
        }
    }

    // Pixel creme com vazio embaixo ou à direita vira sombra (volume como nos ícones do kit)
    private static void AddShade(Color32[] p, int width, int height)
    {
        Color32[] copy = (Color32[])p.Clone();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (copy[y * width + x].a == 0) continue;
                bool openBelow = y == 0 || copy[(y - 1) * width + x].a == 0;
                bool openRight = x == width - 1 || copy[y * width + x + 1].a == 0;
                if (openBelow && openRight) p[y * width + x] = Shade;
            }
        }
    }

    // Contorno de 1 pixel em volta do que está aceso (só nos vazios), como no PixelFx
    private static void AddOutline(Color32[] p, int width, int height)
    {
        bool[] solid = new bool[p.Length];
        for (int i = 0; i < p.Length; i++) solid[i] = p[i].a > 0 && !IsInk(p[i]);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (solid[y * width + x]) continue;

                bool near = (x > 0 && solid[y * width + x - 1]) || (x < width - 1 && solid[y * width + x + 1])
                    || (y > 0 && solid[(y - 1) * width + x]) || (y < height - 1 && solid[(y + 1) * width + x]);
                if (near) p[y * width + x] = Ink;
            }
        }
    }

    private static bool IsInk(Color32 c) => c.r == Ink.r && c.g == Ink.g && c.b == Ink.b && c.a == Ink.a;
}
