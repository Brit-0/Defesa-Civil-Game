using DG.Tweening;
using NUnit.Framework.Internal.Execution;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum Place { }

public enum State 
{
    Intact,
    PowerOutage,
    Damaged,
    OnFire
}

public class House : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [SerializeField] private Image sprite;
    [SerializeField] private Tweener tweener;
    public bool interactable = true;
    public Waypoint entry;
    public State currentState;
    public bool isEnergized;

    [SerializeField] private float workTime;
    [SerializeField] private Image progressCircle;
    //static readonly int outlineFade = Shader.PropertyToID("_OuterOutlineFade");

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

    public void StartWorking()
    {
        progressCircle.fillAmount = 0f;
        progressCircle.gameObject.SetActive(true);
        progressCircle.DOFillAmount(1f, workTime).OnComplete(() =>
        {

        });
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

    public void GenerateCall()
    {
        switch (currentState)
        {
            case State.Intact:
                return;
            case State.PowerOutage:
                break;
            case State.Damaged:
                break;
            case State.OnFire:
                break;
            default:
                break;
        }
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        
    }

    #endregion
}
