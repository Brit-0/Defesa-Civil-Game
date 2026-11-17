using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// DESENTUPIR O BUEIRO (docs/jam/ENCHENTE_METEORO.md): com a equipe no bueiro da enchente, a válvula vermelha aparece
// em cima dele. Segurar o clique perto dela e girar o mouse em círculos sobe a pressão e a água desce; soltar com
// pressão é a falha leve (a pressão zera e a água volta um pouco). Só lê o ponteiro e passa o giro ao BlockIncident;
// a regra é BlockDisasterRules.ValveStep. Fica no mundo (PixelFx.Sky): anda com a câmera e some fora da tela.
public class ValveMinigame : MonoBehaviour
{
    [Header("VÁLVULA")]
    [Tooltip("Tamanho da válvula em relação ao desenho (24 pixels).")]
    [SerializeField] private float valvePixels = 2.4f;
    [Tooltip("Altura da válvula acima do meio do quarteirão, em pixels da arte.")]
    [SerializeField] private float valveLift = 0f;
    [Tooltip("Perto do meio da válvula o ângulo pula: o giro só conta fora deste raio (HUD de 1080).")]
    [SerializeField] private float deadRadius = 14f;
    [SerializeField] private float arrowSpin = 160f;
    [SerializeField] private Color arrowColor = new(1f, .95f, .8f, .9f);

    [Header("PRESSÃO")]
    [SerializeField] private Vector2 gaugeSize = new(34f, 5f);
    [SerializeField] private Color gaugeLow = new(.25f, .5f, 1f);
    [SerializeField] private Color gaugeHigh = new(1f, .82f, .3f);
    [SerializeField] private Color slipColor = new(1f, .35f, .3f);

    [Header("DICA")]
    [SerializeField] private string hintText = "SEGURE E GIRE!";
    [SerializeField] private float labelSize = 16f;
    [SerializeField] private float hintPadding = 3f;
    [SerializeField] private int hintOrder = 10010;
    [SerializeField] private Color hintBack = new(0f, 0f, 0f, .6f);

    private class View
    {
        public BlockIncident incident;
        public RectTransform root;
        public RectTransform valve;
        public Image valveImage;
        public RectTransform arrow;
        public Image gaugeFill;
        public RectTransform hint;
        public bool hasAngle;
        public float lastAngle;
        public float spin = 1f;
    }

    private readonly List<View> views = new();
    private readonly Vector3[] corners = new Vector3[4];
    private static ValveMinigame instance;

    private static BlockDisasterData Data => BlockDisasterData.Instance;

    // Quem está girando agora (o som e a dica perguntam).
    public static bool Turning { get; private set; }
    public static float Pressure { get; private set; }

    // A válvula aberta deste incidente (a dica aponta para ela); nulo antes do ValveOpened.
    public static Transform Of(BlockIncident incident)
    {
        View view = instance != null ? instance.views.Find(v => v.incident == incident) : null;
        return view != null && view.root != null ? view.root : null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        Turning = false;
        Pressure = 0f;
    }

    private void OnEnable()
    {
        instance = this;
        GameEvents.ValveOpened += OnValveOpened;
        GameEvents.SkillCheck += OnSkillCheck;
        GameEvents.BlockIncidentResolved += OnResolved;
    }

    private void OnDisable()
    {
        GameEvents.ValveOpened -= OnValveOpened;
        GameEvents.SkillCheck -= OnSkillCheck;
        GameEvents.BlockIncidentResolved -= OnResolved;
        Turning = false;
    }

    private void OnValveOpened(BlockIncident incident)
    {
        if (incident == null || views.Exists(v => v.incident == incident)) return;
        RectTransform sky = PixelFx.Sky;
        if (sky == null) return;

        float pixel = PixelFx.ArtPixel;
        View view = new() { incident = incident, root = FeelUI.CreateRect("Válvula do bueiro", sky) };

        Image arrow = PixelFx.CreateSprite("Seta", view.root, BlockDisasterArt.TurnArrow, arrowColor, valvePixels);
        view.arrow = arrow.rectTransform;
        arrow.raycastTarget = false;

        view.valveImage = PixelFx.CreateSprite("Volante", view.root, BlockDisasterArt.Valve, Color.white, valvePixels);
        view.valveImage.raycastTarget = false;
        view.valve = view.valveImage.rectTransform;
        view.valve.localScale = Vector3.zero;
        view.valve.DOScale(1f, .25f).SetEase(Ease.OutBack).SetLink(view.valve.gameObject);

        // Barra lisa com 1 pixel de borda escura (como a do jato do incêndio)
        Image back = FeelUI.CreateImage("Pressão", view.root, new Color(0f, 0f, 0f, .7f));
        back.raycastTarget = false;
        back.rectTransform.sizeDelta = gaugeSize * pixel;
        back.rectTransform.anchoredPosition = new Vector2(0f, -(24f * valvePixels * .5f + 4f + gaugeSize.y * .5f) * pixel);
        view.gaugeFill = FeelUI.CreateImage("Fill", back.transform, gaugeLow);
        view.gaugeFill.raycastTarget = false;
        RectTransform fill = view.gaugeFill.rectTransform;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.pivot = new Vector2(0f, .5f);
        fill.offsetMin = new Vector2(pixel, pixel);
        fill.offsetMax = new Vector2(-pixel, -pixel);
        fill.localScale = new Vector3(0f, 1f, 1f);

        Image plate = FeelUI.CreateImage("Dica", view.root, hintBack);
        plate.raycastTarget = false;
        view.hint = plate.rectTransform;
        TextMeshProUGUI label = FeelUI.CreateText("Texto", view.hint, hintText, labelSize * pixel, FeelUI.PixelCream);
        label.raycastTarget = false;
        FeelUI.Outline(label, .3f);
        FeelUI.Stretch(label.rectTransform);
        view.hint.sizeDelta = new Vector2(label.preferredWidth + hintPadding * 2f * pixel, (labelSize + hintPadding * 2f) * pixel);
        view.hint.anchoredPosition = new Vector2(0f, (40f * valvePixels * .5f + labelSize * .5f + hintPadding + 2f) * pixel);
        Canvas above = plate.gameObject.AddComponent<Canvas>();
        above.overrideSorting = true;
        above.sortingLayerID = above.rootCanvas.sortingLayerID;
        above.sortingOrder = hintOrder;

        views.Add(view);
        Place(view);
    }

