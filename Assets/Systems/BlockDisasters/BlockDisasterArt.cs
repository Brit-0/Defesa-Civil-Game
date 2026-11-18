using System.Collections.Generic;
using UnityEngine;

// Pixel art da enchente e do meteoro desenhada por código (Point, sem import), como os sprites do PixelFx:
// água em ladrilho (anda pelo uvRect), espuma, válvula do bueiro, cratera do tamanho do quarteirão, sombra, meteoro e
// pedras. Os ícones dos chamados são PNGs do Pixel Lab ligados no BlockDisasterData.
public static class BlockDisasterArt
{
    public static readonly Color32 Ink = new(0x14, 0x12, 0x18, 255);
    public static readonly Color32 WaterDeep = new(0x24, 0x44, 0x8A, 255);
    public static readonly Color32 Water = new(0x3F, 0x7F, 0xE0, 255);
    public static readonly Color32 WaterLight = new(0x8C, 0xC4, 0xF5, 255);
    public static readonly Color32 Foam = new(0xF2, 0xEB, 0xDD, 255);
    private static readonly Color32 Clear = new(0, 0, 0, 0);

    private static readonly Dictionary<string, Texture2D> textures = new();
    private static readonly Dictionary<string, Sprite> sprites = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        textures.Clear();
        sprites.Clear();
    }

    public static Sprite CallFlood => BlockDisasterData.Instance.callFloodIcon;
    public static Sprite CallRebuild => BlockDisasterData.Instance.callRebuildIcon;

    #region ÁGUA

    // Ladrilho 16x16 da água: azul com cristas claras em fileiras desencontradas. Repeat: a RawImage anda o uvRect.
    public static Texture2D WaterTile => Tile("Water", 16, 16, (p, w, h) =>
    {
        for (int i = 0; i < p.Length; i++) p[i] = Water;
        for (int x = 0; x < w; x++)
        {
            // duas cristas por ladrilho, defasadas, com o fundo da onda mais escuro logo abaixo
            int a = 3 + Mathf.RoundToInt(Mathf.Sin(x / (float)w * Mathf.PI * 2f) * 1.4f);
            int b = 11 + Mathf.RoundToInt(Mathf.Sin((x + 8) / (float)w * Mathf.PI * 2f) * 1.4f);
            if (x % 8 < 5) p[a * w + x] = WaterLight;
            if ((x + 4) % 8 < 4) p[b * w + x] = WaterLight;
            p[((a + 15) % h) * w + x] = WaterDeep;
        }
    });

    // Espuma da frente da água (a linha que sobe nas casas): 16x3, crista creme e uma fileira clara.
    public static Texture2D FoamTile => Tile("Foam", 16, 3, (p, w, h) =>
    {
        for (int x = 0; x < w; x++)
        {
            p[0 * w + x] = Water;
            p[1 * w + x] = x % 6 < 4 ? WaterLight : Water;
            p[2 * w + x] = x % 5 < 3 ? Foam : WaterLight;
        }
    });

    // Grade do bueiro, 14x8 com 1 pixel de contorno: moldura de ferro e barras sobre a água escura (v5C, a água sai e
    // entra por aqui).
    public static Sprite Grate => Cached("Grate", 16, 10, (p, w, h) =>
    {
        Color32 iron = new(0x4A, 0x4A, 0x52, 255);
        Color32 ironLight = new(0x8A, 0x8A, 0x94, 255);
        for (int y = 1; y < h - 1; y++)
        {
            for (int x = 1; x < w - 1; x++)
            {
                bool frame = x == 1 || x == w - 2 || y == 1 || y == h - 2;
                bool bar = x % 3 == 1;
                p[y * w + x] = frame ? (y == h - 2 ? ironLight : iron) : bar ? ironLight : WaterDeep;
            }
        }
        AddOutline(p, w, h);
    });

    #endregion

    #region VÁLVULA

    // Volante vermelho do registro do bueiro, 24x24: aro, quatro raios e o cubo dourado, contorno escuro.
    public static Sprite Valve => Cached("Valve", 24, 24, (p, w, h) =>
    {
        Color32 red = new(0xD9, 0x41, 0x2F, 255);
        Color32 redLight = new(0xF2, 0x8C, 0x28, 255);
        Color32 gold = new(0xFF, 0xD1, 0x4D, 255);
        float c = w * 0.5f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                bool rim = r <= 10.5f && r >= 8f;
                bool spoke = r < 8f && (Mathf.Abs(dx) < 1.1f || Mathf.Abs(dy) < 1.1f);
                bool hub = r <= 2.6f;
                if (hub) p[y * w + x] = gold;
                else if (rim) p[y * w + x] = dy > 3f && dx < 0f ? redLight : red;
                else if (spoke) p[y * w + x] = red;
            }
        }
        AddOutline(p, w, h);
    });

    // Seta curva em volta da válvula (a dica de girar): arco de 270 graus com a ponta, 40x40, branca para tingir.
    public static Sprite TurnArrow => Cached("TurnArrow", 40, 40, (p, w, h) =>
    {
        Color32 white = new(255, 255, 255, 255);
        float c = w * 0.5f;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (r >= 15.5f && r <= 18f && (angle < 60f || angle > 120f)) p[y * w + x] = white;
            }
        }
        // ponta da seta no fim do arco (em cima, à direita do vão)
        for (int i = 0; i < 6; i++)
        {
            for (int j = -i; j <= i; j++)
            {
                int x = 27 - i, y = 33 + j;
                if (x >= 0 && x < w && y >= 0 && y < h) p[y * w + x] = white;
            }
        }
        AddOutline(p, w, h);
    });

    #endregion

    #region METEORO

    // Sombra escura do meteoro: elipse com borda pontilhada, branca para tingir (o tamanho cresce pela escala).
    public static Sprite Shadow => Cached("MeteorShadow", 48, 28, (p, w, h) =>
    {
        Color32 white = new(255, 255, 255, 255);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = (x + 0.5f - w * 0.5f) / (w * 0.5f), dy = (y + 0.5f - h * 0.5f) / (h * 0.5f);
                float d = dx * dx + dy * dy;
                if (d <= 0.7f || (d <= 1f && (x + y) % 2 == 0)) p[y * w + x] = white;
            }
        }
    });

    // Rocha em brasa, 20x20: marrom com borda laranja e veios amarelos, contorno escuro.
    public static Sprite Rock => Cached("MeteorRock", 20, 20, (p, w, h) =>
    {
        Color32 dark = new(0x5A, 0x34, 0x1E, 255);
        Color32 brown = new(0x7A, 0x4A, 0x22, 255);
        Color32 hot = new(0xF2, 0x8C, 0x28, 255);
        Color32 gold = new(0xFF, 0xD1, 0x4D, 255);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x + 0.5f - 10f, dy = y + 0.5f - 10f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) + Mathf.Sin(Mathf.Atan2(dy, dx) * 5f) * 0.8f;
                if (r > 8.5f) continue;
                Color32 color = r > 6.8f ? hot : (dx - dy > 4f ? dark : brown);
                if (r < 6.8f && ((x * 7 + y * 3) % 11 == 0)) color = gold;
                p[y * w + x] = color;
            }
        }
        AddOutline(p, w, h);
    });

    // Pedrinha da explosão e do entulho, 4x4.
    public static Sprite Pebble => Cached("Pebble", 4, 4, (p, w, h) =>
    {
        Color32 brown = new(0x7A, 0x4A, 0x22, 255);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (!((x == 0 || x == w - 1) && (y == 0 || y == h - 1))) p[y * w + x] = x + y < 3 ? brown : Ink;
    });

    // Cratera do tamanho do quarteirão, 1 texel = 1 pixel da arte: borda de terra levantada, fundo escuro, pedras e
    // rachaduras saindo. Pivô no meio.
    public static Sprite Crater(int width, int height, int seed)
    {
        width = Mathf.Clamp(width, 16, 512);
        height = Mathf.Clamp(height, 12, 512);
        string key = $"Crater_{width}x{height}_{seed}";
        return Cached(key, width, height, (p, w, h) =>
        {
            Color32 rim = new(0xA0, 0x70, 0x3A, 255);
            Color32 rimDark = new(0x7A, 0x4A, 0x22, 255);
            Color32 floor = new(0x4A, 0x32, 0x24, 255);
            Color32 deep = new(0x2C, 0x1E, 0x18, 255);
            Color32 ember = new(0xF2, 0x8C, 0x28, 255);
            System.Random random = new(seed);
            float cx = w * 0.5f, cy = h * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f - cx) / (w * 0.5f), dy = (y + 0.5f - cy) / (h * 0.5f);
                    float angle = Mathf.Atan2(dy, dx);
                    float edge = 0.86f + Mathf.Sin(angle * 7f + seed) * 0.05f + Mathf.Sin(angle * 13f) * 0.03f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / edge;
                    if (d > 1f) continue;
                    Color32 color = d > 0.82f ? (dy < 0f ? rimDark : rim) : d > 0.55f ? floor : deep;
                    // o fundo ainda em brasa, em pontos soltos
                    if (d < 0.35f && random.NextDouble() < 0.05) color = ember;
                    p[y * w + x] = color;
                }
            }
            // pedras soltas no fundo e na borda
            int rocks = Mathf.Max(6, w * h / 220);
            for (int i = 0; i < rocks; i++)
            {
                int x = random.Next(2, w - 3), y = random.Next(2, h - 3);
                if (p[y * w + x].a == 0) continue;
                p[y * w + x] = rim;
                p[y * w + x + 1] = rim;
                p[(y + 1) * w + x] = rimDark;
            }
            AddOutline(p, w, h);
        });
    }

    #endregion

    #region HELPERS

    private static Texture2D Tile(string key, int width, int height, System.Action<Color32[], int, int> paint)
    {
        if (textures.TryGetValue(key, out Texture2D texture) && texture != null) return texture;

        Color32[] pixels = new Color32[width * height];
        paint(pixels, width, height);
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "BlockFx_" + key,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        textures[key] = texture;
        return texture;
    }

    private static Sprite Cached(string key, int width, int height, System.Action<Color32[], int, int> paint)
    {
        if (sprites.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;

        Color32[] pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Clear;
        paint(pixels, width, height);
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
        {
            name = "BlockFx_" + key,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        sprites[key] = sprite;
        return sprite;
    }

    private static void AddOutline(Color32[] pixels, int width, int height)
    {
        bool[] solid = new bool[pixels.Length];
        for (int i = 0; i < pixels.Length; i++) solid[i] = pixels[i].a > 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (solid[y * width + x]) continue;

                bool near = (x > 0 && solid[y * width + x - 1]) || (x < width - 1 && solid[y * width + x + 1])
                    || (y > 0 && solid[(y - 1) * width + x]) || (y < height - 1 && solid[(y + 1) * width + x]);
                if (near) pixels[y * width + x] = Ink;
            }
        }
    }

    #endregion
}
