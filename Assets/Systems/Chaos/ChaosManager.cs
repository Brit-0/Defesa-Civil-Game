using UnityEngine;

// Caos da cidade (GDD, seção 6): a barra persegue o alvo, que é a soma dos pesos das casas com problema
// dividida pelo limite. Barra cheia = derrota. Só roda em GameState.Playing.
public class ChaosManager : MonoBehaviour
{
    public static ChaosManager Instance;

    // 0 a 1.
    public float Chaos { get; private set; }
    public float Target { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameEvents.RaiseChaosChanged(Chaos);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

        Target = ComputeTarget();

        float speed = Target > Chaos ? Stats.Balance.chaosRisePerSecond : Stats.Balance.chaosFallPerSecond;
        float newChaos = Mathf.MoveTowards(Chaos, Target, speed * Time.deltaTime);
        if (newChaos != Chaos)
        {
            Chaos = newChaos;
            GameEvents.RaiseChaosChanged(Chaos);
        }

        if (Chaos >= 1f) GameManager.Instance.Lose();
    }

    // Toda casa que não está intacta conta, mesmo com equipe a caminho ou trabalhando.
    private static float ComputeTarget()
    {
        if (LocationsManager.Instance == null) return 0f;

        float limit = Stats.Balance.chaosLimit;
        if (limit <= 0f) return 0f;

        float weight = 0f;
        foreach (House house in LocationsManager.Instance.Houses)
        {
            weight += Stats.GetChaosWeight(house.currentState);
        }
        return Mathf.Clamp01(weight / limit);
    }
}
