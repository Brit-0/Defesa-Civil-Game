using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Água da enchente (docs/jam/ENCHENTE_METEORO.md e VFX_V5.md). Só escuta eventos e lê o BlockIncident.WaterLevel:
// - no chão (PixelFx.Ground, embaixo das casas), a água em ladrilho pixel se espalha do meio do quarteirão para as ruas;
//   enquanto sobe, ondas de espuma saem do bueiro até a borda, e ondulações e respingos abrem e somem pela água;
// - em cada casa alagada, uma faixa de água com espuma sobe no pé do desenho e balança 1 pixel (a onda batendo);
// - o bueiro (grade no meio do quarteirão) entupido cospe jatos de água; com a válvula aberta vira redemoinho, mais
//   rápido com a pressão; soltar a válvula devolve um jato forte; a coleta estoura respingos em todas as casas;
// - tudo anda no GameClock: para no Esc. Desce com a obra e some na coleta.
public class FloodFx : MonoBehaviour
{
    [Header("ÁGUA")]
    [Tooltip("Quanto a água passa da borda do quarteirão, em unidades do mundo (as ruas têm 1,2).")]
    [SerializeField] private float spill = 0.5f;
    [SerializeField, Range(0f, 1f)] private float groundAlpha = 0.82f;
    [SerializeField, Range(0f, 1f)] private float bandAlpha = 0.9f;
    [Tooltip("Altura da faixa de água nas casas com a água cheia, em pixels da arte.")]
    [SerializeField] private float bandHeight = 13f;
    [Tooltip("Onde a faixa começa acima do pé da casa (LightningRodManager.BaseOf), em pixels da arte: o desenho começa uns 11 acima.")]
    [SerializeField] private float bandRise = 9f;
    [Tooltip("Largura da faixa em relação ao desenho da casa.")]
    [SerializeField] private float bandWidth = 0.78f;
    [Tooltip("Texels por segundo que a água anda.")]
    [SerializeField] private float flowSpeed = 6f;
    [SerializeField] private float splashInterval = 0.35f;
    [SerializeField] private float fadeTime = 0.6f;

    [Header("ONDAS")]
    [Tooltip("Ondulações por segundo com a água cheia: anéis claros que abrem e somem na água.")]
    [SerializeField] private float ripplesPerSecond = 5f;
    [SerializeField] private float rippleTime = 1.1f;
    [Tooltip("Largura da ondulação, em alturas de casa.")]
    [SerializeField] private float rippleSize = 0.5f;
    [Tooltip("Segundos entre as ondas que saem do bueiro enquanto a água sobe.")]
    [SerializeField] private float surgeInterval = 1.2f;
    [SerializeField] private float surgeTime = 1.6f;
    [Tooltip("Quanto a espuma das casas sobe e desce, em pixels da arte.")]
    [SerializeField] private float bobPixels = 1f;
    [SerializeField] private float bobSpeed = 2.6f;

    [Header("BUEIRO")]
    [Tooltip("Tamanho da grade, em pixels da arte por pixel do desenho.")]
    [SerializeField] private float grateScale = 1.5f;
    [Tooltip("Segundos entre os jatos do bueiro entupido (mínimo e máximo).")]
    [SerializeField] private Vector2 gushInterval = new(0.55f, 1.05f);
    [Tooltip("Força do jato, em alturas de casa (escala das gotas).")]
    [SerializeField] private float gushScale = 0.9f;
    [Tooltip("Segundos entre os anéis do redemoinho com a pressão cheia.")]
    [SerializeField] private float whirlInterval = 0.18f;
    [SerializeField] private float whirlTime = 0.8f;

    private static readonly Color WaterLight = BlockDisasterArt.WaterLight;
    private static readonly Color Foam = BlockDisasterArt.Foam;

