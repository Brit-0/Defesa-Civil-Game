using UnityEngine;

public class Warehouse : Location
{
    private void Awake()
    {
        defaultCall = new()
        {
            house = this,
            type = CallType.Restock
        };
    }

    private void Start()
    {
        currentCall = defaultCall;
    }

    public override void ExecuteLocationAction()
    {
        GuardManager.Instance.OpenCall(currentCall);
    }
}
