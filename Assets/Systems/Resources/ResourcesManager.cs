using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    public static ResourcesManager Instance;

    [Header("WORKERS")]
    private int workers;
    public static int Workers
    {
        get => Instance.workers;
        set { Instance.workers = value; }
    }

    [Header("FIRSTAID")]
    private int firstAid;
    public static int FirstAid
    {
        get => Instance.firstAid;
        set { Instance.firstAid = value; }
    }

    [Header("MATERIAL")]
    private int buildMaterial;
    public static int BuildMaterial
    {
        get => Instance.buildMaterial;
        set { Instance.buildMaterial = value; }
    }

    private void Awake()
    {
        Instance = this;
    }
}
