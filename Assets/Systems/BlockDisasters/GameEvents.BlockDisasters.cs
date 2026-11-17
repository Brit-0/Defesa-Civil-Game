using System;
using UnityEngine;

// Eventos da enchente e do meteoro (docs/jam/ENCHENTE_METEORO.md). Mesmo contrato do GameEvents.cs:
// os sistemas chamam Raise...; água, cratera, válvula, barra, sons e dicas só assinam no OnEnable e desassinam no OnDisable.
public static partial class GameEvents
{
    public static event Action<BlockIncident> BlockIncidentStarted;   // quarteirão alagou ou foi destruído (chamado aberto)
    public static event Action<BlockIncident> BlockIncidentResolved;  // chamado coletado: o quarteirão volta inteiro
    public static event Action<BlockIncident> ValveOpened;            // a equipe chegou ao bueiro: válvula na mão do jogador
    public static event Action<BlockIncident, float> MeteorIncoming;  // (quarteirão alvo, segundos até cair)
    public static event Action<House> FireDousedByFlood;              // a água apagou um incêndio do quarteirão

    public static void RaiseBlockIncidentStarted(BlockIncident incident) => BlockIncidentStarted?.Invoke(incident);
    public static void RaiseBlockIncidentResolved(BlockIncident incident) => BlockIncidentResolved?.Invoke(incident);
    public static void RaiseValveOpened(BlockIncident incident) => ValveOpened?.Invoke(incident);
    public static void RaiseMeteorIncoming(BlockIncident target, float seconds) => MeteorIncoming?.Invoke(target, seconds);
    public static void RaiseFireDousedByFlood(House house) => FireDousedByFlood?.Invoke(house);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearBlockDisasters()
    {
        BlockIncidentStarted = null;
        BlockIncidentResolved = null;
        ValveOpened = null;
        MeteorIncoming = null;
        FireDousedByFlood = null;
    }
}
