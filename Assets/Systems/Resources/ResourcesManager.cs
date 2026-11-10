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
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Workers = 3;
    }
}
