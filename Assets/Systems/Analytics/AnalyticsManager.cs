using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public interface IAnalyzable
{
    public void EnterViewMode();
}

public enum ViewMode
{
    Eletricity
}

public class AnalyticsManager : MonoBehaviour
{
    [SerializeField] private Material houseMat;
    public ViewMode currentViewMode;

    public void SwitchViewMode(CallbackContext context)
    {
        if (context.started)
        {
            EnterViewMode();
        }
        else if (context.canceled)
        {
            ExitViewMode();
        }

    }

    public void EnterViewMode()
    {
        houseMat.SetFloat("_HologramFade", 1f);
        switch (currentViewMode)
        {
            case ViewMode.Eletricity:
                EletricityManager.main.EnterViewMode();
                break;
            default:
                break;
        }
    }

    public void ExitViewMode()
    {
        houseMat.SetFloat("_HologramFade", 0f);
        switch (currentViewMode)
        {
            case ViewMode.Eletricity:
                EletricityManager.main.ExitViewMode();
                break;
            default:
                break;
        }
    }
}
