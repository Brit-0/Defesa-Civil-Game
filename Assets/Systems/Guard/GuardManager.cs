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
    }

    private void OnDisable()
    {
        GameEvents.UpgradePurchased -= OnUpgradePurchased;
    }

    #region CALL FLOW

    public void OpenCall(Call call)
    {
        if (call == null || call.house == null || call.house.HasTeam) return;

        currentCall = call;
        selectedGuard = null;
        callTab.SetActive(false);
        GameEvents.RaiseCallOpened(call);

        // Automação 1 (Central de rádio): a base mais próxima da casa é escolhida sozinha.
        CityGuard nearestGuard = Stats.AutomationLevel >= AutomationLevels.AutoBase ? GetNearestGuard(call.house) : null;
        if (nearestGuard == null)
        {
            selectBaseMode = true;
            FeedbacksManager.ShowText("Escolha a base encarregada para o chamado");
            return;
        }

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
        return LocationsManager.Instance != null ? LocationsManager.Instance.GetNearestGuard(house.transform.position) : null;
    }

    public void CancelCall(CallbackContext context)
    {
        // O PlayerInput chama em started, performed e canceled: só performed conta (B5).
        if (!context.performed) return;

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
        TeamSize = 0;
        callTab.SetActive(true);
    }

    public void CallSendTeam()
    {
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

        if (size < call.workersNeeded || size > ResourcesManager.Workers)
        {
            GameEvents.RaiseActionDenied(DeniedReason.NoWorkers, house.transform.position);
            return false;
        }

        int materialCost = Stats.GetMaterialCost(call);
        if (materialCost > ResourcesManager.BuildMaterial)
        {
            GameEvents.RaiseActionDenied(DeniedReason.NoMaterial, house.transform.position);
            return false;
        }

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

    #endregion

    #region TEAM SIZE

    public void IncreaseTeamSize()
    {
        if (currentCall == null) return;

        if (TeamSize + 1 > ResourcesManager.Workers)
        {
            GameEvents.RaiseActionDenied(DeniedReason.NoWorkers, currentCall.house.transform.position);
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
