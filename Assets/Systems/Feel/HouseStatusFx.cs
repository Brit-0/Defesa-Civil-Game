using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//O estado da casa lido no chão, embaixo do desenho (nunca cobre a casa nem os ícones):
//- problema: aro que pulsa e solta ondas, com forma e cor do tipo (fogo laranja com pontas, que avermelha;
//  dano creme quebrado; tempestade azul em zigue-zague); as ondas aceleram com a urgência;
//- equipe a caminho: aro tracejado que anda; conserto: anel de gomos dourados que acende com o progresso;
//- pronto: aro verde pulsando; sucesso: onda dourada e brilhos verdes; falha: onda vermelha;
//- rachaduras no chão (terremoto) ou marca de queimado (raio, fogo que virou dano) enquanto a casa está danificada.
//Criado pelo HouseFeedback. Desenha num filho da própria casa, antes do Visual.
public class HouseStatusFx : MonoBehaviour
{
    [System.Serializable]
    public class Settings
    {
        [Header("RING (frações do tamanho da casa; no pé do prédio, sem encostar no vizinho)")]
        public Vector2 ringSize = new Vector2(.9f, .5f);
        public float ringY = -.42f;
        [Tooltip("Traço do aro em pixels da arte, em qualquer zoom (CONSERTOS_V7A: o zoom afastado somava 1 e poluía; 1 abre buracos na elipse)")]
        public int ringThickness = 2;
        [Tooltip("Com menos pixels de tela por pixel da arte que isto (zoom afastado), o halo acende um pouco mais")]
        public float farZoomPixels = 1.6f;

        [Header("COLORS (paleta do kit)")]
        public Color fireColor = PixelFx.Orange;
        public Color fireUrgentColor = PixelFx.Red;
        public Color damageColor = PixelFx.LightGray;
        public Color stormColor = PixelFx.Blue;
        public Color progressColor = PixelFx.Gold;
        public Color progressBackColor = new Color(.08f, .07f, .1f, .7f);
        public Color readyColor = PixelFx.Green;
        public Color successColor = PixelFx.Gold;
        public Color failColor = PixelFx.Red;
        [Range(0f, 1f)] public float haloAlpha = .18f;
        [Range(0f, 1f)] public float haloUrgentAlpha = .35f;
        [Tooltip("Halo vezes isto no zoom afastado (era 1,4)")]
        public float farHaloBoost = 1.2f;

        [Header("URGENCY (ondas por segundo e força do pulso)")]
        public float calmInterval = 1.5f;
        public float urgentInterval = .35f;
        public float pingScale = 1.3f;
        public float pingTime = .7f;
        [Range(0f, 1f)] public float pingAlpha = .5f;
        public float pulseAmount = .06f;
        [Tooltip("Segundos de espera até a casa danificada chegar à urgência máxima")]
        public float damageUrgencyTime = 45f;
        [Range(0f, 1f)] public float damageUrgencyFloor = .15f;

        [Header("TEAM")]
        public float marchInterval = .22f;
        [Range(0f, 1f)] public float enRouteAlpha = .85f;

        [Header("FLASH (sucesso, falha, negado)")]
        public float flashScale = 1.8f;
        public float flashTime = .55f;

        [Header("GROUND MARKS")]
        public int crackCount = 2;
        public float crackSize = .45f;
        public float markFadeTime = .6f;
    }

    private enum Phase { Hidden, Problem, EnRoute, Working, Ready }
    private enum Cause { Earthquake, Storm, Fire }

    private House house;
    private Settings settings;
    private RectTransform root;
    private RectTransform persistent;
    private Image halo;
    private Image ring;
    private Image[] pings;
    private float[] pingAge;
    private Image progressBack;
    private Image progressFill;
    private Image flash;
    private RectTransform marks;
    private CanvasGroup marksGroup;
    private Image progressSource;
    private HouseFire fire;

    private Phase phase = Phase.Hidden;
    private Cause cause;
    private float problemTime;
    private float pingTimer;
    private float marchTimer;
    private bool marchOdd;
    private int progressStep = -1;
    private float zoomTimer;
    private bool farZoom;
    private Vector2 ringPixels;

