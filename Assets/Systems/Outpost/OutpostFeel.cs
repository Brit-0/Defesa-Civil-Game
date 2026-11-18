using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Resposta visual e sonora da guarita (GUARITAS.md): só escuta OutpostEvents. A compra já toca pelo GameAudio e a
// equipe que sai já tem o som e o rastro de sempre (GameEvents.TeamDispatched). Aqui entra o que conta que foi a
// guarita: o rádio chiando, as ondas saindo da antena, os pontos de sinal até a casa, a área piscando em verde, a
// guarita pulando e o "GUARITA!" em cima da casa. Também o aviso do modo de escolha, as marteladas da obra, a guarita
// subindo pronta e a dica da primeira guarita da partida. Tudo no GameClock: para junto com a pausa (Esc).
// Os sons vão pelo SoundBank da v5D (AudioManager.Play com a posição da guarita); o rádio é o outpost_radio.
public class OutpostFeel : MonoBehaviour
{
    // Ids do SoundBank que este script toca (SoundBankTests confere que estão no banco)
    public static readonly string[] UsedIds = { "ui_popup", "build_start", "repair_tool", "build_done", "outpost_radio" };

    private const float HouseHeight = 3.5f; // altura de uma casa em unidades do mundo (escala das partículas)

    [Header("SOUND")]
    [SerializeField] private float hammerInterval = 0.6f;
    [SerializeField, Range(0f, 1f)] private float hammerVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float radioVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float buildVolume = 0.8f;

    [Header("DISPATCH")]
    [SerializeField] private Color waveColor = new Color32(0x5C, 0xC4, 0x5E, 0xFF);
    [SerializeField] private Color textColor = new Color32(0x8B, 0xE0, 0x7A, 0xFF);
    [SerializeField] private int waves = 2;
    [SerializeField] private float waveTime = 0.7f;
    [SerializeField] private int signalDots = 5;
    [SerializeField] private float signalTime = 0.45f;
    [SerializeField] private string dispatchText = "GUARITA!";
    [SerializeField] private float textSize = 26f;
    [SerializeField] private float textRise = 60f;
    [SerializeField] private float textTime = 1.1f;

    [Header("BUILD")]
    [SerializeField] private float popDuration = 0.45f;

    [Header("TIP")]
    [SerializeField] private string tipFormat = "GUARITA PRONTA!\nOs chamados das {0} casas daqui\nsaem sozinhos, se houver gente livre.";
    [SerializeField] private Vector2 tipSize = new Vector2(420f, 108f);

    // A dica aparece uma vez por sessão (sem disco, por causa do WebGL), como as do TutorialTips
    private static bool tipShown;

    private RectTransform tip;
    private OutpostBlock tipSite;
    private float tipUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => tipShown = false;

    private void OnEnable()
    {
        OutpostEvents.PlacementChanged += OnPlacementChanged;
        OutpostEvents.BuildStarted += OnBuildStarted;
        OutpostEvents.BuildFinished += OnBuildFinished;
        OutpostEvents.AutoDispatched += OnAutoDispatched;
    }

    private void OnDisable()
    {
        OutpostEvents.PlacementChanged -= OnPlacementChanged;
        OutpostEvents.BuildStarted -= OnBuildStarted;
        OutpostEvents.BuildFinished -= OnBuildFinished;
        OutpostEvents.AutoDispatched -= OnAutoDispatched;
        StopAllCoroutines();
    }

    private void LateUpdate() => FollowTip();

    #region BUILD

    private void OnPlacementChanged(bool placing)
    {
        if (placing) AudioManager.Play("ui_popup");
    }

    private void OnBuildStarted(OutpostBlock site)
    {
        if (site == null) return;
        AudioManager.Play("build_start", site.BoothFoot, buildVolume);
        StartCoroutine(Hammer(site));
    }

