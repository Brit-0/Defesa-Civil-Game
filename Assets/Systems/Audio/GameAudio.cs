using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Liga os eventos do jogo aos sons do SoundBank (v5, docs/jam/SFX_V5.md). Só escuta eventos; não tem regra de jogo.
//Os sistemas que ainda tocam clipes antigos (moedas, martelo da obra, raio absorvido) ganham o som novo pelo
//mapa de clipes antigos do SoundBank: aqui não se toca de novo o que eles já tocam.
public class GameAudio : MonoBehaviour
{
    [Header("INTERFACE")]
    [SerializeField, Range(0f, 1f)] private float clickVolume = .7f;
    [SerializeField, Range(0f, 1f)] private float tabVolume = .8f;
    [SerializeField, Range(0f, 1f)] private float buyVolume = .9f;
    [SerializeField, Range(0f, 1f)] private float errorVolume = .8f;

    [Header("CALLS")]
    [SerializeField, Range(0f, 1f)] private float newCallVolume = .8f;
    [SerializeField, Range(0f, 1f)] private float dispatchVolume = .8f;
    [SerializeField, Range(0f, 1f)] private float baseSelectVolume = .55f;
    [SerializeField, Range(0f, 1f)] private float arriveVolume = .7f;
    [SerializeField, Range(0f, 1f)] private float repairDoneVolume = .85f;
    [SerializeField, Range(0f, 1f)] private float collectVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float incomeVolume = .5f;
    [Tooltip("Cada conserto em sequência sobe a coleta este tanto (semitons), até o teto")]
    [SerializeField] private float streakSemitones = 1f;
    [SerializeField] private int streakMaxSteps = 7;

    [Header("REPAIR TOOLS (enquanto houver equipe trabalhando)")]
    [SerializeField, Range(0f, 1f)] private float toolVolume = .55f;
    [SerializeField] private float toolIntervalMin = .3f;
    [SerializeField] private float toolIntervalMax = .75f;
    [Tooltip("Quantas obras tocam ferramenta ao mesmo tempo")]
    [SerializeField] private int maxToolSites = 3;

    [Header("URGENCY (caos alto: o aviso de chamado acelera e sobe)")]
    [SerializeField, Range(0f, 1f)] private float dangerChaos = .6f;
    [SerializeField] private float dangerSirenInterval = 9f;
    [SerializeField] private float panicInterval = 3.5f;
    [SerializeField, Range(0f, 1f)] private float urgentVolume = .55f;
    [Tooltip("Casa pegando fogo com menos deste tempo de fogo restante toca o aviso de urgência uma vez")]
    [SerializeField, Range(0f, 1f)] private float fireUrgentAt = .3f;

    [Header("FIRE")]
    [SerializeField, Range(0f, 1f)] private float igniteVolume = .8f;
    [SerializeField, Range(0f, 1f)] private float fireVolume = .5f;
    [SerializeField, Range(0f, 1f)] private float jetVolume = .75f;
    [SerializeField, Range(0f, 1f)] private float extinguishVolume = .8f;
    [SerializeField] private float loopFadeTime = 1f;

    [Header("STORM (chuva fraca, chuva forte que cresce, vento e trovões ao longe)")]
    [SerializeField, Range(0f, 1f)] private float rainLightVolume = .7f;
    [SerializeField, Range(0f, 1f)] private float rainHeavyVolume = .8f;
    [SerializeField, Range(0f, 1f)] private float windVolume = .6f;
    [SerializeField] private float stormBuildTime = 8f;
    [SerializeField] private float farThunderMin = 5f;
    [SerializeField] private float farThunderMax = 12f;
    [SerializeField, Range(0f, 1f)] private float farThunderVolume = .7f;
    [SerializeField, Range(0f, 1f)] private float thunderVolume = .9f;

    [Header("EARTHQUAKE")]
    [SerializeField, Range(0f, 1f)] private float earthquakeVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float quakeHitVolume = .9f;
    [SerializeField] private float[] aftershockDelays = { .7f, 1.6f };

    [Header("WARNINGS AND BUILDING")]
    [SerializeField, Range(0f, 1f)] private float warningVolume = .9f;
    [SerializeField, Range(0f, 1f)] private float buildVolume = .8f;

