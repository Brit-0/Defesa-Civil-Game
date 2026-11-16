using UnityEngine;

// Valores de jogo já prontos para uso: BalanceData + efeito dos upgrades.
// Nenhum sistema guarda número de balanceamento: todos perguntam aqui.
public static class Stats
{
    private const string BalancePath = "Balance/BalanceData";

    private static BalanceData balance;
    public static BalanceData Balance
    {
        get
        {
            if (balance == null)
            {
                balance = Resources.Load<BalanceData>(BalancePath);
                if (balance == null)
                {
                    Debug.LogWarning($"BalanceData não encontrado em Resources/{BalancePath}: usando os valores padrão da classe. Rode Tools/Jam/Create Default Data.");
                    balance = ScriptableObject.CreateInstance<BalanceData>();
                }
            }
            return balance;
        }
    }

    #region WORKERS

    public static float WorkerSpeed => Balance.workerSpeed;
    public static float DispatchInterval => Balance.dispatchInterval;
    public static int MaterialPerWorker => Balance.materialPerWorker;

    #endregion

    #region CALLS

    public static int MinDamage => Balance.minDamage;
    public static int MaxDamage => Balance.maxDamage;

    public static int GetRandomDamage()
    {
        return Random.Range(Balance.minDamage, Balance.maxDamage + 1);
    }

    // GDD 3.1: tempo = tempo por ponto de dano × dano ÷ equipe, com um mínimo. Armazém tem tempo fixo.
    public static float GetWorkTime(Call call)
    {
        if (call.type == CallType.Restock) return Balance.restockTime;

        int team = Mathf.Max(call.workersNeeded, call.workersSent);
        float total = call.house.currentState == State.OnFire ? Balance.fireWorkTime : Balance.workTimePerDamage * call.house.damageAmount;
        return Mathf.Max(Balance.minWorkTime, total / team);
    }

    // GDD 3.1: recompensa = dinheiro por ponto de dano × dano.
    public static int GetReward(Call call)
    {
        if (call.type != CallType.Problem) return 0;

        return call.house.currentState == State.OnFire ? Balance.fireReward : Balance.moneyPerDamage * call.house.damageAmount;
    }

    // Material que o envio gasta: só casa danificada usa material.
    public static int GetMaterialCost(Call call)
    {
        if (call.type != CallType.Problem || call.house.currentState != State.Damaged) return 0;

        return call.house.damageAmount;
    }

    #endregion

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        balance = null;
    }
}
