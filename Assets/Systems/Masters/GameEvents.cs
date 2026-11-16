using System;
using UnityEngine;

public enum GameState { Menu, Playing, Paused, Won, Lost }
public enum DeniedReason { NoWorkers, NoMaterial, NoMoney, MaxLevel }

// Contrato entre sistemas e apresentação (ARCHITECTURE.md, seção 6.1).
// Sistemas chamam Raise...; feel, áudio e HUD só assinam (OnEnable) e desassinam (OnDisable).
public static class GameEvents
{
    // Economia
    public static event Action<int, int> MoneyChanged;          // (antes, depois)
    public static event Action<int> MaterialChanged;
    public static event Action<int> WorkersChanged;
    public static event Action<int> PassiveIncomePaid;          // valor do Repasse da Prefeitura

    // Casas e chamados
    public static event Action<House, State, State> HouseStateChanged; // (casa, antes, depois)
    public static event Action<Call> CallOpened;       // jogador abriu o chamado
    public static event Action<Call> TeamDispatched;   // equipe saiu da base
    public static event Action<Call> WorkStarted;      // último trabalhador chegou
    public static event Action<Call> WorkCompleted;    // pronto para coletar
    public static event Action<Call, int> CallResolved; // (chamado, dinheiro ganho)
    public static event Action<Transform, Call> WorkerSpawned;  // trabalhador nasceu na rua
    public static event Action<Transform, Call> WorkerArrived;  // chegou ao destino, antes de sumir
    public static event Action<DeniedReason, Vector3> ActionDenied; // (motivo, posição em mundo)

    // Desastres
    public static event Action<DisasterData, float> DisasterWarning;   // (tipo, segundos até começar)
    public static event Action<DisasterData, Vector3> DisasterStarted; // (tipo, epicentro em mundo)
    public static event Action<Vector3> LightningStruck;               // raio da tempestade (T20)
    public static event Action<DisasterData> DisasterEnded;

    // Progressão
    public static event Action<UpgradeData, int> UpgradePurchased;     // (upgrade, novo nível)
    public static event Action<float> ChaosChanged;                    // 0..1
    public static event Action<GameState> GameStateChanged;

    #region RAISE

    public static void RaiseMoneyChanged(int before, int after) => MoneyChanged?.Invoke(before, after);
    public static void RaiseMaterialChanged(int amount) => MaterialChanged?.Invoke(amount);
    public static void RaiseWorkersChanged(int amount) => WorkersChanged?.Invoke(amount);
    public static void RaisePassiveIncomePaid(int amount) => PassiveIncomePaid?.Invoke(amount);

    public static void RaiseHouseStateChanged(House house, State before, State after) => HouseStateChanged?.Invoke(house, before, after);
    public static void RaiseCallOpened(Call call) => CallOpened?.Invoke(call);
    public static void RaiseTeamDispatched(Call call) => TeamDispatched?.Invoke(call);
    public static void RaiseWorkStarted(Call call) => WorkStarted?.Invoke(call);
    public static void RaiseWorkCompleted(Call call) => WorkCompleted?.Invoke(call);
    public static void RaiseCallResolved(Call call, int moneyEarned) => CallResolved?.Invoke(call, moneyEarned);
    public static void RaiseWorkerSpawned(Transform worker, Call call) => WorkerSpawned?.Invoke(worker, call);
    public static void RaiseWorkerArrived(Transform worker, Call call) => WorkerArrived?.Invoke(worker, call);
    public static void RaiseActionDenied(DeniedReason reason, Vector3 worldPosition) => ActionDenied?.Invoke(reason, worldPosition);

    public static void RaiseDisasterWarning(DisasterData disaster, float secondsUntilStart) => DisasterWarning?.Invoke(disaster, secondsUntilStart);
    public static void RaiseDisasterStarted(DisasterData disaster, Vector3 epicenter) => DisasterStarted?.Invoke(disaster, epicenter);
    public static void RaiseLightningStruck(Vector3 worldPosition) => LightningStruck?.Invoke(worldPosition);
    public static void RaiseDisasterEnded(DisasterData disaster) => DisasterEnded?.Invoke(disaster);

    public static void RaiseUpgradePurchased(UpgradeData upgrade, int newLevel) => UpgradePurchased?.Invoke(upgrade, newLevel);
    public static void RaiseChaosChanged(float chaos) => ChaosChanged?.Invoke(chaos);
    public static void RaiseGameStateChanged(GameState state) => GameStateChanged?.Invoke(state);

    #endregion

    // Com Domain Reload desligado os estáticos sobrevivem entre um Play e outro: limpa tudo antes da cena carregar.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearAll()
    {
        MoneyChanged = null;
        MaterialChanged = null;
        WorkersChanged = null;
        PassiveIncomePaid = null;

        HouseStateChanged = null;
        CallOpened = null;
        TeamDispatched = null;
        WorkStarted = null;
        WorkCompleted = null;
        CallResolved = null;
        WorkerSpawned = null;
        WorkerArrived = null;
        ActionDenied = null;

        DisasterWarning = null;
        DisasterStarted = null;
        LightningStruck = null;
        DisasterEnded = null;

        UpgradePurchased = null;
        ChaosChanged = null;
        GameStateChanged = null;
    }
}
