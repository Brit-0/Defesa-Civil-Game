using UnityEngine;

// Meteoro anunciado que não cai (BlockDisasterManager.Run sem casa livre no quarteirão, v5F): some o aviso (sombra e
// alvo) e a rocha com halo, luz e cauda. Antes o Pending só saía no impacto e a rocha ficava parada no céu soltando
// fogo e fumaça escura por cima das casas até o fim da partida.
// A assinatura é estática (depois do ClearBlockDisasters do SubsystemRegistration) para não mexer no OnEnable do
// MeteorFx.cs; vale para toda instância ativa.
public partial class MeteorFx
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ListenMeteorCancelled()
    {
        GameEvents.MeteorCancelled -= OnMeteorCancelled;
        GameEvents.MeteorCancelled += OnMeteorCancelled;
    }

    private static void OnMeteorCancelled(BlockIncident incident)
    {
        if (incident == null) return;

        foreach (MeteorFx fx in FindObjectsByType<MeteorFx>()) fx.CancelPending(incident);
    }

    private void CancelPending(BlockIncident incident)
    {
        Pending pending = pendings.Find(p => p.incident == incident);
        if (pending == null) return;

        pendings.Remove(pending);
        Discard(pending);
    }

    // Robôs e testes: quantos meteoros esperam cair agora.
    public int PendingCount => pendings.Count;
}
