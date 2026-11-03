using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Location : House
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !interactable) return;
        ExecuteLocationAction();
    }

    public abstract void ExecuteLocationAction();
}
