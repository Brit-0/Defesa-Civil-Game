// Quadro de força do apagão (docs/jam/ENERGIA_DIA_NOITE.md), sem nada do Unity para dar para testar fora dele.
// Cada chave tem um número (ordem embaralhada); o jogador liga de 1 até o fim. Chave fora da ordem = falha leve:
// as que já subiram descem e o quadro trava um instante. Todas ligadas = a rede volta.
public enum FusePress
{
    Ignored,
    Correct,
    Wrong,
    Done
}

public class FuseBox
{
    public int Count { get; }
    public int Lit { get; private set; }
    public int Mistakes { get; private set; }
    public float LockLeft { get; private set; }

    public bool Done => Lit >= Count;
    public bool Locked => LockLeft > 0f;
    // Chave que liga agora (-1 com tudo ligado): alvo da seta do tutorial.
    public int NextIndex => Done ? -1 : IndexOf(Lit + 1);

    private readonly int[] numbers;
    private readonly bool[] on;
    private readonly float lockTime;

    public FuseBox(int count, float lockTime, System.Random random)
    {
        Count = count < 1 ? 1 : count;
        this.lockTime = lockTime < 0f ? 0f : lockTime;
        numbers = Shuffled(Count, random ?? new System.Random());
        on = new bool[Count];
    }

    public int NumberOf(int index) => numbers[index];
    public bool IsOn(int index) => on[index];

    public int IndexOf(int number)
    {
        for (int i = 0; i < Count; i++)
        {
            if (numbers[i] == number) return i;
        }
        return -1;
    }

    public FusePress Press(int index)
    {
        if (Done || Locked || index < 0 || index >= Count || on[index]) return FusePress.Ignored;

        if (numbers[index] != Lit + 1)
        {
            Mistakes++;
            LockLeft = lockTime;
            for (int i = 0; i < Count; i++) on[i] = false;
            Lit = 0;
            return FusePress.Wrong;
        }

        on[index] = true;
        Lit++;
        return Done ? FusePress.Done : FusePress.Correct;
    }

    public void Tick(float deltaTime)
    {
        if (LockLeft <= 0f) return;

        LockLeft -= deltaTime;
        if (LockLeft < 0f) LockLeft = 0f;
    }

    // Números 1..count embaralhados, nunca em fila da esquerda para a direita (seria só clicar em sequência).
    public static int[] Shuffled(int count, System.Random random)
    {
        int[] result = new int[count];
        for (int i = 0; i < count; i++) result[i] = i + 1;
        if (count < 2) return result;

        do
        {
            for (int i = count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }
        } while (InOrder(result));

        return result;
    }

    private static bool InOrder(int[] values)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] != i + 1) return false;
        }
        return true;
    }
}
