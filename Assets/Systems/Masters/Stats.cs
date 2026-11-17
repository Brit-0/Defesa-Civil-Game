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

    #region UPGRADES

    public static int GetUpgradeLevel(string id)
    {
        return UpgradesManager.Instance != null ? UpgradesManager.Instance.GetLevel(id) : 0;
    }

    // Efeito acumulado do upgrade (effectPerLevel × nível). Sem UpgradesManager na cena, 0.
    public static float GetUpgradeEffect(string id)
    {
        return UpgradesManager.Instance != null ? UpgradesManager.Instance.GetEffect(id) : 0f;
    }

    // Só vale o nível que tem energia (ENERGIA_FOGO.md); antes de a energia entrar no jogo, o nível comprado.
    public static int AutomationLevel => EnergyManager.LimitAutomation(GetUpgradeLevel(UpgradeIds.Automation));

    #endregion

    #region WORKERS

    // speed: +X% de velocidade por nível.
    public static float WorkerSpeed => Balance.workerSpeed * (1f + GetUpgradeEffect(UpgradeIds.Speed));
    public static float DispatchInterval => Balance.dispatchInterval;

    // Até o trabalho começar: o último da equipe sai (equipe − 1) intervalos depois do primeiro e anda o caminho
    // inteiro (GuardManager.SendTeam e GPS.FollowPath); a obra começa quando ele chega. Sem equipe, 0.
    public static float GetTravelTime(float pathLength, int teamSize)
    {
        if (teamSize <= 0) return 0f;

        return (teamSize - 1) * DispatchInterval + pathLength / Mathf.Max(.01f, WorkerSpeed);
    }
    // cart: +X de material por trabalhador por nível.
    public static int MaterialPerWorker => Balance.materialPerWorker + Mathf.RoundToInt(GetUpgradeEffect(UpgradeIds.Cart));

    #endregion

    #region CALLS

    public static int MinDamage => Balance.minDamage;
    public static int MaxDamage => Balance.maxDamage;

    public static int GetRandomDamage()
    {
        return Random.Range(Balance.minDamage, Balance.maxDamage + 1);
    }

    // Equipe sugerida (GDD 3.1): 1 trabalhador por ponto de dano, limitada aos disponíveis, nunca abaixo do mínimo do chamado.
    // No Armazém: gente para trazer o material que falta aos consertos que esperam equipe (GAMELOOP_REVISAO).
    public static int GetSuggestedTeam(Call call)
    {
        int wanted = call.type == CallType.Restock ? GetRestockNeed() : Mathf.Max(call.workersNeeded, call.house.damageAmount);
        return Mathf.Max(call.workersNeeded, Mathf.Min(wanted, ResourcesManager.Workers));
    }

    private static int GetRestockNeed()
    {
        if (LocationsManager.Instance == null) return 1;

        int missing = -ResourcesManager.BuildMaterial;
        foreach (House house in LocationsManager.Instance.Houses)
        {
            if (house.currentState == State.Damaged && !house.HasTeam) missing += house.damageAmount;
        }
        return Mathf.Max(1, Mathf.CeilToInt(missing / (float)Mathf.Max(1, MaterialPerWorker)));
    }

    // Bônus de ocorrência encerrada: por casa atingida, multiplicado se foi rápido.
    public static int GetClearBonus(int houses, bool fast)
    {
        float bonus = Balance.clearBonusPerHouse * houses * (fast ? Balance.fastClearMultiplier : 1f);
        return Mathf.RoundToInt(bonus * (1f + GetUpgradeEffect(UpgradeIds.Reward)));
    }

    // GDD 3.1: tempo = tempo por ponto de dano × dano ÷ equipe, com um mínimo. Armazém tem tempo fixo.
    public static float GetWorkTime(Call call)
    {
        return GetWorkTime(call, call.workersSent);
    }

    // O mesmo tempo para uma equipe de tamanho dado (estimativa da aba de chamado, antes de enviar).
    public static float GetWorkTime(Call call, int teamSize)
    {
        if (call.type == CallType.Restock) return Balance.restockTime;

        int team = Mathf.Max(call.workersNeeded, teamSize);
        float total = call.house.currentState == State.OnFire ? Balance.fireWorkTime : Balance.workTimePerDamage * call.house.damageAmount;
        // tools: -X% de tempo de trabalho por nível.
        total *= Mathf.Max(0f, 1f - GetUpgradeEffect(UpgradeIds.Tools));
        return Mathf.Max(Balance.minWorkTime, total / team);
    }

    // GDD 3.1: recompensa = dinheiro por ponto de dano × dano.
    public static int GetReward(Call call)
    {
        if (call.type != CallType.Problem) return 0;

        int reward = call.house.currentState == State.OnFire ? Balance.fireReward : Balance.moneyPerDamage * call.house.damageAmount;
        // reward: +X% de dinheiro por conserto por nível; mini games somam o bônus do chamado.
        return Mathf.RoundToInt(reward * (1f + GetUpgradeEffect(UpgradeIds.Reward) + call.rewardBonus));
    }

    // Material que a ida ao Armazém traz; o acerto no ponto certo traz uma parte a mais.
    public static int GetRestockAmount(Call call)
    {
        int amount = call.workersSent * MaterialPerWorker;
        return call.critical ? amount + Mathf.CeilToInt(amount * Balance.critRewardBonus) : amount;
    }

    // Material que o envio gasta: só casa danificada usa material.
    public static int GetMaterialCost(Call call)
    {
        if (call.type != CallType.Problem || call.house.currentState != State.Damaged) return 0;

        return call.house.damageAmount;
    }

    #endregion

    #region PASSIVE INCOME

    // Repasse = valor por casa × nível do income × casas intactas × (1 − caos), arredondado.
    public static int GetPassiveIncome()
    {
        if (LocationsManager.Instance == null) return 0;

        int intactHouses = LocationsManager.Instance.GetIntactHouses().Count;
        float chaos = ChaosManager.Instance != null ? ChaosManager.Instance.Chaos : 0f;
        return Mathf.RoundToInt(Balance.incomePerHouse * GetUpgradeEffect(UpgradeIds.Income) * intactHouses * (1f - chaos));
    }

    #endregion

    #region CHAOS

    // Peso de uma casa no caos: intacta não pesa; em chamas pesa mais que danificada.
    public static float GetChaosWeight(State state)
    {
        if (state == State.Intact) return 0f;

        return state == State.OnFire ? Balance.chaosWeightFire : Balance.chaosWeightDamaged;
    }

    #endregion

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        balance = null;
    }
}