    [Header("CITY AMBIENCE (dia e noite pelo DayNightCycle)")]
    [SerializeField, Range(0f, 1f)] private float dayAmbienceVolume = .55f;
    [SerializeField, Range(0f, 1f)] private float nightAmbienceVolume = .6f;
    [Tooltip("Com chuva o ambiente da cidade fica neste tanto (a chuva cobre)")]
    [SerializeField, Range(0f, 1f)] private float ambienceUnderRain = .35f;
    [SerializeField, Range(0f, 1f)] private float cricketNight = .55f;
    [SerializeField] private float cricketMin = 2.5f;
    [SerializeField] private float cricketMax = 7f;
    [SerializeField, Range(0f, 1f)] private float cricketVolume = .5f;

    [Header("MUSIC (camadas pelo caos)")]
    [SerializeField, Range(0f, 1f)] private float tensionStartChaos = .35f;
    [SerializeField, Range(0f, 1f)] private float tensionFullChaos = .7f;
    [Tooltip("A camada de perigo sobe deste caos até 100%")]
    [SerializeField, Range(0f, 1f)] private float dangerLayerChaos = .7f;

    [Header("MUSIC LEVEL (por estado da partida)")]
    [SerializeField, Range(0f, 1f)] private float menuMusicLevel = .7f;
    [SerializeField, Range(0f, 1f)] private float pausedMusicLevel = .35f;
    [SerializeField, Range(0f, 1f)] private float endMusicLevel = .3f;
    [SerializeField] private float musicLevelFadeTime = .6f;

    [Header("START AND END")]
    [SerializeField, Range(0f, 1f)] private float startVolume = .8f;
    [SerializeField, Range(0f, 1f)] private float endVolume = 1f;

    // Nomes que este script toca: o teste de editor confere que todos estão no SoundBank
    public static readonly string[] UsedIds =
    {
        "ui_click", "ui_tab", "ui_popup", "ui_buy", "ui_denied", "ui_coin_tick",
        "call_new_fire", "call_new_storm", "call_new_quake", "call_new_power", "call_urgent",
        "team_depart", "team_arrive", "repair_tool", "repair_done", "call_collect",
        "fire_ignite", "fire_loop", "fire_jet_start", "fire_jet_loop", "fire_out", "warning_fire",
        "rain_light_loop", "rain_heavy_loop", "wind_loop", "thunder_near", "thunder_far", "warning_storm",
        "quake_rumble", "quake_hit", "warning_quake", "build_start", "build_done",
        "amb_day", "amb_night", "amb_cricket", "jingle_win", "jingle_lose", "siren"
    };

    private static readonly string[] StormLoops = { "rain_light_loop", "rain_heavy_loop", "wind_loop" };

    private readonly Dictionary<Call, float> working = new Dictionary<Call, float>();
    private readonly List<Call> workingKeys = new List<Call>();
    private readonly HashSet<House> burning = new HashSet<House>();
    private readonly HashSet<House> urgentFired = new HashSet<House>();
    private readonly Dictionary<DisasterType, int> active = new Dictionary<DisasterType, int>();
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private PointerEventData pointerData;
    private EventSystem pointerEventSystem;
    private float chaos;
    private float nextUrgent;
    private float stormStart = -1f;
    private float nextFarThunder;
    private float nextCricket;
    private float nextAmbience;
    private int streak;
    private bool playing;
    private bool cameFromMenu;

    //No reinício a cena traz outro AudioManager, que se destrói: o GameAudio dele não pode tocar em dobro
    private bool IsDuplicate => TryGetComponent(out AudioManager own) && AudioManager.Instance != null && AudioManager.Instance != own;

    private bool Active(DisasterType type) => active.TryGetValue(type, out int count) && count > 0;

