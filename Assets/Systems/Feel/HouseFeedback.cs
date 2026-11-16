using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//Um por casa (entra no House.prefab). Escuta HouseStateChanged da própria casa:
//ao quebrar, sacudida, rachadura com fade e poeira; ao consertar, pulo, brilho e a rachadura some.
[RequireComponent(typeof(House))]
public class HouseFeedback : MonoBehaviour
{
    [Header("REFERENCES (vazio = acha pelo nome no prefab)")]
    [SerializeField] private Image visual;
    [SerializeField] private Transform callMarker;
    [SerializeField] private Transform completeMarker;

    [Header("BREAK (frações do tamanho da casa)")]
    [SerializeField] private float shakeStrength = .06f;
    [SerializeField] private float shakeTime = .5f;
    [SerializeField] private int shakeVibrato = 18;
    [SerializeField] private float crackInTime = .35f;

    [Header("PARTICLES (escala sobre o tamanho da casa; a receita fica no FeelParticles)")]
    [SerializeField] private float dustScale = 1f;
    [SerializeField] private float debrisScale = 1f;
    [SerializeField] private float sparkleScale = 1f;
    [SerializeField] private float steamScale = 1f;
    [SerializeField] private float smokeScale = 1f;

    [Header("REPAIR (frações do tamanho da casa)")]
    [SerializeField] private float hopHeight = .12f;
    [SerializeField] private float hopTime = .35f;
    [SerializeField] private float crackOutTime = .25f;
    [SerializeField] private float flashStrength = .8f;
    [SerializeField] private float flashTime = .3f;

    [Header("WORK (equipe trabalhando na casa)")]
    [SerializeField] private bool workParticles = true;
    [SerializeField] private float workInterval = .55f;
    [SerializeField] private float workScale = .6f;
    [SerializeField] private int workDebrisCount = 2;
    [SerializeField] private int workDustCount = 2;
    [Tooltip("Fagulhas de solda a cada batida, no alto da casa")]
    [SerializeField] private int workWeldCount = 3;

    [Header("FIRE")]
    [SerializeField] private float igniteShakeStrength = .03f;
    [SerializeField] private float igniteShakeTime = .3f;

    [Header("FIRE VISUAL (chamas, fumaça e temporizador)")]
    [SerializeField] private HouseFire.Settings fire = new HouseFire.Settings();

    [Header("STATUS (anel de estado no chão: tipo, urgência, equipe, progresso, sucesso e falha)")]
    [SerializeField] private bool statusRing = true;
    [SerializeField] private HouseStatusFx.Settings status = new HouseStatusFx.Settings();

    [Header("ACTION DENIED")]
    [SerializeField] private float deniedShakeStrength = .035f;
    [SerializeField] private float deniedShakeTime = .25f;

    [Header("MARKERS")]
    [SerializeField] private Color damagedMarkerColor = Color.white;
    [SerializeField] private Color fireMarkerColor = new Color(1f, .55f, .4f);
    [SerializeField] private float callPulseScale = 1.15f;
    [SerializeField] private float callPulseTime = .45f;
    [SerializeField] private float completePulseScale = 1.25f;
    [SerializeField] private float completePulseTime = .3f;

    private static readonly int CrackFade = Shader.PropertyToID("_TextureLayer1Fade");
    private static readonly int StrongTintFade = Shader.PropertyToID("_StrongTintFade");
    //O brilho branco só existe se o HouseMAT estiver com Strong Tint ligado (variante do shader no build)
    private const string StrongTintKeyword = "_ENABLESTRONGTINT_ON";

    private House house;
    private RectTransform visualRect;
    private Vector2 visualPosition;
    private Image callImage;
    private float crack;
    private float flash;
    private Tween crackTween;
    private Tween moveTween;
    private Tween flashTween;
    private HouseFire fireFx;
    private HouseStatusFx statusFx;
    private bool working;
    private float workTimer;

    private float HouseSize => transform is RectTransform rect ? rect.rect.height : 1f;

    private void Awake()
    {
        house = GetComponent<House>();
        //A partir daqui a House não mexe mais na rachadura do shader
        house.externalFeedback = true;

        if (visual == null)
        {
            Transform found = transform.Find("Visual");
            if (found != null) visual = found.GetComponent<Image>();
        }

        if (callMarker == null) callMarker = transform.Find("Canvas/Call");
        if (completeMarker == null) completeMarker = transform.Find("Canvas/Complete");

        if (visual != null)
        {
            visualRect = visual.rectTransform;
            visualPosition = visualRect.anchoredPosition;
        }

        if (callMarker != null) callImage = callMarker.GetComponent<Image>();

        AddPulse(callMarker, callPulseScale, callPulseTime);
        AddPulse(completeMarker, completePulseScale, completePulseTime);
    }

    private void AddPulse(Transform marker, float scale, float time)
    {
        if (marker == null || marker.GetComponent<MarkerPulse>() != null) return;

        MarkerPulse pulse = marker.gameObject.AddComponent<MarkerPulse>();
        pulse.pulseScale = scale;
        pulse.pulseTime = time;
    }

    private void Start()
    {
        //Casa que já começa quebrada na cena
        if (house.currentState == State.Damaged)
        {
            crack = 1f;
            ApplyCrack();
        }

        if (statusRing && statusFx == null) statusFx = HouseStatusFx.Attach(house, status);

        ApplyMarkerColor(house.currentState);
        SetFire(house.currentState == State.OnFire);
    }

    //O fogo só é criado na primeira vez que a casa queima
    private void SetFire(bool on)
    {
        if (on && fireFx == null)
        {
            fireFx = HouseFire.Create(house, fire);
            if (statusFx != null) statusFx.SetFire(fireFx);
        }

        if (fireFx != null) fireFx.gameObject.SetActive(on);
    }

