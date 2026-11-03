using UnityEngine;

public class CityGuard : Location
{
    public override void ExecuteLocationAction()
    {
        if (GuardManager.selectBaseMode)
        {
            GuardManager.Instance.SelectBase(this);
        }
    }
}
