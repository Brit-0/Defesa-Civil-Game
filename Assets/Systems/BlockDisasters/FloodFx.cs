using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Água da enchente (docs/jam/ENCHENTE_METEORO.md). Só escuta eventos e lê o BlockIncident.WaterLevel:
// - no chão (PixelFx.Ground, embaixo das casas), a água em ladrilho pixel se espalha do meio do quarteirão para as ruas;
// - em cada casa alagada, uma faixa de água com espuma sobe no pé do desenho (fica na ordem da própria casa);
// - a água anda (uvRect em passos de 1 texel) no GameClock: para no Esc. Desce com a obra e some na coleta.
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

    private class View
    {
        public BlockIncident incident;
        public RectTransform ground;
        public RawImage groundWater;
        public Vector2 fullSize;
        public readonly List<RawImage> bands = new();
        public readonly List<RawImage> foams = new();
        public readonly List<float> bandWidths = new();
        public float splashTimer;
    }

    private readonly List<View> views = new();

    private void OnEnable()
    {
        GameEvents.BlockIncidentStarted += OnStarted;
        GameEvents.BlockIncidentResolved += OnResolved;
        GameEvents.FireDousedByFlood += OnFireDoused;
    }

    private void OnDisable()
    {
        GameEvents.BlockIncidentStarted -= OnStarted;
        GameEvents.BlockIncidentResolved -= OnResolved;
        GameEvents.FireDousedByFlood -= OnFireDoused;
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

        foreach (House house in incident.houses) AddBand(view, house);
        views.Add(view);

        FeelParticles.EmitAt(FxKind.Splash, layer, center, 1.2f, 10);
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
        float pixel = PixelFx.ArtPixel;
        foreach (View view in views)
        {
            if (view.ground == null) continue;
            float level = view.incident.resolved ? 0f : view.incident.WaterLevel;
            float spread = Mathf.Sqrt(Mathf.Clamp01(level));

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
                float height = Mathf.Round(bandHeight * level) * px;
                band.rectTransform.sizeDelta = new Vector2(band.rectTransform.sizeDelta.x, height);
                band.color = new Color(1f, 1f, 1f, bandAlpha);
                band.enabled = view.foams[i].enabled = height >= px;
                Flow(band, band.rectTransform.rect.size, px, t, -1f);
                Flow(view.foams[i], view.foams[i].rectTransform.rect.size, px, t * 1.5f, -1f);
            }

            view.splashTimer -= GameClock.DeltaTime;
            if (level > 0.3f && view.splashTimer <= 0f && PixelFx.Ground != null)
            {
                view.splashTimer = splashInterval;
                Rect area = view.incident.area;
                Vector3 at = new(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin, area.yMax), view.incident.Center.z);
                FeelParticles.EmitAt(FxKind.Splash, PixelFx.Ground, at, 0.45f, 2);
            }
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

    private void OnResolved(BlockIncident incident)
    {
        View view = views.Find(v => v.incident == incident);
        if (view == null) return;

        views.Remove(view);
        if (PixelFx.Ground != null) FeelParticles.EmitAt(FxKind.Splash, PixelFx.Ground, incident.Center, 1.4f, 14);
        foreach (RawImage band in view.bands)
        {
            if (band != null) band.DOFade(0f, fadeTime).SetLink(band.gameObject).OnComplete(() => Destroy(band.gameObject));
        }
        if (view.ground != null)
        {
            GameObject ground = view.ground.gameObject;
            view.groundWater.DOFade(0f, fadeTime).SetLink(ground).OnComplete(() => Destroy(ground));
        }
    }

    // A água apagou o fogo: vapor subindo da casa.
    private void OnFireDoused(House house)
    {
        if (house == null || PixelFx.Sky == null) return;
        FeelParticles.EmitAt(FxKind.Steam, PixelFx.Sky, house.transform.position + Vector3.up * 0.6f, 1f, 8);
    }
}