    private class View
    {
        public BlockIncident incident;
        public RectTransform ground;
        public RawImage groundWater;
        public RectTransform waves;
        public Image grate;
        public Vector2 fullSize;
        public readonly List<RawImage> bands = new();
        public readonly List<RawImage> foams = new();
        public readonly List<float> bandWidths = new();
        public float splashTimer;
        public float rippleTimer;
        public float surgeTimer;
        public float gushTimer;
        public float whirlTimer;
        public float lastLevel;
        public bool whirlFlip;
    }

    private readonly List<View> views = new();

    private void OnEnable()
    {
        GameEvents.BlockIncidentStarted += OnStarted;
        GameEvents.BlockIncidentResolved += OnResolved;
        GameEvents.FireDousedByFlood += OnFireDoused;
        GameEvents.SkillCheck += OnSkillCheck;
    }

    private void OnDisable()
    {
        GameEvents.BlockIncidentStarted -= OnStarted;
        GameEvents.BlockIncidentResolved -= OnResolved;
        GameEvents.FireDousedByFlood -= OnFireDoused;
        GameEvents.SkillCheck -= OnSkillCheck;
    }

    private void OnStarted(BlockIncident incident)
    {
        if (incident == null || incident.kind != IncidentKind.Flood) return;
        RectTransform layer = PixelFx.Ground;
        if (layer == null) return;

        View view = new() { incident = incident };
        Rect area = incident.area;
        Vector3 center = incident.Center;
        float scale = Mathf.Max(1e-6f, layer.lossyScale.x);
        view.fullSize = (area.size + Vector2.one * spill * 2f) / scale;

        view.ground = FeelUI.CreateRect("Enchente", layer);
        view.ground.anchoredPosition = PixelFx.ToLocal(layer, center);
        view.ground.sizeDelta = Vector2.zero;
        view.groundWater = NewWater("Agua", view.ground, BlockDisasterArt.WaterTile);
        FeelUI.Stretch(view.groundWater.rectTransform);
        // Borda de espuma: a água não termina reta no asfalto.
        Outline edge = view.groundWater.gameObject.AddComponent<Outline>();
        edge.effectColor = new Color32(BlockDisasterArt.Foam.r, BlockDisasterArt.Foam.g, BlockDisasterArt.Foam.b, 200);
        edge.effectDistance = new Vector2(PixelFx.ArtPixel, -PixelFx.ArtPixel);

        // Ondas por cima da água e, por cima delas, a grade do bueiro no meio do quarteirão.
        view.waves = FeelUI.CreateRect("Ondas", view.ground);
        view.waves.anchorMin = view.waves.anchorMax = new Vector2(0.5f, 0.5f);
        view.waves.anchoredPosition = Vector2.zero;
        view.waves.sizeDelta = Vector2.zero;
        view.grate = PixelFx.CreateSprite("Bueiro", view.ground, BlockDisasterArt.Grate, Color.white, grateScale);
        view.grate.raycastTarget = false;
        view.grate.rectTransform.anchoredPosition = Vector2.zero;
        view.grate.rectTransform.localScale = Vector3.one * 0.2f;
        view.grate.rectTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(view.grate.gameObject);

        foreach (House house in incident.houses) AddBand(view, house);
        views.Add(view);

        Gush(view, 1f, 1.5f);
    }

