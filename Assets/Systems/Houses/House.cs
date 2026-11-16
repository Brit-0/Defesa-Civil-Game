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

    // Incêndio (GDD, seção 4): 1 = acabou de pegar fogo, 0 = apagou. Fora de OnFire vale 0.
    private float fireTimeLeft;
    public float FireTimeLeft01 => currentState == State.OnFire && Stats.Balance.fireDuration > 0f ? Mathf.Clamp01(fireTimeLeft / Stats.Balance.fireDuration) : 0f;

    // Trabalho no local (0 a 1). Anda sozinho; os mini games adiantam ou atrasam.
    private float workProgress;
    private float workDuration;
    public float WorkProgress01 => IsWorking ? workProgress : 0f;
    public Image ProgressCircle => progressCircle;

    // Mini game do ponto certo (GAMELOOP_REVISAO): uma tentativa por trabalho, na janela sorteada.
    public bool CritAvailable { get; private set; }
    public float CritWindowStart { get; private set; }
    public float CritWindowSize => Stats.Balance.critWindowSize;
    // Mutirão no incêndio: cliques neste incêndio.
    public int FireClicks { get; private set; }
    // Escorada no aviso do terremoto.
    public bool IsShored { get; private set; }

    static readonly int crackFade = Shader.PropertyToID("_TextureLayer1Fade");
    //static readonly int outlineFade = Shader.PropertyToID("_OuterOutlineFade");

    protected virtual void Awake()
    {
        sprite.material = new(sprite.material);
    }

    protected virtual void Update()
    {
        TickFire();
        TickWork();
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
        if (TrySkillClick(eventData)) return;
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
        ApplyProblem(State.Damaged, Stats.GetRandomDamage());
    }

    [Button]
    public void GenerateFire()
    {
        ApplyProblem(State.OnFire, 0);
    }

    // Único jeito de uma casa ganhar problema. Só age em casa comum e intacta (GDD, seção 4).
    public bool ApplyProblem(State problem, int severity)
    {
        if (this is Location) return false;
        if (currentState != State.Intact) return false;
        if (problem != State.Damaged && problem != State.OnFire) return false;

        damageAmount = problem == State.Damaged ? Mathf.Max(Stats.MinDamage, severity) : 0;
        fireTimeLeft = problem == State.OnFire ? Stats.Balance.fireDuration : 0f;
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
        if (!TryChargeMissingMaterial())
        {
            SendTeamBack();
            return;
        }

        IsWorking = true;
        callDisplay.SetActive(false);
        workProgress = 0f;
        workDuration = Stats.GetWorkTime(currentCall);
        progressCircle.fillAmount = 0f;
        Sprite icon = GetIconByCallType(currentCall);
        progressIcon.sprite = icon;
        progressIcon.enabled = icon != null;
        progressCircle.gameObject.SetActive(true);

        // Ponto certo vale no conserto e no Armazém; incêndio é mutirão de cliques.
        BalanceData balance = Stats.Balance;
        CritAvailable = currentState != State.OnFire;
        CritWindowStart = Random.Range(balance.critWindowMin, balance.critWindowMax);
        FireClicks = 0;

        GameEvents.RaiseWorkStarted(currentCall);
    }

    // O círculo de progresso anda no tempo da partida: a pausa segura o trabalho.
    private void TickWork()
    {
        if (!IsWorking) return;
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

        AdvanceWork(workDuration > 0f ? Time.deltaTime / workDuration : 1f);
    }

    private void AdvanceWork(float amount)
    {
        workProgress = Mathf.Clamp01(workProgress + amount);
        progressCircle.fillAmount = workProgress;
        if (workProgress >= 1f) CompleteWork();
    }

    private void CompleteWork()
    {
        if (!IsWorking) return;

        CritAvailable = false;
        IsWorking = false;
        isComplete = true;
        interactable = true;
        progressCircle.gameObject.SetActive(false);
        complete.SetActive(true);
        GameEvents.RaiseWorkCompleted(currentCall);

        // Automação 3 (Coleta automática): a recompensa entra sozinha. Vale também para o Armazém.
        if (Stats.AutomationLevel >= AutomationLevels.AutoCollect && !GameManager.InputBlocked) ResolveCompletion();
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
                moneyEarned = Stats.GetReward(call);
                SetState(State.Intact);
                ResourcesManager.Money += moneyEarned;
                break;
            case CallType.Restock:
                ResourcesManager.BuildMaterial += Stats.GetRestockAmount(call);
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

    // O fogo apagou sozinho com a equipe a caminho: a casa virou Damaged e o conserto passa a gastar material, cobrado na chegada.
    private bool TryChargeMissingMaterial()
    {
        int missingMaterial = Stats.GetMaterialCost(currentCall) - currentCall.materialSpent;
        if (missingMaterial <= 0) return true;
        if (missingMaterial > ResourcesManager.BuildMaterial) return false;

        ResourcesManager.BuildMaterial -= missingMaterial;
        currentCall.materialSpent += missingMaterial;
        return true;
    }

    // Sem material para o conserto: a equipe volta para a base e o chamado fica aberto de novo.
    private void SendTeamBack()
    {
        HasTeam = false;
        interactable = true;
        ShowCall();
        GameEvents.RaiseActionDenied(DeniedReason.NoMaterial, transform.position);
        GuardManager.Instance.CallRetrieveTeam(currentCall);
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

    #region FIRE

    // O temporizador só para com a equipe trabalhando no local (ou com o fogo já apagado, esperando a coleta).
    private void TickFire()
    {
        if (currentState != State.OnFire || IsWorking || isComplete) return;
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

        fireTimeLeft -= Time.deltaTime;
        if (fireTimeLeft <= 0f) BurnOut();
    }

    // Fogo ignorado apaga sozinho e deixa a casa danificada. O chamado continua o mesmo.
    private void BurnOut()
    {
        fireTimeLeft = 0f;
        damageAmount = Stats.Balance.fireBurnoutDamage;
        SetState(State.Damaged);
    }

    #endregion

    #region MINI GAMES

    // Mini games (GAMELOOP_REVISAO): cliques de habilidade que nunca são obrigatórios e pagam mais que o automático.
    // Valem com a equipe trabalhando (mesmo com a casa travada para o despacho) e no aviso do terremoto.
    protected bool TrySkillClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || GameManager.InputBlocked) return false;

        if (IsWorking)
        {
            if (currentState == State.OnFire) FireClick();
            else TryCritical();
            return true;
        }

        if (DisasterManager.Instance != null && DisasterManager.Instance.CanShore(this))
        {
            TryShore();
            return true;
        }

        return false;
    }

    // Ponto certo: clicar com o progresso dentro da janela termina o trabalho na hora e paga mais.
    // Fora da janela o progresso volta um pouco. Uma tentativa por trabalho.
    private void TryCritical()
    {
        if (!CritAvailable) return;

        CritAvailable = false;
        BalanceData balance = Stats.Balance;
        bool hit = workProgress >= CritWindowStart && workProgress <= CritWindowStart + balance.critWindowSize;
        if (hit)
        {
            currentCall.critical = true;
            currentCall.rewardBonus += balance.critRewardBonus;
            GameEvents.RaiseSkillCheck(this, SkillResult.Critical);
            AdvanceWork(1f);
            return;
        }

        workProgress = Mathf.Max(0f, workProgress - balance.missPenalty);
        progressCircle.fillAmount = workProgress;
        GameEvents.RaiseSkillCheck(this, SkillResult.Miss);
    }

    // Mutirão: cada clique no incêndio com a equipe trabalhando adianta o trabalho; muitos cliques viram bônus de herói.
    private void FireClick()
    {
        BalanceData balance = Stats.Balance;
        FireClicks++;
        bool hero = FireClicks == balance.fireHeroClicks;
        if (hero) currentCall.rewardBonus += balance.fireHeroBonus;

        GameEvents.RaiseSkillCheck(this, hero ? SkillResult.FireHero : SkillResult.FireClick);
        AdvanceWork(balance.fireClickProgress);
    }

    // Escorar: no aviso do terremoto, casa no alvo gasta material agora para levar menos dano (ou nenhum).
    private void TryShore()
    {
        int cost = Stats.Balance.shoreMaterialCost;
        if (ResourcesManager.BuildMaterial < cost)
        {
            GameEvents.RaiseActionDenied(DeniedReason.NoMaterial, transform.position);
            return;
        }

        ResourcesManager.BuildMaterial -= cost;
        IsShored = true;
        GameEvents.RaiseSkillCheck(this, SkillResult.Shored);
    }

    // Chamado pelo terremoto quando a onda chega na casa escorada.
    public void ResolveShoring(bool held)
    {
        IsShored = false;
        if (!held) return;

        int reward = Stats.Balance.shoreHeldReward;
        if (reward > 0) ResourcesManager.Money += reward;
        GameEvents.RaiseSkillCheck(this, SkillResult.ShoreHeld);
    }

    public void ClearShoring()
    {
        IsShored = false;
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
