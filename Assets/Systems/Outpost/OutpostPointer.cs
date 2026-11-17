using UnityEngine;
using UnityEngine.EventSystems;

// Clique e hover do quarteirão da guarita (área no mundo e marcador no HUD) vão para o OutpostBlock.
public class OutpostPointer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public OutpostBlock site;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && site != null) site.OnClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (site != null) site.OnPointer(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (site != null) site.OnPointer(false);
    }
}
