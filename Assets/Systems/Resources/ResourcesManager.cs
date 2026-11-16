using TMPro;
using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager Instance;

    [Header("WORKERS")]
    [SerializeField] private TMP_Text workersLbl;
    private int workers;
    public static int Workers
    {
        get => Instance.workers;
        set 
        { 
            Instance.workers = value;
            Instance.workersLbl.text = value.ToString();
            GameEvents.RaiseWorkersChanged(value);
        }
    }

    [Header("FIRSTAID")]
    [SerializeField] private TMP_Text kitsLbl;
    private int firstAid;
    public static int FirstAid
    {
        get => Instance.firstAid;
        set 
        { 
            Instance.firstAid = value;
            Instance.kitsLbl.text = value.ToString();
        }
    }

    [Header("MATERIAL")]
    [SerializeField] private TMP_Text materialsLbl;
    public Sprite materialIcon;
    private int buildMaterial;
    public static int BuildMaterial
    {
        get => Instance.buildMaterial;
        set 
        { 
            Instance.buildMaterial = value;
            Instance.materialsLbl.text = value.ToString();
            GameEvents.RaiseMaterialChanged(value);
        }
    }

    [Header("MONEY")]
    [SerializeField] private TMP_Text moneyLbl;
    public Sprite moneylIcon;
    private int money;
    public static int Money
    {
        get => Instance.money;
        set
        {
            int before = Instance.money;
            Instance.money = value;
            Instance.moneyLbl.text = value.ToString();
            GameEvents.RaiseMoneyChanged(before, value);
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Workers = Stats.Balance.startWorkers;
        BuildMaterial = Stats.Balance.startMaterial;
        Money = Stats.Balance.startMoney;
    }
}
