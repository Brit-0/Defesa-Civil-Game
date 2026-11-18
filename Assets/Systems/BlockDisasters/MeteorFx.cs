using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Meteoro (docs/jam/ENCHENTE_METEORO.md). Só escuta eventos:
// - aviso (MeteorIncoming): a sombra escura cresce no quarteirão e o chão treme cada vez mais forte (o trem de tremor);
//   no fim do aviso a rocha em brasa cai do alto com halo, cauda de fogo, brasas e fumaça que fica no céu, e o
//   quarteirão acende laranja com ela chegando (v5C);
// - impacto (BlockIncidentStarted, MeteorFx.Impact.cs): clarão branco que vira laranja, bola de fogo, estouro de
//   entulho, onda de choque no chão e no ar que sacode as casas vizinhas ao passar, tremor forte e a cratera do tamanho
//   do quarteirão; as casas de lá somem (o desenho fica transparente) e o para-raio do quarteirão também;
// - cratera: fundo em brasa que esfria, brasas subindo e colunas de fumaça; na obra, fagulhas de solda mais rápidas
//   com a aceleração; coletada, as casas voltam uma a uma (a festa da obra pronta é da BuildDoneFx).
// Tudo no GameClock: o Esc segura a sombra, o tremor e a queda.
public partial class MeteorFx : MonoBehaviour
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
    [SerializeField] private float fallTime = 1.3f;
    [Tooltip("De onde a rocha vem, em alturas de casa a partir do alvo (perto o bastante para a queda e o rastro ficarem uns 0,5 s na tela).")]
    [SerializeField] private Vector2 fallFrom = new(3.5f, 6.5f);
    [SerializeField] private Vector2 rockPixels = new(1.5f, 3.2f);
    [SerializeField] private float trailInterval = .04f;
    [Tooltip("Bolhas da cauda de fogo atrás da rocha.")]
    [SerializeField] private int trailLength = 9;
    [Tooltip("Comprimento da cauda, em tamanhos da rocha.")]
    [SerializeField] private float trailStretch = 3.2f;
    [Tooltip("Luz laranja no quarteirão com a rocha chegando (alfa no impacto).")]
    [SerializeField, Range(0f, 1f)] private float heatAlpha = .4f;

    [Header("IMPACTO")]
    [SerializeField] private Color flashColor = new(1f, .97f, .88f, .95f);
    [SerializeField] private float flashTime = .55f;
    [Tooltip("Segundos entre as baforadas de fumaça da cratera (uma saída por vez).")]
    [SerializeField] private float smokeInterval = .35f;
    [SerializeField] private float weldInterval = .5f;
    [SerializeField] private float rebuildStagger = .06f;

    [Header("CRATERA")]
    [Tooltip("Segundos até o fundo em brasa da cratera esfriar.")]
    [SerializeField] private float coolTime = 25f;
    [SerializeField] private int smokeVents = 3;
    [SerializeField] private float emberInterval = .25f;

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
        public Image glow;
        public Image heat;
        public readonly List<Image> trail = new();
        public Vector3 rockFrom;
        public float trailTimer;
    }

    private class Crater
    {
        public BlockIncident incident;
        public Image image;
        public Image core;
        public float born;
        public readonly List<Vector3> vents = new();
        public int nextVent;
        public readonly List<CanvasGroup> hidden = new();
        public readonly List<Image> rods = new();
        public float smokeTimer;
        public float emberTimer;
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
                if (PixelFx.Sky != null) FeelParticles.EmitAt(FxKind.Dust, PixelFx.Sky, Center(pending.incident), VfxKit.House(PixelFx.Sky) * (.6f + k), 4 + pending.tremorsDone * 2);
            }

            float left = (1f - t) * pending.seconds;
            if (left <= fallTime) Fall(pending, 1f - left / fallTime);
        }
    }

    // A rocha desce do alto em linha reta, crescendo, com halo, cauda de fogo, brasas e fumaça; o quarteirão acende.
    private void Fall(Pending pending, float k)
    {
        RectTransform sky = PixelFx.Sky;
        if (sky == null) return;

        Vector3 target = Center(pending.incident);
        if (pending.rock == null) CreateRock(pending, sky, target);

        // velocidade quase constante: a rocha entra na tela cedo e o rastro dá tempo de ver
        float eased = Mathf.Lerp(k, k * k, .25f);
        Vector3 at = Vector3.Lerp(pending.rockFrom, target, eased);
        float grow = Mathf.Lerp(1f, rockPixels.y / rockPixels.x, eased);
        RectTransform rock = pending.rock.rectTransform;
        rock.anchoredPosition = PixelFx.ToLocal(sky, at);
        rock.localScale = Vector3.one * grow;
        rock.localEulerAngles = new Vector3(0f, 0f, GameClock.Time * 360f);
        float size = BlockDisasterArt.Rock.rect.width * PixelFx.ArtPixel * rockPixels.x * grow;

        // halo atrás da rocha, tremendo como fogo
        RectTransform glow = pending.glow.rectTransform;
        glow.anchoredPosition = rock.anchoredPosition;
        glow.sizeDelta = Vector2.one * size * 2.6f * (1f + Mathf.Sin(GameClock.Time * 40f) * .08f);

        // cauda: bolhas para trás na linha da queda, da brasa clara até a fumaça
        Vector2 back = PixelFx.ToLocal(sky, pending.rockFrom) - rock.anchoredPosition;
        Vector2 direction = back.sqrMagnitude > 1e-4f ? back.normalized : Vector2.up;
        Vector2 side = Vector2.Perpendicular(direction);
        for (int i = 0; i < pending.trail.Count; i++)
        {
            float t = (i + 1f) / pending.trail.Count;
            RectTransform puff = pending.trail[i].rectTransform;
            float wiggle = Mathf.Sin(GameClock.Time * 30f + i * 1.3f) * size * .08f;
            puff.anchoredPosition = rock.anchoredPosition + direction * size * trailStretch * t + side * wiggle;
            puff.sizeDelta = Vector2.one * size * Mathf.Lerp(.95f, .3f, t) * (1f + Mathf.Sin(GameClock.Time * 25f + i) * .1f);
        }
        pending.heat.color = PixelFx.WithAlpha(PixelFx.Orange, heatAlpha * eased);

        pending.trailTimer -= GameClock.DeltaTime;
        if (pending.trailTimer > 0f) return;
        pending.trailTimer = trailInterval;
        float house = VfxKit.House(sky);
        FeelParticles.EmitPreset(VfxKit.FireTrail, sky, rock.anchoredPosition, house * .9f * grow, 3);
        FeelParticles.EmitAt(FxKind.Embers, sky, at, house * 1.2f, 3);
        FeelParticles.EmitPreset(VfxKit.SmokeColumn, sky, rock.anchoredPosition + direction * size * trailStretch * .8f, house * .5f, 1);
    }

    // Por ordem de desenho: luz no quarteirão, halo, cauda (a bolha mais clara por cima) e a rocha.
    private void CreateRock(Pending pending, RectTransform sky, Vector3 target)
    {
        float unit = PixelFx.HouseUnit * (PixelFx.Container != null ? PixelFx.Container.lossyScale.y : 1f);
        pending.rockFrom = target + new Vector3(fallFrom.x * unit, fallFrom.y * unit, 0f);

        pending.heat = PixelFx.CreateSprite("Luz do meteoro", sky, PixelFx.Glow(24, 24, 4), PixelFx.WithAlpha(PixelFx.Orange, 0f));
        pending.heat.raycastTarget = false;
        pending.heat.rectTransform.anchoredPosition = PixelFx.ToLocal(sky, target);
        pending.heat.rectTransform.sizeDelta = pending.incident.area.size / Mathf.Max(1e-6f, sky.lossyScale.x) * 1.3f;
        pending.glow = PixelFx.CreateSprite("Brasa do meteoro", sky, PixelFx.Glow(24, 24, 4), PixelFx.WithAlpha(PixelFx.Orange, .85f));
        pending.glow.raycastTarget = false;

        int length = Mathf.Max(2, trailLength);
        for (int i = 0; i < length; i++) pending.trail.Add(null);
        for (int i = length - 1; i >= 0; i--)
        {
            pending.trail[i] = PixelFx.CreateSprite("Cauda", sky, PixelFx.Puff, TrailColor(i / (length - 1f)));
            pending.trail[i].raycastTarget = false;
        }

        pending.rock = PixelFx.CreateSprite("Meteoro", sky, BlockDisasterArt.Rock, Color.white, rockPixels.x);
        pending.rock.raycastTarget = false;
    }

    private static Color TrailColor(float t)
    {
        if (t < .15f) return PixelFx.Cream;
        if (t < .35f) return PixelFx.Gold;
        if (t < .6f) return PixelFx.Orange;
        if (t < .8f) return PixelFx.WithAlpha(PixelFx.Red, .9f);
        return new Color(.3f, .25f, .25f, .7f);
    }

    private static void Discard(Pending pending)
    {
        if (pending.rock != null) Destroy(pending.rock.gameObject);
        if (pending.glow != null) Destroy(pending.glow.gameObject);
        if (pending.heat != null) Destroy(pending.heat.gameObject);
        foreach (Image puff in pending.trail)
        {
            if (puff != null) Destroy(puff.gameObject);
        }
        pending.trail.Clear();
        if (pending.shadow != null) Destroy(pending.shadow.gameObject);
        if (pending.zone != null) Destroy(pending.zone.gameObject);
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
            Discard(pending);
        }

        Vector3 center = Center(incident);
        Crater crater = new() { incident = incident, born = GameClock.Time };
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

            // fundo em brasa por cima da cratera (esfria com o tempo) e as saídas de fumaça espalhadas por ela
            crater.core = PixelFx.CreateSprite("Brasa da cratera", ground, PixelFx.Glow(24, 24, 4), PixelFx.WithAlpha(PixelFx.Orange, .8f));
            crater.core.raycastTarget = false;
            crater.core.rectTransform.anchoredPosition = crater.image.rectTransform.anchoredPosition;
            crater.core.rectTransform.sizeDelta = size * .55f;
        }
        Rect area = incident.area;
        for (int i = 0; i < Mathf.Max(1, smokeVents); i++)
        {
            float angle = (i / (float)Mathf.Max(1, smokeVents) + Random.Range(-.08f, .08f)) * Mathf.PI * 2f;
            float reach = i == 0 ? 0f : Random.Range(.2f, .32f);
            crater.vents.Add(new Vector3(area.center.x + Mathf.Cos(angle) * area.width * reach, area.center.y + Mathf.Sin(angle) * area.height * reach, center.z));
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

        Explode(center, incident);
    }

    private void UpdateCraters()
    {
        RectTransform sky = PixelFx.Sky;
        float dt = GameClock.DeltaTime;
        if (sky == null || dt <= 0f) return;
        float house = VfxKit.House(sky);

        foreach (Crater crater in craters)
        {
            if (crater.image == null) continue;
            BlockIncident incident = crater.incident;
            House leader = incident.leader;
            bool working = leader != null && leader.IsWorking;
            Rect area = incident.area;
            Vector3 at = new(Random.Range(area.xMin, area.xMax) * .8f + area.center.x * .2f, Random.Range(area.yMin, area.yMax) * .8f + area.center.y * .2f, Center(incident).z);

            // Fundo em brasa: pulsa e esfria; enquanto quente, solta brasas.
            float heat = Mathf.Clamp01(1f - (GameClock.Time - crater.born) / Mathf.Max(1f, coolTime));
            if (crater.core != null)
            {
                float pulse = .75f + .25f * Mathf.Sin(GameClock.Time * 5f);
                crater.core.color = PixelFx.WithAlpha(PixelFx.Orange, Mathf.Lerp(.12f, .8f, heat) * pulse * (working ? 1f - leader.WorkProgress01 : 1f));
            }
            crater.emberTimer -= dt;
            if (crater.emberTimer <= 0f && heat > 0f)
            {
                crater.emberTimer = emberInterval / Mathf.Max(.2f, heat);
                FeelParticles.EmitPreset(VfxKit.HotEmbers, sky, PixelFx.ToLocal(sky, at), house, 2);
            }

            // Colunas de fumaça, uma saída por vez (mais fracas com a obra andando).
            crater.smokeTimer -= dt;
            if (crater.smokeTimer <= 0f && crater.vents.Count > 0)
            {
                crater.smokeTimer = smokeInterval * (working ? 1.8f : 1f);
                Vector3 vent = crater.vents[crater.nextVent++ % crater.vents.Count];
                FeelParticles.EmitPreset(VfxKit.SmokeColumn, sky, PixelFx.ToLocal(sky, vent), house * Mathf.Lerp(.7f, 1.1f, heat), 1);
            }

            // Obra: solda e poeira; com a aceleração dos acertos as fagulhas saem mais rápido.
            if (working)
            {
                crater.weldTimer -= dt * BlockDisasterRules.RebuildSpeed(incident.Boost);
                crater.image.color = new Color(1f, 1f, 1f, 1f - .55f * leader.WorkProgress01);
                if (crater.weldTimer <= 0f)
                {
                    crater.weldTimer = weldInterval;
                    FeelParticles.EmitAt(FxKind.Weld, sky, at, house * .6f, 5);
                    FeelParticles.EmitAt(FxKind.Dust, sky, at, house * .4f, 2);
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
        if (crater.core != null) Destroy(crater.core.gameObject);
        foreach (Image rod in crater.rods)
        {
            if (rod != null) rod.enabled = true;
        }

        // As casas voltam uma a uma, quicando, com poeira (o confete e a coluna de luz são da BuildDoneFx).
        float house = VfxKit.House(PixelFx.Sky);
        for (int i = 0; i < crater.hidden.Count; i++)
        {
            CanvasGroup group = crater.hidden[i];
            if (group == null) continue;
            Transform visual = group.transform;
            float delay = i * rebuildStagger;
            group.DOFade(1f, .15f).SetDelay(delay).SetLink(group.gameObject);
            visual.localScale = new Vector3(1f, .2f, 1f);
            visual.DOScale(1f, .35f).SetDelay(delay).SetEase(Ease.OutBack).SetLink(group.gameObject);
            if (PixelFx.Sky != null) FeelParticles.EmitAt(FxKind.Dust, PixelFx.Sky, visual.position, house * .7f, 6);
        }
    }

    // Fim de partida no meio do aviso: a sombra e a rocha somem.
    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.Won && state != GameState.Lost) return;

        foreach (Pending pending in pendings) Discard(pending);
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
