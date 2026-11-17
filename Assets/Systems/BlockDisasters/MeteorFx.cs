using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Meteoro (docs/jam/ENCHENTE_METEORO.md). Só escuta eventos:
// - aviso (MeteorIncoming): a sombra escura cresce no quarteirão e o chão treme cada vez mais forte (o trem de tremor);
//   no fim do aviso a rocha em brasa cai do alto com rastro de fagulhas;
// - impacto (BlockIncidentStarted): clarão, estouro de entulho, tremor forte e a cratera do tamanho do quarteirão no
//   chão; as casas de lá somem (o desenho fica transparente) e o para-raio do quarteirão também;
// - obra: fagulhas de solda na cratera, mais rápidas com a aceleração; coletada, as casas voltam uma a uma.
// Tudo no GameClock: o Esc segura a sombra, o tremor e a queda.
public class MeteorFx : MonoBehaviour
{
    [Header("SOMBRA")]
    [SerializeField] private Color shadowColor = new(.05f, .03f, .08f, 1f);
    [SerializeField] private Vector2 shadowAlpha = new(.3f, .75f);
    [SerializeField] private Vector2 shadowScale = new(.3f, 1f);
    [Tooltip("Tamanho da sombra cheia em relação ao quarteirão.")]
    [SerializeField] private float shadowFill = .85f;
    [SerializeField] private Color warnColor = new(1f, .3f, .2f, 1f);
    [Tooltip("Alvo vermelho no quarteirão inteiro desde o começo do aviso: alfa mínimo e máximo do pisca.")]
    [SerializeField] private Vector2 zoneAlpha = new(.15f, .4f);

    [Header("TREMOR (do fraco ao forte, unidades do CameraShaker)")]
    [SerializeField] private Vector2 tremorStrength = new(.006f, .03f);
    [SerializeField] private float tremorTime = .45f;
    [SerializeField] private float impactStrength = .06f;
    [SerializeField] private float impactTime = 1.3f;

    [Header("QUEDA")]
    [Tooltip("Segundos finais do aviso em que a rocha aparece caindo.")]
    [SerializeField] private float fallTime = 1.1f;
    [Tooltip("De onde a rocha vem, em alturas de casa a partir do alvo.")]
    [SerializeField] private Vector2 fallFrom = new(5f, 9f);
    [SerializeField] private Vector2 rockPixels = new(1.5f, 3.2f);
    [SerializeField] private float trailInterval = .04f;

    [Header("IMPACTO")]
    [SerializeField] private Color flashColor = new(1f, .95f, .8f, .85f);
    [SerializeField] private float flashTime = .45f;
    [SerializeField] private float smokeInterval = .9f;
    [SerializeField] private float weldInterval = .5f;
    [SerializeField] private float rebuildStagger = .06f;

    private const string FlashLayer = "Meteoro";

    private class Pending
    {
        public BlockIncident incident;
        public float start;
        public float seconds;
        public int tremorsDone;
        public Image zone;
        public Image shadow;
        public Image rock;
        public Vector3 rockFrom;
        public float trailTimer;
    }

    private class Crater
    {
        public BlockIncident incident;
        public Image image;
        public readonly List<CanvasGroup> hidden = new();
        public readonly List<Image> rods = new();
        public float smokeTimer;
        public float weldTimer;
    }

    private readonly List<Pending> pendings = new();
    private readonly List<Crater> craters = new();

