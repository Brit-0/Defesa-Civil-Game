using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Minigame do jato no incêndio (docs/jam/ENERGIA_FOGO.md). Quando a equipe chega, chamas aparecem em roda da casa
// e uma barra conta a janela (poucos segundos). Arrastar com o botão apertado por cima de uma chama apaga ela
// (House.DouseFlame); todas apagadas = fogo apagado na hora e bônus. Chama sobrando quando a barra zera = o fogo cresce.
// Também mostra o nível do fogo (1 a 3 chamas pequenas) nas casas que queimam esperando equipe.
// Lê o estado das casas e só chama House.DouseFlame; popups e sons de acerto ficam no MiniGameFeel.
public class FireMinigame : MonoBehaviour
{
    [Header("LAYOUT (unidades do HUD em 1080p)")]
    [SerializeField] private float flameSize = 72f;
    [SerializeField] private float ringMin = 80f;
    [SerializeField] private float ringMax = 130f;
    [Tooltip("Raio da roda de chamas em relação à metade do tamanho da casa na tela.")]
    [SerializeField] private float ringScale = .9f;
    [SerializeField] private Vector2 barSize = new Vector2(132f, 15f);
    [SerializeField] private float labelSize = 26f;
    [SerializeField] private string dragHint = "ARRASTE NAS CHAMAS!";
    [Tooltip("Quantas janelas mostram a dica escrita (depois o jogador já sabe).")]
    [SerializeField] private int hintTimes = 4;

    [Header("FIRE LEVEL PIPS")]
    [SerializeField] private float pipSize = 30f;
    [SerializeField] private float pipGap = 2f;

    [Header("COLORS")]
    [SerializeField] private Color waterColor = new Color(.45f, .8f, 1f, .95f);
    [SerializeField] private Color steamColor = new Color(.92f, .97f, 1f, .9f);
    [SerializeField] private Color barFull = new Color(1f, .82f, .25f, 1f);
    [SerializeField] private Color barEmpty = new Color(1f, .3f, .2f, 1f);
    [SerializeField] private Color growColor = new Color(1f, .45f, .15f, 1f);

    [Header("WATER")]
    [SerializeField] private float dropInterval = .03f;
    [SerializeField] private float dropSize = 9f;

    private const string LayerName = "FireJet";

    private class JetView
    {
        public House house;
        public RectTransform root;
        public readonly List<Image> flames = new();
        public readonly List<Vector2> offsets = new();
        public readonly List<bool> alive = new();
        public Image barFill;
        public TMP_Text label;
    }

    private readonly Dictionary<House, JetView> jets = new();
    private readonly Dictionary<House, RectTransform> pips = new();
    private readonly List<House> scratch = new();
    private RectTransform layer;
    private Sprite flameSprite;
    private Sprite pipSprite;
    private float dropTimer;
    private int hintsShown;

    private static FireEnergyData Data => FireEnergyData.Instance;

    private void OnEnable()
    {
        GameEvents.FireJetOpened += OnJetOpened;
        GameEvents.FireLevelChanged += OnFireLevelChanged;
    }

    private void OnDisable()
    {
        GameEvents.FireJetOpened -= OnJetOpened;
        GameEvents.FireLevelChanged -= OnFireLevelChanged;
    }

    private void Update()
    {
        UpdateJets();
        UpdatePips();
    }

    #region JET

