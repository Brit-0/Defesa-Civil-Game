using UnityEngine;

//Sprites de pixel art desenhados por código (Point, sem arquivo nem import), guardados no cache do PixelFx.
//O branco do sprite é tingido pela cor da Image; o contorno sai escuro em qualquer cor.
public static partial class PixelFx
{
    #region SPRITES

    private static readonly Color32 White = new Color32(255, 255, 255, 255);
    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    private static readonly Color32 Outline = new Color32(0x14, 0x12, 0x18, 255);

    private static Sprite Cached(string key, int width, int height, System.Action<Color32[], int, int> paint, Vector2? pivot = null)
    {
        if (cache.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;

        Color32[] pixels = new Color32[width * height];
        paint(pixels, width, height);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "PixelFx_" + key,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        sprite = Sprite.Create(texture, new Rect(0, 0, width, height), pivot ?? new Vector2(.5f, .5f), 32f, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        cache[key] = sprite;
        return sprite;
    }

    //Pixel aceso dentro de uma elipse, medido no centro do pixel
    private static bool InEllipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x + .5f - cx) / Mathf.Max(.01f, rx);
        float dy = (y + .5f - cy) / Mathf.Max(.01f, ry);
        return dx * dx + dy * dy <= 1f;
    }

    //Contorno de 1 pixel em volta do que está aceso (só nos vazios)
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
                if (near) pixels[y * width + x] = Outline;
            }
        }
    }

    //Bolha redonda (fumaça, poeira, vapor): o "Circle" do kit em pixel
    public static Sprite Puff => Cached("Puff", 8, 8, (p, w, h) =>
    {
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (InEllipse(x, y, 4f, 4f, 4.1f, 4.1f)) p[y * w + x] = White;
    });

    //Anel elíptico de w x h pixels: forma diferente para cada tipo de problema
    public static Sprite Ring(int width, int height, int thickness, RingStyle style, bool outline = true)
    {
        string key = $"Ring{width}x{height}t{thickness}{style}{outline}";
        return Cached(key, width + 2, height + 2, (p, w, h) =>
        {
            float cx = w * .5f;
            float cy = h * .5f;
            float rx = width * .5f;
            float ry = height * .5f;
            const float spikes = 12f;
            float spike = Mathf.Max(2f, thickness * 1.5f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float angle = Mathf.Atan2((y + .5f - cy) * rx / ry, x + .5f - cx);
                    float turn = (angle / (Mathf.PI * 2f) + 1f) % 1f;
                    float grow = 0f;
                    float shift = 0f;

                    if (style == RingStyle.Spiky)
                    {
                        float nearest = Mathf.Abs(turn * spikes - Mathf.Round(turn * spikes));
                        grow = spike * Mathf.Max(0f, 1f - nearest * 4f);
                    }
                    else if (style == RingStyle.Zigzag)
                    {
                        float wave = turn * 16f % 1f;
                        shift = (wave < .5f ? wave * 4f - 1f : 3f - wave * 4f) * thickness * .8f;
                    }

                    float margin = style == RingStyle.Spiky ? spike : style == RingStyle.Zigzag ? thickness * .8f : 0f;
                    float outerX = rx - 1f - margin + grow + shift;
                    float outerY = outerX * ry / rx;
                    float innerX = outerX - thickness - grow;
                    float innerY = innerX * ry / rx;

                    bool on = InEllipse(x, y, cx, cy, outerX, outerY) && !InEllipse(x, y, cx, cy, innerX, innerY);
                    if (!on) continue;

                    if (style == RingStyle.Dashed && Mathf.FloorToInt(turn * 16f) % 2 == 1) continue;
                    if (style == RingStyle.DashedOdd && Mathf.FloorToInt(turn * 16f) % 2 == 0) continue;
                    if (style == RingStyle.Broken && (turn % .25f) > .19f && (turn % .25f) < .23f + (turn > .5f ? .01f : 0f)) continue;

                    p[y * w + x] = White;
                }
            }

            if (outline) AddOutline(p, w, h);
        });
    }

    //Brilho em faixas (luz de pixel art, sem degradê liso): centro forte, bordas fracas
    public static Sprite Glow(int width, int height, int bands = 4)
    {
        return Cached($"Glow{width}x{height}b{bands}", width, height, (p, w, h) =>
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + .5f - w * .5f) / (w * .5f);
                    float dy = (y + .5f - h * .5f) / (h * .5f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance > 1f) continue;

                    int band = Mathf.Min(bands - 1, Mathf.FloorToInt(distance * bands));
                    //Pontilhado na troca de faixa, como luz pintada à mão
                    if ((x + y) % 2 == 0 && distance * bands - band > .8f) band = Mathf.Min(bands - 1, band + 1);
                    byte alpha = (byte)(255 * Mathf.Lerp(1f, .18f, band / Mathf.Max(1f, bands - 1f)));
                    p[y * w + x] = new Color32(255, 255, 255, alpha);
                }
            }
        });
    }

    //Rachadura no chão: galhos escuros que saem do centro, com borda clara de um lado (profundidade)
    public static Sprite Crack(int size, int seed)
    {
        return Cached($"Crack{size}s{seed}", size, size, (p, w, h) =>
        {
            System.Random random = new System.Random(seed);
            Color32 lip = new Color32(0xF2, 0xEB, 0xDD, 120);
            int branches = 4 + random.Next(2);

            for (int b = 0; b < branches; b++)
            {
                float angle = (b + (float)random.NextDouble() * .6f) / branches * Mathf.PI * 2f;
                int length = size / 2 - 2 - random.Next(size / 6 + 1);
                Walk(p, w, h, w * .5f, h * .5f, angle, length, random, lip, 2);
            }

            //Buraco do centro
            for (int y = h / 2 - 1; y <= h / 2; y++)
                for (int x = w / 2 - 1; x <= w / 2; x++)
                    p[y * w + x] = Outline;
        });
    }

    private static void Walk(Color32[] p, int w, int h, float x, float y, float angle, int length, System.Random random, Color32 lip, int depth)
    {
        for (int i = 0; i < length; i++)
        {
            angle += ((float)random.NextDouble() - .5f) * .9f;
            x += Mathf.Cos(angle);
            y += Mathf.Sin(angle);

            int px = Mathf.RoundToInt(x);
            int py = Mathf.RoundToInt(y);
            if (px < 0 || py < 0 || px >= w || py >= h) return;

            p[py * w + px] = Outline;
            if (py + 1 < h && p[(py + 1) * w + px].a == 0) p[(py + 1) * w + px] = lip;

            if (depth > 0 && i > 2 && random.NextDouble() < .12)
                Walk(p, w, h, x, y, angle + (random.NextDouble() < .5 ? -.9f : .9f), length / 3, random, lip, depth - 1);
        }
    }

    //Marca de queimado (raio): mancha escura irregular
    public static Sprite Scorch(int size, int seed)
    {
        return Cached($"Scorch{size}s{seed}", size, size, (p, w, h) =>
        {
            System.Random random = new System.Random(seed);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + .5f - w * .5f) / (w * .5f);
                    float dy = (y + .5f - h * .5f) / (h * .5f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy) + ((float)random.NextDouble() - .5f) * .25f;
                    if (distance > 1f) continue;
                    p[y * w + x] = new Color32(0x14, 0x12, 0x18, (byte)(distance < .5f ? 200 : distance < .8f ? 130 : 70));
                }
            }
        });
    }

    //Brilho de 4 pontas (moeda, sucesso, vaga-lume)
    public static Sprite Glint => Cached("Glint", 7, 7, (p, w, h) =>
    {
        byte[] arm = { 255, 255, 170, 90 };
        for (int i = 0; i < 4; i++)
        {
            Color32 c = new Color32(255, 255, 255, arm[i]);
            p[3 * w + 3 + i] = c;
            p[3 * w + 3 - i] = c;
            p[(3 + i) * w + 3] = c;
            p[(3 - i) * w + 3] = c;
        }
    });

    //Cruz de 3x3 (faísca parada, fagulha)
    public static Sprite Plus => Cached("Plus", 3, 3, (p, w, h) =>
    {
        p[1] = p[3] = p[4] = p[5] = p[7] = White;
    });

    //Moeda de 6 px: aro mais escuro e um ponto de brilho (fica dourada pela cor da Image)
    public static Sprite Coin => Cached("Coin", 6, 6, (p, w, h) =>
    {
        Color32 rim = new Color32(190, 150, 110, 255);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (InEllipse(x, y, 3f, 3f, 3.1f, 3.1f)) p[y * w + x] = rim;
        for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
                if (InEllipse(x, y, 3f, 3f, 2.1f, 2.1f)) p[y * w + x] = White;
        p[4 * w + 2] = new Color32(255, 255, 255, 255);
    });

    //Folha de 5x3 com nervura mais escura
    public static Sprite Leaf => Cached("Leaf", 5, 3, (p, w, h) =>
    {
        Color32 vein = new Color32(170, 170, 170, 255);
        p[0 * w + 2] = p[0 * w + 3] = White;
        for (int x = 0; x < 5; x++) p[1 * w + x] = x == 0 ? White : vein;
        p[2 * w + 1] = p[2 * w + 2] = White;
    });

    //Nuvem de pixel: bolhas somadas com a base reta e a parte de baixo um pouco mais escura (volume)
    public static Sprite Cloud => Cached("Cloud", 40, 20, (p, w, h) =>
    {
        Vector3[] blobs = { new Vector3(10, 7, 7), new Vector3(20, 9, 9), new Vector3(30, 7, 7), new Vector3(15, 11, 6), new Vector3(26, 11, 6) };
        Color32 under = new Color32(214, 214, 222, 255);
        for (int y = 2; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                foreach (Vector3 blob in blobs)
                {
                    if (!InEllipse(x, y, blob.x, blob.y, blob.z, blob.z)) continue;
                    p[y * w + x] = y < 6 ? under : White;
                    break;
                }
            }
        }
    });

    //Gota de chuva: risco de 1 pixel que acende para baixo
    public static Sprite Drop => Cached("Drop", 1, 6, (p, w, h) =>
    {
        byte[] alpha = { 255, 230, 180, 130, 80, 40 };
        for (int y = 0; y < h; y++) p[y] = new Color32(255, 255, 255, alpha[y]);
    });

    //Divisa ">" de 2 pixels com contorno (direção da rota)
    public static Sprite Chevron => Cached("Chevron", 8, 9, (p, w, h) =>
    {
        for (int y = 1; y < h - 1; y++)
        {
            int tip = 5 - Mathf.Abs(y - 4);
            for (int x = tip - 1; x <= tip; x++)
                if (x >= 1 && x < w - 1) p[y * w + x] = White;
        }
        AddOutline(p, w, h);
    });

    //Traço da onda do terremoto: length x 2 claro com contorno
    public static Sprite Dash(int length)
    {
        return Cached($"Dash{length}", length + 2, 4, (p, w, h) =>
        {
            for (int x = 1; x <= length; x++) p[1 * w + x] = p[2 * w + x] = White;
            AddOutline(p, w, h);
        });
    }

    //Ponto da rota: 2x2 claro com contorno
    public static Sprite Dot => Cached("Dot", 4, 4, (p, w, h) =>
    {
        p[1 * w + 1] = p[1 * w + 2] = p[2 * w + 1] = p[2 * w + 2] = White;
        AddOutline(p, w, h);
    });

    //Facho de luz da sirene da Base: cunha que nasce no pé (pivô embaixo) e some para cima, em faixas
    public static Sprite Beam => Cached("Beam", 16, 48, (p, w, h) =>
    {
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / h;
            float half = Mathf.Lerp(.5f, w * .5f, t);
            byte alpha = (byte)(255 * (t < .33f ? 1f : t < .66f ? .6f : .3f));
            for (int x = 0; x < w; x++)
                if (Mathf.Abs(x + .5f - w * .5f) <= half) p[y * w + x] = new Color32(255, 255, 255, alpha);
        }
    }, new Vector2(.5f, 0f));

    #endregion
}