    private void OnEnable()
    {
        GameEvents.MeteorIncoming += OnIncoming;
        GameEvents.BlockIncidentStarted += OnStarted;
        GameEvents.BlockIncidentResolved += OnResolved;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.MeteorIncoming -= OnIncoming;
        GameEvents.BlockIncidentStarted -= OnStarted;
        GameEvents.BlockIncidentResolved -= OnResolved;
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    #region AVISO

    private void OnIncoming(BlockIncident incident, float seconds)
    {
        RectTransform ground = PixelFx.Ground;
        if (incident == null || ground == null) return;

        Pending pending = new() { incident = incident, start = GameClock.Time, seconds = Mathf.Max(.5f, seconds) };
        Vector2 at = PixelFx.ToLocal(ground, Center(incident));
        Vector2 size = incident.area.size / Mathf.Max(1e-6f, ground.lossyScale.x) * shadowFill;
        // Alvo vermelho no tamanho cheio (onde cai) e, por cima, a sombra escura que cresce até ele.
        pending.zone = PixelFx.CreateSprite("Alvo do meteoro", ground, BlockDisasterArt.Shadow, warnColor);
        pending.zone.raycastTarget = false;
        pending.zone.rectTransform.anchoredPosition = at;
        pending.zone.rectTransform.sizeDelta = size;
        pending.shadow = PixelFx.CreateSprite("Sombra do meteoro", ground, BlockDisasterArt.Shadow, shadowColor);
        pending.shadow.raycastTarget = false;
        RectTransform rect = pending.shadow.rectTransform;
        rect.anchoredPosition = at;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one * shadowScale.x;
        pendings.Add(pending);
    }

    private void Update()
    {
        UpdatePendings();
        UpdateCraters();
    }

    private void UpdatePendings()
    {
        for (int i = pendings.Count - 1; i >= 0; i--)
        {
            Pending pending = pendings[i];
            if (pending.shadow == null)
            {
                pendings.RemoveAt(i);
                continue;
            }

            float t = Mathf.Clamp01((GameClock.Time - pending.start) / pending.seconds);
            float grow = t * (.5f + .5f * t);
            RectTransform shadow = pending.shadow.rectTransform;
            shadow.localScale = Vector3.one * Mathf.Lerp(shadowScale.x, shadowScale.y, grow);
            // pisca vermelho cada vez mais rápido perto do fim
            float blink = Mathf.Sin(GameClock.Time * Mathf.Lerp(4f, 22f, t)) > .3f ? t : 0f;
            Color color = Color.Lerp(shadowColor, warnColor, blink * .35f);
            color.a = Mathf.Lerp(shadowAlpha.x, shadowAlpha.y, grow);
            pending.shadow.color = color;
            if (pending.zone != null)
            {
                Color zone = warnColor;
                zone.a = Mathf.Lerp(zoneAlpha.x, zoneAlpha.y, .5f + .5f * Mathf.Sin(GameClock.Time * Mathf.Lerp(3f, 14f, t)));
                pending.zone.color = zone;
            }

            // trem de tremor: N trancos, do fraco ao forte, espalhados pelo aviso
            int tremors = Mathf.Max(1, BlockDisasterData.Instance.meteorTremors);
            int due = Mathf.FloorToInt(t * (tremors + 1));
            while (pending.tremorsDone < Mathf.Min(due, tremors))
            {
                pending.tremorsDone++;
                float k = tremors > 1 ? (pending.tremorsDone - 1f) / (tremors - 1f) : 1f;
                if (CameraShaker.Instance != null) CameraShaker.Instance.Shake(Mathf.Lerp(tremorStrength.x, tremorStrength.y, k), tremorTime);
                if (PixelFx.Ground != null) FeelParticles.EmitAt(FxKind.Dust, PixelFx.Ground, Center(pending.incident), .6f + k, 4 + pending.tremorsDone * 2);
            }

            float left = (1f - t) * pending.seconds;
            if (left <= fallTime) Fall(pending, 1f - left / fallTime);
        }
    }

    // A rocha desce do alto em linha reta, crescendo, com rastro de brasa e fumaça.
    private void Fall(Pending pending, float k)
    {
        RectTransform sky = PixelFx.Sky;
        if (sky == null) return;

        Vector3 target = Center(pending.incident);
        if (pending.rock == null)
        {
            float unit = PixelFx.HouseUnit * (PixelFx.Container != null ? PixelFx.Container.lossyScale.y : 1f);
            pending.rockFrom = target + new Vector3(fallFrom.x * unit, fallFrom.y * unit, 0f);
            pending.rock = PixelFx.CreateSprite("Meteoro", sky, BlockDisasterArt.Rock, Color.white, rockPixels.x);
            pending.rock.raycastTarget = false;
        }

        float eased = k * k;
        Vector3 at = Vector3.Lerp(pending.rockFrom, target, eased);
        RectTransform rock = pending.rock.rectTransform;
        rock.anchoredPosition = PixelFx.ToLocal(sky, at);
        rock.localScale = Vector3.one * Mathf.Lerp(1f, rockPixels.y / rockPixels.x, eased);
        rock.localEulerAngles = new Vector3(0f, 0f, GameClock.Time * 360f);

        pending.trailTimer -= GameClock.DeltaTime;
        if (pending.trailTimer > 0f) return;
        pending.trailTimer = trailInterval;
        FeelParticles.EmitAt(FxKind.Embers, sky, at, 1.2f, 3);
        FeelParticles.EmitAt(FxKind.Smoke, sky, at, .8f, 1);
    }

    #endregion

    #region IMPACTO

    private void OnStarted(BlockIncident incident)
    {
        if (incident == null || incident.kind != IncidentKind.Meteor) return;

        Pending pending = pendings.Find(p => p.incident == incident);
        if (pending != null)
        {
            pendings.Remove(pending);
            if (pending.rock != null) Destroy(pending.rock.gameObject);
            if (pending.shadow != null) Destroy(pending.shadow.gameObject);
            if (pending.zone != null) Destroy(pending.zone.gameObject);
        }

        Vector3 center = Center(incident);
        Crater crater = new() { incident = incident };
        RectTransform ground = PixelFx.Ground;
        if (ground != null)
        {
            Vector2 size = incident.area.size / Mathf.Max(1e-6f, ground.lossyScale.x);
            float pixel = Mathf.Max(.01f, PixelFx.ArtPixel);
            Sprite sprite = BlockDisasterArt.Crater(Mathf.RoundToInt(size.x / pixel), Mathf.RoundToInt(size.y / pixel), Mathf.RoundToInt(center.x * 13f + center.y * 7f));
            crater.image = PixelFx.CreateSprite("Cratera", ground, sprite, Color.white);
            crater.image.raycastTarget = false;
            crater.image.rectTransform.anchoredPosition = PixelFx.ToLocal(ground, center);
            crater.image.rectTransform.localScale = Vector3.one * .3f;
            crater.image.rectTransform.DOScale(1f, .25f).SetEase(Ease.OutBack).SetLink(crater.image.gameObject);
        }

        // As casas somem na cratera (o chamado do líder continua visível: balão e círculo ficam fora do Visual).
        foreach (House house in incident.houses)
        {
            Transform visual = house != null ? house.transform.Find("Visual") : null;
            if (visual == null) continue;
            CanvasGroup group = visual.GetComponent<CanvasGroup>();
            if (group == null) group = visual.gameObject.AddComponent<CanvasGroup>();
            // Some só o desenho: o clique continua (o ponto certo e a coleta passam ao canteiro).
            group.alpha = 0f;
            crater.hidden.Add(group);

            RodBlock block = LightningRodManager.Instance != null ? LightningRodManager.Instance.BlockOfHouse(house) : null;
            if (block != null && block.Rod != null && block.Rod.enabled && !crater.rods.Contains(block.Rod))
            {
                block.Rod.enabled = false;
                crater.rods.Add(block.Rod);
            }
        }
        craters.Add(crater);

        Explode(center);
    }

    private void Explode(Vector3 center)
    {
        if (CameraShaker.Instance != null) CameraShaker.Instance.Shake(impactStrength, impactTime);

        RectTransform sky = PixelFx.Sky;
        if (sky != null)
        {
            FeelParticles.EmitAt(FxKind.Burst, sky, center, 3f);
            FeelParticles.EmitAt(FxKind.Debris, sky, center, 2.4f, 30);
            FeelParticles.EmitAt(FxKind.Sparks, sky, center, 2f, 24);
            FeelParticles.EmitAt(FxKind.Embers, sky, center, 2f, 20);
            FeelParticles.EmitAt(FxKind.Smoke, sky, center, 2.6f, 14);
        }
        if (PixelFx.Ground != null) FeelParticles.EmitAt(FxKind.Dust, PixelFx.Ground, center, 3f, 24);

        // Clarão na tela toda, por cima do HUD do mundo e embaixo dos menus.
        RectTransform layer = FeelUI.HudLayer(FlashLayer);
        if (layer == null) return;
        Image flash = FeelUI.CreateImage("Clarão", layer, flashColor);
        flash.raycastTarget = false;
        FeelUI.Stretch(flash.rectTransform, -50f);
        flash.DOFade(0f, flashTime).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(flash.gameObject).OnComplete(() => Destroy(flash.gameObject));
    }

    private void UpdateCraters()
    {
        RectTransform sky = PixelFx.Sky;
        RectTransform ground = PixelFx.Ground;
        foreach (Crater crater in craters)
        {
            if (crater.image == null) continue;
            BlockIncident incident = crater.incident;
            House leader = incident.leader;
            Rect area = incident.area;
            Vector3 at = new(Random.Range(area.xMin, area.xMax) * .8f + area.center.x * .2f, Random.Range(area.yMin, area.yMax) * .8f + area.center.y * .2f, Center(incident).z);

            // Fumaça que sobe da cratera enquanto ninguém conserta.
            crater.smokeTimer -= GameClock.DeltaTime;
            if (crater.smokeTimer <= 0f && sky != null && leader != null && !leader.IsWorking)
            {
                crater.smokeTimer = smokeInterval;
                FeelParticles.EmitAt(FxKind.Smoke, sky, at, 1f, 1);
            }

            // Obra: solda e poeira; com a aceleração dos acertos as fagulhas saem mais rápido.
            if (leader != null && leader.IsWorking)
            {
                crater.weldTimer -= GameClock.DeltaTime * BlockDisasterRules.RebuildSpeed(incident.Boost);
                crater.image.color = new Color(1f, 1f, 1f, 1f - .55f * leader.WorkProgress01);
                if (crater.weldTimer <= 0f && sky != null)
                {
                    crater.weldTimer = weldInterval;
                    FeelParticles.EmitAt(FxKind.Weld, sky, at, 1f, 4);
                    if (ground != null) FeelParticles.EmitAt(FxKind.Dust, ground, at, .6f, 2);
                }
            }
        }
    }

    #endregion

    #region RECONSTRUÇÃO

    private void OnResolved(BlockIncident incident)
    {
        Crater crater = craters.Find(c => c.incident == incident);
        if (crater == null) return;
        craters.Remove(crater);

        if (crater.image != null)
        {
            GameObject image = crater.image.gameObject;
            crater.image.DOFade(0f, .5f).SetLink(image).OnComplete(() => Destroy(image));
        }
        foreach (Image rod in crater.rods)
        {
            if (rod != null) rod.enabled = true;
        }

        // As casas voltam uma a uma, quicando, com poeira e confete.
        for (int i = 0; i < crater.hidden.Count; i++)
        {
            CanvasGroup group = crater.hidden[i];
            if (group == null) continue;
            Transform visual = group.transform;
            float delay = i * rebuildStagger;
            group.DOFade(1f, .15f).SetDelay(delay).SetLink(group.gameObject);
            visual.localScale = new Vector3(1f, .2f, 1f);
            visual.DOScale(1f, .35f).SetDelay(delay).SetEase(Ease.OutBack).SetLink(group.gameObject);
            if (PixelFx.Sky != null) FeelParticles.EmitAt(FxKind.Dust, PixelFx.Sky, visual.position, .8f, 4);
        }
        if (PixelFx.Sky != null) FeelParticles.EmitAt(FxKind.Confetti, PixelFx.Sky, Center(incident), 2f, 30);
    }

    // Fim de partida no meio do aviso: a sombra e a rocha somem.
    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.Won && state != GameState.Lost) return;

        foreach (Pending pending in pendings)
        {
            if (pending.shadow != null) Destroy(pending.shadow.gameObject);
            if (pending.zone != null) Destroy(pending.zone.gameObject);
            if (pending.rock != null) Destroy(pending.rock.gameObject);
        }
        pendings.Clear();
    }

    #endregion

    private static Vector3 Center(BlockIncident incident)
    {
        Vector2 center = incident.area.center;
        float z = incident.leader != null ? incident.leader.transform.position.z : PixelFx.Container != null ? PixelFx.Container.position.z : 0f;
        return new Vector3(center.x, center.y, z);
    }
}