    private void OnEnable()
    {
        if (IsDuplicate) return;

        GameEvents.CallOpened += OnCallOpened;
        GameEvents.BaseSelected += OnBaseSelected;
        GameEvents.TeamDispatched += OnTeamDispatched;
        GameEvents.WorkerArrived += OnWorkerArrived;
        GameEvents.WorkStarted += OnWorkStarted;
        GameEvents.WorkCompleted += OnWorkCompleted;
        GameEvents.CallResolved += OnCallResolved;
        GameEvents.RepairStreak += OnRepairStreak;
        GameEvents.PassiveIncomePaid += OnPassiveIncomePaid;
        GameEvents.UpgradePurchased += OnUpgradePurchased;
        GameEvents.ActionDenied += OnActionDenied;
        GameEvents.HouseStateChanged += OnHouseStateChanged;
        GameEvents.FireJetOpened += OnFireJetOpened;
        GameEvents.FireLevelChanged += OnFireLevelChanged;
        GameEvents.DisasterWarning += OnDisasterWarning;
        GameEvents.DisasterStarted += OnDisasterStarted;
        GameEvents.DisasterEnded += OnDisasterEnded;
        GameEvents.LightningStruck += OnLightningStruck;
        GameEvents.ChaosChanged += OnChaosChanged;
        GameEvents.GameStateChanged += OnGameStateChanged;
        ExpansionEvents.BuildStarted += OnLotBuildStarted;
        ExpansionEvents.BuildFinished += OnLotBuildFinished;
        LightningRodEvents.BuildStarted += OnRodBuildStarted;
        LightningRodEvents.BuildFinished += OnRodBuildFinished;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.CallOpened -= OnCallOpened;
        GameEvents.BaseSelected -= OnBaseSelected;
        GameEvents.TeamDispatched -= OnTeamDispatched;
        GameEvents.WorkerArrived -= OnWorkerArrived;
        GameEvents.WorkStarted -= OnWorkStarted;
        GameEvents.WorkCompleted -= OnWorkCompleted;
        GameEvents.CallResolved -= OnCallResolved;
        GameEvents.RepairStreak -= OnRepairStreak;
        GameEvents.PassiveIncomePaid -= OnPassiveIncomePaid;
        GameEvents.UpgradePurchased -= OnUpgradePurchased;
        GameEvents.ActionDenied -= OnActionDenied;
        GameEvents.HouseStateChanged -= OnHouseStateChanged;
        GameEvents.FireJetOpened -= OnFireJetOpened;
        GameEvents.FireLevelChanged -= OnFireLevelChanged;
        GameEvents.DisasterWarning -= OnDisasterWarning;
        GameEvents.DisasterStarted -= OnDisasterStarted;
        GameEvents.DisasterEnded -= OnDisasterEnded;
        GameEvents.LightningStruck -= OnLightningStruck;
        GameEvents.ChaosChanged -= OnChaosChanged;
        GameEvents.GameStateChanged -= OnGameStateChanged;
        ExpansionEvents.BuildStarted -= OnLotBuildStarted;
        ExpansionEvents.BuildFinished -= OnLotBuildFinished;
        LightningRodEvents.BuildStarted -= OnRodBuildStarted;
        LightningRodEvents.BuildFinished -= OnRodBuildFinished;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    //Este objeto atravessa o reinício junto com o AudioManager: o que era da partida anterior zera aqui
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopMatchSounds();
        AudioManager.SetTension(0f);
        AudioManager.SetDanger(0f);
        AudioManager.SetMusicLevel(1f, musicLevelFadeTime);
        chaos = 0f;
        streak = 0;
        playing = false;
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && IsPointerOverButton())
        {
            AudioManager.Play("ui_click", null, clickVolume);
        }

        //Tudo pelo GameClock: na pausa as ferramentas, os trovões e os grilos esperam
        if (GameClock.Frozen) return;