    private void Update()
    {
        Turning = false;
        if (views.Count == 0)
        {
            Pressure = 0f;
            return;
        }

        bool pressed = PointerHud(out Vector2 pointer);
        bool playing = GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;
        for (int i = views.Count - 1; i >= 0; i--)
        {
            View view = views[i];
            BlockIncident incident = view.incident;
            if (view.root == null || incident.resolved || !incident.ValveOpen || incident.leader == null || !incident.leader.IsWorking)
            {
                Close(view, incident.resolved || (incident.leader != null && incident.leader.IsComplete));
                continue;
            }

            Place(view);
            if (!playing) continue;

            // O giro conta com o botão apertado perto da válvula (alcance no HUD de 1080, como o jato do incêndio).
            Vector2 center = MiniGameFeel.HudCenter(view.valve, out _);
            Vector2 offset = pointer - center;
            bool held = pressed && offset.magnitude <= Data.valveReach;
            float turned = 0f;
            if (held && offset.magnitude >= deadRadius)
            {
                float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
                if (view.hasAngle) turned = BlockDisasterRules.AngleDelta(view.lastAngle, angle);
                view.lastAngle = angle;
                view.hasAngle = true;
            }
            if (!held) view.hasAngle = false;

            // Salto grande entre dois quadros é o ponteiro cruzando o meio, não giro.
            incident.ValveInput(held, Mathf.Abs(turned) < 90f ? turned : 0f);

            ValveState valve = incident.valve;
            Turning |= held && Mathf.Abs(turned) > .5f;
            Pressure = Mathf.Max(Pressure * .9f, valve.pressure);
            if (valve.direction != 0f) view.spin = valve.direction;

            // O volante gira no sentido do jogador; a seta só aparece parada, como dica.
            view.valve.localEulerAngles = new Vector3(0f, 0f, valve.angle * view.spin);
            view.arrow.gameObject.SetActive(!held);
            if (!held) view.arrow.localEulerAngles = new Vector3(0f, 0f, -GameClock.Time * arrowSpin);
            view.gaugeFill.rectTransform.localScale = new Vector3(valve.pressure, 1f, 1f);
            view.gaugeFill.color = Color.Lerp(gaugeLow, gaugeHigh, valve.pressure);
            view.hint.gameObject.SetActive(valve.pressure < .35f);
        }
    }

    // O bueiro fica no meio do quarteirão, na água entre as casas (longe do círculo de progresso do chamado).
    private void Place(View view)
    {
        if (view.root.parent is not RectTransform layer) return;

        Vector2 local = PixelFx.ToLocal(layer, view.incident.Center);
        view.root.anchoredPosition = local + Vector2.up * valveLift * PixelFx.ArtPixel;
        view.root.localScale = Vector3.one;
    }

    // Apertado e com a partida rodando: posição do ponteiro no HUD.
    private static bool PointerHud(out Vector2 hud)
    {
        hud = default;
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.isPressed || GameManager.InputBlocked) return false;

        Canvas canvas = FeelUI.HudCanvas;
        if (canvas == null) return false;

        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, pointer.position.ReadValue(), canvasCamera, out hud);
    }

    // Falha leve: a válvula pisca vermelha e treme.
    private void OnSkillCheck(House house, SkillResult result)
    {
        if (result != SkillResult.ValveSlip) return;

        View view = views.Find(v => v.incident.leader == house);
        if (view == null || view.valve == null) return;

        view.valveImage.DOKill();
        view.valveImage.color = slipColor;
        view.valveImage.DOColor(Color.white, .35f).SetLink(view.valveImage.gameObject);
        view.valve.DOKill(true);
        view.valve.DOShakeAnchorPos(.3f, PixelFx.ArtPixel * 3f, 30).SetLink(view.valve.gameObject);
    }

    private void OnResolved(BlockIncident incident)
    {
        View view = views.Find(v => v.incident == incident);
        if (view != null) Close(view, true);
    }

    // Desentupiu: a válvula estoura em respingos; senão (equipe saiu) só some.
    private void Close(View view, bool drained)
    {
        views.Remove(view);
        if (view.root == null) return;

        if (drained && PixelFx.Sky != null) FeelParticles.EmitAt(FxKind.Splash, PixelFx.Sky, view.root.position, 1.2f, 12);
        GameObject root = view.root.gameObject;
        view.root.DOKill();
        view.root.DOScale(0f, .2f).SetEase(Ease.InBack).SetLink(root).OnComplete(() => Destroy(root));
    }
}