    private IEnumerator Hammer(OutpostBlock site)
    {
        var wait = new WaitForSeconds(hammerInterval);
        while (site != null && site.State == LotState.Building)
        {
            AudioManager.Play("repair_tool", site.BoothFoot, hammerVolume);
            yield return wait;
        }
    }

    // Pronta: a guarita cresce do chão, o rádio chia, sai a primeira onda e a área acende uma vez.
    private void OnBuildFinished(OutpostBlock site)
    {
        if (site == null) return;
        AudioManager.Play("build_done", site.BoothFoot, buildVolume);
        AudioManager.Play("outpost_radio", site.BoothTop, radioVolume * 0.7f);
        site.Flash();
        Pop(site, 0.25f);
        Waves(site);
        RectTransform layer = site.Effects;
        FeelParticles.Emit(FxKind.Glint, layer, ToLocal(layer, site.BoothTop), HouseHeight * LocalPerWorld(layer) * 0.6f, 4);

        if (!tipShown) ShowTip(site);
    }

    #endregion

    #region DISPATCH

    private void OnAutoDispatched(OutpostBlock site, House house)
    {
        if (site == null || house == null) return;

        AudioManager.Play("outpost_radio", site.BoothTop, radioVolume);
        site.Flash();
        Pop(site, 0.12f);
        Waves(site);
        StartCoroutine(Signal(site, house));
        FloatText(house.transform.position + Vector3.up * 2.2f);
    }

    // A guarita pula (escala no pé) sem mudar de lugar.
    private void Pop(OutpostBlock site, float strength)
    {
        if (site.Booth == null) return;
        RectTransform booth = (RectTransform)site.Booth.transform.parent;
        booth.DOKill(true);
        booth.localScale = Vector3.one;
        booth.DOPunchScale(new Vector3(-strength * 0.5f, strength, 0f), popDuration, 6).SetLink(booth.gameObject);
    }

    // Ondas de rádio: anéis finos tracejados que crescem a partir da antena e somem.
    private void Waves(OutpostBlock site)
    {
        RectTransform layer = site.Effects;
        if (layer == null) return;
        float u = LocalPerWorld(layer);
        for (int i = 0; i < waves; i++)
        {
            Image ring = FeelUI.CreateImage("Onda", layer, waveColor, PixelFx.Ring(24, 24, 1, RingStyle.Dashed, false));
            ring.raycastTarget = false;
            RectTransform rect = ring.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = ToLocal(layer, site.BoothTop);
            rect.sizeDelta = Vector2.one * 0.75f * u;
            rect.localScale = Vector3.one * 0.4f;
            ring.color = new Color(waveColor.r, waveColor.g, waveColor.b, 0f);
            DOTween.Sequence().SetLink(ring.gameObject)
                .AppendInterval(i * waveTime * 0.35f)
                .Append(ring.DOFade(0.9f, 0.05f))
                .Append(rect.DOScale(2.4f, waveTime).SetEase(Ease.OutQuad))
                .Join(ring.DOFade(0f, waveTime).SetEase(Ease.InQuad))
                .OnComplete(() => Destroy(ring.gameObject));
        }
    }

    // Pontos de sinal saindo da antena até o telhado da casa: a guarita chamou a equipe para lá.
    private IEnumerator Signal(OutpostBlock site, House house)
    {
        RectTransform layer = site.Effects;
        if (layer == null) yield break;
        float u = LocalPerWorld(layer);
        Vector2 from = ToLocal(layer, site.BoothTop);
        Vector2 to = ToLocal(layer, house.transform.position + Vector3.up * 1.6f);

        var wait = new WaitForSeconds(0.05f);
        for (int i = 0; i < signalDots; i++)
        {
            if (layer == null) yield break;
            Image dot = FeelUI.CreateImage("Sinal", layer, i % 2 == 0 ? waveColor : textColor, PixelFx.Dot);
            dot.raycastTarget = false;
            RectTransform rect = dot.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = from;
            rect.sizeDelta = Vector2.one * (3f / 32f) * u;
            DOTween.Sequence().SetLink(dot.gameObject)
                .Append(rect.DOAnchorPos(to, signalTime).SetEase(Ease.InOutQuad))
                .Append(dot.DOFade(0f, 0.1f))
                .OnComplete(() => Destroy(dot.gameObject));
            yield return wait;
        }
        yield return new WaitForSeconds(signalTime);
        if (layer != null) FeelParticles.Emit(FxKind.Glint, layer, to, HouseHeight * u * 0.4f, 2);
    }

