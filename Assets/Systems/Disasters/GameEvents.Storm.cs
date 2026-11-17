using System;
using UnityEngine;

// Eventos da casa destelhada pela tempestade (docs/jam/TEMPESTADE.md). Mesmo contrato do GameEvents.cs:
// os sistemas chamam Raise...; HUD, feel e áudio só assinam no OnEnable e desassinam no OnDisable.
public static partial class GameEvents
{
    public static event Action<House, int> StormLeaked;   // (casa, dano novo): a chuva entrou e o dano subiu 1

    public static void RaiseStormLeaked(House house, int damage) => StormLeaked?.Invoke(house, damage);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearStorm()
    {
        StormLeaked = null;
    }
}
