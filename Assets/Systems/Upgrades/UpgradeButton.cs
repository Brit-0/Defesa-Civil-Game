using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Botão de um upgrade na aba. Mostra ícone, nome, nível e custo; o clique tenta comprar.
// Campo vazio é procurado no prefab pelo nome do filho (Icon, Name, LvlLbl, Price/PriceLbl).
public class UpgradeButton : MonoBehaviour, IPointerClickHandler
{
    private const string IconPath = "Icon";
    private const string NamePath = "Name";
    private const string LevelPath = "LvlLbl";
    private const string PricePath = "Price/PriceLbl";

    [Header("REFERENCES")]
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameLbl;
    [SerializeField] private TMP_Text levelLbl;
    [SerializeField] private TMP_Text priceLbl;

    [Header("TEXTS")]
    [SerializeField] private string levelFormat = "{0}/{1}";
    [SerializeField] private string maxLevelText = "MÁX";

    public UpgradeData Data { get; private set; }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (icon == null) icon = FindChild<Image>(IconPath);
        if (nameLbl == null) nameLbl = FindChild<TMP_Text>(NamePath);
        if (levelLbl == null) levelLbl = FindChild<TMP_Text>(LevelPath);
        if (priceLbl == null) priceLbl = FindChild<TMP_Text>(PricePath);
    }

    private void OnEnable()
    {
        GameEvents.MoneyChanged += OnMoneyChanged;
        GameEvents.UpgradePurchased += OnUpgradePurchased;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.MoneyChanged -= OnMoneyChanged;
        GameEvents.UpgradePurchased -= OnUpgradePurchased;
    }

    public void Set(UpgradeData data)
    {
        Data = data;
        Refresh();
    }

    // O clique chega aqui mesmo com o Button desabilitado: compra negada também precisa de resposta (ActionDenied).
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (Data == null || UpgradesManager.Instance == null) return;

        UpgradesManager.Instance.TryBuy(Data, transform.position);
    }

    public void Refresh()
    {
        if (Data == null || UpgradesManager.Instance == null) return;

        UpgradesManager manager = UpgradesManager.Instance;
        bool isMaxLevel = manager.IsMaxLevel(Data);

        if (icon != null && Data.icon != null) icon.sprite = Data.icon;
        if (nameLbl != null) nameLbl.text = Data.displayName;
        if (levelLbl != null) levelLbl.text = string.Format(levelFormat, manager.GetLevel(Data), Data.maxLevel);
        if (priceLbl != null) priceLbl.text = isMaxLevel ? maxLevelText : manager.GetCost(Data).ToString();
        if (button != null) button.interactable = manager.CanBuy(Data);
    }

    private void OnMoneyChanged(int before, int after)
    {
        Refresh();
    }

    private void OnUpgradePurchased(UpgradeData upgrade, int newLevel)
    {
        Refresh();
    }

    private T FindChild<T>(string path) where T : Component
    {
        Transform child = transform.Find(path);
        return child != null ? child.GetComponent<T>() : null;
    }
}
