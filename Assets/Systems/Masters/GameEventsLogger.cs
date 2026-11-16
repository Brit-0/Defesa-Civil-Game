using UnityEngine;

// Debug opcional: ponha num GameObject da cena para ver no console cada evento do contrato.
public class GameEventsLogger : MonoBehaviour
{
    private void OnEnable()
    {
        GameEvents.MoneyChanged += OnMoneyChanged;
        GameEvents.MaterialChanged += OnMaterialChanged;
        GameEvents.WorkersChanged += OnWorkersChanged;
        GameEvents.PassiveIncomePaid += OnPassiveIncomePaid;

        GameEvents.HouseStateChanged += OnHouseStateChanged;
        GameEvents.CallOpened += OnCallOpened;
        GameEvents.TeamDispatched += OnTeamDispatched;
        GameEvents.WorkStarted += OnWorkStarted;
        GameEvents.WorkCompleted += OnWorkCompleted;
        GameEvents.CallResolved += OnCallResolved;
        GameEvents.WorkerSpawned += OnWorkerSpawned;
        GameEvents.WorkerArrived += OnWorkerArrived;
        GameEvents.ActionDenied += OnActionDenied;

        GameEvents.DisasterWarning += OnDisasterWarning;
        GameEvents.DisasterStarted += OnDisasterStarted;
        GameEvents.LightningStruck += OnLightningStruck;
        GameEvents.DisasterEnded += OnDisasterEnded;

        GameEvents.UpgradePurchased += OnUpgradePurchased;
        GameEvents.ChaosChanged += OnChaosChanged;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.MoneyChanged -= OnMoneyChanged;
        GameEvents.MaterialChanged -= OnMaterialChanged;
        GameEvents.WorkersChanged -= OnWorkersChanged;
        GameEvents.PassiveIncomePaid -= OnPassiveIncomePaid;

        GameEvents.HouseStateChanged -= OnHouseStateChanged;
        GameEvents.CallOpened -= OnCallOpened;
        GameEvents.TeamDispatched -= OnTeamDispatched;
        GameEvents.WorkStarted -= OnWorkStarted;
        GameEvents.WorkCompleted -= OnWorkCompleted;
        GameEvents.CallResolved -= OnCallResolved;
        GameEvents.WorkerSpawned -= OnWorkerSpawned;
        GameEvents.WorkerArrived -= OnWorkerArrived;
        GameEvents.ActionDenied -= OnActionDenied;

        GameEvents.DisasterWarning -= OnDisasterWarning;
        GameEvents.DisasterStarted -= OnDisasterStarted;
        GameEvents.LightningStruck -= OnLightningStruck;
        GameEvents.DisasterEnded -= OnDisasterEnded;

        GameEvents.UpgradePurchased -= OnUpgradePurchased;
        GameEvents.ChaosChanged -= OnChaosChanged;
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    private void OnMoneyChanged(int before, int after) => Log($"MoneyChanged {before} -> {after}");
    private void OnMaterialChanged(int amount) => Log($"MaterialChanged {amount}");
    private void OnWorkersChanged(int amount) => Log($"WorkersChanged {amount}");
    private void OnPassiveIncomePaid(int amount) => Log($"PassiveIncomePaid {amount}");

    private void OnHouseStateChanged(House house, State before, State after) => Log($"HouseStateChanged {NameOf(house)}: {before} -> {after}");
    private void OnCallOpened(Call call) => Log($"CallOpened {Describe(call)}");
    private void OnTeamDispatched(Call call) => Log($"TeamDispatched {Describe(call)}");
    private void OnWorkStarted(Call call) => Log($"WorkStarted {Describe(call)}");
    private void OnWorkCompleted(Call call) => Log($"WorkCompleted {Describe(call)}");
    private void OnCallResolved(Call call, int moneyEarned) => Log($"CallResolved {Describe(call)}, dinheiro {moneyEarned}");
    private void OnWorkerSpawned(Transform worker, Call call) => Log($"WorkerSpawned {Describe(call)}");
    private void OnWorkerArrived(Transform worker, Call call) => Log($"WorkerArrived {Describe(call)}");
    private void OnActionDenied(DeniedReason reason, Vector3 worldPosition) => Log($"ActionDenied {reason} em {worldPosition}");

    private void OnDisasterWarning(DisasterData disaster, float secondsUntilStart) => Log($"DisasterWarning {NameOf(disaster)} em {secondsUntilStart} s");
    private void OnDisasterStarted(DisasterData disaster, Vector3 epicenter) => Log($"DisasterStarted {NameOf(disaster)} em {epicenter}");
    private void OnLightningStruck(Vector3 worldPosition) => Log($"LightningStruck em {worldPosition}");
    private void OnDisasterEnded(DisasterData disaster) => Log($"DisasterEnded {NameOf(disaster)}");

    private void OnUpgradePurchased(UpgradeData upgrade, int newLevel) => Log($"UpgradePurchased {NameOf(upgrade)} nível {newLevel}");
    private void OnChaosChanged(float chaos) => Log($"ChaosChanged {chaos:0.00}");
    private void OnGameStateChanged(GameState state) => Log($"GameStateChanged {state}");

    private static void Log(string message)
    {
        Debug.Log($"[GameEvents] {message}");
    }

    private static string NameOf(Object target)
    {
        return target != null ? target.name : "nulo";
    }

    private static string Describe(Call call)
    {
        if (call == null) return "chamado nulo";
        return $"{call.type} em {NameOf(call.house)}, base {NameOf(call.guard)}, equipe {call.workersSent}";
    }
}
