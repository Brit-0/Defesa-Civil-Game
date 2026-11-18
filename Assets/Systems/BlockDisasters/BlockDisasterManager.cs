using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// Enchente e meteoro (docs/jam/ENCHENTE_METEORO.md): os dois desastres que atingem um quarteirão inteiro.
// - O DisasterManager sorteia e avisa como sempre; aqui saem o quarteirão (Forecast) e a execução (Run).
// - Enchente: o quarteirão alaga (casas sem equipe viram Flooded, o fogo de lá apaga) e abre um chamado único no
//   bueiro (o líder: a casa mais perto do meio). A equipe chega e a válvula abre (ValveMinigame): segurar e girar escoa.
// - Meteoro: a sombra cresce no aviso, o meteoro cai e o quarteirão vira cratera (casas Destroyed) com um chamado único
//   de reconstrução: equipe mínima alta, obra longa; pontos certos em sequência aceleram a obra.
// - Coletado o chamado, as casas voltam a Intact juntas (e a ocorrência paga o bônus de sempre).
// Para-raio não protege de nenhum dos dois. Nasce sozinho (BlockDisasterBootstrap).
public class BlockDisasterManager : MonoBehaviour
{
    public static BlockDisasterManager Instance;

    private class Block
    {
        public RodBlockSlot slot;
        public readonly List<House> houses = new();
        public bool landmark;   // tem Base ou Armazém: o meteoro e a enchente evitam
    }

    private readonly List<Block> blocks = new();
    private readonly List<BlockIncident> incidents = new();
    private readonly Dictionary<DisasterForecast, Block> planned = new();
    private readonly List<int> freeCounts = new();
    private readonly List<bool> landmarks = new();
    private float lastMeteorTime = float.NegativeInfinity;
    private bool built;

    public IReadOnlyList<BlockIncident> Incidents => incidents;
    // Alvo do meteoro durante o aviso (sombra e tremor do MeteorFx); null fora dele.
    public BlockIncident PendingMeteor { get; private set; }

    private static BlockDisasterData Data => BlockDisasterData.Instance;
    private static float Elapsed => GameManager.Instance != null ? GameManager.Instance.ElapsedTime : 0f;

    private void Awake()
    {
        Instance = this;
    }

    // Debug no Play: selecione [Enchente e meteoro] na Hierarchy (COMO_TESTAR.md).
    [Button("Forçar enchente")]
    private void ForceFlood() => Force(DisasterType.Flood);

    [Button("Forçar meteoro")]
    private void ForceMeteor() => Force(DisasterType.Meteor);

    private static void Force(DisasterType type)
    {
        if (DisasterManager.Instance == null || !DisasterManager.Instance.ForceDisaster(type))
            Debug.LogWarning($"[Enchente e meteoro] {type} não começou: já há um desastre rodando, a partida não está em jogo ou não há quarteirão livre.");
    }

