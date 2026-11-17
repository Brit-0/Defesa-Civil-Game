using UnityEngine;
using UnityEngine.EventSystems;

//Cada linha avisa o painel do hover, para a janelinha com a descrição
public class UpgradeRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private UpgradeButton row;

    private void Awake()
    {
        row = GetComponent<UpgradeButton>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UpgradePanel panel = UpgradePanel.Instance;
        if (panel != null && panel.Tooltip != null) panel.Tooltip.Show(row);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpgradePanel panel = UpgradePanel.Instance;
        if (panel != null && panel.Tooltip != null) panel.Tooltip.Hide(row);
    }

    private void OnDisable()
    {
        UpgradePanel panel = UpgradePanel.Instance;
        if (panel != null && panel.Tooltip != null) panel.Tooltip.Hide(row);
    }
}
