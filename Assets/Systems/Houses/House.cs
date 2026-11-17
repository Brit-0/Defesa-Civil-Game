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
    private Transform amountShadow;
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
    [Tooltip("Lado do ícone v2 do balão em fração do balão")]
    [SerializeField] private float callIconFill = 1.15f;
    private Image callIcon;
    private bool completeStyled;
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
    // Incêndio que cresce (ENERGIA_FOGO.md): nível de 1 ao máximo, 0 fora de OnFire.
    // Jet é a janela do minigame do jato, aberta quando a equipe chega; null fora dela.
    public int FireLevel { get; private set; }
    public FireJet Jet { get; private set; }
    private int fireChunks;  // partes de trabalho deste incêndio (uma por nível, mais uma por falha no jato)
    private float jetChunk;  // progresso que cada chama apagada adianta
    // Escorada no aviso do terremoto.
    public bool IsShored { get; private set; }
    // Quebrada por raio (docs/jam/TEMPESTADE.md): sem telhado, a chuva sobe o dano enquanto espera equipe.
    public bool IsRoofless { get; private set; }
    private float leakTimer;

    static readonly int crackFade = Shader.PropertyToID("_TextureLayer1Fade");
    //static readonly int outlineFade = Shader.PropertyToID("_OuterOutlineFade");

    protected virtual void Awake()
    {
        sprite.material = new(sprite.material);
    }

    protected virtual void Update()
    {
        TickFire();
        TickLeak();
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
    // storm: dano de raio, a casa fica destelhada e piora esperando equipe (docs/jam/TEMPESTADE.md).
    public bool ApplyProblem(State problem, int severity, bool storm = false)
    {
        if (this is Location) return false;
        if (currentState != State.Intact) return false;
        if (problem != State.Damaged && problem != State.OnFire) return false;

        IsRoofless = storm && problem == State.Damaged;
        leakTimer = 0f;
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
        Jet = null;

        SetState(problem);
        if (problem == State.OnFire) SetFireLevel(1);
        ShowCall();
        return true;
    }

    private void ShowCall()
    {
        RefreshCallIcon();
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
        // Fogo maior dá mais trabalho: uma parte por nível.
        fireChunks = currentState == State.OnFire ? Mathf.Max(1, FireLevel) : 0;
        if (fireChunks > 0) workDuration *= fireChunks;
        progressCircle.fillAmount = 0f;
        Sprite icon = GetIconByCallType(currentCall);
        progressIcon.sprite = icon;
        progressIcon.enabled = icon != null;
        progressCircle.gameObject.SetActive(true);

        // Ponto certo vale no conserto e no Armazém; o incêndio tem o jato.
        BalanceData balance = Stats.Balance;
        CritAvailable = currentState != State.OnFire;
        CritWindowStart = Random.Range(balance.critWindowMin, balance.critWindowMax);

        GameEvents.RaiseWorkStarted(currentCall);
        if (currentState == State.OnFire) OpenJet();
    }

    // O círculo de progresso anda no tempo da partida: a pausa segura o trabalho.
    // Com a janela do jato aberta o trabalho automático espera o jogador.
    private void TickWork()
    {
        if (!IsWorking) return;
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) return;

        if (Jet != null)
        {
            TickJet();
            return;
        }

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
        StyleComplete();
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

    // Ícones v2 do tema (docs/jam/ICONES_V2.md); sem eles, os do prefab e o do material.
    private Sprite GetIconByCallType(Call call)
    {
        FeelUITheme theme = FeelUI.Theme;
        return call.type switch
        {
            CallType.Problem => currentState switch
            {
                State.OnFire => Pick(theme != null ? theme.callFire : null, fireIcon),
                State.PowerOutage => Pick(theme != null ? theme.callPower : null, damagedIcon),
                _ => Pick(theme != null ? theme.callRepair : null, damagedIcon),
            },
            CallType.Restock => Pick(theme != null ? theme.callSupply : null, ResourcesManager.Instance.materialIcon),
            _ => null,
        };
    }

    private static Sprite Pick(Sprite preferred, Sprite fallback) => preferred != null ? preferred : fallback;

    // Balão do chamado com o ícone do tipo: era a caixinha crate_0 fixa no prefab para todo chamado (itens 5 e 19).
    // O ícone v2 tem contorno próprio e vai sozinho, sem o círculo liso atrás.
    private void RefreshCallIcon()
    {
        if (currentCall == null || callDisplay == null) return;
        if (callIcon == null)
        {
            Transform icon = callDisplay.transform.Find("Icon");
            callIcon = icon != null ? icon.GetComponent<Image>() : null;
            if (callIcon == null) return;
        }

        Sprite sprite = GetIconByCallType(currentCall);
        if (sprite == null) return;

        callIcon.sprite = sprite;
        callIcon.preserveAspect = true;
        Image balloon = callDisplay.GetComponent<Image>();
        if (sprite.rect.width < 32f || balloon == null) return;

        balloon.enabled = false;
        callIcon.rectTransform.sizeDelta = ((RectTransform)callDisplay.transform).rect.size * callIconFill;
    }

    // Selo de concluído v2 inteiro (disco verde com check) no lugar do círculo com o ícone quebrado (item 12)
    private void StyleComplete()
    {
        FeelUITheme theme = FeelUI.Theme;
        if (completeStyled || complete == null || theme == null || theme.callDone == null) return;

        Image disc = complete.GetComponent<Image>();
        if (disc == null) return;

        completeStyled = true;
        disc.sprite = theme.callDone;
        disc.color = Color.white;
        disc.preserveAspect = true;
        Transform icon = complete.transform.Find("Icon");
        if (icon != null) icon.gameObject.SetActive(false);
    }

    #endregion

    #region FIRE

    // O temporizador só para com a equipe trabalhando no local (ou com o fogo já apagado, esperando a coleta).
    private void TickFire()
    {
        if (currentState != State.OnFire || IsWorking || isComplete) return;
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

        fireTimeLeft -= Time.deltaTime;
        // Esperando equipe o fogo cresce: um nível a cada parte igual do temporizador.
        int level = FireRules.LevelFromTimer(FireTimeLeft01, FireEnergyData.Instance.fireMaxLevel);
        if (level > FireLevel) SetFireLevel(level);
        if (fireTimeLeft <= 0f) BurnOut();
    }

    // Casa destelhada esperando equipe: a chuva sobe o dano (material, tempo e recompensa sobem junto, pelo Stats).
    // Para quando a equipe é enviada: o material já saiu do estoque no envio.
    private void TickLeak()
    {
        if (!IsRoofless || currentState != State.Damaged || HasTeam) return;
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

        BalanceData balance = Stats.Balance;
        int before = damageAmount;
        damageAmount = StormLeak.Tick(damageAmount, ref leakTimer, Time.deltaTime, balance.stormLeakInterval, balance.stormLeakMaxDamage);
        if (damageAmount == before) return;

        RefreshStateVisual();
        GameEvents.RaiseStormLeaked(this, damageAmount);
    }

    private void SetFireLevel(int level)
    {
        if (level == FireLevel) return;

        FireLevel = level;
        GameEvents.RaiseFireLevelChanged(this, level);
    }

    // A equipe chegou: chamas em volta da casa para apagar arrastando o mouse (FireMinigame).
    private void OpenJet()
    {
        FireEnergyData data = FireEnergyData.Instance;
        Jet = new FireJet(FireRules.FlamesFor(FireLevel, data.jetFlamesBase, data.jetFlamesPerLevel), data.jetTime);
        jetChunk = (1f - workProgress) / Jet.Flames;
        GameEvents.RaiseFireJetOpened(this);
    }

    // A janela fechou com chama sobrando: o fogo cresce e a equipe segue no trabalho automático, mais longo.
    private void TickJet()
    {
        if (!Jet.Tick(Time.deltaTime)) return;

        Jet = null;
        FireRules.Grow(fireChunks, ref workProgress, ref workDuration);
        fireChunks++;
        progressCircle.fillAmount = workProgress;
        if (FireLevel < FireEnergyData.Instance.fireMaxLevel) SetFireLevel(FireLevel + 1);
        GameEvents.RaiseSkillCheck(this, SkillResult.FireGrew);
    }

    // Uma chama apagada pelo jato (FireMinigame). Todas apagadas: o fogo acaba na hora e paga o bônus.
    public bool DouseFlame()
    {
        if (Jet == null || !IsWorking || !Jet.Douse()) return false;

        if (!Jet.Succeeded)
        {
            GameEvents.RaiseSkillCheck(this, SkillResult.FireDouse);
            AdvanceWork(jetChunk);
            return true;
        }

        Jet = null;
        currentCall.rewardBonus += FireEnergyData.Instance.jetOutBonus;
        GameEvents.RaiseSkillCheck(this, SkillResult.FireOut);
        AdvanceWork(1f);
        return true;
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

        // No incêndio o clique na casa não faz nada: o jato é arrastar por cima das chamas (FireMinigame).
        if (IsWorking)
        {
            if (currentState != State.OnFire) TryCritical();
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
        if (newState != State.Damaged) IsRoofless = false;
        if (newState != State.OnFire)
        {
            Jet = null;
            SetFireLevel(0);
        }
        RefreshStateVisual();
        GameEvents.RaiseHouseStateChanged(this, before, newState);
    }

    private void RefreshStateVisual()
    {
        bool isDamaged = currentState == State.Damaged;
        damageAmountLbl.text = damageAmount.ToString();
        damageAmountLbl.gameObject.SetActive(isDamaged);
        // O fundo do número vai junto: no incêndio não há número (lista Para Arrumar, item 23)
        if (amountShadow == null) amountShadow = damageAmountLbl.transform.parent.Find("AmountShadow");
        if (amountShadow != null) amountShadow.gameObject.SetActive(isDamaged);
        // O fogo que apaga sozinho deixa a casa Damaged com o mesmo chamado: o balão troca de ícone
        if (currentState != State.Intact) RefreshCallIcon();
        if (!externalFeedback) sprite.material.SetFloat(crackFade, isDamaged ? 1f : 0f);
    }

    #endregion
}
