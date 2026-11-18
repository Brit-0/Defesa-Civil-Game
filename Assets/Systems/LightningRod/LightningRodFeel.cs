using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Resposta visual e sonora do para-raio (PARARAIO.md): só escuta LightningRodEvents. A compra já toca pelo GameAudio e
// o raio absorvido já tem o clarão, o raio do céu até a ponta, o trovão e o tranco de sempre (GameEvents.LightningStruck
// na ponta do para-raio). Aqui entra o que conta que o para-raio segurou o raio: o arco da casa que seria atingida até a
// ponta, o brilho no mastro, a faísca, a descarga descendo até o chão, a área protegida acendendo, o "PROTEGIDO!" e o
// estalo elétrico. Também o aviso do modo de escolha, as marteladas da obra e o para-raio subindo pronto.
public class LightningRodFeel : MonoBehaviour
{
    private const float HouseHeight = 3.5f; // altura de uma casa em unidades do mundo (escala das partículas)

    [Header("SOUND")]
    [SerializeField] private float hammerInterval = 0.6f;
    [SerializeField, Range(0f, 1f)] private float hammerVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float zapVolume = 0.9f;
    [SerializeField, Range(0f, 0.5f)] private float zapPitchVariation = 0.12f;

    [Header("ABSORB")]
    [SerializeField] private Color arcColor = new Color32(0xF2, 0xEB, 0xDD, 0xFF);
    [SerializeField] private Color glowColor = new Color32(0xFF, 0xD1, 0x4D, 0xFF);
    [SerializeField] private int arcSegments = 6;
    [SerializeField] private float arcJitter = 0.35f;     // unidades do mundo
    [SerializeField] private float arcThickness = 0.09f;  // unidades do mundo
    [SerializeField] private float arcTime = 0.45f;
    [SerializeField] private int sparkCount = 16;
    [SerializeField] private float glowTime = 0.6f;
    [SerializeField] private float ringTime = 0.7f;
    [SerializeField] private int dischargeDots = 4;
    [SerializeField] private string protectedText = "PROTEGIDO!";
    [SerializeField] private float textSize = 30f;
    [SerializeField] private float textRise = 70f;
    [SerializeField] private float textTime = 1.2f;

    [Header("BOLT V5C")]
    [Tooltip("Raio grosso do céu até a ponta, no HUD (pixels da tela de referência), por cima do raio comum da WeatherFx.")]
    [SerializeField] private float boltThickness = 10f;
    [SerializeField] private float boltJitter = 70f;
    [SerializeField] private int boltSegments = 9;
    [SerializeField] private int boltBranches = 3;
    [Tooltip("Quantas vezes o raio pisca (os retornos) antes de sumir.")]
    [SerializeField] private int boltFlickers = 3;
    [SerializeField] private float boltTime = 0.35f;
    [SerializeField] private Color boltOuterColor = new Color(0.45f, 0.6f, 1f, 0.55f);
    [SerializeField] private Color strikeFlashColor = new Color(1f, 0.9f, 0.5f, 0.3f);
    [SerializeField] private float strikeShake = 0.025f;
    [SerializeField] private float strikeShakeTime = 0.35f;
    [Tooltip("Raios da estrela de luz na ponta do para-raio.")]
    [SerializeField] private int starRays = 8;

    [Header("BUILD")]
    [SerializeField] private float popDuration = 0.45f;

    [Header("IDLE")]
    [SerializeField] private float idleGlintInterval = 4f;  // brilho na ponta dos para-raios prontos: o quarteirão está protegido

    private AudioClip zap;
    private float nextGlint;

    private void OnEnable()
    {
        LightningRodEvents.PlacementChanged += OnPlacementChanged;
        LightningRodEvents.BuildStarted += OnBuildStarted;
        LightningRodEvents.BuildFinished += OnBuildFinished;
        LightningRodEvents.StrikeAbsorbed += OnStrikeAbsorbed;
    }

    private void OnDisable()
    {
        LightningRodEvents.PlacementChanged -= OnPlacementChanged;
        LightningRodEvents.BuildStarted -= OnBuildStarted;
        LightningRodEvents.BuildFinished -= OnBuildFinished;
        LightningRodEvents.StrikeAbsorbed -= OnStrikeAbsorbed;
        StopAllCoroutines();
    }

    private AudioClip Zap => zap != null ? zap : zap = Resources.Load<AudioClip>("LightningRod/Rod_Zap");

    private void Update()
    {
        if (Time.time < nextGlint) return;
        nextGlint = Time.time + idleGlintInterval;

        LightningRodManager manager = LightningRodManager.Instance;
        if (manager == null) return;
        foreach (RodBlock block in manager.Blocks)
        {
            if (block.State != LotState.Built || block.Effects == null) continue;
            RectTransform layer = block.Effects;
            FeelParticles.Emit(FxKind.Glint, layer, ToLocal(layer, block.RodTip), HouseHeight * LocalPerWorld(layer) * 0.5f, 2);
        }
    }