    private void OnEnable()
    {
        GameEvents.HouseStateChanged += OnHouseStateChanged;
        GameEvents.ActionDenied += OnActionDenied;
        GameEvents.WorkStarted += OnWorkStarted;
        GameEvents.WorkCompleted += OnWorkCompleted;
    }

    private void OnDisable()
    {
        GameEvents.HouseStateChanged -= OnHouseStateChanged;
        GameEvents.ActionDenied -= OnActionDenied;
        GameEvents.WorkStarted -= OnWorkStarted;
        GameEvents.WorkCompleted -= OnWorkCompleted;
        working = false;
    }

    private void OnWorkStarted(Call call)
    {
        if (call == null || call.house != house) return;

        working = true;
        workTimer = 0f;
    }

    private void OnWorkCompleted(Call call)
    {
        if (call != null && call.house == house) working = false;
    }

    //Enquanto a equipe trabalha, lascas e poeirinha no ritmo do martelo do GameAudio
    private void Update()
    {
        if (!working || !workParticles) return;

        workTimer -= Time.deltaTime;
        if (workTimer > 0f) return;

        workTimer = workInterval;
        float size = HouseSize;
        Vector2 spot = new Vector2(Random.Range(-.25f, .25f) * size, -size * .15f);
        FeelParticles.Emit(FxKind.Debris, transform, spot, size * workScale * debrisScale, workDebrisCount);
        FeelParticles.Emit(FxKind.Dust, transform, spot, size * workScale * dustScale, workDustCount);

        //Solda no alto da casa: o conserto se vê de longe
        Vector2 weld = new Vector2(Random.Range(-.2f, .2f) * size, size * Random.Range(.05f, .2f));
        FeelParticles.Emit(FxKind.Weld, transform, weld, size * workScale, workWeldCount, 90f);
    }

    //O GuardManager passa a posição da casa do chamado: ela treme no lugar do clique negado
    private void OnActionDenied(DeniedReason reason, Vector3 worldPosition)
    {
        if (worldPosition == transform.position) Shake(deniedShakeStrength, deniedShakeTime);
    }

    private void OnHouseStateChanged(House changed, State before, State after)
    {
        if (changed != house || before == after) return;

        ApplyMarkerColor(after);
        SetFire(after == State.OnFire);

        if (after != State.Damaged && after != State.OnFire) working = false;

        switch (after)
        {
            case State.Damaged:
                //Fogo ignorado: a fumaça preta sobe antes da rachadura
                if (before == State.OnFire) FeelParticles.Emit(FxKind.Smoke, transform, Vector2.zero, HouseSize * smokeScale);
                PlayBreak();
                break;
            case State.OnFire:
                Shake(igniteShakeStrength, igniteShakeTime);
                FeelParticles.Emit(FxKind.Sparks, transform, Vector2.zero, HouseSize * sparkleScale, 6);
                break;
            case State.Intact:
                if (before == State.OnFire) FeelParticles.Emit(FxKind.Steam, transform, new Vector2(0f, HouseSize * .1f), HouseSize * steamScale);
                PlayRepair();
                break;
        }
    }

    private void PlayBreak()
    {
        float size = HouseSize;
        Vector2 ground = new Vector2(0f, -size * .3f);

        Shake(shakeStrength, shakeTime);
        FadeCrack(1f, crackInTime);
        FeelParticles.Emit(FxKind.Dust, transform, ground, size * dustScale);
        FeelParticles.Emit(FxKind.Debris, transform, new Vector2(0f, size * .1f), size * debrisScale);
    }

    private void PlayRepair()
    {
        float size = HouseSize;

        FadeCrack(0f, crackOutTime);
        Hop(size * hopHeight);
        Flash();
        FeelParticles.Emit(FxKind.Sparks, transform, Vector2.zero, size * sparkleScale);
    }

    //Mexe só na posição do Visual: a escala é do Tweener (hover e clique)
    private void Shake(float strength, float time)
    {
        if (visualRect == null) return;

        ResetMove();
        moveTween = visualRect.DOShakeAnchorPos(time, HouseSize * strength, shakeVibrato).SetLink(gameObject).OnComplete(ResetMove);
    }

    private void Hop(float height)
    {
        if (visualRect == null) return;

        ResetMove();
        moveTween = visualRect.DOPunchAnchorPos(Vector2.up * height, hopTime, 4, .5f).SetLink(gameObject).OnComplete(ResetMove);
    }

    private void ResetMove()
    {
        moveTween?.Kill();
        moveTween = null;
        visualRect.anchoredPosition = visualPosition;
    }

    private void FadeCrack(float target, float time)
    {
        crackTween?.Kill();
        crackTween = DOTween.To(() => crack, x => { crack = x; ApplyCrack(); }, target, time).SetLink(gameObject);
    }

    //visual.material é a cópia que a House cria no Awake (uma por casa); lido na hora, nunca guardado antes disso
    private void ApplyCrack()
    {
        if (visual != null) visual.material.SetFloat(CrackFade, crack);
    }

    private void Flash()
    {
        if (visual == null || !visual.material.IsKeywordEnabled(StrongTintKeyword)) return;

        flashTween?.Kill();
        flash = flashStrength;
        flashTween = DOTween.To(() => flash, x => { flash = x; visual.material.SetFloat(StrongTintFade, flash); }, 0f, flashTime).SetLink(gameObject);
    }

    private void ApplyMarkerColor(State state)
    {
        if (callImage == null) return;

        if (state == State.OnFire) callImage.color = fireMarkerColor;
        else if (state == State.Damaged) callImage.color = damagedMarkerColor;
    }
}
