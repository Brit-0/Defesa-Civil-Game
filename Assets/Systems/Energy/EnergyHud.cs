using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD da energia (docs/jam/ENERGIA_FOGO.md), em pixel art com o kit do FeelUI. Aparece quando a energia entra no jogo.
// Plaquinha embaixo dos contadores (raio, consumo/capacidade, barra verde, amarela ou vermelha); clicar abre o gerador
// com o botão MELHORAR e o que está desligado. No apagão vira vermelha e o botão RELIGAR pisca embaixo dela.
// A tomada riscada em cima de base ou depósito sem energia fica no EnergyMarkers. Só escuta GameEvents e chama
// EnergyManager.Restore e UpgradesManager.TryBuy, como os botões de upgrade.
public class EnergyHud : MonoBehaviour
{
    [Header("LAYOUT (unidades do HUD em 1080p)")]
    [SerializeField] private Vector2 panelSize = new Vector2(236f, 104f);
    [SerializeField] private Vector2 dropdownSize = new Vector2(330f, 178f);
    [SerializeField] private Vector2 restoreSize = new Vector2(236f, 70f);
    [Tooltip("Distância da borda esquerda e do contador de material (BuildMaterial) logo acima.")]
    [SerializeField] private Vector2 margin = new Vector2(18f, 14f);
    [Tooltip("Topo da plaquinha quando o contador de material não é achado (distância do topo da tela).")]
    [SerializeField] private float fallbackTop = 240f;

    [Header("COLORS")]
    [SerializeField] private Color okColor = new Color(.45f, .9f, .4f, 1f);
    [SerializeField] private Color warnColor = new Color(1f, .82f, .25f, 1f);
    [SerializeField] private Color badColor = new Color(1f, .32f, .25f, 1f);
    [SerializeField] private Color darkColor = new Color(.2f, .18f, .24f, 1f);
    [Range(0f, 1f)] [SerializeField] private float warnAt = .8f;

    [Header("TEXTS")]
    [SerializeField] private string title = "ENERGIA";
    [SerializeField] private string overloadText = "SOBRECARGA";
    [SerializeField] private string blackoutText = "APAGÃO!";
    [SerializeField] private string restoreText = "RELIGAR";
    [SerializeField] private string generatorFormat = "GERADOR  NV {0}/{1}";
    [SerializeField] private string upgradeFormat = "MELHORAR  ${0}  (+{1})";
    [SerializeField] private string maxText = "GERADOR NO MÁXIMO";
    [SerializeField] private string allOnText = "TUDO LIGADO";
    [SerializeField] private string offFormat = "SEM ENERGIA: {0}";
    [SerializeField] private string deniedText = "SEM ENERGIA!";
    [SerializeField] private string unlockedText = "A CIDADE AGORA USA ENERGIA!";

    [Header("SOUND")]
    [SerializeField, Range(0f, 1f)] private float volume = .8f;

    private const string LayerName = "EnergyHud";

    private RectTransform root;
    private RectTransform panel;
    private Image barFill;
    private TMP_Text valueLbl;
    private TMP_Text statusLbl;
    private Image boltIcon;
    private RectTransform dropdown;
    private TMP_Text generatorLbl;
    private TMP_Text offLbl;
    private Button upgradeButton;
    private TMP_Text upgradeLbl;
    private Button restoreButton;
    private Image restoreFill;
    private AudioSource source;
    private UpgradeButton generatorRow;
    private float rowCheck;
    private bool built;

    private static EnergyManager Grid => EnergyManager.Instance;

    // Alvos da seta do tutorial da energia.
    public RectTransform Panel => panel;
    public RectTransform RestoreButton => restoreButton != null ? (RectTransform)restoreButton.transform : null;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    private void OnEnable()
    {
        GameEvents.EnergyUnlocked += OnUnlocked;
        GameEvents.EnergyChanged += OnEnergyChanged;
        GameEvents.AutomationPowerChanged += OnAutomationPowerChanged;
        GameEvents.BlackoutChanged += OnBlackoutChanged;
        GameEvents.ActionDenied += OnActionDenied;
        GameEvents.UpgradePurchased += OnUpgradePurchased;
        GameEvents.MoneyChanged += OnMoneyChanged;
    }

