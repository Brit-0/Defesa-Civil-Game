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


public class Location : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Waypoint entry;
    public Place place;
    [SerializeField] private Transform sprite;
    [SerializeField] private Tweener tweener;
    [SerializeField] private Material locationMat;
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

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        tweener.Hold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        tweener.Release();

        GPS.main.StartCoroutine(GPS.main.FollowPath(this));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tweener.HoverEnter();
        sprite.GetComponent<Image>().materialForRendering.SetFloat(outlineFade, 1f);

        GPS.main.FindShortestPath(entry);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tweener.HoverExit();
        sprite.GetComponent<Image>().materialForRendering.SetFloat(outlineFade, 0f);
        GPS.main.ResetPath();
    }
}