    #region BUILD

    private void OnPlacementChanged(bool placing)
    {
        if (placing && AudioManager.Sfx != null) AudioManager.PlaySfx(AudioManager.Sfx.popup);
    }

    private void OnBuildStarted(RodBlock block) => StartCoroutine(Hammer(block));

    private IEnumerator Hammer(RodBlock block)
    {
        var wait = new WaitForSeconds(hammerInterval);
        while (block != null && block.State == LotState.Building)
        {
            if (AudioManager.Sfx != null) AudioManager.PlaySfx(AudioManager.Sfx.hammer, hammerVolume, 0.15f);
            yield return wait;
        }
    }

    // Pronto: o mastro cresce do telhado com um estalo, solta faísca e a área acende uma vez.
    private void OnBuildFinished(RodBlock block)
    {
        if (block == null) return;
        if (AudioManager.Sfx != null) AudioManager.PlaySfx(AudioManager.Sfx.repaired);
        AudioManager.PlaySfx(Zap, zapVolume * 0.5f, zapPitchVariation);
        block.Flash();

        if (block.Rod != null)
        {
            RectTransform rod = block.Rod.rectTransform;
            rod.localScale = new Vector3(1f, 0.2f, 1f);
            rod.DOScale(1f, popDuration).SetEase(Ease.OutBack).SetLink(rod.gameObject);
        }
        Sparks(block, block.RodTip, sparkCount / 2);
    }

    #endregion

    #region ABSORB

    private void OnStrikeAbsorbed(RodBlock block, Vector3 target)
    {
        if (block == null) return;

        AudioManager.PlaySfx(Zap, zapVolume, zapPitchVariation);
        block.Flash();
        SuperBolt(block);
        Arc(block, RoofOf(block, target), block.RodTip);
        Glow(block);
        Sparks(block, block.RodTip, sparkCount);
        StartCoroutine(Discharge(block));
        Ring(block);
        FloatText(block.RodTip);
    }

    // v5C: o raio que o para-raio puxa é mais forte que o comum: clarão dourado, raio grosso em três camadas (azul, dourado,
    // branco) com galhos e retornos, estrela de luz na ponta e um tranco de câmera. No HUD, na camada dos textos que sobem
    // (por cima do clarão da WeatherFx); o "PROTEGIDO!" nasce depois e fica por cima.
    private void SuperBolt(RodBlock block)
    {
        RectTransform hud = FeelUI.HudLayer("FeelPopups");
        if (hud == null) return;
        float pixel = FeelUI.Pixel != null ? FeelUI.PixelSize : 1f;
        Vector2 tip = FeelUI.WorldToHud(block.RodTip);
        Vector2 top = new Vector2(tip.x + Random.Range(-1f, 1f) * boltJitter * 1.5f, hud.rect.height * 0.5f + 40f);

        Image flash = FeelUI.CreateImage("Clarão do para-raio", hud, strikeFlashColor);
        flash.raycastTarget = false;
        FeelUI.Stretch(flash.rectTransform, -50f);
        flash.DOFade(0f, boltTime).SetEase(Ease.InQuad).SetLink(flash.gameObject).OnComplete(() => Destroy(flash.gameObject));

        float thickness = Mathf.Max(1f, Mathf.Round(boltThickness / pixel)) * pixel;
        VfxKit.Bolt(hud, top, tip, thickness, Color.white, glowColor, boltOuterColor, boltSegments, boltJitter, boltBranches, boltTime, boltFlickers);
        VfxKit.Halo(hud, tip, PixelFx.WithAlpha(PixelFx.Cream, 0.9f), Vector2.one * 230f, 0.5f, 0.3f, 1f);
        Star(hud, tip, pixel);
        if (CameraShaker.Instance != null) CameraShaker.Instance.Shake(strikeShake, strikeShakeTime);
    }