    private float HouseSize => house.transform is RectTransform rect ? rect.rect.height : 100f;

    public static HouseStatusFx Attach(House house, Settings settings)
    {
        HouseStatusFx fx = house.gameObject.AddComponent<HouseStatusFx>();
        fx.house = house;
        fx.settings = settings;
        fx.Build();
        return fx;
    }

    public void SetFire(HouseFire houseFire)
    {
        fire = houseFire;
    }

    private void Build()
    {
        //Âncora no centro da casa, antes do Visual: tudo daqui fica embaixo do desenho
        root = FeelUI.CreateRect("Status", house.transform);
        root.SetAsFirstSibling();
        Vector2 center = new Vector2(.5f, .5f);
        float size = HouseSize;
        FeelUI.Place(root, center, center, Vector2.zero, Vector2.one * size);

        marks = FeelUI.CreateRect("Marks", root);
        FeelUI.Place(marks, center, center, Vector2.zero, Vector2.one * size);
        marksGroup = marks.gameObject.AddComponent<CanvasGroup>();
        marksGroup.blocksRaycasts = false;
        marks.gameObject.SetActive(false);

        persistent = FeelUI.CreateRect("Ring", root);
        FeelUI.Place(persistent, center, center, new Vector2(0f, size * settings.ringY), Vector2.one * size);

        float pixel = PixelFx.ArtPixel;
        ringPixels = new Vector2(Mathf.Round(size * settings.ringSize.x / pixel), Mathf.Round(size * settings.ringSize.y / pixel));
        Vector2 ringRect = (ringPixels + Vector2.one * 2f) * pixel;

        halo = FeelUI.CreateImage("Halo", persistent, Color.clear, PixelFx.Glow(24, 15, 3));
        FeelUI.Place(halo.rectTransform, center, center, Vector2.zero, ringRect * 1.15f);

        pings = new Image[2];
        pingAge = new float[2];
        for (int i = 0; i < pings.Length; i++)
        {
            pings[i] = FeelUI.CreateImage("Ping", persistent, Color.clear, RingSprite(RingStyle.Solid, false));
            FeelUI.Place(pings[i].rectTransform, center, center, Vector2.zero, ringRect);
            pingAge[i] = 99f;
        }

        ring = FeelUI.CreateImage("Ring", persistent, Color.white, RingSprite(RingStyle.Solid, true));
        FeelUI.Place(ring.rectTransform, center, center, Vector2.zero, ringRect);

        progressBack = FeelUI.CreateImage("ProgressBack", persistent, settings.progressBackColor, RingSprite(RingStyle.Segmented, true));
        FeelUI.Place(progressBack.rectTransform, center, center, Vector2.zero, ringRect);
        progressFill = FeelUI.CreateImage("ProgressFill", persistent, settings.progressColor, RingSprite(RingStyle.Segmented, true));
        FeelUI.Place(progressFill.rectTransform, center, center, Vector2.zero, ringRect);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Radial360;
        progressFill.fillOrigin = (int)Image.Origin360.Top;
        progressFill.fillClockwise = true;

        flash = FeelUI.CreateImage("Flash", root, Color.clear, RingSprite(RingStyle.Solid, true));
        FeelUI.Place(flash.rectTransform, center, center, new Vector2(0f, size * settings.ringY), ringRect);
        flash.gameObject.SetActive(false);

        //O círculo de progresso da própria casa (House.progressCircle): só leitura
        Transform progress = house.transform.Find("Canvas/Progress");
        if (progress != null)
        {
            foreach (Image image in progress.GetComponentsInChildren<Image>(true))
            {
                if (image.type != Image.Type.Filled) continue;
                progressSource = image;
                break;
            }
        }

        persistent.gameObject.SetActive(false);
    }

    private Sprite RingSprite(RingStyle style, bool outline)
    {
        return PixelFx.Ring(Mathf.Max(8, (int)ringPixels.x), Mathf.Max(6, (int)ringPixels.y), settings.ringThickness, style, outline);
    }

    private void OnEnable()
    {
        GameEvents.HouseStateChanged += OnHouseStateChanged;
        GameEvents.WorkCompleted += OnWorkCompleted;
        GameEvents.ActionDenied += OnActionDenied;
    }