    // Faixa no pé da casa, filha da própria casa (fica por cima do desenho dela e por baixo das casas da frente).
    private void AddBand(View view, House house)
    {
        if (house == null) return;
        Transform visual = house.transform.Find("Visual");
        if (visual is not RectTransform visualRect || house.transform is not RectTransform root) return;

        float pixel = PixelFx.ArtPixel * (PixelFx.Container != null ? PixelFx.Container.lossyScale.y : 1f) / Mathf.Max(1e-6f, root.lossyScale.y);
        Vector3[] corners = new Vector3[4];
        visualRect.GetWorldCorners(corners);
        float width = Mathf.Abs(root.InverseTransformVector(corners[2] - corners[1]).x) * bandWidth;

        Vector2 baseOf = LightningRodManager.BaseOf(house);
        Vector3 foot = new(baseOf.x, baseOf.y, house.transform.position.z);
        Vector2 local = root.InverseTransformPoint(foot);

        RawImage band = NewWater("[Enchente]", root, BlockDisasterArt.WaterTile);
        RectTransform rect = band.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = local - root.rect.center + Vector2.up * pixel * bandRise;
        rect.sizeDelta = new Vector2(width, 0f);
        rect.SetAsLastSibling();

        RawImage foam = NewWater("Espuma", rect, BlockDisasterArt.FoamTile);
        RectTransform foamRect = foam.rectTransform;
        foamRect.anchorMin = new Vector2(0f, 1f);
        foamRect.anchorMax = new Vector2(1f, 1f);
        foamRect.pivot = new Vector2(0.5f, 1f);
        foamRect.sizeDelta = new Vector2(0f, 3f * pixel);
        foamRect.anchoredPosition = Vector2.zero;

        view.bands.Add(band);
        view.foams.Add(foam);
        view.bandWidths.Add(pixel);
    }

    private static RawImage NewWater(string objectName, Transform parent, Texture2D texture)
    {
        RectTransform rect = FeelUI.CreateRect(objectName, parent);
        RawImage image = rect.gameObject.AddComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        return image;
    }

    private void Update()
    {
        if (views.Count == 0) return;

        float t = GameClock.Time;
        float dt = GameClock.DeltaTime;
        float pixel = PixelFx.ArtPixel;
        foreach (View view in views)
        {
            if (view.ground == null) continue;
            float level = view.incident.resolved ? 0f : view.incident.WaterLevel;
            float spread = Mathf.Sqrt(Mathf.Clamp01(level));
            bool rising = !view.incident.ValveOpen && level > view.lastLevel + 1e-5f && level < 0.995f;
            if (dt > 0f) view.lastLevel = level;

            // No chão: espalha do meio do quarteirão; a água anda em passos inteiros de texel.
            Vector2 size = view.fullSize * Mathf.Lerp(0.25f, 1f, spread);
            view.ground.sizeDelta = level > 0.001f ? size : Vector2.zero;
            view.groundWater.color = new Color(1f, 1f, 1f, groundAlpha * Mathf.Clamp01(level * 3f));
            Flow(view.groundWater, size, pixel, t, 1f);

            for (int i = 0; i < view.bands.Count; i++)
            {
                RawImage band = view.bands[i];
                if (band == null) continue;
                float px = view.bandWidths[i];
                // a espuma sobe e desce 1 pixel, cada casa no seu tempo: a onda batendo na parede
                float bob = level > 0.15f ? Mathf.Round(Mathf.Sin(t * bobSpeed + i * 1.7f) * bobPixels) : 0f;
                float height = Mathf.Max(0f, Mathf.Round(bandHeight * level) + bob) * px;
                band.rectTransform.sizeDelta = new Vector2(band.rectTransform.sizeDelta.x, height);
                band.color = new Color(1f, 1f, 1f, bandAlpha);
                band.enabled = view.foams[i].enabled = height >= px;
                Flow(band, band.rectTransform.rect.size, px, t, -1f);
                Flow(view.foams[i], view.foams[i].rectTransform.rect.size, px, t * 1.5f, -1f);
            }

            if (dt <= 0f) continue;
            Waves(view, level, rising, size, dt);
            Drain(view, level, dt);
        }
    }

    // Ladrilho no tamanho da arte (1 texel = 1 pixel da cidade) andando de lado e um pouco para cima.
    private void Flow(RawImage image, Vector2 size, float pixel, float time, float direction)
    {
        Texture texture = image.texture;
        if (texture == null || pixel <= 0f) return;

        float tiles = texture.width * pixel;
        float rows = texture.height * pixel;
        float step = Mathf.Floor(time * flowSpeed);
        image.uvRect = new Rect(direction * step / texture.width, Mathf.Floor(step * 0.35f) / texture.height,
            Mathf.Max(0.01f, size.x / tiles), Mathf.Max(0.01f, size.y / rows));
    }