    // Estrela de luz na ponta: raios curtos e longos alternados que abrem e somem.
    private void Star(RectTransform hud, Vector2 at, float pixel)
    {
        RectTransform holder = FeelUI.CreateRect("Estrela", hud);
        holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0.5f);
        holder.anchoredPosition = at;
        holder.sizeDelta = Vector2.zero;
        CanvasGroup group = holder.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        for (int i = 0; i < starRays; i++)
        {
            float angle = (i * 360f / Mathf.Max(1, starRays) + Random.Range(-8f, 8f)) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            bool longRay = i % 2 == 0;
            VfxKit.Segment(holder, direction * 18f, direction * (longRay ? 95f : 55f), pixel * (longRay ? 3f : 2f), longRay ? Color.white : glowColor);
        }
        holder.localScale = Vector3.one * 0.4f;
        DOTween.Sequence().SetLink(holder.gameObject)
            .Append(holder.DOScale(1.2f, 0.25f).SetEase(Ease.OutQuad))
            .Join(group.DOFade(0f, 0.35f).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(holder.gameObject));
    }

    // Onde o raio ia cair: o telhado da casa sorteada (na própria casa do para-raio, um ponto ao lado dele).
    private static Vector3 RoofOf(RodBlock block, Vector3 target)
    {
        if (block.Anchor != null && Vector2.Distance(block.Anchor.transform.position, target) < 0.01f)
            return block.RodTip + new Vector3(1.6f, 0.9f, 0f);
        foreach (House house in block.Houses)
        {
            if (house == null || Vector2.Distance(house.transform.position, target) > 0.01f) continue;
            Transform visual = house.transform.Find("Visual");
            if (visual is RectTransform rect)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                return new Vector3((corners[0].x + corners[2].x) * 0.5f, Mathf.Lerp(corners[0].y, corners[2].y, 0.8f), target.z);
            }
        }
        return target;
    }

    // Arco em ziguezague da casa até a ponta: o raio sendo puxado para o para-raio. Contorno dourado e miolo claro.
    private void Arc(RodBlock block, Vector3 from, Vector3 to)
    {
        RectTransform layer = block.Effects;
        float u = LocalPerWorld(layer);
        var group = FeelUI.CreateRect("Arco", layer).gameObject.AddComponent<CanvasGroup>();
        RectTransform holder = (RectTransform)group.transform;
        holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0.5f);
        holder.anchoredPosition = Vector2.zero;

        Vector2 a = ToLocal(layer, from), b = ToLocal(layer, to);
        Vector2 side = Vector2.Perpendicular((b - a).normalized);
        Vector2 last = a;
        for (int i = 1; i <= arcSegments; i++)
        {
            float t = (float)i / arcSegments;
            Vector2 next = i == arcSegments ? b : Vector2.Lerp(a, b, t) + side * Random.Range(-arcJitter, arcJitter) * u * Mathf.Sin(t * Mathf.PI);
            Segment(holder, last, next, (arcThickness * 2.2f) * u, glowColor);
            Segment(holder, last, next, arcThickness * u, arcColor);
            last = next;
        }

        DOTween.Sequence().SetLink(holder.gameObject)
            .Append(group.DOFade(0.35f, arcTime * 0.15f))
            .Append(group.DOFade(1f, arcTime * 0.1f))
            .Append(group.DOFade(0f, arcTime * 0.75f).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(holder.gameObject));
    }

    private static void Segment(RectTransform parent, Vector2 from, Vector2 to, float thickness, Color color)
    {
        Image image = FeelUI.CreateImage("Traço", parent, color);
        image.raycastTarget = false;
        RectTransform segment = image.rectTransform;
        segment.anchorMin = segment.anchorMax = new Vector2(0.5f, 0.5f);
        Vector2 line = to - from;
        segment.anchoredPosition = (from + to) * 0.5f;
        segment.sizeDelta = new Vector2(thickness, line.magnitude + thickness * 0.5f);
        segment.localRotation = Quaternion.FromToRotation(Vector3.up, line);
    }

    // Mastro branco por um instante (silhueta por cima) e um halo dourado na ponta.
    private void Glow(RodBlock block)
    {
        if (block.Rod != null)
        {
            Image white = FeelUI.CreateImage("Brilho", block.Rod.rectTransform, Color.white, FeelUI.Silhouette(block.Rod.sprite));
            white.raycastTarget = false;
            Stretch(white.rectTransform);
            white.DOFade(0f, glowTime * 0.6f).SetEase(Ease.InQuad).SetLink(white.gameObject).OnComplete(() => Destroy(white.gameObject));
        }

        RectTransform layer = block.Effects;
        float u = LocalPerWorld(layer);
        Image halo = FeelUI.CreateImage("Halo", layer, glowColor, PixelFx.Glow(24, 24, 4));
        halo.raycastTarget = false;
        RectTransform rect = halo.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = ToLocal(layer, block.RodTip);
        rect.sizeDelta = Vector2.one * 1.6f * u;
        rect.localScale = Vector3.one * 0.3f;
        DOTween.Sequence().SetLink(halo.gameObject)
            .Append(rect.DOScale(1f, glowTime * 0.25f).SetEase(Ease.OutQuad))
            .Join(halo.DOFade(0f, glowTime).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(halo.gameObject));
    }

    private static void Sparks(RodBlock block, Vector3 at, int count)
    {
        RectTransform layer = block.Effects;
        float u = LocalPerWorld(layer);
        FeelParticles.EmitColored(FxKind.Sparks, layer, ToLocal(layer, at), HouseHeight * u, new Color32(0xFF, 0xD1, 0x4D, 0xFF), count);
        FeelParticles.Emit(FxKind.Glint, layer, ToLocal(layer, at), HouseHeight * u * 0.7f, 3);
    }

    // A energia desce pelo mastro e pela parede até o chão: pontos claros em fila, do topo à base da casa.
    private IEnumerator Discharge(RodBlock block)
    {
        RectTransform layer = block.Effects;
        if (layer == null || block.Anchor == null) yield break;
        float u = LocalPerWorld(layer);
        Vector2 top = ToLocal(layer, block.RodTip);
        Vector3 baseWorld = LightningRodManager.BaseOf(block.Anchor);
        Vector2 bottom = ToLocal(layer, new Vector3(block.RodFoot.x, baseWorld.y, block.RodFoot.z));

        var wait = new WaitForSeconds(0.06f);
        for (int i = 0; i < dischargeDots; i++)
        {
            if (layer == null) yield break;
            Image dot = FeelUI.CreateImage("Descarga", layer, i % 2 == 0 ? arcColor : glowColor, PixelFx.Dot);
            dot.raycastTarget = false;
            RectTransform rect = dot.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = top;
            rect.sizeDelta = Vector2.one * 0.28f * u;
            DOTween.Sequence().SetLink(dot.gameObject)
                .Append(rect.DOAnchorPos(bottom, 0.3f).SetEase(Ease.InQuad))
                .Append(dot.DOFade(0f, 0.12f))
                .OnComplete(() => Destroy(dot.gameObject));
            yield return wait;
        }
        yield return new WaitForSeconds(0.3f);
        if (layer != null) FeelParticles.EmitColored(FxKind.Sparks, layer, bottom, HouseHeight * u * 0.6f, glowColor, sparkCount / 2);
    }

    // Anel que corre pelo chão a partir da casa do para-raio: a descarga indo para a terra.
    private void Ring(RodBlock block)
    {
        RectTransform layer = block.Effects;
        if (layer == null || block.Anchor == null) return;
        float u = LocalPerWorld(layer);
        Image ring = FeelUI.CreateImage("Terra", layer, glowColor, PixelFx.Ring(32, 16, 2, RingStyle.Dashed));
        ring.raycastTarget = false;
        RectTransform rect = ring.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = ToLocal(layer, LightningRodManager.BaseOf(block.Anchor));
        rect.sizeDelta = new Vector2(4f, 2f) * u;
        rect.localScale = Vector3.one * 0.2f;
        DOTween.Sequence().SetLink(ring.gameObject)
            .AppendInterval(0.25f)
            .Append(rect.DOScale(1.4f, ringTime).SetEase(Ease.OutQuad))
            .Join(ring.DOFade(0f, ringTime).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(ring.gameObject));
    }

    // "PROTEGIDO!" subindo da ponta do para-raio, na camada dos textos que sobem do HUD (a mesma do ActionFeedback, por
    // cima do clarão da WeatherFx): tamanho de tela, legível em qualquer zoom.
    private void FloatText(Vector3 tip)
    {
        RectTransform hud = FeelUI.HudLayer("FeelPopups");
        if (hud == null) return;

        TMP_Text text = FeelUI.CreateText("Protegido", hud, protectedText, textSize, glowColor);
        text.fontStyle = FontStyles.Bold;
        text.outlineColor = new Color32(0x14, 0x12, 0x18, 0xFF);
        text.outlineWidth = 0.25f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(320f, 60f);
        rect.anchoredPosition = FeelUI.WorldToHud(tip) + Vector2.up * 30f;
        rect.localScale = Vector3.one * 0.6f;

        DOTween.Sequence().SetLink(text.gameObject)
            .Append(rect.DOScale(1f, 0.25f).SetEase(Ease.OutBack))
            .Join(rect.DOAnchorPosY(rect.anchoredPosition.y + textRise, textTime).SetEase(Ease.OutQuad))
            .Insert(textTime * 0.6f, text.DOFade(0f, textTime * 0.4f))
            .OnComplete(() => Destroy(text.gameObject));
    }

    #endregion

    #region HELPERS

    private static float LocalPerWorld(RectTransform layer) => layer != null && layer.lossyScale.x != 0f ? 1f / layer.lossyScale.x : 1f;

    private static Vector2 ToLocal(RectTransform layer, Vector3 world)
    {
        Vector2 local = layer.InverseTransformPoint(world);
        return local - layer.rect.center;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    #endregion
}
