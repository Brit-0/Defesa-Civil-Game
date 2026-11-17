using System;
using System.Collections.Generic;
using UnityEngine;

// Tutorial da primeira partida (GAMELOOP_REVISAO): uns 2 minutos de passos curtos com seta e destaque.
// Segura os desastres enquanto ensina, pode ser pulado em qualquer passo e grava em PlayerPrefs que já foi visto.
// Depois dele, dicas de contexto aparecem uma vez cada (mutirão no incêndio, terremoto forte).
// Nasce sozinho pelo GameloopBootstrap e desliga o TutorialTips da cena, que ele substitui.
public class Tutorial : MonoBehaviour
{
    public const string DoneKey = "DefesaCivil.TutorialDone";
    public const string FireTipKey = "DefesaCivil.TipFire";
    public const string StrongTipKey = "DefesaCivil.TipStrong";

    [Header("TEXTS (aceitam tags do Text Animator)")]
    [SerializeField] private string cardTitle = "DEFESA CIVIL";
    [SerializeField] private string cardBody = "Objetivo: conserte as casas que os desastres quebram e junte dinheiro para comprar o Centro de Operações, antes que o CAOS encha.\n<size=75%><color=#F2C14E>Vamos fazer o primeiro conserto juntos (1 minuto).</color></size>";
    [Tooltip("Tempo do jogo no passo do mini game: o conserto leva 2,5 s e o iniciante precisa ler e mirar")]
    [SerializeField, Range(.1f, 1f)] private float miniGameSlowMotion = .35f;
    [SerializeField] private string fireTip = "JATO: quando a equipe chegar no incêndio, <bounce>arraste o mouse apertado</bounce> por cima das chamas antes do tempo acabar. Sobrou chama? O fogo cresce!";
    [SerializeField] private string strongTip = "<shake>TERREMOTO FORTE!</shake> O aviso é mais longo: escore as casas marcadas e prepare equipes.";
    [SerializeField] private float tipTime = 6f;

    [Header("SOUND")]
    [SerializeField, Range(0f, 1f)] private float stepVolume = .6f;
    [SerializeField] private float stepPitchRise = .03f;

    private class Step
    {
        public string text;
        public Func<bool> done;
        public Func<RectTransform> hud;
        public Func<Transform> world;
        public string ok;
        public bool letDisastersRun;
        public bool slowMotion; // câmera lenta enquanto o passo está na tela (miniGameSlowMotion)
        public Func<bool> skipIf;
        public Func<bool> undo; // o jogador desistiu do chamado: volta ao passo de abrir
    }

    private const int OpenCallStep = 1;

    private struct Counts { public int damaged, opened, dispatched, started, completed, resolved, bought; }

    private TutorialView view;
    private readonly List<Step> steps = new();
    private int index = -1;
    private bool running;
    private bool holding;
    private bool blocking;
    private bool okPressed;
    private bool slowed;
    private float tipHideTime = -1f;
    private Counts now, atStart;
    private House focus;
    private AudioSource stepSource;
    private AudioClip stepClip;

    private static Transform Hud => FeelUI.HudCanvas != null ? FeelUI.HudCanvas.transform : null;

    private void Awake()
    {
        foreach (TutorialTips tips in FindObjectsByType<TutorialTips>(FindObjectsInactive.Include)) tips.enabled = false;

        view = gameObject.AddComponent<TutorialView>();
        view.Build();
        view.SkipPressed += Finish;

        stepSource = gameObject.AddComponent<AudioSource>();
        stepSource.playOnAwake = false;
        stepClip = Resources.Load<AudioClip>("MiniGames/MG_TutorialStep");

        BuildSteps();
    }

    private void OnEnable()
    {
        GameEvents.GameStateChanged += OnGameStateChanged;
        GameEvents.HouseStateChanged += OnHouseStateChanged;
        GameEvents.CallOpened += OnCallOpened;
        GameEvents.TeamDispatched += OnTeamDispatched;
        GameEvents.WorkStarted += OnWorkStarted;
        GameEvents.WorkCompleted += OnWorkCompleted;
        GameEvents.CallResolved += OnCallResolved;
        GameEvents.UpgradePurchased += OnUpgradePurchased;
        GameEvents.DisasterForecasted += OnDisasterForecasted;
    }

