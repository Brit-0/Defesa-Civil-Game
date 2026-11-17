using UnityEngine;

// Números do quadro de força, o minigame do RELIGAR (docs/jam/ENERGIA_DIA_NOITE.md).
// Fora do FireEnergyData para não mexer no mesmo asset que o para-raio (v4C) em paralelo.
// Asset em Assets/Resources/Balance/FuseBoxData.asset, criado por Tools/Jam/Create Fire And Energy Data.
[CreateAssetMenu(fileName = "FuseBoxData", menuName = "Scriptable Objects/FuseBoxData")]
public class FuseBoxData : ScriptableObject
{
    public const string ResourcePath = "Balance/FuseBoxData";

    [Header("QUADRO DE FORÇA")]
    [Tooltip("Chaves no quadro. Cada uma tem um número; o jogador liga de 1 até aqui.")]
    public int switches = 4;
    [Tooltip("Segundos em que o quadro trava depois de uma chave fora da ordem (as que já subiram descem).")]
    public float wrongLock = 0.6f;
    [Tooltip("Segundos entre a última chave e a rede voltar (o zumbido sobe antes do estalo).")]
    public float restoreDelay = 0.4f;
    [Tooltip("Primeiras aberturas em que o número da próxima chave pulsa (depois o jogador já sabe).")]
    public int hintTimes = 2;

    private static FuseBoxData instance;

    public static FuseBoxData Instance
    {
        get
        {
            if (instance != null) return instance;

            instance = Resources.Load<FuseBoxData>(ResourcePath);
            if (instance == null)
            {
                Debug.LogWarning($"FuseBoxData não encontrado em Resources/{ResourcePath}: usando os valores padrão da classe. Rode Tools/Jam/Create Fire And Energy Data.");
                instance = CreateInstance<FuseBoxData>();
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
