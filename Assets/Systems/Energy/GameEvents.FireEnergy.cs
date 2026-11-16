using System;
using UnityEngine;

// Eventos do fogo que cresce e da energia (docs/jam/ENERGIA_FOGO.md). Mesmo contrato do GameEvents.cs:
// os sistemas chamam Raise...; HUD, feel e áudio só assinam no OnEnable e desassinam no OnDisable.
public static partial class GameEvents
{
    // Fogo
    public static event Action<House, int> FireLevelChanged;    // (casa, nível novo); 0 quando o fogo acaba
    public static event Action<House> FireJetOpened;            // a equipe chegou: as chamas do jato aparecem

    // Energia
    public static event Action EnergyUnlocked;                  // a energia entrou no jogo (segundo tutorial)
    public static event Action<int, int> EnergyChanged;         // (consumo pedido, capacidade do gerador)
    public static event Action<House, bool> PowerChanged;       // (base ou depósito, ligado)
    public static event Action<int, int> AutomationPowerChanged; // (níveis ligados, níveis comprados)
    public static event Action<bool, Vector3> BlackoutChanged;  // (apagão começou, onde caiu o raio; zero no religar)

    public static void RaiseFireLevelChanged(House house, int level) => FireLevelChanged?.Invoke(house, level);
    public static void RaiseFireJetOpened(House house) => FireJetOpened?.Invoke(house);

    public static void RaiseEnergyUnlocked() => EnergyUnlocked?.Invoke();
    public static void RaiseEnergyChanged(int demand, int capacity) => EnergyChanged?.Invoke(demand, capacity);
    public static void RaisePowerChanged(House location, bool powered) => PowerChanged?.Invoke(location, powered);
    public static void RaiseAutomationPowerChanged(int powered, int bought) => AutomationPowerChanged?.Invoke(powered, bought);
    public static void RaiseBlackoutChanged(bool active, Vector3 worldPosition) => BlackoutChanged?.Invoke(active, worldPosition);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearFireEnergy()
    {
        FireLevelChanged = null;
        FireJetOpened = null;

        EnergyUnlocked = null;
        EnergyChanged = null;
        PowerChanged = null;
        AutomationPowerChanged = null;
        BlackoutChanged = null;
    }
}