    private void OnEnable()
    {
        GameEvents.CallResolved += OnCallResolved;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.CallResolved -= OnCallResolved;
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    #region SORTEIO

    // O DisasterManager pergunta antes de sortear. Os outros tipos sempre valem; enchente e meteoro só com quarteirão
    // livre, um de cada por vez, e o meteoro fora do intervalo mínimo entre um e outro.
    public static bool Allows(DisasterType type)
    {
        if (type != DisasterType.Flood && type != DisasterType.Meteor) return true;

        BlockDisasterManager manager = Instance;
        if (manager == null) return false;

        bool meteor = type == DisasterType.Meteor;
        if (meteor && Elapsed - manager.lastMeteorTime < Data.meteorCooldown) return false;
        if (manager.HasIncident(meteor ? IncidentKind.Meteor : IncidentKind.Flood)) return false;
        return manager.PickBlock(meteor ? Data.meteorMinHouses : Data.floodMinHouses, 0f) >= 0;
    }

    public bool HasIncident(IncidentKind kind)
    {
        foreach (BlockIncident incident in incidents)
        {
            if (incident.kind == kind && !incident.resolved) return true;
        }
        return kind == IncidentKind.Meteor && PendingMeteor != null;
    }

    // Escolhe no aviso o quarteirão que vai ser atingido: o jogador vê onde (sombra do meteoro, previsão).
    public bool Forecast(DisasterForecast forecast)
    {
        bool meteor = forecast.disaster.type == DisasterType.Meteor;
        int index = PickBlock(meteor ? Data.meteorMinHouses : Data.floodMinHouses, Random.value);
        if (index < 0) return false;

        Block block = blocks[index];
        Rect area = block.slot.Area;
        float z = block.houses.Count > 0 ? block.houses[0].transform.position.z : 0f;
        forecast.area = area;
        forecast.position = new Vector3(area.center.x, area.center.y, z);
        forecast.radius = area.size.magnitude * 0.5f;
        forecast.targets.AddRange(block.houses);
        forecast.houses = block.houses.Count;
        planned[forecast] = block;

        if (meteor)
        {
            PendingMeteor = new BlockIncident { kind = IncidentKind.Meteor, disaster = forecast.disaster, area = area };
            GameEvents.RaiseMeteorIncoming(PendingMeteor, forecast.seconds);
        }
        return true;
    }

    // Quarteirão com casas livres (sem equipe e fora de outro quarteirão atingido). roll em [0, 1).
    private int PickBlock(int minHouses, float roll)
    {
        EnsureBlocks();
        freeCounts.Clear();
        landmarks.Clear();
        foreach (Block block in blocks)
        {
            int free = 0;
            bool busy = false;
            foreach (House house in block.houses)
            {
                if (house == null) continue;
                if (house.IncidentLeader != null) busy = true;
                else if (!house.HasTeam && house.currentState == State.Intact) free++;
            }
            freeCounts.Add(busy ? 0 : free);
            landmarks.Add(block.landmark);
        }
        return BlockDisasterRules.PickBlock(freeCounts, landmarks, minHouses, roll);
    }

    // Quarteirões do mapa com as casas comuns de cada um (a base visível da casa dentro do retângulo, como no para-raio).
    private void EnsureBlocks()
    {
        if (built) return;

        LocationsManager locations = LocationsManager.Instance;
        if (locations == null) return;
        built = true;

        IReadOnlyList<RodBlockSlot> slots = Data.Blocks;
        for (int i = 0; i < slots.Count; i++) blocks.Add(new Block { slot = slots[i] });

        foreach (House house in locations.Houses)
        {
            int index = LightningRodMath.BlockOf(slots, LightningRodManager.BaseOf(house), Data.houseMargin);
            if (index >= 0) blocks[index].houses.Add(house);
        }
        MarkLandmarks(locations.Guards);
        MarkLandmarks(locations.Warehouses);
        blocks.RemoveAll(block => block.houses.Count == 0);
    }

    private void MarkLandmarks<T>(IEnumerable<T> landmarks) where T : House
    {
        if (landmarks == null) return;

        IReadOnlyList<RodBlockSlot> slots = Data.Blocks;
        foreach (T landmark in landmarks)
        {
            if (landmark == null) continue;
            int index = LightningRodMath.BlockOf(slots, LightningRodManager.BaseOf(landmark), Data.houseMargin);
            if (index >= 0) blocks[index].landmark = true;
        }
    }

    #endregion

    #region EXECUÇÃO

    // Depois do aviso: as casas do quarteirão entram no chamado único. Chamado pelo DisasterManager.RunDisaster.
    public IEnumerator Run(DisasterData disaster, DisasterForecast forecast, Occurrence occurrence)
    {
        bool meteor = disaster.type == DisasterType.Meteor;
        BlockIncident incident = meteor && PendingMeteor != null ? PendingMeteor : new BlockIncident();
        PendingMeteor = null;

        if (!planned.Remove(forecast, out Block block))
        {
            if (meteor) GameEvents.RaiseMeteorCancelled(incident);
            yield break;
        }

        incident.kind = meteor ? IncidentKind.Meteor : IncidentKind.Flood;
        incident.disaster = disaster;
        incident.area = block.slot.Area;
        incident.startTime = GameClock.Time;

        Vector2 center = incident.area.center;
        List<House> order = new(block.houses);
        order.RemoveAll(house => house == null);
        order.Sort((a, b) => ((Vector2)a.transform.position - center).sqrMagnitude.CompareTo(((Vector2)b.transform.position - center).sqrMagnitude));

        // O bueiro (ou o canteiro da obra) fica na casa livre mais perto do meio do quarteirão.
        House leader = order.Find(house => CanJoin(house, meteor));
        if (leader == null)
        {
            // Sem casa livre o meteoro não cai: sem este aviso a rocha ficava parada no céu do quarteirão, soltando
            // fogo e fumaça escura por cima das casas até o fim da partida (v5F).
            if (meteor) GameEvents.RaiseMeteorCancelled(incident);
            if (meteor) lastMeteorTime = Elapsed;
            GameEvents.RaiseDisasterStarted(disaster, forecast.position);
            GameEvents.RaiseDisasterEnded(disaster);
            yield break;
        }

        incident.leader = leader;
        incident.call = new Call
        {
            house = leader,
            type = CallType.Problem,
            workersNeeded = Mathf.Max(1, meteor ? Data.meteorWorkers : Data.floodWorkers),
            rule = incident,
        };

        GameEvents.RaiseDisasterStarted(disaster, incident.Center);

        Join(incident, leader, occurrence, meteor);
        foreach (House house in order)
        {
            if (house == leader) continue;
            // Fogo com equipe já a caminho ou trabalhando: a água apaga (a obra acaba agora ou na chegada).
            if (!meteor && house.currentState == State.OnFire && house.HasTeam)
            {
                house.DouseByFlood();
                continue;
            }
            if (CanJoin(house, meteor)) Join(incident, house, occurrence, meteor);
        }

        incidents.Add(incident);
        if (meteor) lastMeteorTime = Elapsed;
        GameEvents.RaiseBlockIncidentStarted(incident);
        GameEvents.RaiseDisasterEnded(disaster);
    }

    // Enchente pega casa intacta ou em chamas (o fogo apaga; danificada segue com o chamado dela).
    // Meteoro pega qualquer casa sem equipe. Casa com equipe fica de fora nos dois.
    private static bool CanJoin(House house, bool meteor)
    {
        if (house == null || house.HasTeam || house.IsComplete || house.IncidentLeader != null) return false;
        return meteor || house.currentState == State.Intact || house.currentState == State.OnFire;
    }

    private static void Join(BlockIncident incident, House house, Occurrence occurrence, bool meteor)
    {
        bool wasFire = house.currentState == State.OnFire;
        Call call = house == incident.leader ? incident.call : null;
        if (!house.JoinIncident(meteor ? State.Destroyed : State.Flooded, incident.leader, call)) return;

        incident.houses.Add(house);
        if (occurrence != null && !occurrence.houses.Contains(house)) occurrence.houses.Add(house);
        if (wasFire && !meteor) GameEvents.RaiseFireDousedByFlood(house);
    }

    // Chamado único coletado: o líder já voltou a Intact; as outras casas do quarteirão voltam junto.
    private void OnCallResolved(Call call, int money)
    {
        if (call == null || call.rule is not BlockIncident incident || incident.resolved) return;

        incident.resolved = true;
        incident.CloseValve();
        foreach (House house in incident.houses)
        {
            if (house != null) house.LeaveIncident();
        }
        incidents.Remove(incident);
        GameEvents.RaiseBlockIncidentResolved(incident);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Won || state == GameState.Lost) PendingMeteor = null;
    }

    #endregion
}
