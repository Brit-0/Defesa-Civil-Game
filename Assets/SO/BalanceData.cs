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
    public float restockTime = 5f; // BALANCEAMENTO_V5: 3 (o Pietro: estocar estava fácil demais; volta ao GDD)

    [Header("REWARD")]
    public int moneyPerDamage = 52; // BALANCEAMENTO_V6F: 18 (o começo vem do conserto, não do repasse; V5: 15; GDD: 10)
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
    public float chaosLimit = 25f; // BALANCEAMENTO_V5: 20 (folga para a escalada mais rápida; RITMO_V3: 22)
    public float chaosRisePerSecond = 0.06f;
    public float chaosFallPerSecond = 0.10f;

    [Header("DISASTERS")]
    public float firstDisasterDelay = 6f;
    public int firstDisasterHouses = 2;
    [Tooltip("Intervalo entre desastres: fica em Start na calma do começo (disasterRampDelay) e cai em linha reta até End ao longo de disasterRampDuration.")]
    public float disasterIntervalStart = 50f; // RITMO_V3: 45 (GDD: 35)
    public float disasterIntervalEnd = 21f; // BALANCEAMENTO_V5: 22 (RITMO_V3: 20; GDD: 12)
    [Tooltip("Segundos de calma no começo, antes de a rampa começar.")]
    public float disasterRampDelay = 60f; // BALANCEAMENTO_V5 (só o início é calmo; antes a rampa começava no 0:00)
    public float disasterRampDuration = 480f; // BALANCEAMENTO_V5: 720 (depois da calma o intervalo cai logo; teto aos 9:00, antes aos 12:00; GDD: 600)
    [Tooltip("Casas por desastre, na mesma rampa.")]
    public int disasterHousesStart = 2; // RITMO_V3: 3 (2 casas até 1:30, 3 até 4:30, 4 até 7:30)
    public int disasterHousesEnd = 6; // GDD: 9
    [Tooltip("Depois da rampa (o teto), o intervalo continua caindo estes segundos por minuto, bem mais devagar que na rampa.")]
    public float disasterLateIntervalPerMinute = 0.4f; // BALANCEAMENTO_V5 (na rampa: 3,6 s por minuto)
    [Tooltip("Piso do intervalo depois do teto.")]
    public float disasterIntervalFloor = 15f; // BALANCEAMENTO_V5: chega aos 24:00
    [Tooltip("Depois da rampa, casas a mais por desastre a cada minuto (arredondado).")]
    public float disasterLateHousesPerMinute = 0.15f; // BALANCEAMENTO_V5 (na rampa: 0,5 por minuto)
    [Tooltip("Teto das casas por desastre depois da rampa (o terremoto forte multiplica este número).")]
    public int disasterHousesMax = 9; // BALANCEAMENTO_V5: chega aos 29:00
    public float warningTime = 4f; // T25: 3 (GAMELOOP_REVISAO: 1 s a mais para ler o alvo e escorar)

    [Header("STRONG EARTHQUAKE (GAMELOOP_REVISAO)")]
    [Tooltip("A cada N desastres (sem contar o primeiro) vem um terremoto forte. 0 desliga.")]
    public int strongDisasterEvery = 8; // RITMO_V3: 5. O primeiro cai perto de 6:10, depois de todos os sistemas apresentados.
    [Tooltip("Casas do terremoto forte = casas da rampa x este valor.")]
    public float strongDisasterHouseMultiplier = 2f; // BALANCEAMENTO_V5: 2,5 (o 2º forte, perto de 10:30, tinha 15 casas e derrubava quem jogava mais de 10 min; agora 12)
    [Tooltip("Aviso mais longo: dá tempo de se preparar.")]
    public float strongDisasterWarningTime = 6f;
    [Tooltip("Segundos a mais de calma depois do terremoto forte.")]
    public float strongDisasterCalm = 20f;

    [Header("ADAPTIVE PACE (ADAPTATIVA_V6B)")]
    [Tooltip("Segundos da média do caos e da fila de chamados que medem a eficiência do jogador (esquece devagar).")]
    public float paceAverageTime = 45f;
    [Tooltip("Caos médio: até paceChaosLow é calma total; de paceChaosHigh para cima, nenhuma.")]
    public float paceChaosLow = 0.08f;
    public float paceChaosHigh = 0.25f;
    [Tooltip("Chamados esperando equipe, em média: até paceQueueLow a fila é curta; de paceQueueHigh para cima, longa.")]
    public float paceQueueLow = 0.5f;
    public float paceQueueHigh = 3f;
    [Tooltip("Poder do jogador, o maior dos dois: níveis de upgrade comprados (sem a meta) de paceLevelsStart a " +
             "paceLevelsFull, ou dinheiro guardado até paceMoneyFull. Quem ainda não cresceu fica sem ritmo a mais.")]
    public int paceLevelsStart = 16;
    public int paceLevelsFull = 30;
    public int paceMoneyFull = 4000;
    [Tooltip("O ritmo (0 a 1) sobe no máximo isto por minuto e desce isto por minuto quando o jogador começa a apanhar.")]
    public float paceRisePerMinute = 0.35f;
    public float paceFallPerMinute = 1f;
    [Tooltip("Com ritmo 1, o intervalo entre desastres encolhe esta fração, sem passar de paceIntervalFloor. 0 desliga.")]
    public float paceIntervalCut = 0.45f;
    public float paceIntervalFloor = 12f;
    [Tooltip("Com ritmo 1, as casas por desastre crescem esta fração, até paceHousesMax (o terremoto forte fica de fora). 0 desliga.")]
    public float paceHouseBoost = 0.6f;
    public int paceHousesMax = 9;

    [Header("OCCURRENCE CLEARED (GAMELOOP_REVISAO)")]
    [Tooltip("Bônus por casa quando todas as casas de um desastre voltam a ficar intactas.")]
    public int clearBonusPerHouse = 8;
    [Tooltip("Encerrar a ocorrência em até estes segundos depois do início multiplica o bônus.")]
    public float fastClearTime = 60f;
    public float fastClearMultiplier = 2f;

    [Header("DISASTER TYPES")]
    [Tooltip("Pesos do sorteio entre os tipos já liberados.")]
    public float earthquakeWeight = 40f; // RITMO_V3: 50
    public float stormWeight = 50f; // RITMO_V3: 35 (pesos maiores para o tipo novo aparecer logo depois de liberado)
    public float fireWeight = 30f; // RITMO_V3: 15
    [Tooltip("Segundos de partida até o tipo entrar no sorteio. Ordem do RITMO_V3: terremoto, incêndio (com o jato), " +
             "tempestade, expansão (compra do jogador) e energia (FireEnergyData.unlockTime).")]
    public float stormUnlockTime = 170f; // RITMO_V3: 90
    public float fireUnlockTime = 90f; // RITMO_V3: 180 (o incêndio vem antes da tempestade, que também põe fogo)

    [Header("EARTHQUAKE")]
    [Tooltip("Atraso da onda, em segundos por unidade de distância ao epicentro.")]
    public float earthquakeWaveDelayPerUnit = 0.15f;

    [Header("STORM")]
    public float stormDuration = 8f;
    [Range(0f, 1f)] public float lightningFireChance = 0.2f;
    public int stormMinDamage = 1;
    public int stormMaxDamage = 2;
    [Tooltip("Casa quebrada por raio fica destelhada: esperando equipe, a chuva sobe o dano em 1 a cada tantos segundos (0 desliga). docs/jam/TEMPESTADE.md")]
    public float stormLeakInterval = 25f;
    [Tooltip("Até onde a chuva leva o dano da casa destelhada.")]
    public int stormLeakMaxDamage = 3;

    [Header("ISOLATED FIRE")]
    public int fireMinHouses = 1;
    public int fireMaxHouses = 2;

    [Header("PASSIVE INCOME")]
    public float incomeInterval = 10f;
    [Tooltip("Repasse = nível x (incomeFloorPerHouse x casas intactas + incomeMoneyShare x dinheiro no caixa), até o teto incomePerHouse x casas intactas, vezes (1 - caos) elevado a incomeChaosPower, arredondado.")]
    public float incomeFloorPerHouse = 0.3f; // BALANCEAMENTO_V6F: novo (o repasse não olhava o caixa: 1,2 por casa no nível 1 já pagava o upgrade em 10 s)
    [Tooltip("Fração do dinheiro no caixa que o repasse soma, por nível.")]
    public float incomeMoneyShare = 0.15f; // BALANCEAMENTO_V6F: novo
    [Tooltip("O repasse é multiplicado por (1 - caos) elevado a este expoente: acima de 1, o caos alto corta mais.")]
    public float incomeChaosPower = 3f; // BALANCEAMENTO_V6F: novo (antes 1)
    [Tooltip("Teto do repasse por casa intacta, o mesmo em todo nível.")]
    public float incomePerHouse = 14f; // BALANCEAMENTO_V6F: 1,2 por nível (virou o teto; 14 x (1 - caos)^3 paga a meta de 33000 no mesmo tempo)

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
