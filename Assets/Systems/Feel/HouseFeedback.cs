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
    [SerializeField] private int dustCount = 7;
    [SerializeField] private Color dustColor = new Color(.78f, .72f, .62f, .9f);
    [SerializeField] private float dustSize = .09f;
    [SerializeField] private float dustDistance = .45f;
    [SerializeField] private float dustTime = .6f;

    [Header("REPAIR (frações do tamanho da casa)")]
    [SerializeField] private float hopHeight = .12f;
    [SerializeField] private float hopTime = .35f;
    [SerializeField] private float crackOutTime = .25f;
    [SerializeField] private int sparkleCount = 8;
    [SerializeField] private Color sparkleColor = new Color(1f, .95f, .6f);
    [SerializeField] private float sparkleSize = .06f;
    [SerializeField] private float sparkleDistance = .6f;
    [SerializeField] private float sparkleTime = .5f;
    [SerializeField] private float flashStrength = .8f;
    [SerializeField] private float flashTime = .3f;

    [Header("FIRE")]
    [SerializeField] private float igniteShakeStrength = .03f;
    [SerializeField] private float igniteShakeTime = .3f;

    [Header("FIRE VISUAL (chamas, fumaça e temporizador)")]
    [SerializeField] private HouseFire.Settings fire = new HouseFire.Settings();

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

        ApplyMarkerColor(house.currentState);
        SetFire(house.currentState == State.OnFire);
    }

    //O fogo só é criado na primeira vez que a casa queima
    private void SetFire(bool on)
    {
        if (on && fireFx == null) fireFx = HouseFire.Create(house, fire);
        if (fireFx != null) fireFx.gameObject.SetActive(on);
    }

    private void OnEnable()
    {
        GameEvents.HouseStateChanged += OnHouseStateChanged;
        GameEvents.ActionDenied += OnActionDenied;
    }

    private void OnDisable()
    {
        GameEvents.HouseStateChanged -= OnHouseStateChanged;
        GameEvents.ActionDenied -= OnActionDenied;
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

        switch (after)
        {
            case State.Damaged:
                PlayBreak();
                break;
            case State.OnFire:
                Shake(igniteShakeStrength, igniteShakeTime);
                break;
            case State.Intact:
                PlayRepair();
                break;
        }
    }

    private void PlayBreak()
    {
        float size = HouseSize;

        Shake(shakeStrength, shakeTime);
        FadeCrack(1f, crackInTime);
        FeelFx.Burst(transform, new Vector2(0f, -size * .3f), dustCount, dustColor, size * dustSize, size * dustDistance, dustTime, .35f);
    }

    private void PlayRepair()
    {
        float size = HouseSize;

        FadeCrack(0f, crackOutTime);
        Hop(size * hopHeight);
        Flash();
        FeelFx.Burst(transform, Vector2.zero, sparkleCount, sparkleColor, size * sparkleSize, size * sparkleDistance, sparkleTime, .2f);
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
