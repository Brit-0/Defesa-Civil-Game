using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Janelinha ao lado do painel de melhorias com o que a melhoria faz (lista Para Arrumar, item 14):
//nome, descrição do UpgradeData, nível e quanto custa o próximo. Aparece no hover da linha, na altura dela.
public class UpgradeTooltip : MonoBehaviour
{
    [Header("TEXTS")]
    [SerializeField] private string levelFormat = "Nível {0}/{1}";
    [SerializeField] private string nextFormat = "Próximo: {0} moedas";
    [SerializeField] private string missingFormat = "faltam {0}";
    [SerializeField] private string maxText = "Nível máximo";

    [Header("LAYOUT (tela de referência 1920x1080)")]
    [SerializeField] private float width = 450f;
    [SerializeField] private float gap = 12f;
    [Tooltip("Margem interna: a moldura pixel já ocupa 18 unidades, o texto fica 21 além dela (#531)")]
    [SerializeField] private float padding = 39f;
    [SerializeField] private float nameSize = 32f;
    [SerializeField] private float textSize = 26f;

    [Header("TIMING")]
    [SerializeField] private float fadeTime = .1f;

    private RectTransform rect;
    private CanvasGroup group;
    private TMP_Text nameLbl;
    private TMP_Text descriptionLbl;
    private TMP_Text levelLbl;
    private TMP_Text costLbl;
    private UpgradeButton shown;
    private float panelHeight;

    public static UpgradeTooltip Create(RectTransform panel, float panelHeight)
    {
        Image window = FeelUI.CreateWindow("Tooltip", panel);
        UpgradeTooltip tooltip = window.gameObject.AddComponent<UpgradeTooltip>();
        tooltip.panelHeight = panelHeight;
        tooltip.Build();
        return tooltip;
    }

    //Presa no canto de baixo à direita do painel; a altura acompanha o texto
    private void Build()
    {
        rect = (RectTransform)transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0f, .5f);
        rect.sizeDelta = new Vector2(width, 0f);

        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        VerticalLayoutGroup column = gameObject.AddComponent<VerticalLayoutGroup>();
        int pad = Mathf.RoundToInt(padding);
        column.padding = new RectOffset(pad, pad, pad, pad);
        column.spacing = 9f;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        nameLbl = Line("Name", nameSize, FeelUI.PixelGold);
        descriptionLbl = Line("Description", textSize, FeelUI.PixelCream);
        levelLbl = Line("Level", textSize, FeelUI.PixelLilac);
        costLbl = Line("Cost", textSize, FeelUI.PixelCream);

        gameObject.SetActive(false);
    }

    private TMP_Text Line(string lineName, float size, Color color)
    {
        TextMeshProUGUI label = FeelUI.CreateText(lineName, transform, "", size, color, TextAlignmentOptions.TopLeft);
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    public void Show(UpgradeButton row)
    {
        if (row == null || row.Data == null) return;

        bool wasShown = gameObject.activeSelf;
        shown = row;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Fill();

        //Na altura da linha, sem passar do topo nem do pé do painel
        RectTransform panel = (RectTransform)transform.parent;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        float half = rect.rect.height * .5f;
        float y = panel.InverseTransformPoint(row.transform.position).y - panel.rect.yMin;
        y = Mathf.Clamp(y, half, Mathf.Max(half, panelHeight - half));
        rect.anchoredPosition = new Vector2(gap, y);

        if (wasShown) return;

        group.DOKill();
        group.alpha = 0f;
        group.DOFade(1f, fadeTime).SetUpdate(true).SetLink(gameObject);
    }

    //Só esconde se ainda é a linha que saiu (o mouse passa de uma linha para a outra sem buraco)
    public void Hide(UpgradeButton row)
    {
        if (row == shown) Hide();
    }

    public void Hide()
    {
        shown = null;
        if (group != null) group.DOKill();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.MoneyChanged += OnMoneyChanged;
        GameEvents.UpgradePurchased += OnUpgradePurchased;
    }

    private void OnDisable()
    {
        GameEvents.MoneyChanged -= OnMoneyChanged;
        GameEvents.UpgradePurchased -= OnUpgradePurchased;
    }

    private void OnMoneyChanged(int before, int after) => Fill();

    private void OnUpgradePurchased(UpgradeData upgrade, int newLevel) => Fill();

    private void Fill()
    {
        UpgradesManager manager = UpgradesManager.Instance;
        if (shown == null || shown.Data == null || manager == null) return;

        UpgradeData data = shown.Data;
        int level = manager.GetLevel(data);
        nameLbl.text = data.displayName;
        descriptionLbl.text = data.description;
        levelLbl.text = string.Format(levelFormat, level, manager.GetMaxLevel(data));

        if (manager.IsMaxLevel(data))
        {
            costLbl.text = maxText;
            return;
        }

        int cost = manager.GetCost(data);
        int missing = cost - ResourcesManager.Money;
        costLbl.text = string.Format(nextFormat, cost) + (missing > 0 ? " (" + string.Format(missingFormat, missing) + ")" : "");
    }
}
