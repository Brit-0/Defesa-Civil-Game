using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Peças de VFX da v5C (docs/jam/VFX_V5.md), usadas pela enchente, pelo meteoro, pela guarita, pelo para-raio e pela obra
// pronta: anel que abre (ou fecha) e some, redemoinho, halo, coluna de luz, raio em ziguezague e as receitas de partícula
// que o FxPresets não tem. As camadas do mundo (PixelFx.Ground e Sky) têm ~218 unidades por casa: partícula com escala 1
// ali tem 1/200 de casa e não aparece. Por isso tudo aqui mede em alturas de casa da camada (House).
public static class VfxKit
{
    #region RÉGUA

    // Altura de uma casa nas unidades locais da camada (vale para o chão, o céu e as camadas dos quarteirões).
    public static float House(Transform layer)
    {
        if (layer == null) return 0f;
        float world = PixelFx.HouseUnit * (PixelFx.Container != null ? PixelFx.Container.lossyScale.y : 1f);
        return world / Mathf.Max(1e-6f, Mathf.Abs(layer.lossyScale.y));
    }

    #endregion

    #region FORMAS

    // Anel que abre (from < to) ou fecha e some: from e to são escalas sobre 'size' (unidades da camada).
    public static Image Ring(RectTransform layer, Vector2 at, Sprite sprite, Color color, Vector2 size, float from, float to, float time, float delay = 0f)
    {
        if (layer == null || sprite == null) return null;

        Image ring = NewImage("Anel", layer, sprite, color, at, size);
        RectTransform rect = ring.rectTransform;
        rect.localScale = Vector3.one * from;
        Animate(ring, rect.DOScale(to, time).SetEase(from < to ? Ease.OutQuad : Ease.InQuad), color.a, time, delay);
        return ring;
    }

    // Redemoinho: anel redondo tracejado que gira dentro de um pai achatado (o tracejado corre pela elipse, como água
    // descendo pelo ralo visto de cima) e fecha até o meio.
    public static void Swirl(RectTransform layer, Vector2 at, Sprite circle, Color color, float diameter, float squash, float from, float to, float time, float degrees)
    {
        if (layer == null || circle == null) return;

        RectTransform holder = FeelUI.CreateRect("Redemoinho", layer);
        holder.anchorMin = holder.anchorMax = new Vector2(.5f, .5f);
        holder.anchoredPosition = at;
        holder.sizeDelta = Vector2.zero;
        holder.localScale = new Vector3(1f, squash, 1f);

        Image ring = NewImage("Anel", holder, circle, color, Vector2.zero, Vector2.one * diameter);
        RectTransform rect = ring.rectTransform;
        rect.localScale = Vector3.one * from;
        rect.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
        Tween turn = rect.DOLocalRotate(new Vector3(0f, 0f, rect.localEulerAngles.z + degrees), time, RotateMode.FastBeyond360).SetEase(Ease.InQuad);
        DOTween.Sequence().SetLink(holder.gameObject)
            .Append(rect.DOScale(to, time).SetEase(Ease.InQuad))
            .Join(turn)
            .Insert(time * .55f, ring.DOFade(0f, time * .45f))
            .OnComplete(() => Object.Destroy(holder.gameObject));
    }

    // Halo pixel (luz em faixas) que cresce rápido e some devagar.
    public static Image Halo(RectTransform layer, Vector2 at, Color color, Vector2 size, float time, float from = .3f, float to = 1f, float delay = 0f)
    {
        if (layer == null) return null;

        Image halo = NewImage("Halo", layer, PixelFx.Glow(24, 24, 4), color, at, size);
        RectTransform rect = halo.rectTransform;
        rect.localScale = Vector3.one * from;
        Sequence sequence = DOTween.Sequence().SetLink(halo.gameObject);
        if (delay > 0f)
        {
            halo.color = PixelFx.WithAlpha(color, 0f);
            sequence.AppendInterval(delay).Append(halo.DOFade(color.a, .03f));
        }
        sequence.Append(rect.DOScale(to, time * .25f).SetEase(Ease.OutQuad))
            .Join(halo.DOFade(0f, time).SetEase(Ease.InQuad))
            .OnComplete(() => Object.Destroy(halo.gameObject));
        return halo;
    }

