using System.Collections.Generic;
using UnityEngine;

// Sons da enchente e do meteoro (docs/jam/ENCHENTE_METEORO.md). Só escuta. Tudo toca pelo SoundBank da v5D
// (AudioManager.Play com a posição do quarteirão: lado pela tela, variação, grupo do mixer, música abaixando). Os clipes
// próprios ficam em Resources/BlockDisasters (Tools/BlockDisasters/sons_quarteirao.py) e entram no banco pelo
// SoundBankBuilder (flood_*, valve_*, meteor_*).
// - enchente: a água chegando com o aviso de chamado novo, o rangido da válvula a cada quarto de volta, o gorgolejo
//   que sobe com a pressão (fonte própria, o tom segue a pressão), o tranco da falha leve e o ralo engolindo no fim;
// - meteoro: um ronco por tremor do aviso, o assobio da queda, a explosão com o baque do terremoto e as réplicas, o
//   aviso do chamado da obra, ferramentas nos acertos, o erro abafado e o quarteirão de volta com confete.
public class BlockDisasterAudio : MonoBehaviour
{
    // Ids do SoundBank que este script toca (SoundBankTests confere que estão no banco)
    public static readonly string[] UsedIds =
    {
        "flood_rush", "flood_drained", "valve_creak", "valve_slip", "valve_drain_loop",
        "meteor_rumble", "meteor_whoosh", "meteor_impact", "quake_hit", "quake_rumble",
        "call_new_storm", "call_new_quake", "repair_tool", "ui_denied", "build_done", "ui_confetti", "fire_out"
    };

    [Header("VOLUME")]
    [SerializeField, Range(0f, 1f)] private float volume = .9f;
    [SerializeField, Range(0f, 1f)] private float drainLoopVolume = .55f;
    [SerializeField, Range(0f, 1f)] private float missVolume = .45f;
    [Tooltip("Graus de giro da válvula entre um rangido e outro.")]
    [SerializeField] private float creakEvery = 90f;
    [Tooltip("Segundos antes do impacto em que o assobio da queda começa (o MeteorFx mostra a rocha no mesmo tempo).")]
    [SerializeField] private float whooshLead = 1.1f;

    [Header("METEORO")]
    [Tooltip("Réplicas do baque depois da explosão (segundos do GameClock).")]
    [SerializeField] private float[] impactEchoes = { .45f, 1.1f };
    [Tooltip("Segundos depois da explosão até o aviso do chamado da obra.")]
    [SerializeField] private float meteorCallDelay = 1.4f;

    private AudioSource drainSource;

    private class Warning
    {
        public Vector3? at;
        public float start;
        public float seconds;
        public int tremorsDone;
        public bool whoosh;
    }

    private readonly List<Warning> warnings = new();
    private readonly Dictionary<BlockIncident, float> creakAngles = new();

    private void Awake()
    {
        drainSource = gameObject.AddComponent<AudioSource>();
        drainSource.playOnAwake = false;
        drainSource.loop = true;
        drainSource.volume = 0f;
    }

    private void OnEnable()
    {
        GameEvents.MeteorIncoming += OnMeteorIncoming;
        GameEvents.BlockIncidentStarted += OnStarted;
        GameEvents.BlockIncidentResolved += OnResolved;
        GameEvents.ValveOpened += OnValveOpened;
        GameEvents.SkillCheck += OnSkillCheck;
        GameEvents.FireDousedByFlood += OnFireDoused;
    }

    private void OnDisable()
    {
        GameEvents.MeteorIncoming -= OnMeteorIncoming;
        GameEvents.BlockIncidentStarted -= OnStarted;
        GameEvents.BlockIncidentResolved -= OnResolved;
        GameEvents.ValveOpened -= OnValveOpened;
        GameEvents.SkillCheck -= OnSkillCheck;
        GameEvents.FireDousedByFlood -= OnFireDoused;
    }

    private void Play(string id, Vector3? at, float scale = 1f) => AudioManager.Play(id, at, volume * scale);

    private static Vector3? At(House house) => house != null ? house.transform.position : (Vector3?)null;

    private void OnMeteorIncoming(BlockIncident incident, float seconds)
    {
        warnings.Add(new Warning { at = incident?.Center, start = GameClock.Time, seconds = Mathf.Max(.5f, seconds) });
    }