    // "GUARITA!" subindo da casa, na camada dos textos que sobem do HUD (a mesma do para-raio e do ActionFeedback).
    private void FloatText(Vector3 world)
    {
        RectTransform hud = FeelUI.HudLayer("FeelPopups");
        if (hud == null) return;

        TMP_Text text = FeelUI.CreateText("Guarita", hud, dispatchText, textSize, textColor);
        text.fontStyle = FontStyles.Bold;
        text.outlineColor = new Color32(0x14, 0x12, 0x18, 0xFF);
        text.outlineWidth = 0.25f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 50f);
        rect.anchoredPosition = FeelUI.WorldToHud(world);
        rect.localScale = Vector3.one * 0.6f;

        DOTween.Sequence().SetLink(text.gameObject)
            .Append(rect.DOScale(1f, 0.2f).SetEase(Ease.OutBack))
            .Join(rect.DOAnchorPosY(rect.anchoredPosition.y + textRise, textTime).SetEase(Ease.OutQuad))
            .Insert(textTime * 0.6f, text.DOFade(0f, textTime * 0.4f))
            .OnComplete(() => Destroy(text.gameObject));
    }

    #endregion

    #region TIP

    // Dica curta em cima da primeira guarita pronta: o que ela faz, numa placa pixel que segue a guarita.
    private void ShowTip(OutpostBlock site)
    {
        RectTransform hud = FeelUI.HudLayer("FeelPopups");
        if (hud == null) return;
        tipShown = true;

        tip = FeelUI.CreateRect("Dica da guarita", hud);
        tip.anchorMin = tip.anchorMax = new Vector2(0.5f, 0.5f);
        tip.pivot = new Vector2(0.5f, 0f);
        tip.sizeDelta = tipSize;
        Image plate = tip.gameObject.AddComponent<Image>();
        plate.sprite = Resources.Load<Sprite>("Expansion/Lot_Plate");
        plate.type = Image.Type.Sliced;
        plate.pixelsPerUnitMultiplier = 100f / (32f * 3f);
        plate.raycastTarget = false;

        TMP_Text text = FeelUI.CreateText("Texto", tip, string.Format(tipFormat, site.Houses.Count), 20f, new Color32(0xF2, 0xEB, 0xDD, 0xFF));
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(14f, 10f);
        rect.offsetMax = new Vector2(-14f, -10f);

        tipSite = site;
        tipUntil = GameClock.Time + Mathf.Max(1f, OutpostManager.Instance != null ? OutpostManager.Instance.Data.tipTime : 7f);
        tip.localScale = Vector3.one * 0.6f;
        tip.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetLink(tip.gameObject);
        FollowTip();
    }

    private void FollowTip()
    {
        if (tip == null) return;
        if (tipSite == null || GameClock.Time >= tipUntil)
        {
            Destroy(tip.gameObject);
            tip = null;
            return;
        }
        tip.anchoredPosition = FeelUI.WorldToHud(tipSite.BoothTop) + Vector2.up * 24f;
    }

    #endregion

    #region HELPERS

    private static float LocalPerWorld(RectTransform layer) => layer != null && layer.lossyScale.x != 0f ? 1f / layer.lossyScale.x : 1f;

    private static Vector2 ToLocal(RectTransform layer, Vector3 world)
    {
        Vector2 local = layer.InverseTransformPoint(world);
        return local - layer.rect.center;
    }

    #endregion
}
