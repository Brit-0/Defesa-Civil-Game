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

    public int moneyPerCall = 10;

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

    // Provisório: no T05 estes números passam para o BalanceData e são lidos por Stats.
    [Header("PROVISIONAL BALANCE")]
    [SerializeField] private float dispatchInterval = .8f;
    [SerializeField] private float workTimePerDamage = 5f;
    [SerializeField] private float minWorkTime = 2f;
    [SerializeField] private float fireWorkTime = 4f;
    [SerializeField] private int fireReward = 15;
    [SerializeField] private int materialPerWorker = 1;
    [SerializeField] private int minDamage = 1;
    [SerializeField] private int maxDamage = 3;

    public int MaterialPerWorker => materialPerWorker;
    public int MinDamage => minDamage;

    private void Awake()
    {
        Instance = this;
    }

    #region BALANCE

    public int GetRandomDamage()
    {
        return Random.Range(minDamage, maxDamage + 1);
    }

    // GDD 3.1: tempo = tempo por ponto de dano × dano ÷ equipe, com um mínimo. Armazém tem tempo fixo.
    public float GetWorkTime(Call call)
    {
        if (call.type == CallType.Restock) return call.house.RestockWorkTime;

        int team = Mathf.Max(call.workersNeeded, call.workersSent);
        float total = call.house.currentState == State.OnFire ? fireWorkTime : workTimePerDamage * call.house.damageAmount;
        return Mathf.Max(minWorkTime, total / team);
    }

    // GDD 3.1: recompensa = dinheiro por ponto de dano × dano.
    public int GetReward(Call call)
    {
        if (call.type != CallType.Problem) return 0;

        return call.house.currentState == State.OnFire ? fireReward : moneyPerCall * call.house.damageAmount;
    }

    // Material que o envio gasta: só casa danificada usa material.
    public int GetMaterialCost(Call call)
    {
        if (call.type != CallType.Problem || call.house.currentState != State.Damaged) return 0;

        return call.house.damageAmount;
    }

    #endregion

    #region CALL FLOW

    public void OpenCall(Call call)
    {
        if (call == null || call.house == null || call.house.HasTeam) return;

        currentCall = call;
        selectedGuard = null;
        callTab.SetActive(false);
        selectBaseMode = true;
        FeedbacksManager.ShowText("Escolha a base encarregada para o chamado");
        GameEvents.RaiseCallOpened(call);
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

        int materialCost = GetMaterialCost(call);
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
            yield return new WaitForSeconds(dispatchInterval);
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
            yield return new WaitForSeconds(dispatchInterval);
        }
    }

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
