using System.Collections.Generic;
using UnityEngine;

public static class EnergyIds
{
    public const string Generator = "generator";
}

// Energia (docs/jam/ENERGIA_FOGO.md). Entra no meio da partida, com o segundo tutorial.
// Bases, depósitos e cada nível de automação gastam energia; o gerador (upgrade generator) dá a capacidade.
// Sobrecarga: o que não cabe desliga (automação primeiro, depois depósitos, por último bases).
// Apagão: um raio de tempestade derruba a rede inteira até o jogador clicar em RELIGAR (ou passar o tempo).
// Base sem energia não manda equipe; depósito sem energia não entrega material; automação sem energia não age.
public class EnergyManager : MonoBehaviour
{
    public static EnergyManager Instance;

    [Header("TECH")]
    [Tooltip("Segundos entre uma recontagem das bases e depósitos ativos: base ou depósito novo entra sozinho na rede.")]
    [SerializeField] private float recountInterval = 1f;

    public bool Unlocked { get; private set; }
    public bool Blackout { get; private set; }
    public float BlackoutTimeLeft { get; private set; }
    public int Demand { get; private set; }
    public int Capacity { get; private set; }
    public int Used { get; private set; }
    public int AutomationBought { get; private set; }
    public int AutomationPowered { get; private set; }
    public bool Overloaded => Unlocked && !Blackout && Demand > Capacity;
    public int GeneratorLevel => Stats.GetUpgradeLevel(EnergyIds.Generator);
    public float BlackoutTimeLeft01 => Data.blackoutAutoRestore > 0f ? Mathf.Clamp01(BlackoutTimeLeft / Data.blackoutAutoRestore) : 0f;
    public IReadOnlyList<EnergyConsumer> Consumers => consumers;

    private static FireEnergyData Data => FireEnergyData.Instance;

