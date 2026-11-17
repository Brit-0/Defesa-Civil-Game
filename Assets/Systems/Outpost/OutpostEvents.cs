using System;

// Eventos da guarita (GUARITAS.md), no molde do LightningRodEvents: a OutpostManager dispara, a apresentação
// (OutpostFeel) só escuta. A compra passa por GameEvents.UpgradePurchased e ActionDenied; a equipe que a guarita manda
// passa também por GameEvents.TeamDispatched, como a do jogador.
public static class OutpostEvents
{
    public static event Action<bool> PlacementChanged;                // true = entrou no modo de escolher quarteirão
    public static event Action<OutpostBlock> BuildStarted;            // quarteirão pago, obra começou
    public static event Action<OutpostBlock> BuildFinished;           // guarita pronta
    public static event Action<OutpostBlock, House> AutoDispatched;   // a guarita mandou a equipe para esta casa

    public static void RaisePlacementChanged(bool placing) => PlacementChanged?.Invoke(placing);
    public static void RaiseBuildStarted(OutpostBlock block) => BuildStarted?.Invoke(block);
    public static void RaiseBuildFinished(OutpostBlock block) => BuildFinished?.Invoke(block);
    public static void RaiseAutoDispatched(OutpostBlock block, House house) => AutoDispatched?.Invoke(block, house);

    // Com Domain Reload desligado os estáticos sobrevivem entre um Play e outro.
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearAll()
    {
        PlacementChanged = null;
        BuildStarted = null;
        BuildFinished = null;
        AutoDispatched = null;
    }
}
