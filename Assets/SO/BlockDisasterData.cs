using System.Collections.Generic;
using UnityEngine;

// Números da enchente e do meteoro (docs/jam/ENCHENTE_METEORO.md): os dois atingem um quarteirão inteiro e abrem um
// chamado único. Fora do BalanceData para não mexer no mesmo asset que a v5A em paralelo; entrada e peso no sorteio
// também ficam aqui (o DisasterManager pergunta por GetWeight/GetUnlockTime).
// Asset em Assets/Systems/BlockDisasters/Resources/BlockDisasters/BlockDisasterData.asset, criado por
// Tools/Jam/Enchente e meteoro/Criar dados. Os quarteirões são os do para-raio (LightningRodData.blocks).
[CreateAssetMenu(fileName = "BlockDisasterData", menuName = "Scriptable Objects/BlockDisasterData")]
public class BlockDisasterData : ScriptableObject
{
    public const string ResourcePath = "BlockDisasters/BlockDisasterData";

    [Header("QUARTEIRÕES")]
    [Tooltip("Polígonos Block do city_layout.json (os mesmos do para-raio), copiados pelo menu. Vazio: usa os do LightningRodData.")]
    public List<RodBlockSlot> blocks = new();
    [Tooltip("Folga em volta do quarteirão, em unidades, para a base de uma casa contar como dele.")]
    public float houseMargin = 0.4f;

    [Header("ENCHENTE: ENTRADA")]
    [Tooltip("Segundos de partida até a enchente entrar no sorteio. Estreia no primeiro sorteio depois disso (RITMO_V3).")]
    public float floodUnlockTime = 225f;
    [Tooltip("Peso no sorteio, junto dos pesos do BalanceData (terremoto 40, tempestade 50, incêndio 30).")]
    public float floodWeight = 25f;

    [Header("ENCHENTE: QUARTEIRÃO")]
    [Tooltip("Casas comuns que o quarteirão precisa ter para alagar.")]
    public int floodMinHouses = 3;
    [Tooltip("Segundos para a água subir toda depois do aviso.")]
    public float floodRiseTime = 6f;
    [Tooltip("Peso de cada casa alagada no caos (danificada pesa 1).")]
    public float floodChaosPerHouse = 0.6f;
    [Tooltip("Equipe mínima do chamado da enchente.")]
    public int floodWorkers = 2;
    [Tooltip("Dinheiro por casa alagada quando o bueiro desentope.")]
    public int floodRewardPerHouse = 12;
    [Tooltip("Sem a válvula a equipe escoa sozinha, devagar: segundos com a equipe mínima (0 = só pela válvula).")]
    public float floodAutoDrainTime = 30f;

    [Header("ENCHENTE: VÁLVULA DO BUEIRO")]
    [Tooltip("Pressão que cada volta do mouse em torno da válvula soma (1 = cheia).")]
    public float valvePressurePerTurn = 0.55f;
    [Tooltip("Pressão que vaza por segundo, girando ou não.")]
    public float valvePressureDecay = 0.3f;
    [Tooltip("Água que escoa por segundo com a pressão cheia (1 = o quarteirão inteiro).")]
    public float valveDrainPerSecond = 0.28f;
    [Tooltip("Soltar o botão com pressão acima disto é a falha leve: a pressão zera e a água volta um pouco.")]
    [Range(0f, 1f)] public float valveSlipPressure = 0.2f;
    [Tooltip("Água que volta na falha leve.")]
    public float valveSlipPenalty = 0.08f;
    [Tooltip("Raio em volta da válvula onde o giro conta, em pixels de uma tela de 1080 de altura.")]
    public float valveReach = 300f;

    [Header("METEORO: ENTRADA")]
    [Tooltip("Segundos de partida até o meteoro entrar no sorteio. Estreia no primeiro sorteio depois disso.")]
    public float meteorUnlockTime = 470f;
    [Tooltip("Peso no sorteio depois da estreia: baixo, é raro.")]
    public float meteorWeight = 6f;
    [Tooltip("Segundos mínimos entre um meteoro e outro (fora do sorteio nesse tempo).")]
    public float meteorCooldown = 180f;

    [Header("METEORO: QUEDA")]
    [Tooltip("Aviso do meteoro: a sombra cresce no quarteirão e o chão treme cada vez mais.")]
    public float meteorWarningTime = 7f;
    [Tooltip("Tremores do aviso (o trem de tremor), do fraco ao forte, antes do impacto.")]
    public int meteorTremors = 5;
    [Tooltip("Casas comuns que o quarteirão precisa ter para o meteoro cair nele.")]
    public int meteorMinHouses = 3;
    [Tooltip("Peso de cada casa destruída no caos (danificada pesa 1).")]
    public float meteorChaosPerHouse = 0.75f;

    [Header("METEORO: RECONSTRUÇÃO (chamado único)")]
    [Tooltip("Equipe mínima fixa e alta: o chamado não sai com menos.")]
    public int meteorWorkers = 8;
    [Tooltip("Material que a obra gasta no envio.")]
    public int meteorMaterial = 6;
    [Tooltip("Segundos de obra com a equipe mínima e sem acerto (equipe maior divide).")]
    public float meteorWorkTime = 90f;
    [Tooltip("Dinheiro por casa reconstruída.")]
    public int meteorRewardPerHouse = 45;

    [Header("METEORO: PONTOS CERTOS EM SEQUÊNCIA")]
    [Tooltip("Tamanho de cada janela, em fração da obra.")]
    public float rebuildWindowSize = 0.06f;
    [Tooltip("Distância da próxima janela à frente do progresso (sorteada entre os dois).")]
    public float rebuildWindowGapMin = 0.04f;
    public float rebuildWindowGapMax = 0.09f;
    [Tooltip("Cada acerto adianta este tanto da obra.")]
    public float rebuildHitProgress = 0.02f;
    [Tooltip("Cada acerto soma esta aceleração (0,5 = 50% mais rápido).")]
    public float rebuildHitBoost = 0.5f;
    [Tooltip("Aceleração máxima somada (2 = a obra anda até 3 vezes mais rápido).")]
    public float rebuildMaxBoost = 2f;
    [Tooltip("Aceleração que se perde por segundo.")]
    public float rebuildBoostDecay = 0.04f;
    [Tooltip("Erro: a obra volta este tanto e a aceleração cai pela metade.")]
    public float rebuildMissPenalty = 0.02f;

    [Header("ARTE (o menu liga os PNGs de Assets/Arts/UI/Chamados)")]
    [Tooltip("Balão e progresso do chamado da enchente.")]
    public Sprite callFloodIcon;
    [Tooltip("Balão e progresso do chamado de reconstrução do meteoro.")]
    public Sprite callRebuildIcon;

    // Quarteirões do mapa: os copiados aqui ou, sem eles, os do para-raio.
    public IReadOnlyList<RodBlockSlot> Blocks => blocks.Count > 0 ? blocks : LightningRodData.Load().blocks;

    private static BlockDisasterData instance;

    // O asset de Resources ou, sem ele, uma cópia com os padrões (o jogo roda igual; o aviso diz como criar).
    public static BlockDisasterData Instance
    {
        get
        {
            if (instance != null) return instance;

            instance = Resources.Load<BlockDisasterData>(ResourcePath);
            if (instance == null)
            {
                Debug.LogWarning($"BlockDisasterData não encontrado em Resources/{ResourcePath}: usando os valores padrão da classe. Rode Tools/Jam/Enchente e meteoro/Criar dados.");
                instance = CreateInstance<BlockDisasterData>();
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
