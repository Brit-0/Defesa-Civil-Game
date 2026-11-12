using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
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
    [Header("BASIC REFERENCES")]
    [SerializeField] private Image sprite;
    [SerializeField] private Tweener tweener;
    public Waypoint entry;

    [Header("VARIABLES")]
    public bool interactable = true;
    public State currentState;
    public bool isEnergized;
    public int damageAmount;
    [SerializeField] private TMP_Text damageAmountLbl;

    [Header("CALL")]
    public Call currentCall;
    [SerializeField] private GameObject callDisplay;
    [SerializeField] private float workTime;
    [SerializeField] private Image progressCircle;
    [SerializeField] private Image progressIcon;
    [SerializeField] private GameObject complete;
    protected bool isComplete;
    //static readonly int outlineFade = Shader.PropertyToID("_OuterOutlineFade");

    private void Awake()
    {
        sprite.material = new(sprite.material);
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

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable) return;
        tweener.HoverEnter();
        //sprite.GetComponent<Image>().materialForRendering.SetFloat(outlineFade, 1f);
        //GPS.main.FindShortestPath(entry);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (!interactable) return;
        tweener.HoverExit();
        //sprite.GetComponent<Image>().materialForRendering.SetFloat(outlineFade, 0f);
        //GPS.main.ResetPath();
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !interactable) return;

        if (isComplete)
        {
            ResolveCompletion();
            return;
        }

        if (currentState != State.Intact)
        {
            GuardManager.Instance.OpenCall(currentCall);
        }
    }

    #endregion

    #region CALL

    [Button]
    public void GenerateCall()
    {
        Call newCall = new()
        {
            house = this,
            type = CallType.Problem
        };

        State problem = (State)Random.Range(1, 4);

        switch (problem)
        {
            case State.Intact:
                return;
            case State.PowerOutage:
                break;
            case State.Damaged:
                Break();
                break;
            case State.OnFire:
                break;
            default:
                break;
        }

        currentState = problem;
        currentCall = newCall;
        ShowCall();
    }

    private void ShowCall()
    {
        callDisplay.SetActive(true);
    }

    public void StartWorking()
    {
        callDisplay.SetActive(false);
        progressCircle.fillAmount = 0f;
        progressIcon.sprite = GetIconByCallType(currentCall);
        progressCircle.gameObject.SetActive(true);
        progressCircle.DOFillAmount(1f, workTime).SetEase(Ease.Linear).OnComplete(() =>
        {
            isComplete = true;
            interactable = true;
            progressCircle.gameObject.SetActive(false);
            complete.SetActive(true);
        });
    }

    public void ResolveCompletion()
    {
        complete.SetActive(false);

        switch (currentCall.type)
        {
            case CallType.Problem:
                switch (currentState)
                {
                    case State.Intact:
                        break;
                    case State.PowerOutage:
                        break;
                    case State.Damaged:
                        if (ResourcesManager.BuildMaterial >= damageAmount)
                        {
                            ResourcesManager.BuildMaterial -= damageAmount;
                            Repair();
                        }
                        break;
                    case State.OnFire:
                        break;
                    default:
                        break;
                }
                break;
            case CallType.Restock:
                ResourcesManager.BuildMaterial += currentCall.workersSent;
                break;
            default:
                break;
        }

        callDisplay.SetActive(false);
        GuardManager.Instance.CallRetrieveTeam(currentCall);
        isComplete = false;
    }

    private Sprite GetIconByCallType(Call call)
    {
        return call.type switch
        {
            CallType.Problem => currentState switch
            {
                State.Intact => null,
                State.PowerOutage => null,
                State.Damaged => null,
                State.OnFire => null,
                _ => null,
            },
            CallType.Restock => ResourcesManager.Instance.materialIcon,
            _ => null,
        };
    }

    #endregion

    #region STATES

    [Button]
    public void Break()
    {
        currentState = State.Damaged;
        damageAmount = Random.Range(1, 4);
        damageAmountLbl.text = damageAmount.ToString();
        damageAmountLbl.gameObject.SetActive(true);
        sprite.material.SetFloat("_TextureLayer1Fade", 1f);
    }

    public void Repair()
    {
        currentState = State.Intact;
        damageAmountLbl.gameObject.SetActive(false);
        sprite.material.SetFloat("_TextureLayer1Fade", 0f);
    }

    #endregion
}