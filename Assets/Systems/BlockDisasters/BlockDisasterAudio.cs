using System.Collections.Generic;
using UnityEngine;

// Sons da enchente e do meteoro (docs/jam/ENCHENTE_METEORO.md). Só escuta: os clipes ficam em
// Resources/BlockDisasters (feitos por script, Tools/BlockDisasters/sons_quarteirao.py) e tocam pelo AudioManager.
// - enchente: a água chegando, o rangido da válvula a cada quarto de volta, o gorgolejo que sobe com a pressão
//   (loop próprio), o tranco da falha leve e o ralo engolindo no fim;
// - meteoro: um ronco por tremor do aviso, o assobio da queda, a explosão; martelo nos acertos da obra.
public class BlockDisasterAudio : MonoBehaviour
{
    private const string SoundPath = "BlockDisasters/";

    [Header("VOLUME")]
    [SerializeField, Range(0f, 1f)] private float volume = .9f;
    [SerializeField, Range(0f, 1f)] private float drainLoopVolume = .55f;
    [Tooltip("Graus de giro da válvula entre um rangido e outro.")]
    [SerializeField] private float creakEvery = 90f;
    [Tooltip("Segundos antes do impacto em que o assobio da queda começa (o MeteorFx mostra a rocha no mesmo tempo).")]
    [SerializeField] private float whooshLead = 1.1f;

    private AudioClip floodRush, valveCreak, valveDrain, valveSlip, floodDrained, meteorRumble, meteorWhoosh, meteorImpact;
    private AudioSource drainSource;

    private class Warning
    {
        public float start;
        public float seconds;
        public int tremorsDone;
        public bool whoosh;
    }

    private readonly List<Warning> warnings = new();
    private readonly Dictionary<BlockIncident, float> creakAngles = new();

    private static SfxLibrary Sfx => AudioManager.Sfx;

    private void Awake()
    {
        floodRush = Load("Flood_Rush");
        valveCreak = Load("Valve_Creak");
        valveDrain = Load("Valve_Drain");
        valveSlip = Load("Valve_Slip");
        floodDrained = Load("Flood_Drained");
        meteorRumble = Load("Meteor_Rumble");
        meteorWhoosh = Load("Meteor_Whoosh");
        meteorImpact = Load("Meteor_Impact");

        drainSource = gameObject.AddComponent<AudioSource>();
        drainSource.playOnAwake = false;
        drainSource.loop = true;
        drainSource.clip = valveDrain;
        drainSource.volume = 0f;
    }

    private static AudioClip Load(string clipName) => Resources.Load<AudioClip>(SoundPath + clipName);

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

    private void Play(AudioClip clip, float scale = 1f, float pitch = .05f)
    {
        AudioManager.PlaySfx(clip, SoundCategory.Disaster, volume * scale, pitch);
    }

    private void OnMeteorIncoming(BlockIncident incident, float seconds)
    {
        warnings.Add(new Warning { start = GameClock.Time, seconds = Mathf.Max(.5f, seconds) });
    }

    private void OnStarted(BlockIncident incident)
    {
        if (incident == null) return;
        if (incident.kind == IncidentKind.Flood)
        {
            Play(floodRush);
            return;
        }
        warnings.Clear();
        Play(meteorImpact, 1f, 0f);
    }

    private void OnResolved(BlockIncident incident)
    {
        if (incident == null) return;
        creakAngles.Remove(incident);
        if (incident.kind == IncidentKind.Flood) Play(floodDrained);
        else AudioManager.PlaySfx(Sfx?.repaired, SoundCategory.Gameplay, volume);
    }

    private void OnValveOpened(BlockIncident incident)
    {
        creakAngles[incident] = 0f;
        AudioManager.PlaySfx(valveCreak, SoundCategory.Gameplay, volume, .1f);
    }

    private void OnSkillCheck(House house, SkillResult result)
    {
        if (result == SkillResult.ValveSlip) AudioManager.PlaySfx(valveSlip, SoundCategory.Gameplay, volume, .08f);
        else if (result == SkillResult.RebuildHit) AudioManager.PlaySfx(Sfx?.hammer, SoundCategory.Gameplay, volume, .12f);
    }

    private void OnFireDoused(House house)
    {
        AudioManager.PlaySfx(Sfx?.extinguish, SoundCategory.Disaster, volume * .6f, .1f);
    }

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
                Play(meteorRumble, Mathf.Lerp(.4f, 1f, due / (float)tremors));
            }
            if (!warning.whoosh && (1f - t) * warning.seconds <= whooshLead)
            {
                warning.whoosh = true;
                Play(meteorWhoosh, 1f, 0f);
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
                    AudioManager.PlaySfx(valveCreak, SoundCategory.Gameplay, volume * .7f, .15f);
                }
                creakAngles[incident] = Mathf.Min(last, incident.valve.angle);
            }
        }

        if (drainSource.clip == null) return;
        float target = playing ? pressure * drainLoopVolume * volume * AudioManager.CategoryVolume(SoundCategory.Gameplay) : 0f;
        drainSource.volume = Mathf.MoveTowards(drainSource.volume, target, Time.unscaledDeltaTime * 2f);
        drainSource.pitch = .85f + pressure * .35f;
        if (drainSource.volume > .001f && !drainSource.isPlaying) drainSource.Play();
        else if (drainSource.volume <= .001f && drainSource.isPlaying) drainSource.Stop();
    }
}
