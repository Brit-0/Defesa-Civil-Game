using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Obra pronta (v5C, docs/jam/VFX_V5.md): o mesmo momento de festa para tudo que se constrói. Só escuta eventos:
// - ExpansionEvents.BuildFinished (base e armazém novos), LightningRodEvents.BuildFinished (para-raio),
//   OutpostEvents.BuildFinished (guarita) e GameEvents.BlockIncidentResolved do meteoro (quarteirão reconstruído);
// - no pé da obra: anel dourado tracejado no chão, poeira para os lados, coluna de luz subindo, brilhos no alto e
//   confete; em cima, o texto dourado (a guarita já tem a placa "GUARITA PRONTA!": sem texto nela).
// O quarteirão do meteoro ganha a versão grande: anel do tamanho do quarteirão e uma coluna em cada casa, em escada.
// Os sons de pronto já tocam pelo GameAudio e pelas Feel de cada sistema. Tudo no GameClock (tweens param no Esc).
public class BuildDoneFx : MonoBehaviour
{
    public const string ObjectName = "[Obra pronta]";

    [Header("FESTA")]
    [SerializeField] private Color ringColor = new Color32(0xFF, 0xD1, 0x4D, 0xE6);
    [SerializeField] private Color pillarColor = new Color32(0xF2, 0xEB, 0xDD, 0xC0);
    [Tooltip("Coluna de luz: largura e altura em alturas de casa.")]
    [SerializeField] private Vector2 pillarSize = new Vector2(0.75f, 2.8f);
    [SerializeField] private float pillarTime = 1.1f;
    [SerializeField] private float ringTime = 0.75f;
    [SerializeField] private int confetti = 30;
    [SerializeField] private float stagger = 0.06f;

    [Header("TEXTO")]
    [SerializeField] private string lotText = "OBRA PRONTA!";
    [SerializeField] private string rodText = "PARA-RAIO PRONTO!";
    [SerializeField] private string rebuildText = "RECONSTRUÍDO!";
    [SerializeField] private Color textColor = new Color32(0xFF, 0xD1, 0x4D, 0xFF);
    [SerializeField] private float textSize = 30f;
    [SerializeField] private float textRise = 70f;
    [SerializeField] private float textTime = 1.3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterFirstScene() => Ensure();

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    // Sem tocar na cena: em toda cena com GameManager, um objeto próprio.
    public static BuildDoneFx Ensure()
    {
        BuildDoneFx fx = FindAnyObjectByType<BuildDoneFx>(FindObjectsInactive.Include);
        if (fx != null || FindAnyObjectByType<GameManager>() == null) return fx;
        return new GameObject(ObjectName).AddComponent<BuildDoneFx>();
    }

    private void OnEnable()
    {
        ExpansionEvents.BuildFinished += OnLotFinished;
        LightningRodEvents.BuildFinished += OnRodFinished;
        OutpostEvents.BuildFinished += OnOutpostFinished;
        GameEvents.BlockIncidentResolved += OnIncidentResolved;
    }

    private void OnDisable()
    {
        ExpansionEvents.BuildFinished -= OnLotFinished;
        LightningRodEvents.BuildFinished -= OnRodFinished;
        OutpostEvents.BuildFinished -= OnOutpostFinished;
        GameEvents.BlockIncidentResolved -= OnIncidentResolved;
    }

    private void OnLotFinished(BuildLot lot)
    {
        if (lot == null) return;
        Vector3 foot = lot.Building != null ? lot.Building.transform.position : (Vector3)lot.SitePosition;
        Celebrate(foot, 1.4f, 1.2f, null, 0f);
        // acima da etiqueta com o nome do prédio, que fica no topo dele
        FloatText(foot + Vector3.up * HouseWorld() * 1.6f, lotText);
    }

    private void OnRodFinished(RodBlock block)
    {
        if (block == null || block.Anchor == null) return;
        Celebrate(Foot(block.Anchor), 1.2f, 1f, rodText, 0f);
    }

    private void OnOutpostFinished(OutpostBlock site)
    {
        if (site == null) return;
        Celebrate(site.BoothFoot, 1f, 0.9f, null, 0f);
    }