    #region ONDAS E BUEIRO

    private static Sprite RippleSprite => PixelFx.Ring(16, 8, 1, RingStyle.Solid, false);
    private static Sprite SurgeSprite => PixelFx.Ring(96, 56, 2, RingStyle.Dashed, false);
    private static Sprite WhirlSprite => PixelFx.Ring(48, 48, 3, RingStyle.Dashed, false);

    // Ondulações e respingos soltos pela água; enquanto ela sobe, ondas de espuma saem do bueiro até a borda.
    private void Waves(View view, float level, bool rising, Vector2 size, float dt)
    {
        if (view.waves == null || level < 0.08f) return;
        float house = VfxKit.House(view.waves);

        view.rippleTimer -= dt * ripplesPerSecond * level * VfxQuality.Density;
        while (view.rippleTimer <= 0f)
        {
            view.rippleTimer += 1f;
            Vector2 at = new(Random.Range(-0.45f, 0.45f) * size.x, Random.Range(-0.45f, 0.45f) * size.y);
            float width = rippleSize * house * Random.Range(0.7f, 1.2f);
            VfxKit.Ring(view.waves, at, RippleSprite, PixelFx.WithAlpha(WaterLight, 0.85f), new Vector2(width, width * 0.5f), 0.3f, 1.25f, rippleTime);
        }

        view.splashTimer -= dt;
        if (level > 0.3f && view.splashTimer <= 0f)
        {
            view.splashTimer = splashInterval;
            Vector2 at = new(Random.Range(-0.4f, 0.4f) * size.x, Random.Range(-0.4f, 0.4f) * size.y);
            FeelParticles.EmitPreset(VfxKit.WaterSplash, view.waves, at, house * 0.35f, 4);
            VfxKit.Ring(view.waves, at, RippleSprite, PixelFx.WithAlpha(Foam, 0.9f), new Vector2(house * 0.3f, house * 0.15f), 0.4f, 1.4f, 0.6f);
        }

        if (!rising) return;
        view.surgeTimer -= dt;
        if (view.surgeTimer > 0f) return;
        view.surgeTimer = surgeInterval;
        VfxKit.Ring(view.waves, Vector2.zero, SurgeSprite, PixelFx.WithAlpha(Foam, 0.75f), size, 0.12f, 1f, surgeTime);
    }

    // Bueiro entupido cospe água; com a válvula aberta, a água desce num redemoinho que gira mais com a pressão.
    private void Drain(View view, float level, float dt)
    {
        if (view.waves == null || level < 0.05f) return;
        BlockIncident incident = view.incident;

        // girando a válvula (pressão) a água já desce: redemoinho no lugar do jato
        bool turning = incident.valve.pressure > 0.05f;
        if (!incident.ValveOpen && !turning)
        {
            view.gushTimer -= dt;
            if (view.gushTimer > 0f || level < 0.2f) return;
            view.gushTimer = Random.Range(gushInterval.x, gushInterval.y);
            Gush(view, Mathf.Lerp(0.6f, 1f, level), 1f);
            return;
        }

        float pressure = Mathf.Clamp01(incident.valve.pressure);
        view.whirlTimer -= dt * Mathf.Lerp(0.35f, 1f, pressure);
        if (view.whirlTimer > 0f) return;
        view.whirlTimer = whirlInterval;

        float house = VfxKit.House(view.waves);
        Color color = PixelFx.WithAlpha(view.whirlFlip ? Foam : (Color)BlockDisasterArt.WaterDeep, 1f);
        VfxKit.Swirl(view.waves, Vector2.zero, WhirlSprite, color, house * Mathf.Lerp(0.8f, 1.2f, pressure), 0.6f, 1.7f, 0.6f, whirlTime, view.whirlFlip ? 260f : 200f);
        view.whirlFlip = !view.whirlFlip;
        if (pressure > 0.3f) FeelParticles.EmitPreset(VfxKit.WaterSplash, view.waves, Vector2.zero, house * 0.4f, 2);
    }