    private void OnDisable()
    {
        GameEvents.HouseStateChanged -= OnHouseStateChanged;
        GameEvents.WorkCompleted -= OnWorkCompleted;
        GameEvents.ActionDenied -= OnActionDenied;
    }

    #region EVENTS

    private void OnHouseStateChanged(House changed, State before, State after)
    {
        if (changed != house || before == after || settings == null) return;

        if (after == State.Damaged || after == State.OnFire)
        {
            problemTime = 0f;
            cause = after == State.OnFire || before == State.OnFire ? Cause.Fire
                : changed.IsRoofless ? Cause.Storm : Cause.Earthquake;
        }

        if (after == State.Damaged) ShowMarks(cause == Cause.Earthquake);

        //Fogo que ninguém apagou virou dano: falha
        if (before == State.OnFire && after == State.Damaged)
        {
            Flash(settings.failColor);
            FeelParticles.EmitColored(FxKind.Burst, root, new Vector2(0f, HouseSize * settings.ringY), HouseSize, settings.failColor, 10);
        }

        //Consertada: sucesso
        if (after == State.Intact && (before == State.Damaged || before == State.OnFire))
        {
            HideMarks();
            Flash(settings.successColor);
            FeelParticles.EmitColored(FxKind.Glint, root, new Vector2(0f, HouseSize * .1f), HouseSize, settings.successColor, 6);
            FeelParticles.EmitColored(FxKind.Burst, root, new Vector2(0f, HouseSize * settings.ringY), HouseSize, settings.readyColor, 10);
        }
    }

    private void OnWorkCompleted(Call call)
    {
        if (call == null || call.house != house) return;

        Flash(settings.readyColor);
        FeelParticles.EmitColored(FxKind.Burst, root, new Vector2(0f, HouseSize * settings.ringY), HouseSize, settings.readyColor, 8);
    }

    //O GuardManager passa a posição da casa do chamado negado. Sem settings é a cópia que a expansão faz da Guarda
    //(Instantiate não copia house): sem a guarda, o erro cortava os outros ouvintes, como o "Sem trabalhadores"
    private void OnActionDenied(DeniedReason reason, Vector3 worldPosition)
    {
        if (settings == null || worldPosition != house.transform.position) return;

        Flash(settings.failColor, .6f);
    }

    #endregion

    #region STATE

    private Phase Evaluate()
    {
        if (house.IsComplete) return Phase.Ready;
        if (house.IsWorking) return Phase.Working;
        if (house.HasTeam) return Phase.EnRoute;
        if (house.currentState == State.Damaged || house.currentState == State.OnFire) return Phase.Problem;
        // Quarteirão alagado ou destruído: o anel só no chamado único (ENCHENTE_METEORO.md)
        if (house.IsIncidentLeader) return Phase.Problem;
        return Phase.Hidden;
    }

    private float Urgency()
    {
        if (house.currentState == State.OnFire) return fire != null ? fire.Urgency : 1f - house.FireTimeLeft01;
        return Mathf.Lerp(settings.damageUrgencyFloor, 1f, Mathf.Clamp01(problemTime / Mathf.Max(1f, settings.damageUrgencyTime)));
    }

    private Color ProblemColor(float urgency)
    {
        if (house.currentState == State.OnFire) return Color.Lerp(settings.fireColor, settings.fireUrgentColor, urgency);
        if (house.currentState == State.Flooded) return settings.stormColor;
        return cause == Cause.Storm ? settings.stormColor : settings.damageColor;
    }

    private RingStyle ProblemStyle()
    {
        if (house.currentState == State.OnFire) return RingStyle.Spiky;
        if (house.currentState == State.Flooded) return RingStyle.Zigzag;
        return cause == Cause.Storm ? RingStyle.Zigzag : RingStyle.Broken;
    }

    private void Update()
    {
        if (settings == null) return;

        Phase now = Evaluate();
        if (now != phase) Enter(now);
        if (phase == Phase.Hidden) return;

        float delta = Time.deltaTime;
        UpdateZoom();

        switch (phase)
        {
            case Phase.Problem: UpdateProblem(delta); break;
            case Phase.EnRoute: UpdateEnRoute(delta); break;
            case Phase.Working: UpdateWorking(); break;
            case Phase.Ready: UpdateReady(); break;
        }

        UpdatePings(delta);
    }