    private readonly List<EnergyConsumer> consumers = new();
    private readonly Dictionary<House, bool> powered = new();
    private readonly Dictionary<House, int> firstSeen = new();
    private int seenCount;
    private float playTime;
    private float recountTimer;
    private bool stormBlackoutPending;
    private Tutorial firstTutorial;
    private bool tutorialChecked;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.UpgradePurchased += OnUpgradePurchased;
        GameEvents.DisasterStarted += OnDisasterStarted;
        GameEvents.LightningStruck += OnLightningStruck;
        GameEvents.DisasterEnded += OnDisasterEnded;
    }

    private void OnDisable()
    {
        GameEvents.UpgradePurchased -= OnUpgradePurchased;
        GameEvents.DisasterStarted -= OnDisasterStarted;
        GameEvents.LightningStruck -= OnLightningStruck;
        GameEvents.DisasterEnded -= OnDisasterEnded;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

        // O tempo em que o tutorial segura os desastres não conta, igual à rampa do DisasterManager.
        if (DisasterManager.Instance == null || !DisasterManager.Instance.IsHeld) playTime += Time.deltaTime;

        if (!Unlocked)
        {
            if (playTime >= Data.unlockTime && FirstTutorialDone()) Unlock();
            return;
        }

        if (Blackout)
        {
            BlackoutTimeLeft -= Time.deltaTime;
            if (BlackoutTimeLeft <= 0f) Restore();
        }

        recountTimer -= Time.deltaTime;
        if (recountTimer <= 0f) Refresh();
    }

    #region PUBLIC API

    // Sem o EnergyManager na cena, ou antes de a energia entrar no jogo, tudo funciona como antes.
    public static bool IsPowered(House location)
    {
        EnergyManager grid = Instance;
        if (grid == null || !grid.Unlocked || location == null) return true;
        if (grid.Blackout) return false;

        return !grid.powered.TryGetValue(location, out bool on) || on;
    }

    // Stats.AutomationLevel passa por aqui: só vale o nível de automação que tem energia.
    public static int LimitAutomation(int level)
    {
        EnergyManager grid = Instance;
        if (grid == null || !grid.Unlocked) return level;

        return Mathf.Min(level, grid.AutomationPowered);
    }

    // GuardManager: a base com energia mais perto da casa (null se nenhuma tem).
    public static CityGuard NearestPoweredGuard(Vector3 position)
    {
        if (LocationsManager.Instance == null) return null;

        CityGuard nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (CityGuard guard in LocationsManager.Instance.Guards)
        {
            if (guard == null || !guard.isActiveAndEnabled || !IsPowered(guard)) continue;

            float distance = LocationsManager.Distance(guard.transform.position, position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = guard;
            }
        }
        return nearest;
    }

    // GuardManager.TrySendTeam: base sem energia não manda equipe; depósito sem energia não recebe.
    public static bool DeniesDispatch(Call call, CityGuard guard)
    {
        if (!IsPowered(guard)) return true;
        return call != null && call.type == CallType.Restock && !IsPowered(call.house);
    }

    // Botão RELIGAR (EnergyHud) e segundo tutorial.
    public void Restore()
    {
        if (!Blackout) return;

        Blackout = false;
        BlackoutTimeLeft = 0f;
        Refresh();
        GameEvents.RaiseBlackoutChanged(false, Vector3.zero);
    }

    // Raio da tempestade e segundo tutorial.
    public void StartBlackout(Vector3 worldPosition)
    {
        if (!Unlocked || Blackout) return;

        Blackout = true;
        BlackoutTimeLeft = Data.blackoutAutoRestore;
        Refresh();
        GameEvents.RaiseBlackoutChanged(true, worldPosition);
    }

    // Debug no Inspector e testes no Play: liga a energia sem esperar o unlockTime.
    [Sirenix.OdinInspector.Button]
    public void Unlock()
    {
        if (Unlocked) return;

        Unlocked = true;
        Refresh();
        GameEvents.RaiseEnergyUnlocked();
    }

    #endregion

    #region GRID

    public void Refresh()
    {
        recountTimer = recountInterval;
        if (!Unlocked) return;

        int oldDemand = Demand, oldCapacity = Capacity, oldAutomation = AutomationPowered, oldBought = AutomationBought;

        consumers.Clear();
        foreach (CityGuard guard in FindObjectsByType<CityGuard>()) AddLocation(guard, EnergyConsumerKind.Base, Data.baseDemand);
        foreach (Warehouse depot in FindObjectsByType<Warehouse>()) AddLocation(depot, EnergyConsumerKind.Depot, Data.depotDemand);

        AutomationBought = Stats.GetUpgradeLevel(UpgradeIds.Automation);
        for (int level = 1; level <= AutomationBought; level++)
        {
            consumers.Add(new EnergyConsumer { kind = EnergyConsumerKind.Automation, demand = Data.automationDemandPerLevel, order = level });
        }

        Capacity = Data.startCapacity + Mathf.RoundToInt(Stats.GetUpgradeEffect(EnergyIds.Generator));
        Used = EnergyGrid.Distribute(consumers, Capacity, Blackout);
        Demand = EnergyGrid.TotalDemand(consumers);
        AutomationPowered = EnergyGrid.PoweredAutomation(consumers);

        foreach (EnergyConsumer consumer in consumers)
        {
            if (consumer.location == null) continue;

            bool known = powered.TryGetValue(consumer.location, out bool was);
            powered[consumer.location] = consumer.powered;
            if (!known || was != consumer.powered) GameEvents.RaisePowerChanged(consumer.location, consumer.powered);
        }

        if (Demand != oldDemand || Capacity != oldCapacity) GameEvents.RaiseEnergyChanged(Demand, Capacity);
        if (AutomationPowered != oldAutomation || AutomationBought != oldBought)
        {
            GameEvents.RaiseAutomationPowerChanged(AutomationPowered, AutomationBought);
            // A coleta automática voltou: o que ficou pronto no escuro é coletado agora.
            if (AutomationPowered > oldAutomation && Stats.AutomationLevel >= AutomationLevels.AutoCollect) CollectCompleted();
        }
    }

    private void AddLocation(House location, EnergyConsumerKind kind, int demand)
    {
        // Quem entrou na rede depois (base ou depósito novo) é o primeiro a desligar na sobrecarga.
        if (!firstSeen.TryGetValue(location, out int order))
        {
            order = seenCount++;
            firstSeen[location] = order;
        }

        consumers.Add(new EnergyConsumer { kind = kind, demand = demand, order = order, location = location });
    }

    private static void CollectCompleted()
    {
        if (LocationsManager.Instance == null || GameManager.InputBlocked) return;

        foreach (House house in LocationsManager.Instance.Houses)
        {
            if (house.IsComplete) house.ResolveCompletion();
        }
        foreach (Warehouse depot in LocationsManager.Instance.Warehouses)
        {
            if (depot.IsComplete) depot.ResolveCompletion();
        }
    }

    // O segundo tutorial espera o primeiro acabar (ou ter sido pulado). Sem tutorial na cena, não espera.
    private bool FirstTutorialDone()
    {
        if (!tutorialChecked)
        {
            tutorialChecked = true;
            firstTutorial = FindAnyObjectByType<Tutorial>();
        }

        return firstTutorial == null || PlayerPrefs.GetInt(Tutorial.DoneKey, 0) == 1;
    }

    #endregion

    #region EVENTS

    private void OnUpgradePurchased(UpgradeData upgrade, int level)
    {
        Refresh();
    }

    // Uma vez por tempestade, com stormBlackoutChance, o primeiro raio derruba a rede.
    private void OnDisasterStarted(DisasterData disaster, Vector3 epicenter)
    {
        stormBlackoutPending = Unlocked && disaster != null && disaster.type == DisasterType.Storm && Random.value < Data.stormBlackoutChance;
    }

    private void OnLightningStruck(Vector3 worldPosition)
    {
        if (!stormBlackoutPending) return;

        stormBlackoutPending = false;
        StartBlackout(worldPosition);
    }

    private void OnDisasterEnded(DisasterData disaster)
    {
        stormBlackoutPending = false;
    }

    #endregion
}
