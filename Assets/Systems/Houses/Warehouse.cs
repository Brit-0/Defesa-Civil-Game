using UnityEngine;

public class Warehouse : Location
{
    public override void ExecuteLocationAction()
    {
        GuardManager.Instance.OpenCall(new() { house = this, type = CallType.Restock });
    }
}