    // Coluna de luz que sobe do pé (pivô embaixo), segura e afina até sumir.
    public static Image Pillar(RectTransform layer, Vector2 foot, Color color, float width, float height, float time, float delay = 0f)
    {
        if (layer == null) return null;

        Image beam = NewImage("Coluna de luz", layer, PillarSprite, color, foot, new Vector2(width, height));
        RectTransform rect = beam.rectTransform;
        rect.pivot = new Vector2(.5f, 0f);
        rect.anchoredPosition = foot;
        rect.localScale = new Vector3(.4f, 0f, 1f);
        Sequence sequence = DOTween.Sequence().SetLink(beam.gameObject);
        if (delay > 0f) sequence.AppendInterval(delay);
        sequence.Append(rect.DOScale(Vector3.one, time * .2f).SetEase(Ease.OutBack))
            .AppendInterval(time * .25f)
            .Append(rect.DOScaleX(0f, time * .55f).SetEase(Ease.InQuad))
            .Join(beam.DOFade(0f, time * .55f))
            .OnComplete(() => Object.Destroy(beam.gameObject));
        return beam;
    }

    // Raio em ziguezague de 'from' a 'to' (unidades da camada): brilho largo por fora, contorno e miolo claro, galhos
    // curtos. Pisca 'flickers' vezes (os retornos do raio) e some em 'time'.
    public static void Bolt(RectTransform layer, Vector2 from, Vector2 to, float thickness, Color core, Color edge, Color glow, int segments, float jitter, int branches, float time, int flickers)
    {
        if (layer == null) return;

        RectTransform holder = FeelUI.CreateRect("Raio", layer);
        holder.anchorMin = holder.anchorMax = new Vector2(.5f, .5f);
        holder.anchoredPosition = Vector2.zero;
        holder.sizeDelta = Vector2.zero;
        CanvasGroup group = holder.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        segments = Mathf.Max(2, segments);
        Vector2[] joints = new Vector2[segments + 1];
        Vector2 side = Vector2.Perpendicular((to - from).normalized);
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float wobble = i == 0 || i == segments ? 0f : Random.Range(-jitter, jitter) * Mathf.Lerp(1f, .4f, t);
            joints[i] = Vector2.Lerp(from, to, t) + side * wobble;
        }

        float[] widths = { thickness * 3.4f, thickness * 1.8f, thickness };
        Color[] colors = { glow, edge, core };
        for (int pass = 0; pass < 3; pass++)
        {
            for (int i = 0; i < segments; i++) Segment(holder, joints[i], joints[i + 1], widths[pass], colors[pass]);

            // galhos: saem das juntas do meio, para o lado e para a frente
            for (int b = 0; b < branches; b++)
            {
                int joint = 1 + (b * 2 + 1) % Mathf.Max(1, segments - 1);
                Vector2 start = joints[joint];
                Vector2 along = (joints[Mathf.Min(segments, joint + 1)] - start).normalized;
                Vector2 end = start + (Vector2)(Quaternion.Euler(0f, 0f, b % 2 == 0 ? 38f : -38f) * along) * (to - from).magnitude * .16f;
                if (pass > 0) Segment(holder, start, end, widths[pass] * .5f, colors[pass]);
            }
        }

