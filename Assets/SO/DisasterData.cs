using UnityEngine;

public enum DisasterType { Earthquake, Storm, Fire }

[CreateAssetMenu(fileName = "DisasterSO", menuName = "Scriptable Objects/DisasterSO")]
public class DisasterData : ScriptableObject
{
    [Header("IDENTITY")]
    public string id;
    public DisasterType type;
    public string displayName;
    public Sprite icon;
}