    private void OnJetOpened(House house)
    {
        if (house == null || house.Jet == null || Layer() == null) return;

        if (jets.TryGetValue(house, out JetView old)) Close(old, false);

        JetView view = new JetView { house = house, root = FeelUI.CreateRect("Jet_" + house.name, layer) };
        view.root.sizeDelta = Vector2.zero;

        int count = house.Jet.Flames;
        float start = Random.Range(0f, 360f);
        for (int i = 0; i < count; i++)
        {
            float angle = (start + 360f * i / count + Random.Range(-12f, 12f)) * Mathf.Deg2Rad;
            view.offsets.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
            view.alive.Add(true);

            Image flame = FeelUI.CreateImage("Flame", view.root, Color.white, FlameSprite());
            flame.preserveAspect = true;
            flame.rectTransform.sizeDelta = Vector2.one * flameSize;
            flame.rectTransform.localScale = Vector3.zero;
            flame.rectTransform.DOScale(1f, .22f).SetDelay(i * .05f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(flame.gameObject);
            view.flames.Add(flame);
        }

        Image barBack = FeelUI.CreateInset("Bar", view.root, new Color(0f, 0f, 0f, .7f));
        barBack.rectTransform.sizeDelta = barSize;
        view.barFill = FeelUI.CreateImage("Fill", barBack.transform, barFull);
        RectTransform fill = view.barFill.rectTransform;
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(1f, 1f);
        fill.pivot = new Vector2(0f, .5f);
        float inset = FeelUI.PixelSize;
        fill.offsetMin = new Vector2(inset, inset);
        fill.offsetMax = new Vector2(-inset, -inset);

        if (hintsShown < hintTimes)
        {
            hintsShown++;
            view.label = FeelUI.CreateText("Hint", view.root, dragHint, labelSize, FeelUI.PixelCream);
            FeelUI.Outline(view.label, .3f);
            view.label.rectTransform.sizeDelta = new Vector2(420f, 40f);
        }

        jets[house] = view;
        HidePips(house);
    }

    private void UpdateJets()
    {
        if (jets.Count == 0) return;

        Vector2 pointer = default;
        bool spraying = PointerHud(out pointer);

        scratch.Clear();
        scratch.AddRange(jets.Keys);
        foreach (House house in scratch)
        {
            JetView view = jets[house];
            if (house == null)
            {
                Close(view, false);
                continue;
            }

            FireJet jet = house.Jet;
            if (jet == null)
            {
                // Fechou: com chama sobrando o fogo cresceu; sem chama, apagou.
                Close(view, AnyAlive(view));
                continue;
            }

            Vector2 center = MiniGameFeel.HudCenter(house.transform, out Vector2 size);
            float ring = Mathf.Clamp(size.magnitude * .5f * ringScale, ringMin, ringMax);
            center = OnScreen(center, ring + flameSize);
            view.root.anchoredPosition = center;
            view.root.localScale = Vector3.one;

            float t = Time.unscaledTime;
            for (int i = 0; i < view.flames.Count; i++)
            {
                if (!view.alive[i]) continue;

                Image flame = view.flames[i];
                Vector2 at = view.offsets[i] * ring;
                flame.rectTransform.anchoredPosition = at;
                float flicker = 1f + Mathf.Sin(t * 13f + i * 1.7f) * .07f;
                flame.rectTransform.localScale = flame.rectTransform.localScale.x >= .98f ? new Vector3(flicker, 2f - flicker, 1f) : flame.rectTransform.localScale;

                if (spraying && Vector2.Distance(pointer, center + at) <= Data.jetHitRadius && house.DouseFlame())
                {
                    view.alive[i] = false;
                    Douse(view, flame);
                    if (house.Jet == null) break;
                }
            }

            if (view.barFill != null)
            {
                float left = house.Jet != null ? house.Jet.TimeLeft01 : 0f;
                view.barFill.rectTransform.localScale = new Vector3(left, 1f, 1f);
                view.barFill.color = Color.Lerp(barEmpty, barFull, left);
                view.barFill.transform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -ring - flameSize * .5f - barSize.y);
            }

            if (view.label != null) view.label.rectTransform.anchoredPosition = new Vector2(0f, ring + flameSize * .5f + labelSize);
        }

        if (spraying) Spray(pointer);
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

    // Casa fora da tela: o anel fica preso na borda, inteiro, para o jato sempre dar para jogar.
    private static Vector2 OnScreen(Vector2 center, float extent)
    {
        Canvas canvas = FeelUI.HudCanvas;
        if (canvas == null) return center;

        Rect area = ((RectTransform)canvas.transform).rect;
        return new Vector2(Mathf.Clamp(center.x, area.xMin + extent, area.xMax - extent),
            Mathf.Clamp(center.y, area.yMin + extent, area.yMax - extent));
    }

    private static bool AnyAlive(JetView view)
    {
        foreach (bool alive in view.alive)
        {
            if (alive) return true;
        }
        return false;
    }

    // Chama apagada: clareia, incha e some numa nuvem de vapor.
    private void Douse(JetView view, Image flame)
    {
        RectTransform rect = flame.rectTransform;
        rect.DOKill();
        flame.color = steamColor;
        rect.DOScale(1.5f, .2f).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(flame.gameObject);
        flame.DOFade(0f, .2f).SetUpdate(true).SetLink(flame.gameObject);

        Image puff = FeelUI.CreateCircle("Steam", view.root, steamColor);
        puff.rectTransform.anchoredPosition = rect.anchoredPosition;
        puff.rectTransform.sizeDelta = Vector2.one * flameSize * .6f;
        puff.rectTransform.DOScale(2.2f, .35f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(puff.gameObject);
        puff.DOFade(0f, .35f).SetUpdate(true).SetLink(puff.gameObject).OnComplete(() => Destroy(puff.gameObject));
    }

    // Janela fechada. Fogo que cresceu: as chamas que sobraram incham em laranja antes de sumir.
    private void Close(JetView view, bool grew)
    {
        if (view.house != null) jets.Remove(view.house);
        if (view.root == null) return;

        GameObject root = view.root.gameObject;
        float time = grew ? .45f : .2f;
        for (int i = 0; i < view.flames.Count; i++)
        {
            if (!view.alive[i]) continue;

            Image flame = view.flames[i];
            flame.rectTransform.DOKill();
            if (grew)
            {
                flame.color = growColor;
                flame.rectTransform.DOScale(1.7f, time).SetEase(Ease.OutBack).SetUpdate(true).SetLink(flame.gameObject);
            }
            flame.DOFade(0f, time).SetUpdate(true).SetLink(flame.gameObject);
        }

        DOVirtual.DelayedCall(time + .05f, () => { if (root != null) Destroy(root); }, true).SetLink(root);
    }

    // Gotas de água caindo do ponteiro enquanto o jato está apertado sobre alguma janela aberta.
    private void Spray(Vector2 pointer)
    {
        dropTimer -= Time.unscaledDeltaTime;
        if (dropTimer > 0f) return;
        dropTimer = dropInterval;

        Image drop = FeelUI.CreateImage("Drop", layer, waterColor);
        RectTransform rect = drop.rectTransform;
        rect.sizeDelta = Vector2.one * dropSize;
        rect.anchoredPosition = pointer + Random.insideUnitCircle * 10f;
        Vector2 to = rect.anchoredPosition + new Vector2(Random.Range(-28f, 28f), Random.Range(-50f, -26f));
        rect.DOAnchorPos(to, .3f).SetEase(Ease.InQuad).SetUpdate(true).SetLink(drop.gameObject);
        drop.DOFade(0f, .3f).SetUpdate(true).SetLink(drop.gameObject).OnComplete(() => Destroy(drop.gameObject));
    }

    #endregion

    #region FIRE LEVEL PIPS

    private void OnFireLevelChanged(House house, int level)
    {
        if (house == null) return;
        if (level <= 0 || jets.ContainsKey(house))
        {
            HidePips(house);
            return;
        }

        if (Layer() == null) return;

        if (!pips.TryGetValue(house, out RectTransform row) || row == null)
        {
            row = FeelUI.CreateRect("FireLevel_" + house.name, layer);
            pips[house] = row;
        }

        for (int i = row.childCount; i < level; i++)
        {
            Image pip = FeelUI.CreateImage("Pip", row, Color.white, PipSprite());
            pip.preserveAspect = true;
            pip.rectTransform.sizeDelta = Vector2.one * pipSize;
            pip.rectTransform.localScale = Vector3.zero;
            pip.rectTransform.DOScale(1f, .25f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(pip.gameObject);
        }

        float width = level * pipSize + (level - 1) * pipGap;
        for (int i = 0; i < row.childCount; i++)
        {
            RectTransform pip = (RectTransform)row.GetChild(i);
            pip.gameObject.SetActive(i < level);
            pip.anchoredPosition = new Vector2(-width * .5f + pipSize * .5f + i * (pipSize + pipGap), 0f);
        }

        // Soco no nível novo: o jogador vê o fogo crescer.
        if (level > 1)
        {
            row.DOKill();
            row.localScale = Vector3.one * 1.4f;
            row.DOScale(1f, .3f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(row.gameObject);
        }
    }

    private void UpdatePips()
    {
        if (pips.Count == 0) return;

        scratch.Clear();
        scratch.AddRange(pips.Keys);
        foreach (House house in scratch)
        {
            RectTransform row = pips[house];
            if (house == null || row == null || house.FireLevel <= 0)
            {
                HidePips(house);
                continue;
            }

            // Escondido com a equipe trabalhando: o círculo de progresso e o jato ocupam o lugar.
            bool show = !house.IsWorking && !house.IsComplete;
            row.gameObject.SetActive(show);
            if (!show) continue;

            Vector2 center = MiniGameFeel.HudCenter(house.transform, out Vector2 size);
            row.anchoredPosition = center + new Vector2(0f, -size.y * .5f - pipSize * .3f);
        }
    }

    private void HidePips(House house)
    {
        if (house == null || !pips.TryGetValue(house, out RectTransform row)) return;

        pips.Remove(house);
        if (row != null) Destroy(row.gameObject);
    }

    #endregion

    private RectTransform Layer()
    {
        if (layer == null) layer = FeelUI.HudLayer(LayerName);
        return layer;
    }

    // Ícone do Pixel Lab (fire_flame) se chegou; senão o fogo do kit pixel.
    private Sprite FlameSprite()
    {
        if (flameSprite == null) flameSprite = FireEnergyArt.Load(FireEnergyArt.Flame);
        if (flameSprite == null && FeelUI.Theme != null) flameSprite = FeelUI.Theme.iconFire;
        return flameSprite;
    }

    private Sprite PipSprite()
    {
        if (pipSprite == null && FeelUI.Theme != null) pipSprite = FeelUI.Theme.iconFire;
        if (pipSprite == null) pipSprite = FlameSprite();
        return pipSprite;
    }
}