    // Jato do bueiro: gotas para o alto (no céu, por cima das casas), névoa e um anel de espuma na água.
    private void Gush(View view, float strength, float amount)
    {
        if (view == null || view.waves == null) return;
        float house = VfxKit.House(view.waves);
        RectTransform sky = PixelFx.Sky;
        if (sky != null)
        {
            Vector2 at = PixelFx.ToLocal(sky, view.incident.Center);
            FeelParticles.EmitPreset(VfxKit.WaterJet, sky, at, VfxKit.House(sky) * gushScale * strength, Mathf.RoundToInt(10 * amount));
            FeelParticles.EmitPreset(VfxKit.Mist, sky, at, VfxKit.House(sky) * 0.8f, Mathf.RoundToInt(3 * amount));
        }
        VfxKit.Ring(view.waves, Vector2.zero, RippleSprite, PixelFx.WithAlpha(Foam, 0.95f), new Vector2(house * 0.7f, house * 0.35f) * strength, 0.3f, 1.5f, 0.55f);
        if (view.grate != null)
        {
            RectTransform grate = view.grate.rectTransform;
            grate.DOKill(true);
            grate.DOPunchScale(new Vector3(0.15f, 0.25f, 0f) * strength, 0.3f, 6).SetLink(grate.gameObject);
        }
    }

    // Soltou a válvula com pressão: a água volta num jato forte.
    private void OnSkillCheck(House house, SkillResult result)
    {
        if (result != SkillResult.ValveSlip || house == null) return;
        View view = views.Find(v => v.incident.leader == house);
        if (view != null) Gush(view, 1.3f, 1.8f);
    }

    #endregion

    private void OnResolved(BlockIncident incident)
    {
        View view = views.Find(v => v.incident == incident);
        if (view == null) return;

        views.Remove(view);
        // A água desce de uma vez: jato no bueiro, onda até a borda e respingo em cada casa que volta seca.
        Gush(view, 1.2f, 2f);
        if (view.waves != null)
            VfxKit.Ring(view.waves, Vector2.zero, SurgeSprite, PixelFx.WithAlpha(Foam, 0.9f), view.ground.sizeDelta, 0.2f, 1.1f, fadeTime);
        RectTransform sky = PixelFx.Sky;
        if (sky != null)
        {
            float house = VfxKit.House(sky);
            foreach (House member in incident.houses)
            {
                if (member == null) continue;
                Vector2 baseOf = LightningRodManager.BaseOf(member);
                Vector3 foot = new(baseOf.x, baseOf.y, member.transform.position.z);
                FeelParticles.EmitPreset(VfxKit.WaterSplash, sky, PixelFx.ToLocal(sky, foot) + Vector2.up * house * 0.15f, house * 0.6f, 6);
            }
        }

        foreach (RawImage band in view.bands)
        {
            if (band != null) band.DOFade(0f, fadeTime).SetLink(band.gameObject).OnComplete(() => Destroy(band.gameObject));
        }
        if (view.grate != null) view.grate.DOFade(0f, fadeTime).SetLink(view.grate.gameObject);
        if (view.ground != null)
        {
            GameObject ground = view.ground.gameObject;
            view.groundWater.DOFade(0f, fadeTime).SetLink(ground).OnComplete(() => Destroy(ground));
        }
    }

    // A água apagou o fogo: vapor subindo da casa.
    private void OnFireDoused(House house)
    {
        RectTransform sky = PixelFx.Sky;
        if (house == null || sky == null) return;
        float unit = VfxKit.House(sky);
        FeelParticles.EmitAt(FxKind.Steam, sky, house.transform.position + Vector3.up * 0.6f, unit * 0.6f, 10);
        FeelParticles.EmitAt(FxKind.Steam, sky, house.transform.position + Vector3.up * 1.2f, unit * 0.45f, 6);
    }
}
