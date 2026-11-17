using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Apresentação dos mini games (GAMELOOP_REVISAO): a janela dourada do ponto certo no círculo de progresso,
// os textos de acerto e erro, os alvos do terremoto para escorar e os sons com o tom subindo em sequência.
// Só escuta GameEvents e lê o estado das casas. Nasce sozinha pelo GameloopBootstrap se a cena não tiver uma.
public class MiniGameFeel : MonoBehaviour
{
    [Header("PONTO CERTO")]
    [SerializeField] private Color critColor = new Color(1f, .8f, .2f, .9f);
    [SerializeField] private Color critHotColor = new Color(1f, 1f, .75f, 1f);
    [SerializeField] private float critHotScale = 1.12f;
    [SerializeField] private float critFadeTime = .2f;

    [Header("POPUPS")]
    [SerializeField] private float popupSize = 36f;
    [SerializeField] private float bigPopupSize = 48f;
    [SerializeField] private Vector2 popupOffset = new Vector2(0f, 40f);
    [SerializeField] private float stackWidth = 260f;
    [SerializeField] private float popupRise = 70f;
    [SerializeField] private float popupTime = .9f;
    [SerializeField] private Color criticalColor = new Color(1f, .85f, .2f);
    [SerializeField] private Color missColor = new Color(1f, .4f, .35f);
    [SerializeField] private Color fireColor = new Color(.5f, .85f, 1f);
    [SerializeField] private Color heroColor = new Color(1f, .62f, .2f);
    [SerializeField] private Color shoreColor = new Color(.55f, 1f, .55f);

    [Header("TEXTS")]
    [SerializeField] private string criticalFormat = "NO PONTO! +{0}%";
    [SerializeField] private string missText = "FORA DO PONTO";
    [SerializeField] private string fireOutFormat = "APAGOU! +{0}%";
    [SerializeField] private string fireGrewText = "O FOGO CRESCEU!";
    [SerializeField] private string shoredText = "ESCORADA!";
    [SerializeField] private string shoreHeldFormat = "FICOU DE PÉ! +{0}";
    [SerializeField] private string shoreHint = "TERREMOTO VINDO: CLIQUE NAS CASAS MARCADAS PARA ESCORAR ({0} MATERIAL)";

    [Header("SHORING TARGETS")]
    [SerializeField] private Color targetColor = new Color(1f, .45f, .2f, .95f);
    [SerializeField] private float targetSize = 110f;
    [SerializeField] private float targetPulse = .18f;
    [SerializeField] private float targetPulseSpeed = 7f;
    [SerializeField] private float hintTop = 196f;
    [SerializeField] private float hintSize = 28f;
    [SerializeField] private Color hintBackColor = new Color(.16f, .15f, .18f, .9f);

    [Header("SOUND")]
    [SerializeField, Range(0f, 1f)] private float volume = .8f;
    [Tooltip("Cada acerto seguido sobe o tom este tanto; volta ao normal depois de comboTime sem acertar.")]
    [SerializeField] private float pitchStep = .07f;
    [SerializeField] private float pitchMax = 1.5f;
    [SerializeField] private float comboTime = 3f;

    private const string PopupLayer = "MiniGamePopups";
    private const string SoundPath = "MiniGames/";

    private readonly Dictionary<House, Image> arcs = new();
    private readonly Dictionary<House, Image> targets = new();
    private readonly List<House> scratch = new();
    private readonly Stack<TMP_Text> pool = new();
    private readonly List<Vector3> recent = new(); // x, y e hora dos popups que ainda sobem
    private readonly AudioSource[] sources = new AudioSource[4];
    private int nextSource;
    private int combo;
    private float lastHitTime = float.NegativeInfinity;
    private TMP_Text hintLbl;

    private AudioClip critClip, missClip, fireClip, heroClip, shoredClip, heldClip;

