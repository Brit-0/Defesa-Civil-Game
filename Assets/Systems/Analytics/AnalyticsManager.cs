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

    // O Tab agora abre e fecha a aba de melhorias (UpgradePanelShortcuts, v6A). Esta visao do prototipo (casas em
    // holograma e area dos postes enquanto o Tab fica apertado) saiu do Tab; o PlayerInput da cena ainda chama aqui.
    public void SwitchViewMode(CallbackContext context)
    {
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