    // Quarteirão do meteoro de pé: anel do tamanho dele, coluna em cada casa em escada e um texto só.
    private void OnIncidentResolved(BlockIncident incident)
    {
        if (incident == null || incident.kind != IncidentKind.Meteor) return;
        RectTransform sky = PixelFx.Sky;
        if (sky == null) return;

        Vector2 block = incident.area.size / Mathf.Max(1e-6f, sky.lossyScale.x);
        VfxKit.Ring(sky, PixelFx.ToLocal(sky, incident.Center), PixelFx.Ring(64, 36, 2, RingStyle.Dashed), ringColor, block * 1.1f, 0.3f, 1.3f, ringTime * 1.3f);
        for (int i = 0; i < incident.houses.Count; i++)
        {
            House house = incident.houses[i];
            if (house != null) Celebrate(Foot(house), 0.9f, 0.6f, null, i * stagger, i % 2 == 0, 0.5f);
        }
        FloatText(new Vector3(incident.Center.x, incident.area.yMax + HouseWorld() * 0.5f, incident.Center.z), rebuildText);
    }

    // A festa no pé de uma obra: escala em alturas de casa (altura da coluna e largura do anel).
    // glow: força da coluna de luz (o quarteirão do meteoro tem uma em cada casa e usa menos).
    private void Celebrate(Vector3 foot, float height, float width, string text, float delay, bool withConfetti = true, float glow = 1f)
    {
        RectTransform sky = PixelFx.Sky;
        if (sky == null) return;
        float house = VfxKit.House(sky);
        Vector2 at = PixelFx.ToLocal(sky, foot);

        VfxKit.Ring(sky, at, PixelFx.Ring(48, 24, 2, RingStyle.Dashed), ringColor, new Vector2(1.6f, 0.8f) * house * width, 0.3f, 1.5f, ringTime, delay);
        VfxKit.Pillar(sky, at, PixelFx.WithAlpha(pillarColor, pillarColor.a * glow), house * pillarSize.x * width, house * pillarSize.y * height, pillarTime, delay);
        VfxKit.Halo(sky, at + Vector2.up * house * 0.6f * height, PixelFx.WithAlpha(PixelFx.Gold, 0.55f), new Vector2(1.8f, 1.5f) * house * width, pillarTime * 0.7f, 0.3f, 1f, delay);
        DOVirtual.DelayedCall(delay, () =>
        {
            if (sky == null) return;
            FeelParticles.EmitAt(FxKind.Dust, sky, foot, house * 0.9f * width, 12);
            FeelParticles.Emit(FxKind.Glint, sky, at + Vector2.up * house * height, house * 0.9f, 7);
            if (withConfetti) FeelParticles.Emit(FxKind.Confetti, sky, at + Vector2.up * house * 0.4f * height, house * 2.5f * width, confetti);
        }).SetLink(gameObject);

        if (!string.IsNullOrEmpty(text)) FloatText(foot + Vector3.up * house * sky.lossyScale.y * (height + 0.3f), text);
    }

    // Altura de uma casa em unidades do mundo.
    private static float HouseWorld() => PixelFx.Sky != null ? VfxKit.House(PixelFx.Sky) * PixelFx.Sky.lossyScale.y : 1f;

    // Texto dourado que sobe, na camada dos textos do HUD (a mesma do "GUARITA!" e do "PROTEGIDO!").
    private void FloatText(Vector3 world, string message)
    {
        RectTransform hud = FeelUI.HudLayer("FeelPopups");
        if (hud == null) return;

        TMP_Text label = FeelUI.CreateText("Obra pronta", hud, message, textSize, textColor);
        label.fontStyle = FontStyles.Bold;
        label.outlineColor = new Color32(0x14, 0x12, 0x18, 0xFF);
        label.outlineWidth = 0.25f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(360f, 50f);
        rect.anchoredPosition = FeelUI.WorldToHud(world);
        rect.localScale = Vector3.one * 0.6f;

        DOTween.Sequence().SetLink(label.gameObject)
            .Append(rect.DOScale(1f, 0.2f).SetEase(Ease.OutBack))
            .Join(rect.DOAnchorPosY(rect.anchoredPosition.y + textRise, textTime).SetEase(Ease.OutQuad))
            .Insert(textTime * 0.65f, label.DOFade(0f, textTime * 0.35f))
            .OnComplete(() => Destroy(label.gameObject));
    }

    // Pé visível da casa (o BaseOf fica uns 11 pixels da arte abaixo do desenho).
    private static Vector3 Foot(House house)
    {
        Vector2 baseOf = LightningRodManager.BaseOf(house);
        float pixel = PixelFx.ArtPixel * (PixelFx.Container != null ? PixelFx.Container.lossyScale.y : 1f);
        return new Vector3(baseOf.x, baseOf.y + pixel * 9f, house.transform.position.z);
    }
}