    private void OnDisable()
    {
        GameEvents.EnergyUnlocked -= OnUnlocked;
        GameEvents.EnergyChanged -= OnEnergyChanged;
        GameEvents.AutomationPowerChanged -= OnAutomationPowerChanged;
        GameEvents.BlackoutChanged -= OnBlackoutChanged;
        GameEvents.ActionDenied -= OnActionDenied;
        GameEvents.UpgradePurchased -= OnUpgradePurchased;
        GameEvents.MoneyChanged -= OnMoneyChanged;
    }

    private void Start()
    {
        if (Grid != null && Grid.Unlocked) Show(false);
    }

    private void Update()
    {
        bool unlocked = Grid != null && Grid.Unlocked;
        UpdateGeneratorRow(unlocked);
        if (!built || !unlocked) return;

        if (Grid.Blackout && restoreFill != null) restoreFill.rectTransform.localScale = new Vector3(Grid.BlackoutTimeLeft01, 1f, 1f);
    }

    #region BUILD

    private void Build()
    {
        if (built) return;

        RectTransform layer = FeelUI.HudLayer(LayerName);
        if (layer == null) return;
        built = true;

        root = FeelUI.CreateRect("Energy", layer);
        root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = panelSize;
        root.anchoredPosition = TopLeft();

        // Plaquinha: raio, consumo/capacidade, estado e a barra. Clicar abre o gerador.
        Image window = FeelUI.CreateWindow("Panel", root);
        window.raycastTarget = true;
        panel = window.rectTransform;
        FeelUI.Place(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, panelSize);
        Button panelButton = window.gameObject.AddComponent<Button>();
        panelButton.transition = Selectable.Transition.None;
        panelButton.onClick.AddListener(ToggleDropdown);

        Sprite bolt = FireEnergyArt.Load(FireEnergyArt.Bolt);
        boltIcon = FeelUI.CreateImage("Bolt", panel, Color.white, bolt);
        boltIcon.preserveAspect = true;
        FeelUI.Place(boltIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -10f), Vector2.one * 54f);

        TMP_Text titleLbl = FeelUI.CreateText("Title", panel, title, 20f, FeelUI.PixelLilac, TextAlignmentOptions.Left);
        FeelUI.Place(titleLbl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(74f, -12f), new Vector2(150f, 22f));

        valueLbl = FeelUI.CreateText("Value", panel, "0/0", 36f, FeelUI.PixelCream, TextAlignmentOptions.Left);
        FeelUI.Place(valueLbl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(74f, -32f), new Vector2(90f, 38f));

