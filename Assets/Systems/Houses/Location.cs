using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class Location : House
{
    protected Call defaultCall;

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !interactable) return;

        if (isComplete)
        {
            base.OnPointerClick(eventData);
        }
        else
        {
            base.OnPointerClick(eventData);
            ExecuteLocationAction();
        }
    }

    public abstract void ExecuteLocationAction();
}
