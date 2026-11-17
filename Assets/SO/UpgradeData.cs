using UnityEngine;

// Aba do painel de melhorias em que o upgrade aparece (lista Para Arrumar, item 10)
public enum UpgradeCategory
{
    Workers,
    Automation,
    Expansion,
    Energy
}

[CreateAssetMenu(fileName = "UpgradeSO", menuName = "Scriptable Objects/UpgradeSO")]
public class UpgradeData : ScriptableObject
{
    [Header("IDENTITY")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public int order;
    public UpgradeCategory category;

    [Header("COST")]
    public int maxLevel = 1;
    public int baseCost;
    public float costGrowth = 1f;

    [Header("EFFECT")]
    public float effectPerLevel;
}
