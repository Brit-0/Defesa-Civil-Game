using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Base e Depósito sempre reconhecíveis, de perto e de longe, sem cobrir nada (tudo no chão, embaixo do desenho):
//- Base (CityGuard): halo laranja da Defesa Civil e dois fachos de sirene girando; no modo de escolher a base,
//  o halo pulsa forte e solta ondas, porque é ali que o jogador precisa clicar.
//- Depósito (Warehouse): halo dourado e brilhos subindo; com o material zerado, pulsa e solta ondas.
//Um na cena (Feel.prefab). Acha as duas pelo tipo; refaz no Reiniciar (a cena recarrega com ele).
public class LandmarkFx : MonoBehaviour
{
    [Header("BASE")]
    [SerializeField] private Color baseColor = PixelFx.Orange;
    [SerializeField, Range(0f, 1f)] private float baseHaloAlpha = .32f;
    [SerializeField, Range(0f, 1f)] private float beamAlpha = .2f;
    [SerializeField] private float beamTurnsPerSecond = .25f;
    [Tooltip("Comprimento do facho em tamanhos da casa")]
    [SerializeField] private float beamLength = 1.1f;

    [Header("DEPOT")]
    [SerializeField] private Color depotColor = PixelFx.Gold;
    [SerializeField, Range(0f, 1f)] private float depotHaloAlpha = .3f;
    [SerializeField] private float glintInterval = .8f;

    [Header("CALL ATTENTION (escolher base, material zerado)")]
    [SerializeField, Range(0f, 1f)] private float attentionHaloAlpha = .65f;
    [SerializeField] private float attentionPulseSpeed = 4f;
    [SerializeField] private float pingInterval = .7f;
    [SerializeField] private float pingTime = .6f;
    [SerializeField] private float pingScale = 1.6f;

    [Header("SIZE (frações do tamanho da construção)")]
    [SerializeField] private Vector2 haloSize = new Vector2(1.6f, 1f);
    [SerializeField] private float groundY = -.2f;

    private class Mark
    {
        public House house;
        public bool isBase;
        public RectTransform root;
        public Image halo;
        public Image ping;
        public RectTransform beams;
        public float size;
        public float pingAge = 99f;
        public float pingTimer;
        public float glintTimer;
    }

    private readonly List<Mark> marks = new List<Mark>();

    private void Start()
    {
        foreach (CityGuard guard in FindObjectsByType<CityGuard>()) Add(guard, true);
        foreach (Warehouse depot in FindObjectsByType<Warehouse>()) Add(depot, false);
    }

    private void Add(House house, bool isBase)
    {
        RectTransform houseRect = house.transform as RectTransform;
        float size = houseRect != null ? houseRect.rect.height : PixelFx.HouseUnit;
        Vector2 center = new Vector2(.5f, .5f);
        Color color = isBase ? baseColor : depotColor;

        Mark mark = new Mark { house = house, isBase = isBase, size = size };
        mark.root = FeelUI.CreateRect("Landmark", house.transform);
        mark.root.SetAsFirstSibling();
        FeelUI.Place(mark.root, center, center, new Vector2(0f, size * groundY), Vector2.one * size);

        Vector2 halo = Vector2.Scale(haloSize, Vector2.one * size);
        mark.halo = FeelUI.CreateImage("Halo", mark.root, PixelFx.WithAlpha(color, isBase ? baseHaloAlpha : depotHaloAlpha), PixelFx.Glow(30, 19, 4));
        FeelUI.Place(mark.halo.rectTransform, center, center, Vector2.zero, halo);

        float pixel = PixelFx.ArtPixel;
        Vector2 ringPixels = new Vector2(Mathf.Round(halo.x * .8f / pixel), Mathf.Round(halo.y * .8f / pixel));
        mark.ping = FeelUI.CreateImage("Ping", mark.root, Color.clear, PixelFx.Ring((int)ringPixels.x, (int)ringPixels.y, 2, isBase ? RingStyle.Solid : RingStyle.Dashed));
        FeelUI.Place(mark.ping.rectTransform, center, center, Vector2.zero, (ringPixels + Vector2.one * 2f) * pixel);

        if (isBase)
        {
            //Os fachos giram num pai achatado: no chão visto de cima eles varrem uma elipse
            mark.beams = FeelUI.CreateRect("Beams", mark.root);
            FeelUI.Place(mark.beams, center, center, Vector2.zero, Vector2.zero);
            mark.beams.localScale = new Vector3(1f, .62f, 1f);

            for (int i = 0; i < 2; i++)
            {
                Image beam = PixelFx.CreateSprite("Beam", mark.beams, PixelFx.Beam, PixelFx.WithAlpha(color, beamAlpha));
                float scale = size * beamLength / beam.rectTransform.sizeDelta.y;
                beam.rectTransform.sizeDelta *= scale;
                beam.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 180f);
            }
        }

        marks.Add(mark);
    }

    private bool NeedsAttention(Mark mark)
    {
        if (mark.isBase) return GuardManager.selectBaseMode;
        return ResourcesManager.Instance != null && ResourcesManager.BuildMaterial <= 0 && !mark.house.HasTeam;
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        for (int i = marks.Count - 1; i >= 0; i--)
        {
            Mark mark = marks[i];
            if (mark.root == null)
            {
                marks.RemoveAt(i);
                continue;
            }

            Color color = mark.isBase ? baseColor : depotColor;
            float rest = mark.isBase ? baseHaloAlpha : depotHaloAlpha;
            bool attention = NeedsAttention(mark);

            float alpha = attention ? Mathf.Lerp(rest, attentionHaloAlpha, .5f + .5f * Mathf.Sin(Time.unscaledTime * attentionPulseSpeed)) : rest;
            mark.halo.color = PixelFx.WithAlpha(color, alpha);

            if (mark.beams != null) mark.beams.localRotation = Quaternion.Euler(0f, 0f, -Time.time * beamTurnsPerSecond * 360f);

            UpdatePing(mark, color, attention, delta);

            if (!mark.isBase)
            {
                mark.glintTimer -= delta;
                if (mark.glintTimer <= 0f)
                {
                    mark.glintTimer = glintInterval * (attention ? .5f : 1f) * Random.Range(.7f, 1.3f);
                    Vector2 at = new Vector2(Random.Range(-.35f, .35f) * mark.size, Random.Range(.05f, .4f) * mark.size);
                    FeelParticles.EmitColored(FxKind.Glint, mark.root, at, mark.size * .8f, depotColor, 1);
                }
            }
        }
    }

    private void UpdatePing(Mark mark, Color color, bool attention, float delta)
    {
        if (attention)
        {
            mark.pingTimer -= delta;
            if (mark.pingTimer <= 0f)
            {
                mark.pingTimer = pingInterval;
                mark.pingAge = 0f;
            }
        }

        if (mark.pingAge > pingTime)
        {
            if (mark.ping.color.a > 0f) mark.ping.color = Color.clear;
            return;
        }

        mark.pingAge += delta;
        float t = Mathf.Clamp01(mark.pingAge / pingTime);
        mark.ping.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, pingScale, 1f - (1f - t) * (1f - t));
        mark.ping.color = PixelFx.WithAlpha(color, 1f - t);
    }
}