        statusLbl = FeelUI.CreateText("Status", panel, "", 18f, badColor, TextAlignmentOptions.Right);
        FeelUI.Place(statusLbl.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -40f), new Vector2(110f, 22f));

        Image barBack = FeelUI.CreateInset("Bar", panel, new Color(0f, 0f, 0f, .6f));
        FeelUI.Place(barBack.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(12f, 12f), new Vector2(panelSize.x - 24f, 20f));
        barFill = CreateFill(barBack.transform, okColor);

        BuildDropdown();
        BuildRestore();

        root.gameObject.SetActive(false);
    }

    private void BuildDropdown()
    {
        Image box = FeelUI.CreateWindow("Generator", root);
        dropdown = box.rectTransform;
        FeelUI.Place(dropdown, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -panelSize.y - 6f), dropdownSize);

        Image icon = FeelUI.CreateImage("Icon", dropdown, Color.white, GeneratorSprite());
        icon.preserveAspect = true;
        FeelUI.Place(icon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), Vector2.one * 64f);

        generatorLbl = FeelUI.CreateText("Level", dropdown, "", 22f, FeelUI.PixelCream, TextAlignmentOptions.Left);
        FeelUI.Place(generatorLbl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -16f), new Vector2(230f, 26f));

        offLbl = FeelUI.CreateText("Off", dropdown, "", 18f, okColor, TextAlignmentOptions.Left);
        offLbl.textWrappingMode = TextWrappingModes.Normal;
        FeelUI.Place(offLbl.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(86f, -46f), new Vector2(232f, 40f));

        upgradeButton = FeelUI.CreateButton("Upgrade", dropdown, "", new Vector2(dropdownSize.x - 24f, 58f), FeelUI.ButtonColor, 22f, true);
        FeelUI.Place((RectTransform)upgradeButton.transform, new Vector2(.5f, 0f), new Vector2(.5f, 0f), new Vector2(0f, 12f), new Vector2(dropdownSize.x - 24f, 58f));
        upgradeLbl = upgradeButton.GetComponentInChildren<TMP_Text>();
        upgradeButton.onClick.AddListener(BuyGenerator);

        dropdown.gameObject.SetActive(false);
    }

    private void BuildRestore()
    {
        restoreButton = FeelUI.CreateButton("Restore", root, restoreText, restoreSize, FeelUI.ButtonColor, 30f, true);
        RectTransform rect = (RectTransform)restoreButton.transform;
        FeelUI.Place(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -panelSize.y - 6f), restoreSize);
        restoreButton.onClick.AddListener(Restore);

        // Tempo até a rede voltar sozinha: uma linha fina no pé do botão.
        Image line = FeelUI.CreateImage("AutoRestore", rect, new Color(0f, 0f, 0f, .45f));
        FeelUI.Place(line.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(9f, 6f), new Vector2(restoreSize.x - 18f, 6f));
        restoreFill = CreateFill(line.transform, FeelUI.PixelCream);

        restoreButton.gameObject.SetActive(false);
    }

    private static Image CreateFill(Transform parent, Color color)
    {
        Image fill = FeelUI.CreateImage("Fill", parent, color);
        RectTransform rect = fill.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0f, .5f);
        float inset = FeelUI.PixelSize;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        return fill;
    }

    // Logo abaixo do contador de material, encostado na borda esquerda; sem ele, a uma altura fixa.
    private Vector2 TopLeft()
    {
        Canvas canvas = FeelUI.HudCanvas;
        if (canvas == null) return Vector2.zero;

        RectTransform canvasRect = (RectTransform)canvas.transform;
        Rect area = canvasRect.rect;
        float top = area.yMax - fallbackTop;

        RectTransform material = FeelUI.FindChild(canvas.transform, "BuildMaterial") as RectTransform;
        if (material != null && material.gameObject.activeInHierarchy)
        {
            Vector3[] corners = new Vector3[4];
            material.GetWorldCorners(corners);
            float bottom = float.MaxValue;
            foreach (Vector3 corner in corners) bottom = Mathf.Min(bottom, canvasRect.InverseTransformPoint(corner).y);
            top = bottom - margin.y - FeelUI.PixelSize * 3f;
        }

        return new Vector2(area.xMin + margin.x, top);
    }

    private Sprite GeneratorSprite()
    {
        UpgradeData data = GeneratorData();
        Sprite lab = FeelUI.Theme != null ? FeelUI.Theme.LabIconFor("upgrade_" + EnergyIds.Generator) : null;
        if (lab != null) return lab;
        if (data != null && data.icon != null) return data.icon;
        return FireEnergyArt.Load(FireEnergyArt.Generator);
    }

    #endregion

    #region SHOW

    private void Show(bool animate)
    {
        Build();
        if (root == null) return;

        root.gameObject.SetActive(true);
        Refresh();
        if (!animate) return;

        panel.localScale = Vector3.zero;
        panel.DOScale(1f, .35f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(panel.gameObject);
        Popup(unlockedText, panelSize.y + 30f, warnColor);
        Play(FireEnergyArt.SfxUnlock);
    }

    private void Refresh()
    {
        if (!built || Grid == null || !Grid.Unlocked) return;

        bool blackout = Grid.Blackout;
        int capacity = Mathf.Max(1, Grid.Capacity);
        float load = blackout ? 0f : Grid.Demand / (float)capacity;

        valueLbl.text = blackout ? $"0/{Grid.Capacity}" : $"{Grid.Demand}/{Grid.Capacity}";
        barFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(load), 1f, 1f);
        barFill.color = load > 1f ? badColor : load >= warnAt ? warnColor : okColor;
        statusLbl.text = blackout ? blackoutText : Grid.Overloaded ? overloadText : "";
        statusLbl.color = badColor;
        valueLbl.color = blackout || Grid.Overloaded ? badColor : FeelUI.PixelCream;
        if (boltIcon != null) boltIcon.color = blackout ? darkColor : Color.white;

        restoreButton.gameObject.SetActive(blackout);
        if (blackout) dropdown.gameObject.SetActive(false);
        RefreshDropdown();
    }

    private void RefreshDropdown()
    {
        if (dropdown == null || !dropdown.gameObject.activeSelf) return;

        UpgradeData data = GeneratorData();
        int level = Grid.GeneratorLevel;
        int max = data != null ? data.maxLevel : 0;
        generatorLbl.text = string.Format(generatorFormat, level, max);

        string off = OffList();
        offLbl.text = string.IsNullOrEmpty(off) ? allOnText : string.Format(offFormat, off);
        offLbl.color = string.IsNullOrEmpty(off) ? okColor : badColor;

        bool canUpgrade = data != null && UpgradesManager.Instance != null && !UpgradesManager.Instance.IsMaxLevel(data);
        upgradeLbl.text = canUpgrade ? string.Format(upgradeFormat, UpgradesManager.Instance.GetCost(data), Mathf.RoundToInt(data.effectPerLevel)) : maxText;
        upgradeButton.interactable = canUpgrade && UpgradesManager.Instance.CanBuy(data);
    }

    // O que ficou sem energia, em palavras curtas: "AUTOMAÇÃO 3-4, 1 ARMAZÉM".
    private string OffList()
    {
        int bases = 0, depots = 0;
        foreach (EnergyConsumer consumer in Grid.Consumers)
        {
            if (consumer.powered || consumer.location == null) continue;
            if (consumer.kind == EnergyConsumerKind.Base) bases++;
            else depots++;
        }

        List<string> parts = new();
        if (Grid.AutomationPowered < Grid.AutomationBought)
        {
            int first = Grid.AutomationPowered + 1;
            parts.Add(first == Grid.AutomationBought ? $"AUTOMAÇÃO {first}" : $"AUTOMAÇÃO {first}-{Grid.AutomationBought}");
        }
        if (depots > 0) parts.Add(depots == 1 ? "1 ARMAZÉM" : $"{depots} ARMAZÉNS");
        if (bases > 0) parts.Add(bases == 1 ? "1 BASE" : $"{bases} BASES");
        return string.Join(", ", parts);
    }

    private void ToggleDropdown()
    {
        if (Grid == null || Grid.Blackout || GameManager.InputBlocked) return;

        bool open = !dropdown.gameObject.activeSelf;
        dropdown.gameObject.SetActive(open);
        if (!open) return;

        RefreshDropdown();
        dropdown.localScale = new Vector3(1f, 0f, 1f);
        dropdown.DOScaleY(1f, .18f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(dropdown.gameObject);
    }

    private void BuyGenerator()
    {
        UpgradeData data = GeneratorData();
        if (data == null || UpgradesManager.Instance == null || GameManager.InputBlocked) return;

        UpgradesManager.Instance.TryBuy(data, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
    }

    private void Restore()
    {
        if (Grid == null || GameManager.InputBlocked) return;

        Grid.Restore();
    }

    private static UpgradeData GeneratorData()
    {
        return UpgradesManager.Instance != null ? UpgradesManager.Instance.GetUpgrade(EnergyIds.Generator) : null;
    }

    // A linha do gerador na aba de melhorias só aparece quando a energia entra no jogo.
    private void UpdateGeneratorRow(bool unlocked)
    {
        rowCheck -= Time.unscaledDeltaTime;
        if (rowCheck > 0f) return;
        rowCheck = .5f;

        if (generatorRow == null)
        {
            foreach (UpgradeButton button in FindObjectsByType<UpgradeButton>(FindObjectsInactive.Include))
            {
                if (button.Data != null && button.Data.id == EnergyIds.Generator) generatorRow = button;
            }
        }

        if (generatorRow != null && generatorRow.gameObject.activeSelf != unlocked) generatorRow.gameObject.SetActive(unlocked);
    }

    #endregion

    #region EVENTS

    private void OnUnlocked() => Show(true);

    private void OnEnergyChanged(int demand, int capacity)
    {
        bool wasOverloaded = statusLbl != null && statusLbl.text == overloadText;
        Refresh();
        if (Grid != null && Grid.Overloaded && !wasOverloaded)
        {
            Shake();
            Play(FireEnergyArt.SfxOverload);
        }
    }

    private void OnAutomationPowerChanged(int powered, int bought) => Refresh();

    private void OnBlackoutChanged(bool active, Vector3 worldPosition)
    {
        Refresh();
        Play(active ? FireEnergyArt.SfxBlackout : FireEnergyArt.SfxRestore);
        if (!built) return;

        if (active)
        {
            Shake();
            RectTransform button = (RectTransform)restoreButton.transform;
            button.DOKill();
            button.localScale = Vector3.one;
            button.DOScale(1.08f, .4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(button.gameObject);
            return;
        }

        panel.DOKill(true);
        panel.DOPunchScale(Vector3.one * .15f, .3f, 6).SetUpdate(true).SetLink(panel.gameObject);
    }

    private void OnActionDenied(DeniedReason reason, Vector3 worldPosition)
    {
        if (reason != DeniedReason.NoEnergy) return;

        Play(FireEnergyArt.SfxDenied);
        if (!built) return;

        Shake();
        Popup(deniedText, -panelSize.y * .5f, badColor);
    }

    private void OnUpgradePurchased(UpgradeData upgrade, int level)
    {
        if (upgrade == null || upgrade.id != EnergyIds.Generator || !built) return;

        Refresh();
        panel.DOKill(true);
        panel.DOPunchScale(Vector3.one * .2f, .35f, 6).SetUpdate(true).SetLink(panel.gameObject);
    }

    private void OnMoneyChanged(int before, int after) => RefreshDropdown();

    #endregion

    #region FEEL

    private void Shake()
    {
        if (panel == null) return;

        panel.DOKill(true);
        panel.DOShakeAnchorPos(.35f, 10f, 30).SetUpdate(true).SetLink(panel.gameObject);
    }

    // Texto curto ao lado da plaquinha: sobe e some.
    private void Popup(string text, float offsetY, Color color)
    {
        if (root == null) return;

        TextMeshProUGUI label = FeelUI.CreateText("EnergyPopup", root, text, 24f, color, TextAlignmentOptions.Left);
        FeelUI.Outline(label, .3f);
        RectTransform rect = label.rectTransform;
        FeelUI.Place(rect, new Vector2(0f, 1f), new Vector2(0f, .5f), new Vector2(panelSize.x + 14f, -offsetY), new Vector2(520f, 34f));
        rect.localScale = Vector3.zero;

        Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(label.gameObject);
        sequence.Append(rect.DOScale(1f, .2f).SetEase(Ease.OutBack));
        sequence.Join(rect.DOAnchorPosY(rect.anchoredPosition.y + 40f, 1.4f).SetEase(Ease.OutCubic));
        sequence.Insert(1f, label.DOFade(0f, .4f));
        sequence.OnComplete(() => Destroy(label.gameObject));
    }

    private void Play(string key)
    {
        AudioClip clip = FireEnergyArt.Clip(key);
        if (clip != null) source.PlayOneShot(clip, volume);
    }

    #endregion
}
