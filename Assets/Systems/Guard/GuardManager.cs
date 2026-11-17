using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

[System.Serializable]
public class Call
{
    public House house;
    public int workersNeeded = 1;
    public int workersSent;
    public int materialSpent;
    public CallType type;
    public CityGuard guard;
    // Mini games (GAMELOOP_REVISAO): fração a mais na recompensa (acerto no ponto certo, herói do incêndio).
    public float rewardBonus;
    public bool critical;
}

public enum CallType
{
    Problem,
    Restock
}

public class GuardManager : MonoBehaviour
{
    public static GuardManager Instance;

    private CityGuard selectedGuard;
    private Call currentCall;

    // A aba de chamado aberta agora (nulos fora dela): a estimativa de tempo da aba lê daqui.
    public Call CurrentCall => currentCall;
    public CityGuard SelectedGuard => selectedGuard;

    public static bool selectBaseMode;

    [Header("WORKERS")]
    [SerializeField] private GameObject workerPf;
    [SerializeField] private Transform workersParent;
    [SerializeField] private TMP_Text teamSizeLbl;
    private int teamSize;
    public int TeamSize
    {
        get => teamSize;
        set
        {
            teamSize = value;
            teamSizeLbl.text = teamSize.ToString();
        }
    }

    [Header("CALL")]
    [SerializeField] private GameObject callTab;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.UpgradePurchased += OnUpgradePurchased;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.UpgradePurchased -= OnUpgradePurchased;
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    // Partida encerrada: a aba de equipe e o "Escolha a base" não ficam abertos atrás da tela final.
    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Won || state == GameState.Lost) CloseCall();
    }

    #region CALL FLOW

    public void OpenCall(Call call)
    {
        if (call == null || call.house == null || call.house.HasTeam) return;

        currentCall = call;
        selectedGuard = null;
        callTab.SetActive(false);

        // Automação 1 (Central de rádio): a base mais próxima da casa é escolhida sozinha.
        // O modo sai antes do evento: quem escuta CallOpened (o "Escolha a base" do FeedbacksManager) já sabe se a base
        // vai ser pedida; antes o texto aparecia e sumia a cada chamado com a automação comprada (#535).
        CityGuard nearestGuard = Stats.AutomationLevel >= AutomationLevels.AutoBase ? GetNearestGuard(call.house) : null;
        selectBaseMode = nearestGuard == null;
        GameEvents.RaiseCallOpened(call);
        if (nearestGuard == null) return;

        // Automação 2 (Protocolo padrão): a equipe sugerida sai num clique. No Armazém o jogador ainda escolhe a equipe.
        if (Stats.AutomationLevel >= AutomationLevels.AutoTeam && call.type == CallType.Problem)
        {
            if (!TrySendTeam(call, nearestGuard, Stats.GetSuggestedTeam(call))) CloseCall();
            return;
        }

        SelectBase(nearestGuard);
    }

    private static CityGuard GetNearestGuard(House house)
    {
        // Só base com energia (ENERGIA_FOGO.md); antes de a energia entrar no jogo, todas têm.
        return EnergyManager.NearestPoweredGuard(house.transform.position);
    }

    public void CancelCall(CallbackContext context)
    {
        // O PlayerInput chama em started, performed e canceled: só performed conta (B5).
        if (!context.performed) return;
        // Na pausa o Esc só continua o jogo (PauseMenu): o chamado aberto fica como estava (#535)
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Paused) return;

        CloseCall();
    }

    private void CloseCall()
    {
        currentCall = null;
        selectedGuard = null;
        selectBaseMode = false;
        FeedbacksManager.HideText();
        callTab.SetActive(false);
    }

    public void SelectBase(CityGuard guard)
    {
        if (currentCall == null) return;

        selectBaseMode = false;
        selectedGuard = guard;
        FeedbacksManager.HideText();
        // A aba abre sempre vazia: o jogador escolhe quantos vão no + e no - (pedido do Pietro 17/11, #531).
        TeamSize = 0;
        callTab.SetActive(true);
        GameEvents.RaiseBaseSelected(currentCall, guard);
    }

    public void CallSendTeam()
    {
        // Os botões da aba não passam pelo clique das casas: fora de Playing não enviam ninguém.
        if (GameManager.InputBlocked) return;
        // Equipe ainda vazia não é falta de gente: a aba responde (CallEstimate) apontando o +.
        if (currentCall != null && TeamSize < currentCall.workersNeeded) return;

        TrySendTeam(currentCall, selectedGuard, TeamSize);
    }

    // Envio: a equipe inteira e o material saem do estoque aqui (GDD, seção 3). Negado dispara ActionDenied.
    public bool TrySendTeam(Call call, CityGuard guard, int size)
    {
        if (call == null || call.house == null || guard == null) return false;

        House house = call.house;
        if (house.HasTeam)
        {
            if (call == currentCall) CloseCall();
            return false;
        }

        DeniedReason? denied = EnergyManager.DeniesDispatch(call, guard) ? DeniedReason.NoEnergy : GetDeniedReason(call, size);
        if (denied.HasValue)
        {
            // Falta de gente é da base (é lá que ficam os trabalhadores); o resto aparece na casa.
            Vector3 where = denied.Value == DeniedReason.NoWorkers ? guard.transform.position : house.transform.position;
            GameEvents.RaiseActionDenied(denied.Value, where);
            return false;
        }

        int materialCost = Stats.GetMaterialCost(call);

        // B10: sem caminho nada é debitado, a casa continua livre e com o chamado aberto.
        List<Waypoint> path = GPS.main.FindShortestPath(guard.entry, house.entry);
        if (path == null)
        {
            Debug.LogError($"Sem caminho entre {guard.name} e {house.name}: equipe não enviada.");
            return false;
        }

        ResourcesManager.Workers -= size;
        ResourcesManager.BuildMaterial -= materialCost;

        call.workersSent = size;
        call.materialSpent = materialCost;
        call.guard = guard;
        house.AssignTeam();

        if (call == currentCall) CloseCall();

        GameEvents.RaiseTeamDispatched(call);
        StartCoroutine(SendTeam(call, guard, size, path));
        return true;
    }

    // O que impede o envio agora; null quando há gente e material.
    private static DeniedReason? GetDeniedReason(Call call, int size)
    {
        if (size < call.workersNeeded || size > ResourcesManager.Workers) return DeniedReason.NoWorkers;
        if (Stats.GetMaterialCost(call) > ResourcesManager.BuildMaterial) return DeniedReason.NoMaterial;
        return null;
    }

    // Tamanho, base e caminho chegam por parâmetro: abrir outro chamado no meio da saída não muda esta equipe (B14).
    private IEnumerator SendTeam(Call call, CityGuard guard, int size, List<Waypoint> path)
    {
        for (int i = 0; i < size; i++)
        {
            Transform newWorker = Instantiate(workerPf, guard.entry.transform.position, Quaternion.identity, workersParent).transform;
            StartCoroutine(GPS.main.FollowPath(newWorker, path, call, i == size - 1));
            yield return new WaitForSeconds(Stats.DispatchInterval);
        }
    }

    public void CallRetrieveTeam(Call call)
    {
        StartCoroutine(RetrieveTeam(call));
    }

    public IEnumerator RetrieveTeam(Call call)
    {
        int size = call.workersSent;
        CityGuard guard = call.guard;
        House house = call.house;

        List<Waypoint> path = GPS.main.FindShortestPath(house.entry, guard.entry);
        if (path == null)
        {
            Debug.LogError($"Sem caminho de volta entre {house.name} e {guard.name}: equipe devolvida direto ao estoque.");
            ResourcesManager.Workers += size;
            yield break;
        }

        for (int i = 0; i < size; i++)
        {
            Transform newWorker = Instantiate(workerPf, house.entry.transform.position, Quaternion.identity, workersParent).transform;
            StartCoroutine(GPS.main.FollowPath(newWorker, path, call, i == size - 1, true));
            yield return new WaitForSeconds(Stats.DispatchInterval);
        }
    }

    #endregion

    #region AUTOMATION

    private void OnUpgradePurchased(UpgradeData upgrade, int newLevel)
    {
        if (upgrade.id != UpgradeIds.Automation) return;

        // Automação 3 (Coleta automática): o que já estava pronto esperando o clique é coletado na hora da compra.
        if (newLevel >= AutomationLevels.AutoCollect) CollectAllCompleted();
    }

    private static void CollectAllCompleted()
    {
        if (LocationsManager.Instance == null) return;

        foreach (House house in LocationsManager.Instance.Houses)
        {
            if (house.IsComplete) house.ResolveCompletion();
        }
        foreach (Warehouse warehouse in LocationsManager.Instance.Warehouses)
        {
            if (warehouse.IsComplete) warehouse.ResolveCompletion();
        }
    }

    // Despacho automático da guarita do quarteirão (GUARITAS.md, v5A; antes era a Automação 4, na cidade inteira):
    // a equipe sugerida sai da base com energia mais perto da casa. Falta de gente, de material ou de energia não é
    // erro do jogador: o chamado só espera, sem ActionDenied. O Armazém continua manual.
    public bool TryAutoDispatch(House house)
    {
        if (!WaitsForTeam(house)) return false;

        Call call = house.currentCall;
        int size = Stats.GetSuggestedTeam(call);
        if (GetDeniedReason(call, size).HasValue) return false;

        CityGuard guard = GetNearestGuard(house);
        if (guard == null || EnergyManager.DeniesDispatch(call, guard)) return false;
        return TrySendTeam(call, guard, size);
    }

    // Casa com problema de verdade e sem equipe. Call é [Serializable]: casa intacta da cena tem um currentCall vazio
    // (house nulo), nunca null; por isso o estado decide, como no LocationsManager.GetTroubledHouses.
    public static bool WaitsForTeam(House house)
    {
        return house != null && house.currentState != State.Intact && !house.HasTeam
            && house.currentCall != null && house.currentCall.house == house && house.currentCall.type == CallType.Problem;
    }

    // Fogo primeiro (o que está mais perto de apagar sozinho); depois os consertos de menor dano, que gastam menos.
    public static int CompareUrgency(House a, House b)
    {
        bool aOnFire = a.currentState == State.OnFire;
        bool bOnFire = b.currentState == State.OnFire;
        if (aOnFire != bOnFire) return aOnFire ? -1 : 1;

        return aOnFire ? a.FireTimeLeft01.CompareTo(b.FireTimeLeft01) : a.damageAmount.CompareTo(b.damageAmount);
    }

    #endregion

    #region TEAM SIZE

    public void IncreaseTeamSize()
    {
        if (currentCall == null || GameManager.InputBlocked) return;

        if (TeamSize + 1 > ResourcesManager.Workers)
        {
            Transform from = selectedGuard != null ? selectedGuard.transform : currentCall.house.transform;
            GameEvents.RaiseActionDenied(DeniedReason.NoWorkers, from.position);
            return;
        }
        TeamSize++;
    }

    public void DecreaseTeamSize()
    {
        if (TeamSize - 1 < 0) return;
        TeamSize--;
    }

    #endregion
}
