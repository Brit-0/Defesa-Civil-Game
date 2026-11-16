using System.Collections.Generic;

public enum EnergyConsumerKind { Base, Depot, Automation }

// Um consumidor da rede: uma base, um depósito ou um nível de automação.
public class EnergyConsumer
{
    public EnergyConsumerKind kind;
    public int demand;
    public int order;     // dentro do tipo, quem tem o menor número liga primeiro
    public bool powered;
    public House location; // base ou depósito; null na automação
}

// Rede de energia (docs/jam/ENERGIA_FOGO.md), sem nada do Unity para dar para testar fora dele.
// A capacidade do gerador vai primeiro para as bases, depois para os depósitos e por último para a automação.
// O que não cabe fica desligado (sobrecarga); no apagão tudo desliga.
public static class EnergyGrid
{
    // Liga quem cabe e devolve a energia usada. A automação só liga em sequência: o nível 3 não liga sem o 2.
    public static int Distribute(List<EnergyConsumer> consumers, int capacity, bool blackout)
    {
        consumers.Sort(ComparePriority);

        int used = 0;
        bool automationCut = false;
        foreach (EnergyConsumer consumer in consumers)
        {
            bool fits = !blackout && used + consumer.demand <= capacity;
            if (consumer.kind == EnergyConsumerKind.Automation)
            {
                fits &= !automationCut;
                automationCut |= !fits;
            }

            consumer.powered = fits;
            if (fits) used += consumer.demand;
        }

        return used;
    }

    public static int TotalDemand(List<EnergyConsumer> consumers)
    {
        int total = 0;
        foreach (EnergyConsumer consumer in consumers) total += consumer.demand;
        return total;
    }

    // Níveis de automação ligados (os primeiros da fila, em sequência).
    public static int PoweredAutomation(List<EnergyConsumer> consumers)
    {
        int levels = 0;
        foreach (EnergyConsumer consumer in consumers)
        {
            if (consumer.kind == EnergyConsumerKind.Automation && consumer.powered) levels++;
        }

        return levels;
    }

    private static int ComparePriority(EnergyConsumer a, EnergyConsumer b)
    {
        int byKind = a.kind.CompareTo(b.kind);
        return byKind != 0 ? byKind : a.order.CompareTo(b.order);
    }
}
