using UnityEngine;

// Uma fase do dia: a luz global (ambiente), o "sol" pontual da cena e o quanto é noite (postes e janelas).
// Entre duas fases a luz passa suave (DayNightModel.Evaluate).
[System.Serializable]
public struct DayNightKey
{
    public string name;
    [Tooltip("Posição no ciclo, de 0 a 1. 0 é o amanhecer.")]
    [Range(0f, 1f)] public float phase;
    public Color ambient;
    public float ambientIntensity;
    public Color sun;
    public float sunIntensity;
    [Tooltip("0 = dia (postes e janelas apagados), 1 = noite cheia.")]
    [Range(0f, 1f)] public float night;

    public DayNightKey(string name, float phase, Color ambient, float ambientIntensity, Color sun, float sunIntensity, float night)
    {
        this.name = name;
        this.phase = phase;
        this.ambient = ambient;
        this.ambientIntensity = ambientIntensity;
        this.sun = sun;
        this.sunIntensity = sunIntensity;
        this.night = night;
    }
}

// Ciclo de dia e noite, só iluminação (docs/jam/ENERGIA_DIA_NOITE.md). Não mexe em nada da jogabilidade.
// Asset em Assets/Resources/Balance/DayNightData.asset, criado por Tools/Jam/Create Day Night Data.
[CreateAssetMenu(fileName = "DayNightData", menuName = "Scriptable Objects/DayNightData")]
public class DayNightData : ScriptableObject
{
    public const string ResourcePath = "Balance/DayNightData";

    [Header("CICLO")]
    [Tooltip("Segundos de partida para um dia inteiro (amanhecer, dia, entardecer, noite e de volta).")]
    public float cycleSeconds = 420f;
    [Tooltip("Onde o ciclo começa na partida (0 a 1). 0,1 = manhã: o primeiro tutorial roda de dia.")]
    [Range(0f, 1f)] public float startPhase = .1f;

    [Header("FASES")]
    public DayNightKey[] keys = DefaultKeys();

    [Header("LUZES DA CIDADE")]
    [Tooltip("Postes de dia, em fração da luz da cena (0 = apagados).")]
    [Range(0f, 1f)] public float lampDay = 0f;
    [Tooltip("Postes na noite cheia, em fração da luz da cena.")]
    public float lampNight = 1.1f;
    [Tooltip("Luz da Guarda e dos armazéns de dia, em fração da luz da cena.")]
    [Range(0f, 1f)] public float baseDay = .3f;
    [Tooltip("Luz da Guarda e dos armazéns na noite cheia, em fração da luz da cena.")]
    public float baseNight = 1.15f;
    [Tooltip("Pedaço da noite (0 a 1) em que as luzes vão acendendo uma a uma, cada uma no seu momento.")]
    [Range(0f, 1f)] public float lightsSpread = .45f;
    [Tooltip("Fração das casas com janela acesa à noite.")]
    [Range(0f, 1f)] public float windowsLitShare = .75f;
    public Color windowColor = new Color(1f, .78f, .42f, 1f);
    [Tooltip("Brilho das janelas (passa de 1 para o bloom pegar).")]
    public float windowIntensity = 1.5f;
    [Tooltip("Sprites (começo do nome) em que a cor quente é telhado, não janela: só o vidro acende.")]
    public string[] warmKeyOff = { "Fabrica_Armazem_A", "Fabrica_Armazem_B" };

    [Header("APAGÃO")]
    [Tooltip("Segundos em que as luzes piscam e apagam quando a rede cai.")]
    public float blackoutFlicker = .35f;
    [Tooltip("Velocidade (unidades do mundo por segundo) da onda que reacende a cidade a partir da Guarda.")]
    public float restoreWaveSpeed = 30f;

    private static DayNightData instance;

    public static DayNightData Instance
    {
        get
        {
            if (instance != null) return instance;

            instance = Resources.Load<DayNightData>(ResourcePath);
            if (instance == null)
            {
                Debug.LogWarning($"DayNightData não encontrado em Resources/{ResourcePath}: usando os valores padrão da classe. Rode Tools/Jam/Create Day Night Data.");
                instance = CreateInstance<DayNightData>();
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    // O dia (0,25) é a luz que a cena já tinha (CityBuilder.Lighting): ambiente frio 0,68 e sol quente 0,5. Mais forte
    // que isso a praça e as calçadas estouravam (print 81_dia da #536); manhã e tarde ficam só um pouco abaixo.
    public static DayNightKey[] DefaultKeys() => new[]
    {
        new DayNightKey("Amanhecer", 0f, new Color(.98f, .80f, .78f), .52f, new Color(1f, .72f, .55f), .3f, .45f),
        new DayNightKey("Manhã", .08f, new Color(.92f, .92f, .98f), .64f, new Color(1f, .9f, .78f), .45f, 0f),
        new DayNightKey("Dia", .25f, new Color(.88f, .91f, 1f), .68f, new Color(1f, .93f, .8f), .5f, 0f),
        new DayNightKey("Tarde", .4f, new Color(.92f, .9f, .96f), .65f, new Color(1f, .88f, .72f), .48f, 0f),
        new DayNightKey("Entardecer", .5f, new Color(1f, .68f, .52f), .52f, new Color(1f, .58f, .32f), .42f, .55f),
        new DayNightKey("Noite", .58f, new Color(.48f, .55f, .95f), .38f, new Color(.62f, .72f, 1f), .13f, 1f),
        new DayNightKey("Madrugada", .9f, new Color(.45f, .52f, .92f), .36f, new Color(.62f, .72f, 1f), .12f, 1f),
    };
}
