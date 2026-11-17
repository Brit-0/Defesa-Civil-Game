using UnityEngine;
using UnityEngine.EventSystems;

// Clique e hover do quarteirão (área no mundo e marcador no HUD) vão para o RodBlock.
public class RodPointer : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public RodBlock block;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && block != null) block.OnClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (block != null) block.OnPointer(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (block != null) block.OnPointer(false);
    }
}
