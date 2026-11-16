using UnityEngine;

// Números do minigame do fogo e da energia (docs/jam/ENERGIA_FOGO.md), calibrados no simulador docs/jam/sim.
// Ficam fora do BalanceData para os dois trabalhos andarem em paralelo sem mexer no mesmo asset.
// Asset em Assets/Resources/Balance/FireEnergyData.asset, criado por Tools/Jam/Create Fire & Energy Data.
[CreateAssetMenu(fileName = "FireEnergyData", menuName = "Scriptable Objects/FireEnergyData")]
public class FireEnergyData : ScriptableObject
{
    public const string ResourcePath = "Balance/FireEnergyData";

    [Header("FIRE: O FOGO CRESCE")]
    [Tooltip("Nível máximo do incêndio. Sobe com o tempo sem equipe (partes iguais do fireDuration) e quando o jato falha. " +
             "Cada nível soma um fireWorkTime (BalanceData) ao trabalho da equipe.")]
    public int fireMaxLevel = 3;

    [Header("FIRE: JATO (MINIGAME)")]
    [Tooltip("Segundos da janela do jato quando a equipe chega. Enquanto ela está aberta, o trabalho automático espera.")]
    public float jetTime = 4.5f;
    [Tooltip("Chamas para apagar no nível 1.")]
    public int jetFlamesBase = 3;
    [Tooltip("Chamas a mais por nível acima do 1.")]
    public int jetFlamesPerLevel = 1;
    [Tooltip("Todas as chamas apagadas: o fogo apaga na hora e paga este tanto a mais.")]
    public float jetOutBonus = 0.5f;
    [Tooltip("Distância (unidades do HUD em 1080p) entre o ponteiro e a chama para o jato pegar.")]
    public float jetHitRadius = 46f;

    [Header("ENERGY")]
    [Tooltip("Segundos de partida (sem contar o tempo parado no tutorial) até a energia entrar no jogo.")]
    public float unlockTime = 150f;
    [Tooltip("Capacidade do gerador antes de qualquer melhoria. O upgrade generator soma effectPerLevel por nível.")]
    public int startCapacity = 5;
    public int baseDemand = 2;
    public int depotDemand = 1;
    [Tooltip("Cada nível de automação comprado gasta este tanto (rádio, protocolo, coleta, despacho).")]
    public int automationDemandPerLevel = 1;

    [Header("BLACKOUT")]
    [Tooltip("Chance de uma tempestade derrubar a rede num dos raios (uma vez por tempestade).")]
    [Range(0f, 1f)] public float stormBlackoutChance = 0.5f;
    [Tooltip("Segundos até a rede voltar sozinha se o jogador não clicar em RELIGAR.")]
    public float blackoutAutoRestore = 15f;

    private static FireEnergyData instance;

    public static FireEnergyData Instance
    {
        get
        {
            if (instance != null) return instance;

            instance = Resources.Load<FireEnergyData>(ResourcePath);
            if (instance == null)
            {
                Debug.LogWarning($"FireEnergyData não encontrado em Resources/{ResourcePath}: usando os valores padrão da classe. Rode Tools/Jam/Create Fire & Energy Data.");
                instance = CreateInstance<FireEnergyData>();
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }
}
