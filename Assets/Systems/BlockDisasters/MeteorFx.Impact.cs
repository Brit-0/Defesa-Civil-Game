using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Impacto do meteoro (v5C, docs/jam/VFX_V5.md): clarão branco que vira o laranja da queimada, bola de fogo, estouro de
// entulho, onda de choque (anel grosso no chão, tracejado no ar, poeira levantada na frente dela) e o tranco nas casas
// vizinhas no instante em que a onda passa por cada uma. Só apresentação: chamado pelo OnStarted do MeteorFx.
public partial class MeteorFx
{
    [Header("ONDA DE CHOQUE")]
    [Tooltip("Até onde a onda vai, em tamanhos do quarteirão (escala final do anel).")]
    [SerializeField] private float shockReach = 2.4f;
    [SerializeField] private float shockTime = .8f;
    [SerializeField] private Color shockColor = new(1f, .93f, .75f, .9f);
    [Tooltip("Graus do tranco nas casas vizinhas quando a onda passa por elas (as mais perto, mais forte).")]
    [SerializeField] private float joltDegrees = 6f;
    [SerializeField] private float joltTime = .45f;

    [Header("BOLA DE FOGO")]
    [Tooltip("Tamanho do halo de fogo, em tamanhos do quarteirão.")]
    [SerializeField] private float fireballSize = 1.4f;
    [SerializeField] private float fireballTime = .9f;
    [Tooltip("Cor que o clarão branco vira antes de sumir.")]
    [SerializeField] private Color burnColor = new(1f, .55f, .2f, .55f);

    private const float ShockStart = .25f;

    private static Sprite GroundShock => PixelFx.Ring(64, 36, 2, RingStyle.Solid);
    private static Sprite AirShock => PixelFx.Ring(64, 36, 1, RingStyle.Dashed, false);

    private void Explode(Vector3 center, BlockIncident incident)
    {
        if (CameraShaker.Instance != null) CameraShaker.Instance.Shake(impactStrength, impactTime);
        Flash();

        RectTransform sky = PixelFx.Sky;
        RectTransform ground = PixelFx.Ground;
        if (sky == null) return;
        float house = VfxKit.House(sky);
        Vector2 at = PixelFx.ToLocal(sky, center);
        Vector2 block = incident.area.size / Mathf.Max(1e-6f, sky.lossyScale.x);

        // bola de fogo: halo claro, halo laranja maior e as bolhas que viram fumaça
        VfxKit.Halo(sky, at, PixelFx.WithAlpha(PixelFx.Cream, .95f), Vector2.one * block.x * fireballSize * .6f, fireballTime * .6f, .2f, 1f);
        VfxKit.Halo(sky, at, PixelFx.WithAlpha(PixelFx.Orange, .8f), new Vector2(block.x, block.y * 1.2f) * fireballSize, fireballTime, .3f, 1.1f);
        FeelParticles.EmitPreset(VfxKit.Fireball, sky, at, house * 1.3f, 16);
        FeelParticles.EmitAt(FxKind.Debris, sky, center, house * 2.2f, 30);
        FeelParticles.EmitAt(FxKind.Sparks, sky, center, house * 1.6f, 24);
        FeelParticles.EmitAt(FxKind.Embers, sky, center, house * 1.4f, 20);
        FeelParticles.EmitAt(FxKind.Smoke, sky, center, house * 1.6f, 12);
        FeelParticles.EmitPreset(VfxKit.SmokeColumn, sky, at, house * 1.4f, 8);

        // onda de choque: anel grosso no chão (passa embaixo das casas, aparece nas ruas) e tracejado no ar
        Vector2 ring = block * 1.1f;
        if (ground != null) VfxKit.Ring(ground, PixelFx.ToLocal(ground, center), GroundShock, shockColor, ring, ShockStart, shockReach, shockTime);
        VfxKit.Ring(sky, at, AirShock, PixelFx.WithAlpha(PixelFx.Cream, .8f), ring, ShockStart, shockReach * .92f, shockTime * .85f, .04f);
        StartCoroutine(ShockDust(sky, at, ring * .5f, house));
        Jolt(center, incident, ring * .5f * shockReach * sky.lossyScale.x);
    }

    // Clarão em dois tempos: branco num instante e o laranja da queimada esvaindo (por cima do HUD do mundo).
    private void Flash()
    {
        RectTransform layer = FeelUI.HudLayer(FlashLayer);
        if (layer == null) return;

        Image flash = FeelUI.CreateImage("Clarão", layer, flashColor);
        flash.raycastTarget = false;
        FeelUI.Stretch(flash.rectTransform, -50f);
        DOTween.Sequence().SetUpdate(true).SetLink(flash.gameObject)
            .AppendInterval(.05f)
            .Append(flash.DOColor(burnColor, flashTime * .35f).SetEase(Ease.OutQuad))
            .Append(flash.DOFade(0f, flashTime * .9f).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(flash.gameObject));
    }

    // Escala do anel da onda no instante t (0 a 1), a mesma curva do VfxKit.Ring (OutQuad).
    private float ShockScale(float t) => Mathf.Lerp(ShockStart, shockReach, 1f - (1f - t) * (1f - t));

    // Poeira levantada na frente da onda, para fora, enquanto ela abre.
    private IEnumerator ShockDust(RectTransform sky, Vector2 at, Vector2 radius, float house)
    {
        float start = GameClock.Time;
        float next = 0f;
        while (sky != null)
        {
            float t = (GameClock.Time - start) / Mathf.Max(.05f, shockTime);
            if (t >= 1f) yield break;
            if (GameClock.Time >= next)
            {
                next = GameClock.Time + .06f;
                float scale = ShockScale(t);
                for (int i = 0; i < 4; i++)
                {
                    float angle = Random.Range(0f, 360f);
                    Vector2 direction = new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                    FeelParticles.EmitPreset(VfxKit.ShockDust, sky, at + Vector2.Scale(direction, radius) * scale, house * .8f * (1f - t * .5f), 1, angle);
                }
            }
            yield return null;
        }
    }

    // As casas de fora do quarteirão levam um tranco (giro curto no desenho) quando a onda chega nelas.
    private void Jolt(Vector3 center, BlockIncident incident, Vector2 reach)
    {
        if (reach.x <= 0f || reach.y <= 0f) return;

        foreach (House house in FindObjectsByType<House>())
        {
            if (house == null || incident.houses.Contains(house)) continue;
            Vector2 offset = house.transform.position - center;
            float distance = Mathf.Sqrt(offset.x * offset.x / (reach.x * reach.x) + offset.y * offset.y / (reach.y * reach.y));
            if (distance > 1f) continue;
            Transform visual = house.transform.Find("Visual");
            if (visual == null) continue;

            // quando o anel (OutQuad) chega a esta distância
            float q = Mathf.Clamp01((distance * shockReach - ShockStart) / Mathf.Max(.01f, shockReach - ShockStart));
            float delay = shockTime * (1f - Mathf.Sqrt(1f - q));
            float degrees = joltDegrees * (1f - distance * .7f) * (offset.x >= 0f ? -1f : 1f);
            visual.DOPunchRotation(new Vector3(0f, 0f, degrees), joltTime, 8, .6f).SetDelay(delay).SetLink(visual.gameObject);
        }
    }
}