    private void OnStarted(BlockIncident incident)
    {
        if (incident == null) return;

        Vector3 at = incident.Center;
        Vector3 call = At(incident.leader) ?? at;
        if (incident.kind == IncidentKind.Flood)
        {
            Play("flood_rush", at);
            Play("call_new_storm", call);
            return;
        }

        warnings.Clear();
        Play("meteor_impact", at);
        Play("quake_hit", at);
        Play("quake_rumble", at, .8f);
        for (int i = 0; i < impactEchoes.Length; i++)
        {
            Vector3 near = at + (Vector3)(Random.insideUnitCircle * 2f);
            AudioManager.PlayDelayed("quake_hit", impactEchoes[i], near, volume * Mathf.Max(.2f, .7f - .2f * i));
        }
        AudioManager.PlayDelayed("call_new_quake", meteorCallDelay, call, volume);
    }

    private void OnResolved(BlockIncident incident)
    {
        if (incident == null) return;

        creakAngles.Remove(incident);
        Vector3 at = incident.Center;
        if (incident.kind == IncidentKind.Flood)
        {
            Play("flood_drained", at);
            return;
        }
        Play("build_done", at);
        Play("ui_confetti", at, .8f);
    }

    private void OnValveOpened(BlockIncident incident)
    {
        if (incident == null) return;

        creakAngles[incident] = 0f;
        Play("valve_creak", incident.Center);
    }

    private void OnSkillCheck(House house, SkillResult result)
    {
        switch (result)
        {
            case SkillResult.ValveSlip: Play("valve_slip", At(house)); break;
            case SkillResult.RebuildHit: Play("repair_tool", At(house)); break;
            case SkillResult.RebuildMiss: Play("ui_denied", At(house), missVolume); break;
        }
    }

    private void OnFireDoused(House house) => Play("fire_out", At(house), .6f);

    private void Update()
    {
        bool playing = GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;
        UpdateWarnings(playing);
        UpdateValves(playing);
    }

    // Um ronco a cada tremor do aviso (o mesmo calendário do MeteorFx) e o assobio no fim.
    private void UpdateWarnings(bool playing)
    {
        if (!playing) return;

        int tremors = Mathf.Max(1, BlockDisasterData.Instance.meteorTremors);
        for (int i = warnings.Count - 1; i >= 0; i--)
        {
            Warning warning = warnings[i];
            float t = Mathf.Clamp01((GameClock.Time - warning.start) / warning.seconds);
            int due = Mathf.Min(tremors, Mathf.FloorToInt(t * (tremors + 1)));
            if (warning.tremorsDone < due)
            {
                warning.tremorsDone = due;
                Play("meteor_rumble", warning.at, Mathf.Lerp(.4f, 1f, due / (float)tremors));
            }
            if (!warning.whoosh && (1f - t) * warning.seconds <= whooshLead)
            {
                warning.whoosh = true;
                Play("meteor_whoosh", warning.at);
            }
            if (t >= 1f) warnings.RemoveAt(i);
        }
    }

    // Válvula: rangido a cada quarto de volta e gorgolejo no volume da pressão.
    private void UpdateValves(bool playing)
    {
        BlockDisasterManager manager = BlockDisasterManager.Instance;
        float pressure = 0f;
        if (manager != null)
        {
            foreach (BlockIncident incident in manager.Incidents)
            {
                if (!incident.ValveOpen) continue;
                pressure = Mathf.Max(pressure, incident.valve.pressure);
                if (!creakAngles.TryGetValue(incident, out float last)) last = incident.valve.angle;
                if (incident.valve.angle - last >= creakEvery)
                {
                    last = incident.valve.angle;
                    Play("valve_creak", incident.Center, .7f);
                }
                creakAngles[incident] = Mathf.Min(last, incident.valve.angle);
            }
        }

        if (!EnsureDrainClip()) return;
        float target = playing ? pressure * drainLoopVolume * volume * AudioManager.CategoryVolume(SoundCategory.Gameplay) : 0f;
        drainSource.volume = Mathf.MoveTowards(drainSource.volume, target, GameClock.DeltaTime * 2f);
        drainSource.pitch = .85f + pressure * .35f;
        if (drainSource.volume > .001f && !drainSource.isPlaying) drainSource.Play();
        else if (drainSource.volume <= .001f && drainSource.isPlaying) drainSource.Stop();
    }

    // O gorgolejo muda de tom com a pressão, então não é um loop do banco: fonte própria com o clipe do banco,
    // no grupo do mixer dele (o volume de SFX e a pausa valem para ele).
    private bool EnsureDrainClip()
    {
        if (drainSource.clip != null) return true;

        SoundBank bank = AudioManager.Bank;
        SoundEntry entry = bank != null ? bank.Find("valve_drain_loop") : null;
        if (entry == null || entry.clips == null || entry.clips.Length == 0 || entry.clips[0] == null) return false;

        drainSource.clip = entry.clips[0];
        if (AudioManager.Instance != null) drainSource.outputAudioMixerGroup = AudioManager.Instance.Group(entry.category);
        return true;
    }
}