    private void Awake()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].playOnAwake = false;
        }

        critClip = Resources.Load<AudioClip>(SoundPath + "MG_Critical");
        missClip = Resources.Load<AudioClip>(SoundPath + "MG_Miss");
        fireClip = Resources.Load<AudioClip>(SoundPath + "MG_FireClick");
        heroClip = Resources.Load<AudioClip>(SoundPath + "MG_FireHero");
        shoredClip = Resources.Load<AudioClip>(SoundPath + "MG_Shored");
        heldClip = Resources.Load<AudioClip>(SoundPath + "MG_ShoreHeld");
    }

    private void OnEnable()
    {
        GameEvents.WorkStarted += OnWorkStarted;
        GameEvents.SkillCheck += OnSkillCheck;
    }

    private void OnDisable()
    {
        GameEvents.WorkStarted -= OnWorkStarted;
        GameEvents.SkillCheck -= OnSkillCheck;
    }

    private void Update()
    {
        UpdateArcs();
        UpdateTargets();
    }

    #region PONTO CERTO

    // A janela vira um arco dourado por cima do círculo de progresso, no mesmo sprite e no mesmo sentido.
    private void OnWorkStarted(Call call)
    {
        House house = call != null ? call.house : null;
        if (house == null || !house.CritAvailable || house.ProgressCircle == null) return;

        Image arc = GetArc(house);
        Image circle = house.ProgressCircle;
        float sign = circle.fillClockwise ? -1f : 1f;

        arc.fillAmount = house.CritWindowSize;
        arc.rectTransform.localEulerAngles = new Vector3(0f, 0f, sign * house.CritWindowStart * 360f);
        arc.rectTransform.localScale = Vector3.one;
        arc.color = critColor;
        arc.DOKill();
        arc.gameObject.SetActive(true);
    }

    private Image GetArc(House house)
    {
        if (arcs.TryGetValue(house, out Image arc) && arc != null) return arc;

        Image circle = house.ProgressCircle;
        arc = FeelUI.CreateImage("CritWindow", circle.transform, critColor, circle.sprite);
        arc.type = Image.Type.Filled;
        arc.fillMethod = circle.fillMethod;
        arc.fillOrigin = circle.fillOrigin;
        arc.fillClockwise = circle.fillClockwise;
        arc.preserveAspect = circle.preserveAspect;
        FeelUI.Stretch(arc.rectTransform);
        arcs[house] = arc;
        return arc;
    }

    private void UpdateArcs()
    {
        foreach (KeyValuePair<House, Image> pair in arcs)
        {
            House house = pair.Key;
            Image arc = pair.Value;
            if (house == null || arc == null || !arc.gameObject.activeSelf || DOTween.IsTweening(arc)) continue;

            if (!house.IsWorking || !house.CritAvailable)
            {
                arc.DOFade(0f, critFadeTime).SetLink(arc.gameObject).OnComplete(() => arc.gameObject.SetActive(false));
                continue;
            }

            float progress = house.WorkProgress01;
            bool inside = progress >= house.CritWindowStart && progress <= house.CritWindowStart + house.CritWindowSize;
            arc.color = inside ? critHotColor : critColor;
            float pulse = inside ? critHotScale + .04f * Mathf.Sin(GameClock.Time * 20f) : 1f;
            arc.rectTransform.localScale = Vector3.one * pulse;
        }
    }

    #endregion

    #region SKILL CHECKS

    private void OnSkillCheck(House house, SkillResult result)
    {
        if (house == null) return;

        BalanceData balance = Stats.Balance;
        Vector2 center = HudCenter(house.transform, out Vector2 size);
        Vector2 at = center + Vector2.up * size.y * .5f;

        switch (result)
        {
            case SkillResult.Critical:
                Popup(string.Format(criticalFormat, Percent(balance.critRewardBonus)), at, criticalColor, bigPopupSize, true);
                PlayHit(critClip);
                FlashArc(house);
                break;
            case SkillResult.Miss:
                Popup(missText, at, missColor, popupSize, false);
                Play(missClip, 1f);
                combo = 0;
                break;
            // Jato no incêndio (ENERGIA_FOGO.md): cada chama só toca o som subindo; o FireMinigame desenha o vapor.
            case SkillResult.FireDouse:
                PlayHit(fireClip);
                break;
            case SkillResult.FireOut:
                Popup(string.Format(fireOutFormat, Percent(FireEnergyData.Instance.jetOutBonus)), at, fireColor, bigPopupSize, true);
                PlayHit(heroClip);
                break;
            case SkillResult.FireGrew:
                Popup(fireGrewText, at, heroColor, popupSize, false);
                Play(missClip, .8f);
                combo = 0;
                break;
            case SkillResult.Shored:
                Popup(shoredText, at, shoreColor, popupSize, false);
                PlayHit(shoredClip);
                break;
            case SkillResult.ShoreHeld:
                Popup(string.Format(shoreHeldFormat, balance.shoreHeldReward), at, shoreColor, bigPopupSize, true);
                PlayHit(heldClip);
                break;
        }
    }

    private static int Percent(float bonus) => Mathf.RoundToInt(bonus * 100f);

    private static readonly Vector3[] corners = new Vector3[4];

    // Centro e tamanho no HUD de um objeto do mundo. A casa é Image do Canvas World Space com o pivô no pé:
    // o centro sai do retângulo, não do transform.position. Também usado pelo tutorial.
    public static Vector2 HudCenter(Transform target, out Vector2 size)
    {
        size = Vector2.zero;
        if (target == null) return Vector2.zero;
        if (target is not RectTransform rect) return FeelUI.WorldToHud(target.position);

        rect.GetWorldCorners(corners);
        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;
        foreach (Vector3 corner in corners)
        {
            Vector2 point = FeelUI.WorldToHud(corner);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        size = max - min;
        return (min + max) * .5f;
    }

    // O arco acertado estoura em branco antes do círculo sumir.
    private void FlashArc(House house)
    {
        if (!arcs.TryGetValue(house, out Image arc) || arc == null) return;

        arc.DOKill();
        arc.color = Color.white;
        arc.rectTransform.DOScale(1.5f, .25f).SetEase(Ease.OutQuad).SetLink(arc.gameObject);
        arc.DOFade(0f, .25f).SetLink(arc.gameObject).OnComplete(() => arc.gameObject.SetActive(false));
    }

    private void Popup(string text, Vector2 hudPosition, Color color, float size, bool big)
    {
        RectTransform layer = FeelUI.HudLayer(PopupLayer);
        if (layer == null) return;

        TMP_Text popup = GetPopup(layer);
        popup.text = text;
        popup.color = color;
        popup.fontSize = size;

        RectTransform rect = popup.rectTransform;
        Vector2 from = Unstack(hudPosition + popupOffset, size);
        rect.anchoredPosition = from;
        rect.localScale = Vector3.zero;
        rect.localEulerAngles = new Vector3(0f, 0f, big ? Random.Range(-6f, 6f) : 0f);

        Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(popup.gameObject);
        sequence.Append(rect.DOScale(big ? 1.25f : 1f, .18f).SetEase(Ease.OutBack));
        if (big) sequence.Append(rect.DOScale(1f, .12f));
        sequence.Join(rect.DOAnchorPos(from + Vector2.up * popupRise, popupTime).SetEase(Ease.OutCubic));
        sequence.Insert(popupTime * .6f, popup.DOFade(0f, popupTime * .4f));
        sequence.OnComplete(() => Release(popup));
    }

    // Casas vizinhas pontuam juntas: o popup novo sobe acima do recente que estiver no caminho.
    private Vector2 Unstack(Vector2 at, float size)
    {
        float now = Time.unscaledTime;
        recent.RemoveAll(r => now - r.z > popupTime * .6f);
        for (int i = 0; i < recent.Count; i++)
        {
            Vector3 other = recent[i];
            if (Mathf.Abs(other.x - at.x) < stackWidth && Mathf.Abs(other.y - at.y) < size + 6f)
            {
                at.y = other.y + size + 6f;
                i = -1;
            }
        }
        recent.Add(new Vector3(at.x, at.y, now));
        return at;
    }

    private TMP_Text GetPopup(RectTransform layer)
    {
        while (pool.Count > 0)
        {
            TMP_Text pooled = pool.Pop();
            if (pooled == null) continue;

            pooled.gameObject.SetActive(true);
            pooled.transform.SetAsLastSibling();
            return pooled;
        }

        TextMeshProUGUI label = FeelUI.CreateText("MiniGamePopup", layer, "", popupSize, Color.white);
        FeelUI.Outline(label);
        label.rectTransform.sizeDelta = new Vector2(600f, 80f);
        return label;
    }

    private void Release(TMP_Text popup)
    {
        if (popup == null) return;

        popup.gameObject.SetActive(false);
        pool.Push(popup);
    }

    #endregion

    #region SHORING TARGETS

    // Durante o aviso do terremoto: um aro pulsando em cada casa que ainda dá para escorar, verde nas escoradas.
    private void UpdateTargets()
    {
        DisasterManager disasters = DisasterManager.Instance;
        DisasterForecast forecast = disasters != null ? disasters.CurrentForecast : null;
        bool active = forecast != null && forecast.disaster != null && forecast.disaster.type == DisasterType.Earthquake;

        scratch.Clear();
        foreach (House house in targets.Keys) scratch.Add(house);

        foreach (House house in scratch)
        {
            if (!active || house == null || !forecast.targets.Contains(house)) HideTarget(house);
        }

        if (!active)
        {
            if (hintLbl != null) hintLbl.transform.parent.gameObject.SetActive(false);
            return;
        }

        // Pulso no brilho, não na escala: o aro pixel esticado por fração quadriculava (#535)
        float pulse = 1f - targetPulse * 2f * (.5f + .5f * Mathf.Sin(GameClock.Time * targetPulseSpeed));
        bool any = false;

        foreach (House house in forecast.targets)
        {
            bool canShore = disasters.CanShore(house);
            if (house == null || (!canShore && !house.IsShored)) { HideTarget(house); continue; }

            Image ring = GetTarget(house);
            ring.rectTransform.anchoredPosition = HudCenter(house.transform, out Vector2 size);
            ring.rectTransform.sizeDelta = Vector2.one * RingSize(size, targetSize);
            Color color = house.IsShored ? shoreColor : targetColor;
            if (!house.IsShored) color.a *= pulse;
            ring.color = color;
            any |= canShore;
        }

        ShowHint(any);
    }

    private Image GetTarget(House house)
    {
        if (targets.TryGetValue(house, out Image ring) && ring != null)
        {
            ring.gameObject.SetActive(true);
            return ring;
        }

        // O aro fino do tutorial (Mk_Ring, Simple e quadrado): o Button2 fatiado esticava e quadriculava (#535)
        RectTransform layer = FeelUI.HudLayer(PopupLayer);
        ring = FeelUI.CreateRing("ShoreTarget", layer, targetColor);
        ring.rectTransform.sizeDelta = Vector2.one * targetSize;
        ring.transform.SetAsFirstSibling();
        targets[house] = ring;
        return ring;
    }

    // Aro um pouco maior que a casa, sem sumir nem cobrir a quadra quando a câmera aproxima ou afasta. O lado cai no
    // passo de 32 do sprite (FeelUI.RingSide): cada pixel do aro vira um número inteiro de pixels do HUD.
    public static float RingSize(Vector2 targetSize, float fallback)
    {
        float biggest = Mathf.Max(targetSize.x, targetSize.y);
        return biggest > 0f ? FeelUI.RingSide(targetSize) : FeelUI.RingSide(Vector2.one * fallback, 0f);
    }

    private void HideTarget(House house)
    {
        if (ReferenceEquals(house, null) || !targets.TryGetValue(house, out Image ring)) return;

        if (ring != null) ring.gameObject.SetActive(false);
        if (house == null) targets.Remove(house);
    }

    private void ShowHint(bool show)
    {
        if (hintLbl == null)
        {
            RectTransform layer = FeelUI.HudLayer(PopupLayer);
            if (layer == null) return;

            // Faixa escura atrás: texto direto sobre a cidade fica poluído (prévia B3).
            Image back = FeelUI.CreatePanel("ShoreHint", layer, hintBackColor);
            FeelUI.Place(back.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -hintTop), new Vector2(0f, hintSize * 2f));
            hintLbl = FeelUI.CreateText("Lbl", back.transform, "", hintSize, Color.white);
            FeelUI.Stretch(hintLbl.rectTransform);
        }

        hintLbl.text = string.Format(shoreHint, Stats.Balance.shoreMaterialCost);
        RectTransform hint = (RectTransform)hintLbl.transform.parent;
        hint.sizeDelta = new Vector2(hintLbl.GetPreferredValues(hintLbl.text).x + hintSize * 1.5f, hint.sizeDelta.y);
        hint.gameObject.SetActive(show);
    }

    #endregion

    #region SOUND

    // Acertos em sequência sobem o tom (combo); um erro ou uma pausa longa zera.
    private void PlayHit(AudioClip clip)
    {
        float now = Time.unscaledTime;
        combo = now - lastHitTime <= comboTime ? combo + 1 : 0;
        lastHitTime = now;
        Play(clip, Mathf.Min(1f + combo * pitchStep, pitchMax));
    }

    private void Play(AudioClip clip, float pitch)
    {
        if (clip == null) return;

        AudioSource source = sources[nextSource];
        nextSource = (nextSource + 1) % sources.Length;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }

    #endregion
}
