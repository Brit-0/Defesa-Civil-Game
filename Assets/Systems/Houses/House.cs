using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum Place { }

// A cena guarda currentState como número: nunca reordenar nem remover valores (B18).
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
    // Ligado pelo HouseFeedback (Agente C): com ele, House não mexe mais na rachadura do shader.
    public bool externalFeedback;

    [Header("CALL")]
    public Call currentCall;
    [SerializeField] private GameObject callDisplay;
    [SerializeField] private float workTime;
    [SerializeField] private Image progressCircle;
    [SerializeField] private Image progressIcon;
    [SerializeField] private GameObject complete;
    [SerializeField] private Sprite damagedIcon;
    [SerializeField] private Sprite fireIcon;
    protected bool isComplete;

    // Equipe a caminho, trabalhando ou esperando a coleta.
    public bool HasTeam { get; private set; }
    public bool IsWorking { get; private set; }
    public bool IsComplete => isComplete;
    public float RestockWorkTime => workTime;

    static readonly int crackFade = Shader.PropertyToID("_TextureLayer1Fade");
    //static readonly int outlineFade = Shader.PropertyToID("_OuterOutlineFade");

    protected virtual void Awake()
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

    protected bool CanClick(PointerEventData eventData)
    {
        return eventData.button == PointerEventData.InputButton.Left && interactable && !GameManager.InputBlocked;
    }

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
        // Sai do hover mesmo sem poder interagir: a casa pode ter sido travada com o mouse em cima.
        tweener.HoverExit();
        //sprite.GetComponent<Image>().materialForRendering.SetFloat(outlineFade, 0f);
        //GPS.main.ResetPath();
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (!CanClick(eventData)) return;

        if (isComplete)
        {
            ResolveCompletion();
            return;
        }

        if (currentState != State.Intact && !HasTeam)
        {
            GuardManager.Instance.OpenCall(currentCall);
        }
    }

    #endregion

    #region CALL

    [Button]
    public void GenerateCall()
    {
        ApplyProblem(State.Damaged, GuardManager.Instance.GetRandomDamage());
    }

    // Único jeito de uma casa ganhar problema. Só age em casa comum e intacta (GDD, seção 4).
    public bool ApplyProblem(State problem, int severity)
    {
        if (this is Location) return false;
        if (currentState != State.Intact) return false;
        if (problem != State.Damaged && problem != State.OnFire) return false;

        damageAmount = problem == State.Damaged ? Mathf.Max(GuardManager.Instance.MinDamage, severity) : 0;
        currentCall = new()
        {
            house = this,
            type = CallType.Problem
        };
        HasTeam = false;
        IsWorking = false;
        isComplete = false;

        SetState(problem);
        ShowCall();
        return true;
    }

    private void ShowCall()
    {
        callDisplay.SetActive(true);
    }

    // Chamado pelo GuardManager quando a equipe é enviada.
    public void AssignTeam()
    {
        HasTeam = true;
        interactable = false;
    }

    public void StartWorking()
    {
        IsWorking = true;
        callDisplay.SetActive(false);
        progressCircle.fillAmount = 0f;
        Sprite icon = GetIconByCallType(currentCall);
        progressIcon.sprite = icon;
        progressIcon.enabled = icon != null;
        progressCircle.gameObject.SetActive(true);
        GameEvents.RaiseWorkStarted(currentCall);
        progressCircle.DOFillAmount(1f, GuardManager.Instance.GetWorkTime(currentCall)).SetEase(Ease.Linear).SetLink(gameObject).OnComplete(CompleteWork);
    }

    private void CompleteWork()
    {
        IsWorking = false;
        isComplete = true;
        interactable = true;
        progressCircle.gameObject.SetActive(false);
        complete.SetActive(true);
        GameEvents.RaiseWorkCompleted(currentCall);
    }

    // Coleta. O material já saiu do estoque no envio, então a casa sempre é consertada.
    public void ResolveCompletion()
    {
        if (!isComplete) return;

        isComplete = false;
        complete.SetActive(false);
        callDisplay.SetActive(false);

        Call call = currentCall;
        int moneyEarned = 0;

        switch (call.type)
        {
            case CallType.Problem:
                moneyEarned = GuardManager.Instance.GetReward(call);
                SetState(State.Intact);
                ResourcesManager.Money += moneyEarned;
                break;
            case CallType.Restock:
                ResourcesManager.BuildMaterial += call.workersSent * GuardManager.Instance.MaterialPerWorker;
                break;
            default:
                break;
        }

        HasTeam = false;
        GuardManager.Instance.CallRetrieveTeam(call);
        GameEvents.RaiseCallResolved(call, moneyEarned);
        OnCallResolved();
    }

    protected virtual void OnCallResolved()
    {

    }

    private Sprite GetIconByCallType(Call call)
    {
        return call.type switch
        {
            CallType.Problem => currentState == State.OnFire ? fireIcon : damagedIcon,
            CallType.Restock => ResourcesManager.Instance.materialIcon,
            _ => null,
        };
    }

    #endregion

    #region STATES

    // Único lugar que muda currentState.
    public void SetState(State newState)
    {
        State before = currentState;
        if (before == newState) return;

        currentState = newState;
        if (newState == State.Intact) damageAmount = 0;
        RefreshStateVisual();
        GameEvents.RaiseHouseStateChanged(this, before, newState);
    }

    private void RefreshStateVisual()
    {
        bool isDamaged = currentState == State.Damaged;
        damageAmountLbl.text = damageAmount.ToString();
        damageAmountLbl.gameObject.SetActive(isDamaged);
        if (!externalFeedback) sprite.material.SetFloat(crackFade, isDamaged ? 1f : 0f);
    }

    #endregion
}
