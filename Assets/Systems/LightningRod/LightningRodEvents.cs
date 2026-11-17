using System;
using UnityEngine;

// Eventos do para-raio (PARARAIO.md), no molde do ExpansionEvents: a LightningRodManager dispara, a apresentação
// (LightningRodFeel, som, partículas) só escuta. A compra passa por GameEvents.UpgradePurchased e ActionDenied, e o
// raio absorvido também dispara GameEvents.LightningStruck na ponta do para-raio (clarão, raio, trovão e tranco).
public static class LightningRodEvents
{
    public static event Action<bool> PlacementChanged;              // true = entrou no modo de escolher quarteirão
    public static event Action<RodBlock> BuildStarted;              // quarteirão pago, obra começou
    public static event Action<RodBlock> BuildFinished;             // para-raio pronto no telhado
    public static event Action<RodBlock, Vector3> StrikeAbsorbed;   // quarteirão e a casa onde o raio ia cair

    public static void RaisePlacementChanged(bool placing) => PlacementChanged?.Invoke(placing);
    public static void RaiseBuildStarted(RodBlock block) => BuildStarted?.Invoke(block);
    public static void RaiseBuildFinished(RodBlock block) => BuildFinished?.Invoke(block);
    public static void RaiseStrikeAbsorbed(RodBlock block, Vector3 target) => StrikeAbsorbed?.Invoke(block, target);

    // Com Domain Reload desligado os estáticos sobrevivem entre um Play e outro.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearAll()
    {
        PlacementChanged = null;
        BuildStarted = null;
        BuildFinished = null;
        StrikeAbsorbed = null;
    }
}