    private void OnDisable()
    {
        GameEvents.GameStateChanged -= OnGameStateChanged;
        GameEvents.HouseStateChanged -= OnHouseStateChanged;
        GameEvents.CallOpened -= OnCallOpened;
        GameEvents.TeamDispatched -= OnTeamDispatched;
        GameEvents.WorkStarted -= OnWorkStarted;
        GameEvents.WorkCompleted -= OnWorkCompleted;
        GameEvents.CallResolved -= OnCallResolved;
        GameEvents.UpgradePurchased -= OnUpgradePurchased;
        GameEvents.DisasterForecasted -= OnDisasterForecasted;
        SetHold(false);
        SetBlock(false);
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing) Begin();
    }

    #region FLOW

    private void Begin()
    {
        if (running || index >= 0 || PlayerPrefs.GetInt(DoneKey, 0) == 1) return;

        running = true;
        SetHold(true);
        SetBlock(true);
        view.ShowCard(cardTitle, cardBody, "COMEÇAR", "PULAR", () => { view.HideCard(); SetBlock(false); Go(0); }, Finish);
    }

    private void Go(int next)
    {
        index = next;
        while (index < steps.Count && steps[index].skipIf != null && steps[index].skipIf()) index++;
        if (index >= steps.Count) { Finish(); return; }

        Step step = steps[index];
        atStart = now;
        okPressed = false;
        SetHold(!step.letDisastersRun);
        SetSlowMotion(step.slowMotion);
        view.ShowStep($"PASSO {index + 1}/{steps.Count}", step.text, step.ok, () => okPressed = true, true);
        PlayStep(index);
    }

    private void Finish()
    {
        if (!running) return;

        running = false;
        index = steps.Count;
        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();
        view.HideCard();
        view.HideBubble();
        SetHold(false);
        SetBlock(false);
        SetSlowMotion(false);
    }

    // Só mexe no tempo com a partida rodando (a pausa e as telas de fim cuidam do próprio timeScale).
    private void SetSlowMotion(bool slow)
    {
        if (slow == slowed) return;

        slowed = slow;
        if (GameManager.Instance == null) return;
        float scale = slow ? miniGameSlowMotion : 1f;
        // Na pausa (Esc, #535) o passo pode avançar: a volta da pausa devolve o ritmo novo
        GameManager.Instance.SetResumeTimeScale(scale);
        if (GameManager.Instance.State != GameState.Playing) return;
        Time.timeScale = scale;
    }

    private void Update()
    {
        if (tipHideTime > 0f && Time.unscaledTime >= tipHideTime)
        {
            tipHideTime = -1f;
            // o tutorial de energia pode ter tomado o balão no meio da dica: escondê-lo travava o passo dele
            if (!EnergyTutorialRunning()) view.HideBubble();
        }

        if (!running || index < 0 || index >= steps.Count) return;

        Step step = steps[index];
        view.SetTarget(step.hud?.Invoke(), step.world?.Invoke());

        if (okPressed || (step.done != null && step.done())) Go(index + 1);
        else if (step.undo != null && step.undo()) Go(OpenCallStep);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Playing) Begin();
        view.SetVisible(state == GameState.Playing);
        if (state == GameState.Won || state == GameState.Lost) Finish();
    }

    private void SetHold(bool hold)
    {
        if (hold == holding || DisasterManager.Instance == null) return;

        holding = hold;
        if (hold) DisasterManager.Instance.Hold();
        else DisasterManager.Instance.Release();
    }

    private void SetBlock(bool block)
    {
        if (block == blocking) return;

        blocking = block;
        if (block) GameManager.BlockInput();
        else GameManager.UnblockInput();
    }

    private void PlayStep(int number)
    {
        if (stepClip == null) return;

        stepSource.pitch = 1f + number * stepPitchRise;
        stepSource.PlayOneShot(stepClip, stepVolume);
    }

    #endregion

    #region STEPS

    private void BuildSteps()
    {
        steps.Add(new Step
        {
            text = "Um <bounce>terremoto</bounce> vem aí! O aviso no topo diz quando, e os aros laranja marcam as casas na mira.",
            letDisastersRun = true,
            done = () => now.damaged > atStart.damaged,
        });
        steps.Add(new Step
        {
            text = "Casa quebrada! <bounce>Clique nela</bounce> para abrir um chamado.",
            world = () => FocusHouse() is House house ? house.transform : null,
            done = () => now.opened > atStart.opened || now.dispatched > atStart.dispatched,
        });
        steps.Add(new Step
        {
            text = "Agora clique na <bounce>base da Guarda</bounce> que vai mandar a equipe.",
            world = () => NearestGuard(),
            done = () => TabOpen() || now.dispatched > atStart.dispatched,
            skipIf = () => !GuardManager.selectBaseMode,
            undo = () => CallDropped(),
        });
        steps.Add(new Step
        {
            text = "Monte a equipe no <bounce>+</bounce> (1 trabalhador por ponto de dano; o topo da aba mostra os tempos) e clique em <bounce>ENVIAR</bounce>.",
            // A aba abre vazia (#531): a seta mostra o + até ter gente na equipe, depois o Enviar.
            hud = () => TeamEmpty() ? HudChild("PlusButton") : HudChild("SendButton"),
            done = () => now.dispatched > atStart.dispatched,
            skipIf = () => now.dispatched > 0,
            undo = () => CallDropped(),
        });
        steps.Add(new Step
        {
            text = "Cada trabalhador leva <bounce>1 material</bounce> do estoque. Fique de olho neste número.",
            hud = () => HudChild("BuildMaterial"),
            done = () => now.started > atStart.started,
        });
        steps.Add(new Step
        {
            text = "MINI GAME: clique na casa quando o círculo passar na <bounce>faixa dourada</bounce>. Acertou: termina na hora e paga mais!",
            world = () => focus != null ? focus.transform : null,
            done = () => now.completed > atStart.completed,
            slowMotion = true,
        });
        steps.Add(new Step
        {
            text = "Pronto! <bounce>Clique na casa</bounce> para receber o pagamento.",
            world = () => focus != null ? focus.transform : null,
            done = () => now.resolved > atStart.resolved,
            skipIf = () => now.resolved > 0,
        });
        steps.Add(new Step
        {
            text = "Conserte a outra casa! Fechar <bounce>todas as casas</bounce> de um desastre dá bônus, e cada marco de consertos traz um voluntário.",
            world = () => OtherTroubledHouse(),
            done = () => now.resolved > atStart.resolved || (OtherTroubledHouse() == null && !AnyTeamOut()),
        });
        steps.Add(new Step
        {
            text = "Dinheiro compra <bounce>melhorias</bounce>. Abra o painel aqui.",
            hud = () => HudChild("OpenUpgrades"),
            done = () => UpgradesOpen() || now.bought > atStart.bought,
        });
        steps.Add(new Step
        {
            text = "Contrate mais um <bounce>trabalhador</bounce>: mais equipes na rua ao mesmo tempo.",
            hud = () => UpgradeRow(UpgradeIds.Workers),
            done = () => now.bought > atStart.bought,
            ok = "DEPOIS",
        });
        steps.Add(new Step
        {
            text = "Sua <bounce>meta</bounce>: comprar o Centro de Operações. Esta barra mostra quanto falta.",
            hud = () => GoalTracker.Instance != null ? GoalTracker.Instance.Frame : null,
            ok = "ENTENDI",
        });
        steps.Add(new Step
        {
            text = "Material acabando? Clique no <bounce>Armazém</bounce> e mande uma equipe buscar mais.",
            world = () => NearestWarehouse(),
            ok = "ENTENDI",
        });
        steps.Add(new Step
        {
            text = "Casas quebradas e incêndios enchem o <bounce>CAOS</bounce>. Se encher, a cidade cai. Agora é com você!",
            hud = () => HudChild("ChaosBar", "Bar"),
            ok = "BORA!",
        });
    }

    private House FocusHouse()
    {
        if (focus != null && focus.currentState != State.Intact && !focus.HasTeam) return focus;

        focus = ClosestTroubled(null);
        return focus;
    }

    private Transform OtherTroubledHouse()
    {
        House other = ClosestTroubled(focus);
        return other != null ? other.transform : null;
    }

    // Casa com problema e sem equipe mais perto do meio da tela.
    private static House ClosestTroubled(House except)
    {
        if (LocationsManager.Instance == null) return null;

        House best = null;
        float bestDistance = float.MaxValue;
        foreach (House house in LocationsManager.Instance.GetTroubledHouses())
        {
            if (house == except || house.HasTeam) continue;

            float distance = FeelUI.WorldToHud(house.transform.position).sqrMagnitude;
            if (distance >= bestDistance) continue;

            best = house;
            bestDistance = distance;
        }

        return best;
    }

    private static bool AnyTeamOut()
    {
        if (LocationsManager.Instance == null) return false;

        foreach (House house in LocationsManager.Instance.GetTroubledHouses())
        {
            if (house.HasTeam) return true;
        }

        return false;
    }

    private Transform NearestGuard()
    {
        House house = focus != null ? focus : FocusHouse();
        if (house == null || LocationsManager.Instance == null) return null;

        CityGuard guard = LocationsManager.Instance.GetNearestGuard(house.transform.position);
        return guard != null ? guard.transform : null;
    }

    private Transform NearestWarehouse()
    {
        if (LocationsManager.Instance == null) return null;

        Warehouse best = null;
        float bestDistance = float.MaxValue;
        foreach (Warehouse warehouse in LocationsManager.Instance.Warehouses)
        {
            if (warehouse == null) continue;

            float distance = FeelUI.WorldToHud(warehouse.transform.position).sqrMagnitude;
            if (distance >= bestDistance) continue;

            best = warehouse;
            bestDistance = distance;
        }

        return best != null ? best.transform : null;
    }

    private static RectTransform HudChild(params string[] path)
    {
        return Hud != null ? FeelUI.FindChild(Hud, path) as RectTransform : null;
    }

    private bool CallDropped()
    {
        return !GuardManager.selectBaseMode && !TabOpen() && now.dispatched == atStart.dispatched;
    }

    private static bool TabOpen()
    {
        RectTransform tab = HudChild("GuardTab");
        return tab != null && tab.gameObject.activeInHierarchy;
    }

    private static bool TeamEmpty()
    {
        GuardManager guards = GuardManager.Instance;
        Call call = guards != null ? guards.CurrentCall : null;
        return call != null && guards.TeamSize < Mathf.Max(1, call.workersNeeded);
    }

    private static bool UpgradesOpen()
    {
        RectTransform tab = HudChild("UpgradeTab");
        return tab != null && tab.gameObject.activeInHierarchy;
    }

    private static RectTransform UpgradeRow(string id)
    {
        foreach (UpgradeButton button in FindObjectsByType<UpgradeButton>(FindObjectsInactive.Exclude))
        {
            if (button.Data != null && button.Data.id == id) return button.transform as RectTransform;
        }

        return null;
    }

    #endregion

    #region EVENTS

    private void OnHouseStateChanged(House house, State before, State after)
    {
        if (before == State.Intact && after != State.Intact) now.damaged++;
    }

    private void OnCallOpened(Call call)
    {
        now.opened++;
        if (running && call != null && call.type == CallType.Problem) focus = call.house;
    }

    private void OnTeamDispatched(Call call) => now.dispatched++;
    private void OnWorkStarted(Call call)
    {
        now.started++;
        if (!running && call != null && call.house != null && call.house.currentState == State.OnFire)
            ShowTip(FireTipKey, string.Format(fireTip, Stats.Balance.fireHeroClicks));
    }

    private void OnWorkCompleted(Call call) => now.completed++;
    private void OnCallResolved(Call call, int money) => now.resolved++;
    private void OnUpgradePurchased(UpgradeData upgrade, int level) => now.bought++;

    private void OnDisasterForecasted(DisasterForecast forecast)
    {
        if (!running && forecast.isStrong) ShowTip(StrongTipKey, strongTip);
    }

    // Dica de contexto: uma vez por jogador, só fora do tutorial, some sozinha.
    private void ShowTip(string key, string text)
    {
        // o tutorial de energia usa o mesmo balão; a dica fica para a próxima vez
        if (PlayerPrefs.GetInt(key, 0) == 1 || EnergyTutorialRunning()) return;

        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        view.SetTarget(null, null);
        view.ShowStep("DICA", text, null, null, false);
        PlayStep(0);
        tipHideTime = Time.unscaledTime + tipTime;
    }

    private static bool EnergyTutorialRunning()
    {
        EnergyTutorial energy = FindAnyObjectByType<EnergyTutorial>();
        return energy != null && energy.Running;
    }

    #endregion
}
