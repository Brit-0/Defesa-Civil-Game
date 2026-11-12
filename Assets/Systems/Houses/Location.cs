using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Location : House
{
    protected Call defaultCall;
    [Header("INFO")]
    [SerializeField] private LocationData data;
    [SerializeField] private InfoPanel infoPanel;

    private void Start()
    {
        infoPanel.Set(data);
    }

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

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        infoPanel.Show();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        infoPanel.Hide();
    }

    public abstract void ExecuteLocationAction();
}