    private void Enter(Phase next)
    {
        phase = next;
        progressStep = -1;
        persistent.gameObject.SetActive(next != Phase.Hidden);

        ring.gameObject.SetActive(next != Phase.Working);
        progressBack.gameObject.SetActive(next == Phase.Working);
        progressFill.gameObject.SetActive(next == Phase.Working);
        ring.rectTransform.localScale = Vector3.one;

        for (int i = 0; i < pings.Length; i++)
        {
            pingAge[i] = 99f;
            pings[i].color = Color.clear;
        }

        RefreshSprites();
    }

    private void RefreshSprites()
    {
        switch (phase)
        {
            case Phase.Problem: ring.sprite = RingSprite(ProblemStyle(), true); break;
            case Phase.EnRoute: ring.sprite = RingSprite(marchOdd ? RingStyle.DashedOdd : RingStyle.Dashed, true); break;
            case Phase.Ready: ring.sprite = RingSprite(RingStyle.Solid, true); break;
        }

        progressBack.sprite = progressFill.sprite = RingSprite(RingStyle.Segmented, true);
        foreach (Image ping in pings) ping.sprite = RingSprite(RingStyle.Solid, false);
    }

    //Zoom afastado: halo um pouco mais forte, para continuar legível (o aro não engrossa: CONSERTOS_V7A)
    private void UpdateZoom()
    {
        zoomTimer -= Time.unscaledDeltaTime;
        if (zoomTimer > 0f) return;

        zoomTimer = .5f;
        bool far = PixelFx.ScreenPixelsPerArtPixel(root) < settings.farZoomPixels;
        if (far == farZoom) return;

        farZoom = far;
        RefreshSprites();
    }

    private void UpdateProblem(float delta)
    {
        problemTime += delta;
        float urgency = Urgency();
        Color color = ProblemColor(urgency);
        float interval = Mathf.Lerp(settings.calmInterval, settings.urgentInterval, urgency);

        //Pulso do aro no ritmo das ondas
        float beat = Mathf.Repeat(Time.time / interval, 1f);
        float pulse = 1f + settings.pulseAmount * (1f + urgency) * Mathf.Max(0f, Mathf.Cos(beat * Mathf.PI * 2f));
        ring.rectTransform.localScale = Vector3.one * pulse;
        ring.color = color;

        float haloAlpha = Mathf.Lerp(settings.haloAlpha, settings.haloUrgentAlpha, urgency) * (farZoom ? settings.farHaloBoost : 1f);
        halo.color = PixelFx.WithAlpha(color, Mathf.Clamp01(haloAlpha * Mathf.Lerp(.75f, 1f, pulse - 1f > 0f ? (pulse - 1f) / (settings.pulseAmount * 2f) : 0f)));

        pingTimer -= delta;
        if (pingTimer <= 0f)
        {
            pingTimer = interval;
            Ping();
        }

        foreach (Image ping in pings) ping.color = PixelFx.WithAlpha(color, ping.color.a);
    }

    private void UpdateEnRoute(float delta)
    {
        Color color = house.currentState == State.Intact ? PixelFx.Cream : ProblemColor(0f);
        ring.color = PixelFx.WithAlpha(color, settings.enRouteAlpha);
        halo.color = PixelFx.WithAlpha(color, settings.haloAlpha * .6f);

        marchTimer -= delta;
        if (marchTimer > 0f) return;

        marchTimer = settings.marchInterval;
        marchOdd = !marchOdd;
        ring.sprite = RingSprite(marchOdd ? RingStyle.DashedOdd : RingStyle.Dashed, true);
    }

    private void UpdateWorking()
    {
        float progress = progressSource != null ? progressSource.fillAmount : 0f;
        int step = Mathf.Clamp(Mathf.FloorToInt(progress * 8f + .001f), 0, 8);

        progressFill.fillAmount = step / 8f;
        halo.color = PixelFx.WithAlpha(settings.progressColor, settings.haloAlpha * (.6f + .4f * step / 8f));

        if (step == progressStep) return;

        //Cada gomo novo dá um brilho no lugar dele
        if (progressStep >= 0 && step > progressStep)
        {
            float angle = Mathf.PI * .5f - (step - .5f) / 8f * Mathf.PI * 2f;
            Vector2 radius = ringPixels * PixelFx.ArtPixel * .5f;
            Vector2 at = new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y + HouseSize * settings.ringY);
            FeelParticles.EmitColored(FxKind.Glint, root, at, HouseSize * .7f, settings.progressColor, 2);
        }

