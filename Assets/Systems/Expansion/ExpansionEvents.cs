using System;
using UnityEngine;

// Eventos da expansão (EXPANSAO.md), no mesmo molde do GameEvents: a ExpansionManager dispara, a apresentação
// (marcadores, som, partículas) só escuta. Ficam fora do GameEvents.cs para a expansão não disputar o arquivo do
// contrato com outras tarefas; a compra em si já passa por GameEvents.UpgradePurchased e ActionDenied.
public static class ExpansionEvents
{
    public static event Action<BuildLot> BuildStarted;      // terreno pago, obra começou
    public static event Action<BuildLot> BuildFinished;     // obra acabou (o prédio já existe em lot.Building)
    public static event Action<Location> LocationBuilt;     // base ou armazém novo, já no LocationsManager
    public static event Action<LotKind?> PlacementChanged;  // null = saiu do modo de escolher terreno

    public static void RaiseBuildStarted(BuildLot lot) => BuildStarted?.Invoke(lot);
    public static void RaiseBuildFinished(BuildLot lot) => BuildFinished?.Invoke(lot);
    public static void RaiseLocationBuilt(Location location) => LocationBuilt?.Invoke(location);
    public static void RaisePlacementChanged(LotKind? kind) => PlacementChanged?.Invoke(kind);

    // Com Domain Reload desligado os estáticos sobrevivem entre um Play e outro.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearAll()
    {
        BuildStarted = null;
        BuildFinished = null;
        LocationBuilt = null;
        PlacementChanged = null;
    }
}
