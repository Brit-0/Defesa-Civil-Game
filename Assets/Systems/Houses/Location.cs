using UnityEngine;
using UnityEngine.EventSystems;

public abstract class Location : House
{
    protected Call defaultCall;
    [Header("INFO")]
    [SerializeField] private LocationData data;
    [SerializeField] private InfoPanel infoPanel;

    protected virtual void Start()
    {
        if (infoPanel != null && data != null) infoPanel.Set(data);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        // Ponto certo também vale no Armazém com a equipe carregando (GAMELOOP_REVISAO).
        if (TrySkillClick(eventData)) return;
        if (!CanClick(eventData)) return;

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
        if (infoPanel != null) infoPanel.Show();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        if (infoPanel != null) infoPanel.Hide();
    }

    public abstract void ExecuteLocationAction();
}