        progressStep = step;
    }

    private void UpdateReady()
    {
        float beat = Mathf.Repeat(Time.time * .9f, 1f);
        float pulse = 1f + settings.pulseAmount * Mathf.Max(0f, Mathf.Cos(beat * Mathf.PI * 2f));
        ring.rectTransform.localScale = Vector3.one * pulse;
        ring.color = settings.readyColor;
        halo.color = PixelFx.WithAlpha(settings.readyColor, settings.haloAlpha * (farZoom ? settings.farHaloBoost : 1f));
    }

    private void Ping()
    {
        int oldest = pingAge[0] >= pingAge[1] ? 0 : 1;
        pingAge[oldest] = 0f;
    }

    private void UpdatePings(float delta)
    {
        for (int i = 0; i < pings.Length; i++)
        {
            if (pingAge[i] > settings.pingTime)
            {
                if (pings[i].color.a > 0f) pings[i].color = Color.clear;
                continue;
            }

            pingAge[i] += delta;
            float t = Mathf.Clamp01(pingAge[i] / settings.pingTime);
            pings[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, settings.pingScale, 1f - (1f - t) * (1f - t));
            pings[i].color = PixelFx.WithAlpha(pings[i].color, (1f - t) * settings.pingAlpha);
        }
    }

    #endregion

    #region FLASH AND MARKS

    //Onda única que abre e some: sucesso, falha, pronto
    private void Flash(Color color, float strength = 1f)
    {
        flash.DOKill();
        flash.rectTransform.DOKill();
        flash.gameObject.SetActive(true);
        flash.color = color;
        flash.rectTransform.localScale = Vector3.one * .8f;

        float time = settings.flashTime * Mathf.Lerp(.6f, 1f, strength);
        flash.rectTransform.DOScale(Mathf.Lerp(1.2f, settings.flashScale, strength), time).SetEase(Ease.OutQuad).SetLink(flash.gameObject);
        flash.DOFade(0f, time).SetEase(Ease.InQuad).SetLink(flash.gameObject).OnComplete(() => flash.gameObject.SetActive(false));
    }

    //Rachaduras saindo da base (terremoto) ou mancha de queimado (raio, fogo): ficam até a casa ser consertada
    private void ShowMarks(bool cracks)
    {
        for (int i = marks.childCount - 1; i >= 0; i--) Destroy(marks.GetChild(i).gameObject);

        float size = HouseSize;
        int pixels = Mathf.Max(12, Mathf.RoundToInt(size * settings.crackSize / PixelFx.ArtPixel));
        int seed = house.transform.GetSiblingIndex() % 7;
        int count = cracks ? settings.crackCount : 1;

        for (int i = 0; i < count; i++)
        {
            Sprite sprite = cracks ? PixelFx.Crack(pixels, seed + i) : PixelFx.Scorch(pixels, seed);
            Image mark = PixelFx.CreateSprite("Mark", marks, sprite, Color.white);
            float side = i == 0 ? -1f : 1f;
            mark.rectTransform.anchoredPosition = cracks
                ? new Vector2(side * size * Random.Range(.25f, .4f), size * (settings.ringY - .05f))
                : new Vector2(0f, size * settings.ringY);
        }

        marks.gameObject.SetActive(true);
        marksGroup.DOKill();
        marksGroup.alpha = 0f;
        marksGroup.DOFade(1f, settings.markFadeTime * .5f).SetLink(marks.gameObject);
    }

    private void HideMarks()
    {
        if (!marks.gameObject.activeSelf) return;

        marksGroup.DOKill();
        marksGroup.DOFade(0f, settings.markFadeTime).SetLink(marks.gameObject).OnComplete(() => marks.gameObject.SetActive(false));
    }

    #endregion
}
