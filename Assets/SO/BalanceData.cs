using UnityEngine;

// Todos os números de balanceamento do jogo. Partiram da seção 13 do GDD e foram calibrados no T25 pelo simulador
// docs/jam/sim (16/11): os que mudaram têm o valor do GDD no comentário. A calibragem final é com partida jogada.
// Asset em Assets/Resources/Balance/BalanceData.asset, criado por Tools/Jam/Create Default Data e lido por Stats.
[CreateAssetMenu(fileName = "BalanceData", menuName = "Scriptable Objects/BalanceData")]
public class BalanceData : ScriptableObject
{
    [Header("START")]
    public int startWorkers = 3;
    public int startMaterial = 5;
    public int startMoney = 0;

    [Header("WORKERS")]
    public float workerSpeed = 2.5f; // GDD: 1 (a caminhada pela rua até uma casa é de 27 unidades na mediana)
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
    public float restockTime = 3f; // GDD: 5

    [Header("REWARD")]
    public int moneyPerDamage = 15; // GDD: 10
    public int fireReward = 20; // GDD: 15

    [Header("DAMAGE")]
    public int minDamage = 1;
    public int maxDamage = 3;

    [Header("FIRE")]
    [Tooltip("Segundos até o fogo apagar sozinho e a casa virar Damaged. 40 (era 25): o nível 1 dura 13 s e dá tempo de " +
             "mandar a equipe antes de o fogo crescer (lista Para Arrumar, item 19).")]
    public float fireDuration = 40f;
    [Tooltip("Dano da casa quando o fogo apaga sozinho.")]
    public int fireBurnoutDamage = 3;

    [Header("CHAOS")]
    public float chaosWeightDamaged = 1f;
    public float chaosWeightFire = 2f;
    [Tooltip("Soma dos pesos que enche a barra de caos.")]
    public float chaosLimit = 22f; // GDD: 20
    public float chaosRisePerSecond = 0.06f;
    public float chaosFallPerSecond = 0.10f;

    [Header("DISASTERS")]
    public float firstDisasterDelay = 6f;
    public int firstDisasterHouses = 2;
    [Tooltip("Intervalo entre desastres: cai em linha reta de Start a End ao longo de disasterRampDuration.")]
    public float disasterIntervalStart = 45f; // GDD: 35
    public float disasterIntervalEnd = 20f; // GDD: 12
    public float disasterRampDuration = 780f; // GDD: 600
    [Tooltip("Casas por desastre, na mesma rampa.")]
    public int disasterHousesStart = 3;
    public int disasterHousesEnd = 6; // GDD: 9
    public float warningTime = 4f; // T25: 3 (GAMELOOP_REVISAO: 1 s a mais para ler o alvo e escorar)

    [Header("STRONG EARTHQUAKE (GAMELOOP_REVISAO)")]
    [Tooltip("A cada N desastres (sem contar o primeiro) vem um terremoto forte. 0 desliga.")]
    public int strongDisasterEvery = 5;
    [Tooltip("Casas do terremoto forte = casas da rampa x este valor.")]
    public float strongDisasterHouseMultiplier = 2f;
    [Tooltip("Aviso mais longo: dá tempo de se preparar.")]
    public float strongDisasterWarningTime = 6f;
    [Tooltip("Segundos a mais de calma depois do terremoto forte.")]
    public float strongDisasterCalm = 20f;

    [Header("OCCURRENCE CLEARED (GAMELOOP_REVISAO)")]
    [Tooltip("Bônus por casa quando todas as casas de um desastre voltam a ficar intactas.")]
    public int clearBonusPerHouse = 8;
    [Tooltip("Encerrar a ocorrência em até estes segundos depois do início multiplica o bônus.")]
    public float fastClearTime = 60f;
    public float fastClearMultiplier = 2f;

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

    [Header("MILESTONES (GAMELOOP_REVISAO)")]
    [Tooltip("Casas consertadas que viram marco. Cada marco traz voluntários (trabalhadores grátis).")]
    public int[] milestoneRepairs = { 5, 15, 30, 50, 80 };
    public int volunteersPerMilestone = 1;

    [Header("MINI GAME: PONTO CERTO (GAMELOOP_REVISAO)")]
    [Tooltip("Onde a janela de acerto começa, em fração do progresso do trabalho (sorteado entre os dois).")]
    public float critWindowMin = 0.35f;
    public float critWindowMax = 0.7f;
    [Tooltip("Tamanho da janela, em fração do progresso.")]
    public float critWindowSize = 0.18f;
    [Tooltip("Acerto: o trabalho termina na hora e paga este tanto a mais (no Armazém, material a mais).")]
    public float critRewardBonus = 0.5f;
    [Tooltip("Erro: o progresso volta este tanto. Uma tentativa por trabalho.")]
    public float missPenalty = 0.2f;

    [Header("MINI GAME: MUTIRÃO NO INCÊNDIO (GAMELOOP_REVISAO)")]
    [Tooltip("Cada clique na casa em chamas com a equipe trabalhando adianta este tanto do trabalho.")]
    public float fireClickProgress = 0.1f;
    [Tooltip("Cliques no mesmo incêndio para o bônus de herói.")]
    public int fireHeroClicks = 5;
    public float fireHeroBonus = 0.5f;

    [Header("MINI GAME: ESCORAR NO AVISO (GAMELOOP_REVISAO)")]
    [Tooltip("Material gasto para escorar uma casa no alvo do terremoto, durante o aviso.")]
    public int shoreMaterialCost = 1;
    [Tooltip("Dano que a escora tira. Se o dano zera, a casa fica de pé.")]
    public int shoreDamageReduction = 2;
    [Tooltip("Dinheiro quando a casa escorada fica de pé.")]
    public int shoreHeldReward = 40;

    [Header("REPAIR STREAK (GAMELOOP_REVISAO)")]
    [Tooltip("Consertos coletados com até estes segundos de intervalo contam como sequência (o som sobe de tom).")]
    public float streakWindow = 5f;
}
