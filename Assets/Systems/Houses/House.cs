using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum Place { }

public enum State 
{
    Intact,
    PowerOutage,
    Damaged,
}


public class House : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [SerializeField] private Image sprite;
    [SerializeField] private Tweener tweener;
    public bool interactable = true;
    public Waypoint entry;
    public State currentState;
    public bool isEnergized;
    static readonly int outlineFade = Shader.PropertyToID("_OuterOutlineFade");

    private void Awake()
    {
        //sprite.GetComponent<Image>().material = new(locationMat);
    }

    public void Energize()
    {
        isEnergized = true;
    }

    public void ShowEnergyStatus()
    {
        
    }

    #region INTERFACES

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !interactable) return;

        tweener.Hold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !interactable) return;

        tweener.Release();

        //GPS.main.StartCoroutine(GPS.main.FollowPath(this));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable) return;
        tweener.HoverEnter();
        //sprite.GetComponent<Image>().materialForRendering.SetFloat(outlineFade, 1f);

        //GPS.main.FindShortestPath(entry);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!interactable) return;
        tweener.HoverExit();
        //sprite.GetComponent<Image>().materialForRendering.SetFloat(outlineFade, 0f);
        //GPS.main.ResetPath();
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        
    }

    #endregion
}
