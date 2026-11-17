using UnityEngine;

// Valor novo só no fim: os .asset guardam o tipo como número. Flood e Meteor: docs/jam/ENCHENTE_METEORO.md
public enum DisasterType { Earthquake, Storm, Fire, Flood, Meteor }

[CreateAssetMenu(fileName = "DisasterSO", menuName = "Scriptable Objects/DisasterSO")]
public class DisasterData : ScriptableObject
{
    [Header("IDENTITY")]
    public string id;
    public DisasterType type;
    public string displayName;
    public Sprite icon;
}
