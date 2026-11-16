using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeSO", menuName = "Scriptable Objects/UpgradeSO")]
public class UpgradeData : ScriptableObject
{
    [Header("IDENTITY")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public int order;

    [Header("COST")]
    public int maxLevel = 1;
    public int baseCost;
    public float costGrowth = 1f;

    [Header("EFFECT")]
    public float effectPerLevel;
}
