using UnityEngine;

// Dicas curtas da enchente e do meteoro na primeira vez (docs/jam/ENCHENTE_METEORO.md): o mesmo balão do tutorial,
// com a seta no chamado do quarteirão. Uma por jogador (PlayerPrefs), somem sozinhas ou no ENTENDI; a partida segue.
// Com o tutorial (o primeiro ou o de energia) na tela a dica espera a próxima vez.
public class BlockDisasterTips : MonoBehaviour
{
    public const string FloodKey = "DefesaCivil.FloodTipDone";
    public const string ValveKey = "DefesaCivil.ValveTipDone";
    public const string MeteorKey = "DefesaCivil.MeteorTipDone";
    public const string RebuildKey = "DefesaCivil.RebuildTipDone";
    public static readonly string[] Keys = { FloodKey, ValveKey, MeteorKey, RebuildKey };

    [Header("TEXTOS")]
    [SerializeField, TextArea] private string floodTip = "ENCHENTE! O quarteirão alagou e o fogo de lá apagou. Mande uma equipe ao BUEIRO: um chamado só resolve o quarteirão todo.";
    [SerializeField, TextArea] private string valveTip = "DESENTUPA O BUEIRO: segure o clique perto da válvula e gire o mouse em círculos. A pressão sobe e a água desce. Soltar no meio faz a água voltar!";
    [SerializeField, TextArea] private string meteorTip = "METEORO! A sombra mostra onde ele vai cair: o quarteirão inteiro vira cratera. Depois do impacto, a reconstrução pede {0} trabalhadores.";
    [SerializeField, TextArea] private string rebuildTip = "RECONSTRUÇÃO: clique quando o círculo passar no arco dourado. Acertos seguidos aceleram a obra; errar tira o embalo.";
    [SerializeField] private string header = "DICA";
    [SerializeField] private string okLabel = "ENTENDI";
    [SerializeField] private float tipTime = 9f;

    private TutorialView view;
    private float hideTime = -1f;

    private void OnEnable()
    {
        GameEvents.BlockIncidentStarted += OnStarted;
        GameEvents.ValveOpened += OnValveOpened;
        GameEvents.MeteorIncoming += OnMeteorIncoming;
        GameEvents.WorkStarted += OnWorkStarted;
    }

    private void OnDisable()
    {
        GameEvents.BlockIncidentStarted -= OnStarted;
        GameEvents.ValveOpened -= OnValveOpened;
        GameEvents.MeteorIncoming -= OnMeteorIncoming;
        GameEvents.WorkStarted -= OnWorkStarted;
    }

    private void OnStarted(BlockIncident incident)
    {
        if (incident != null && incident.kind == IncidentKind.Flood) Show(FloodKey, floodTip, incident.leader != null ? incident.leader.transform : null);
    }

    // A seta aponta para a válvula (o ValveMinigame a cria antes, no mesmo evento); sem ela, para o bueiro.
    private void OnValveOpened(BlockIncident incident)
    {
        if (incident == null) return;
        Transform valve = ValveMinigame.Of(incident);
        Show(ValveKey, valveTip, valve != null ? valve : incident.leader != null ? incident.leader.transform : null);
    }

    private void OnMeteorIncoming(BlockIncident incident, float seconds)
    {
        Show(MeteorKey, string.Format(meteorTip, BlockDisasterData.Instance.meteorWorkers), null);
    }

    private void OnWorkStarted(Call call)
    {
        if (call != null && call.rule is BlockIncident incident && incident.kind == IncidentKind.Meteor) Show(RebuildKey, rebuildTip, call.house != null ? call.house.transform : null);
    }

    private void Show(string key, string text, Transform target)
    {
        if (PlayerPrefs.GetInt(key, 0) == 1 || TutorialBusy()) return;

        if (view == null) view = FindAnyObjectByType<TutorialView>();
        if (view == null)
        {
            view = gameObject.AddComponent<TutorialView>();
            view.Build();
        }

        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
        view.ShowStep(header, text, okLabel, Hide, false);
        view.SetTarget(null, target);
        hideTime = GameClock.Time + tipTime;
    }

    private void Update()
    {
        if (hideTime > 0f && GameClock.Time >= hideTime) Hide();
    }

    private void Hide()
    {
        hideTime = -1f;
        if (view != null) view.HideBubble();
    }

    // O primeiro tutorial ainda não acabou ou o de energia está com o balão.
    private static bool TutorialBusy()
    {
        EnergyTutorial energy = FindAnyObjectByType<EnergyTutorial>();
        if (energy != null && energy.Running) return true;
        return FindAnyObjectByType<Tutorial>() != null && PlayerPrefs.GetInt(Tutorial.DoneKey, 0) == 0;
    }
}
