using UnityEngine;

// Todos os números de balanceamento do jogo. Os valores padrão são os da seção 13 do GDD (PLACEHOLDER até o T25).
// Asset em Assets/Resources/Balance/BalanceData.asset, criado por Tools/Jam/Create Default Data e lido por Stats.
[CreateAssetMenu(fileName = "BalanceData", menuName = "Scriptable Objects/BalanceData")]
public class BalanceData : ScriptableObject
{
    [Header("START")]
    public int startWorkers = 3;
    public int startMaterial = 5;
    public int startMoney = 0;

    [Header("WORKERS")]
    public float workerSpeed = 1f;
    [Tooltip("Segundos entre um trabalhador e outro ao sair da base.")]
    public float dispatchInterval = 0.8f;
    [Tooltip("Material que cada trabalhador traz do Armazém.")]
    public int materialPerWorker = 1;
    [Tooltip("Automação nível 4: segundos entre um despacho automático e o próximo.")]
    public float autoDispatchInterval = 1f;

    [Header("WORK")]
    [Tooltip("Tempo de conserto = workTimePerDamage x dano / equipe, nunca menos que minWorkTime.")]
    public float workTimePerDamage = 5f;
    public float minWorkTime = 2f;
    [Tooltip("Tempo para apagar um incêndio, dividido pela equipe, com o mesmo mínimo.")]
    public float fireWorkTime = 4f;
    [Tooltip("Tempo fixo de uma ida ao Armazém.")]
    public float restockTime = 5f;

    [Header("REWARD")]
    public int moneyPerDamage = 10;
    public int fireReward = 15;

    [Header("DAMAGE")]
    public int minDamage = 1;
    public int maxDamage = 3;

    [Header("FIRE")]
    [Tooltip("Segundos até o fogo apagar sozinho e a casa virar Damaged.")]
    public float fireDuration = 25f;
    [Tooltip("Dano da casa quando o fogo apaga sozinho.")]
    public int fireBurnoutDamage = 3;

    [Header("CHAOS")]
    public float chaosWeightDamaged = 1f;
    public float chaosWeightFire = 2f;
    [Tooltip("Soma dos pesos que enche a barra de caos.")]
    public float chaosLimit = 20f;
    public float chaosRisePerSecond = 0.06f;
    public float chaosFallPerSecond = 0.10f;

    [Header("DISASTERS")]
    public float firstDisasterDelay = 6f;
    public int firstDisasterHouses = 2;
    [Tooltip("Intervalo entre desastres: cai em linha reta de Start a End ao longo de disasterRampDuration.")]
    public float disasterIntervalStart = 35f;
    public float disasterIntervalEnd = 12f;
    public float disasterRampDuration = 600f;
    [Tooltip("Casas por desastre, na mesma rampa.")]
    public int disasterHousesStart = 3;
    public int disasterHousesEnd = 9;
    public float warningTime = 3f;

    [Header("DISASTER TYPES")]
    [Tooltip("Pesos do sorteio entre os tipos já liberados.")]
    public float earthquakeWeight = 50f;
    public float stormWeight = 35f;
    public float fireWeight = 15f;
    [Tooltip("Segundos de partida até o tipo entrar no sorteio.")]
    public float stormUnlockTime = 90f;
    public float fireUnlockTime = 180f;

    [Header("EARTHQUAKE")]
    [Tooltip("Atraso da onda, em segundos por unidade de distância ao epicentro.")]
    public float earthquakeWaveDelayPerUnit = 0.15f;

    [Header("STORM")]
    public float stormDuration = 8f;
    [Range(0f, 1f)] public float lightningFireChance = 0.2f;
    public int stormMinDamage = 1;
    public int stormMaxDamage = 2;

    [Header("ISOLATED FIRE")]
    public int fireMinHouses = 1;
    public int fireMaxHouses = 2;

    [Header("PASSIVE INCOME")]
    public float incomeInterval = 10f;
    [Tooltip("Repasse = incomePerHouse x nível x casas intactas x (1 - caos), arredondado.")]
    public float incomePerHouse = 0.5f;
}