        Sequence sequence = DOTween.Sequence().SetLink(holder.gameObject);
        for (int i = 0; i < flickers; i++) sequence.Append(group.DOFade(.12f, .035f)).Append(group.DOFade(1f, .025f));
        sequence.Append(group.DOFade(0f, time).SetEase(Ease.InQuad)).OnComplete(() => Object.Destroy(holder.gameObject));
    }

    public static Image Segment(RectTransform parent, Vector2 from, Vector2 to, float thickness, Color color)
    {
        Image image = FeelUI.CreateImage("Traço", parent, color);
        image.raycastTarget = false;
        RectTransform segment = image.rectTransform;
        segment.anchorMin = segment.anchorMax = new Vector2(.5f, .5f);
        Vector2 line = to - from;
        segment.anchoredPosition = (from + to) * .5f;
        segment.sizeDelta = new Vector2(thickness, line.magnitude + thickness * .5f);
        segment.localRotation = Quaternion.FromToRotation(Vector3.up, line);
        return image;
    }

    private static Image NewImage(string objectName, RectTransform parent, Sprite sprite, Color color, Vector2 at, Vector2 size)
    {
        Image image = FeelUI.CreateImage(objectName, parent, color, sprite);
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = at;
        rect.sizeDelta = size;
        return image;
    }

    private static void Animate(Image image, Tween motion, float alpha, float time, float delay)
    {
        Sequence sequence = DOTween.Sequence().SetLink(image.gameObject);
        if (delay > 0f)
        {
            image.color = PixelFx.WithAlpha(image.color, 0f);
            sequence.AppendInterval(delay).Append(image.DOFade(alpha, .04f));
        }
        sequence.Append(motion).Join(image.DOFade(0f, time).SetEase(Ease.InQuad)).OnComplete(() => Object.Destroy(image.gameObject));
    }

    #endregion

    #region SPRITES

    private static readonly Dictionary<string, Sprite> sprites = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => sprites.Clear();

    // Coluna de luz 9x40: miolo forte, bordas fracas e o topo some em faixas (pivô embaixo).
    public static Sprite PillarSprite => Paint("Pillar", 9, 40, new Vector2(.5f, 0f), (p, w, h) =>
    {
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            float band = t < .45f ? 1f : t < .7f ? .65f : t < .88f ? .4f : .2f;
            for (int x = 0; x < w; x++)
            {
                int dx = Mathf.Abs(x - w / 2);
                float side = dx == 0 ? 1f : dx == 1 ? .85f : dx == 2 ? .55f : dx == 3 ? .3f : .14f;
                // pontilhado na troca de faixa, como luz pintada à mão
                if (dx >= 3 && (x + y) % 2 == 0) continue;
                p[y * w + x] = new Color32(255, 255, 255, (byte)(255 * side * band));
            }
        }
    });

    private static Sprite Paint(string key, int width, int height, Vector2 pivot, System.Action<Color32[], int, int> paint)
    {
        if (sprites.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;

        Color32[] pixels = new Color32[width * height];
        paint(pixels, width, height);
        Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
        {
            name = "VfxKit_" + key,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        sprite = Sprite.Create(texture, new Rect(0, 0, width, height), pivot, 32f, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        sprites[key] = sprite;
        return sprite;
    }

    #endregion

    #region RECEITAS (escala = altura da casa na camada; tempo no GameClock)

    private static readonly Color WaterLight = PixelFx.Hex(0x8CC4F5);
    private static readonly Color WaterMid = PixelFx.Hex(0x3F7FE0, .9f);

    // Jato do bueiro entupido: gotas compridas que sobem e caem.
    public static readonly FxPreset WaterJet = new()
    {
        count = new Vector2Int(10, 14), size = new Vector2(.022f, .034f), speed = new Vector2(1.5f, 2.3f), lifetime = new Vector2(.6f, .9f),
        spread = 34f, spawnRadius = .04f, gravity = 5.2f, drag = .5f, endSize = .7f, alignToVelocity = true, stretch = 2.4f,
        fadeOutFrom = .65f, unscaledTime = true, colors = new[] { WaterLight, PixelFx.Cream, WaterMid }
    };

    // Respingo baixo (gota que cai na água, casa voltando seca)
    public static readonly FxPreset WaterSplash = new()
    {
        count = new Vector2Int(8, 12), size = new Vector2(.018f, .03f), speed = new Vector2(.6f, 1.1f), lifetime = new Vector2(.35f, .55f),
        spread = 150f, spawnRadius = .05f, gravity = 4.5f, drag = 1.2f, endSize = .5f, alignToVelocity = true, stretch = 1.8f,
        fadeOutFrom = .5f, unscaledTime = true, colors = new[] { WaterLight, PixelFx.Cream }
    };

    // Névoa do jato
    public static readonly FxPreset Mist = new()
    {
        shape = FxShape.Circle, count = new Vector2Int(3, 5), size = new Vector2(.08f, .14f), speed = new Vector2(.15f, .35f),
        lifetime = new Vector2(.6f, 1f), spread = 140f, spawnRadius = .08f, gravity = -.15f, drag = 2f, endSize = 1.8f, fadeIn = .1f,
        fadeOutFrom = .25f, unscaledTime = true, colors = new[] { PixelFx.WithAlpha(PixelFx.Cream, .55f), PixelFx.WithAlpha(WaterLight, .5f) }
    };

    // Bola de fogo do impacto: bolhas claras que viram fumaça
    public static readonly FxPreset Fireball = new()
    {
        shape = FxShape.Circle, count = new Vector2Int(12, 16), size = new Vector2(.2f, .34f), speed = new Vector2(.5f, 1.3f),
        lifetime = new Vector2(.5f, .85f), spread = 360f, spawnRadius = .18f, gravity = -.7f, drag = 3.2f, endSize = 1.9f,
        fadeOutFrom = .35f, unscaledTime = true, colors = new[] { PixelFx.Cream, PixelFx.Gold, PixelFx.Orange },
        useEndColor = true, endColor = new Color(.3f, .22f, .2f, .9f)
    };

    // Rastro de fogo do meteoro: bolhas que nascem na rocha e esfriam no ar
    public static readonly FxPreset FireTrail = new()
    {
        shape = FxShape.Circle, count = new Vector2Int(2, 3), size = new Vector2(.09f, .15f), speed = new Vector2(.05f, .2f),
        lifetime = new Vector2(.3f, .5f), spread = 360f, spawnRadius = .05f, gravity = -.3f, drag = 2f, endSize = .25f,
        fadeOutFrom = .4f, unscaledTime = true, colors = new[] { PixelFx.Cream, PixelFx.Gold, PixelFx.Orange },
        useEndColor = true, endColor = PixelFx.Red
    };

    // Coluna de fumaça da cratera: sobe alto, abre e balança
    public static readonly FxPreset SmokeColumn = new()
    {
        shape = FxShape.Circle, count = new Vector2Int(1, 2), size = new Vector2(.16f, .26f), speed = new Vector2(.3f, .55f),
        lifetime = new Vector2(2.4f, 3.4f), spread = 24f, spawnRadius = .12f, gravity = -.22f, drag = .5f, endSize = 3.2f,
        sway = .09f, swayFrequency = 1.1f, fadeIn = .12f, fadeOutFrom = .45f, unscaledTime = true,
        colors = new[] { new Color(.18f, .17f, .2f, .8f), new Color(.28f, .26f, .29f, .75f), new Color(.38f, .35f, .36f, .7f) }
    };

    // Brasas que sobem do fundo quente da cratera
    public static readonly FxPreset HotEmbers = new()
    {
        count = new Vector2Int(2, 4), size = new Vector2(.012f, .022f), speed = new Vector2(.5f, 1f), lifetime = new Vector2(.8f, 1.3f),
        spread = 60f, gravity = -.5f, drag = .6f, endSize = .4f, sway = .06f, swayFrequency = 5f, fadeOutFrom = .55f,
        unscaledTime = true, colors = new[] { PixelFx.Gold, PixelFx.Orange }, useEndColor = true, endColor = PixelFx.Red
    };

    // Poeira levantada pela onda de choque (quem emite dá a direção: para fora)
    public static readonly FxPreset ShockDust = new()
    {
        shape = FxShape.Circle, count = new Vector2Int(1, 1), size = new Vector2(.12f, .2f), speed = new Vector2(.6f, .9f),
        lifetime = new Vector2(.6f, .9f), spread = 20f, gravity = -.1f, drag = 3f, endSize = 2f, fadeOutFrom = .3f, unscaledTime = true,
        colors = new[] { new Color(.8f, .74f, .64f, .85f), new Color(.7f, .64f, .55f, .8f) }
    };

    #endregion
}