        UpdateTools();
        UpdateUrgency();
        UpdateStorm();
        UpdateAmbience();
    }

    //Clique em qualquer botão de UI ganha som sem precisar de componente no botão
    private bool IsPointerOverButton()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        if (pointerData == null || pointerEventSystem != eventSystem)
        {
            pointerData = new PointerEventData(eventSystem);
            pointerEventSystem = eventSystem;
        }

        pointerData.position = Mouse.current.position.ReadValue();
        raycastResults.Clear();
        eventSystem.RaycastAll(pointerData, raycastResults);
        if (raycastResults.Count == 0) return false;

        Selectable selectable = raycastResults[0].gameObject.GetComponentInParent<Selectable>();
        return selectable != null && selectable.IsInteractable();
    }

    private static Vector3? At(House house) => house != null ? house.transform.position : (Vector3?)null;
    private static Vector3? At(Call call) => call != null ? At(call.house) : null;

    #region LOOPS BY CLOCK

    //Cada obra com equipe sorteia a próxima ferramenta (martelo, catraca, furadeira) no próprio ritmo
    private void UpdateTools()
    {
        if (working.Count == 0) return;

        float now = GameClock.Time;
        int sounding = 0;
        workingKeys.Clear();
        workingKeys.AddRange(working.Keys);
        foreach (Call call in workingKeys)
        {
            if (call == null || call.house == null)
            {
                working.Remove(call);
                continue;
            }

            if (sounding++ >= maxToolSites || now < working[call]) continue;

            working[call] = now + Random.Range(toolIntervalMin, toolIntervalMax);
            AudioManager.Play("repair_tool", At(call), toolVolume);
        }
    }

    //Com o caos acima do limite de perigo, o aviso de chamado volta cada vez mais rápido e mais agudo (GDD, seção 6)
    private void UpdateUrgency()
    {
        if (!playing) return;

        float now = GameClock.Time;
        if (chaos >= dangerChaos && now >= nextUrgent)
        {
            float panic = Mathf.InverseLerp(dangerChaos, 1f, chaos);
            nextUrgent = now + Mathf.Lerp(dangerSirenInterval, panicInterval, panic);
            AudioManager.Play("call_urgent", null, urgentVolume * Mathf.Lerp(.6f, 1f, panic), SoundMath.SemitonesToPitch(3f * panic));
        }

        foreach (House house in burning)
        {
            if (house == null || urgentFired.Contains(house) || house.FireTimeLeft01 <= 0f || house.FireTimeLeft01 > fireUrgentAt) continue;

            urgentFired.Add(house);
            AudioManager.Play("call_urgent", At(house), urgentVolume);
            break;
        }
    }

    //Tempestade: a chuva forte cresce em stormBuildTime e os trovões ao longe caem de tempos em tempos, de um lado ou do outro
    private void UpdateStorm()
    {
        if (stormStart < 0f) return;

        float now = GameClock.Time;
        float grow = Mathf.Clamp01((now - stormStart) / Mathf.Max(.01f, stormBuildTime));
        AudioManager.StartLoop("rain_heavy_loop", null, rainHeavyVolume * grow, 1f);

        if (now < nextFarThunder) return;

        nextFarThunder = now + Random.Range(farThunderMin, farThunderMax);
        Camera cam = Camera.main;
        Vector3? side = cam != null ? cam.transform.position + Vector3.right * Random.Range(-1.6f, 1.6f) * cam.orthographicSize * cam.aspect : (Vector3?)null;
        AudioManager.Play("thunder_far", side, farThunderVolume);
    }

    //Cidade de dia (trânsito, vozes) cruza com a de noite (grilos) pelo DayNightCycle; grilos soltos de tempos em tempos
    private void UpdateAmbience()
    {
        float now = GameClock.Time;
        if (now < nextAmbience) return;
        nextAmbience = now + .25f;

        float night = DayNightCycle.Instance != null ? DayNightCycle.Instance.Night : 0f;
        float cover = Active(DisasterType.Storm) ? ambienceUnderRain : 1f;
        AudioManager.StartLoop("amb_day", null, dayAmbienceVolume * (1f - night) * cover, 2f);
        AudioManager.StartLoop("amb_night", null, nightAmbienceVolume * night * cover, 2f);

        if (night < cricketNight || now < nextCricket) return;

        nextCricket = now + Random.Range(cricketMin, cricketMax);
        Camera cam = Camera.main;
        Vector3? spot = cam != null ? cam.transform.position + Vector3.right * Random.Range(-.9f, .9f) * cam.orthographicSize * cam.aspect : (Vector3?)null;
        AudioManager.Play("amb_cricket", spot, cricketVolume * night * cover);
    }

    #endregion

    #region CALLS

    private void OnCallOpened(Call call)
    {
        AudioManager.Play("ui_tab", null, tabVolume);
    }

    //Escolher a base: a aba da equipe abre com o estalo de aba e o pop
    private void OnBaseSelected(Call call, House guard)
    {
        AudioManager.Play("ui_tab", null, tabVolume);
        AudioManager.Play("ui_popup", null, baseSelectVolume);
    }

    private void OnTeamDispatched(Call call)
    {
        AudioManager.Play("team_depart", At(call), dispatchVolume);
    }

    private void OnWorkerArrived(Transform worker, Call call)
    {
        AudioManager.Play("team_arrive", worker != null ? worker.position : At(call), arriveVolume);
    }

    private void OnWorkStarted(Call call)
    {
        if (call != null) working[call] = GameClock.Time + .1f;
    }

    private void OnWorkCompleted(Call call)
    {
        if (call != null) working.Remove(call);

        AudioManager.Play("repair_done", At(call), repairDoneVolume);
    }

    private void OnCallResolved(Call call, int moneyEarned)
    {
        if (call != null) working.Remove(call);

        if (moneyEarned <= 0)
        {
            AudioManager.Play("ui_click", null, clickVolume);
            return;
        }

        float semitones = Mathf.Min(Mathf.Max(0, streak - 1), streakMaxSteps) * streakSemitones;
        AudioManager.Play("call_collect", At(call), collectVolume, SoundMath.SemitonesToPitch(semitones));
    }

    private void OnRepairStreak(int count, Vector3 position)
    {
        streak = count;
    }

    private void OnPassiveIncomePaid(int amount)
    {
        if (amount > 0) AudioManager.Play("ui_coin_tick", null, incomeVolume);
    }

    private void OnUpgradePurchased(UpgradeData upgrade, int newLevel)
    {
        AudioManager.Play("ui_buy", null, buyVolume);
    }

    private void OnActionDenied(DeniedReason reason, Vector3 worldPosition)
    {
        AudioManager.Play("ui_denied", null, errorVolume);
    }

    #endregion

    #region DISASTERS

    private void OnHouseStateChanged(House house, State before, State after)
    {
        if (before == after) return;

        Vector3? at = At(house);
        if (after == State.OnFire)
        {
            burning.Add(house);
            AudioManager.Play("fire_ignite", at, igniteVolume);
            AudioManager.Play("call_new_fire", at, newCallVolume);
        }
        else if (after == State.Damaged && before == State.Intact)
        {
            //A tempestade danifica pelo vazamento; com terremoto no ar, o dano é dele
            AudioManager.Play(Active(DisasterType.Earthquake) || !Active(DisasterType.Storm) ? "call_new_quake" : "call_new_storm", at, newCallVolume);
        }
        else if (after == State.PowerOutage)
        {
            AudioManager.Play("call_new_power", at, newCallVolume);
        }

        if (before == State.OnFire)
        {
            burning.Remove(house);
            urgentFired.Remove(house);
            AudioManager.StopLoop("fire_jet_loop", house, .4f);
            if (after == State.Intact) AudioManager.Play("fire_out", at, extinguishVolume);
        }

        burning.RemoveWhere(h => h == null);
        AudioManager.StartLoop("fire_loop", null, burning.Count > 0 ? fireVolume * Mathf.Min(1f, .55f + .15f * burning.Count) : 0f, loopFadeTime);
    }

    //A equipe chegou ao fogo: o jato abre com o estouro de pressão e fica em loop até o fogo acabar
    private void OnFireJetOpened(House house)
    {
        AudioManager.Play("fire_jet_start", At(house), jetVolume);
        AudioManager.StartLoop("fire_jet_loop", house, jetVolume, .15f);
    }

    private void OnFireLevelChanged(House house, int level)
    {
        if (level <= 0) AudioManager.StopLoop("fire_jet_loop", house, .4f);
    }

    private void OnDisasterWarning(DisasterData disaster, float secondsUntilStart)
    {
        string id = disaster == null ? "siren" : disaster.type switch
        {
            DisasterType.Earthquake => "warning_quake",
            DisasterType.Storm => "warning_storm",
            DisasterType.Fire => "warning_fire",
            _ => "siren"
        };
        AudioManager.Play(AudioManager.HasSound(id) ? id : "siren", null, warningVolume);
    }

    private void OnDisasterStarted(DisasterData disaster, Vector3 epicenter)
    {
        if (disaster == null) return;

        active.TryGetValue(disaster.type, out int count);
        active[disaster.type] = count + 1;
        AudioManager.SetMusicActivity(1f);

        switch (disaster.type)
        {
            case DisasterType.Earthquake:
                AudioManager.Play("quake_rumble", epicenter, earthquakeVolume);
                AudioManager.Play("quake_hit", epicenter, quakeHitVolume);
                for (int i = 0; i < aftershockDelays.Length; i++)
                {
                    Vector3 near = epicenter + (Vector3)(Random.insideUnitCircle * 2f);
                    AudioManager.PlayDelayed("quake_hit", aftershockDelays[i], near, quakeHitVolume * (.8f - .15f * i));
                }
                break;
            case DisasterType.Storm:
                stormStart = GameClock.Time;
                nextFarThunder = stormStart + Random.Range(1.5f, 4f);
                AudioManager.StartLoop("rain_light_loop", null, rainLightVolume, 2.5f);
                AudioManager.StartLoop("wind_loop", null, windVolume, 3f);
                break;
        }
    }

    private void OnDisasterEnded(DisasterData disaster)
    {
        if (disaster == null) return;

        active.TryGetValue(disaster.type, out int count);
        active[disaster.type] = Mathf.Max(0, count - 1);

        if (disaster.type == DisasterType.Storm && !Active(DisasterType.Storm)) StopStorm(3f);
        if (!Active(DisasterType.Earthquake) && !Active(DisasterType.Storm) && !Active(DisasterType.Fire)) AudioManager.SetMusicActivity(0f);
    }

    private void StopStorm(float fadeTime)
    {
        stormStart = -1f;
        foreach (string id in StormLoops) AudioManager.StopAllLoops(id, fadeTime);
    }

    //O trovão perto começa no estalo (sem pré-ataque): soa no mesmo quadro do clarão do raio
    private void OnLightningStruck(Vector3 worldPosition)
    {
        AudioManager.Play("thunder_near", worldPosition, thunderVolume);
    }

    private void OnLotBuildStarted(BuildLot lot) => AudioManager.Play("build_start", lot != null ? lot.transform.position : (Vector3?)null, buildVolume);
    private void OnLotBuildFinished(BuildLot lot) => AudioManager.Play("build_done", lot != null ? lot.transform.position : (Vector3?)null, buildVolume);
    private void OnRodBuildStarted(RodBlock block) => AudioManager.Play("build_start", block != null ? block.transform.position : (Vector3?)null, buildVolume);
    private void OnRodBuildFinished(RodBlock block) => AudioManager.Play("build_done", block != null ? block.transform.position : (Vector3?)null, buildVolume);

    #endregion

    #region PROGRESSION

    private void OnChaosChanged(float value)
    {
        chaos = value;
        AudioManager.SetTension(Mathf.InverseLerp(tensionStartChaos, tensionFullChaos, value));
        AudioManager.SetDanger(Mathf.InverseLerp(dangerLayerChaos, 1f, value));
    }

    private void OnGameStateChanged(GameState state)
    {
        playing = state == GameState.Playing;

        switch (state)
        {
            case GameState.Menu:
                cameFromMenu = true;
                AudioManager.SetTension(0f);
                AudioManager.SetDanger(0f);
                AudioManager.SetMusicActivity(0f);
                AudioManager.SetMusicLevel(menuMusicLevel, musicLevelFadeTime);
                break;
            case GameState.Playing:
                AudioManager.SetMusicLevel(1f, musicLevelFadeTime);
                //Só quando o jogador clicou em Jogar, não a cada volta da pausa
                if (cameFromMenu) AudioManager.Play("team_depart", null, startVolume);
                cameFromMenu = false;
                break;
            case GameState.Paused:
                AudioManager.SetMusicLevel(pausedMusicLevel, musicLevelFadeTime);
                break;
            case GameState.Won:
                EndMatch();
                AudioManager.SetTension(0f);
                AudioManager.SetDanger(0f);
                AudioManager.Play("jingle_win", null, endVolume);
                break;
            case GameState.Lost:
                EndMatch();
                AudioManager.Play("jingle_lose", null, endVolume);
                break;
        }
    }

    //Fim de partida: os loops param e a música abaixa para a vinheta de vitória ou derrota aparecer
    private void EndMatch()
    {
        StopMatchSounds();
        AudioManager.SetMusicLevel(endMusicLevel, musicLevelFadeTime);
    }

    private void StopMatchSounds()
    {
        working.Clear();
        burning.Clear();
        urgentFired.Clear();
        active.Clear();
        StopStorm(loopFadeTime);
        AudioManager.StopAllLoops("fire_loop", loopFadeTime);
        AudioManager.StopAllLoops("fire_jet_loop", loopFadeTime);
        AudioManager.SetMusicActivity(0f);
        AudioManager.ClearDelayed();
    }

    #endregion
}
