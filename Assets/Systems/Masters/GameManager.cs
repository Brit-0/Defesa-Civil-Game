using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("STATE")]
    [SerializeField] private bool autoStart = true;
    public GameState State { get; private set; } = GameState.Menu;
    public float ElapsedTime { get; private set; }
    public int HousesRepaired { get; private set; }
    public int TotalMoneyEarned { get; private set; }

    [Header("DEBUG")]
    public float debugTimeScale = 4f;
    private float playTimeScale = 1f;

    private static int inputBlocks;
    // Reiniciar pela tela final ou pela pausa volta direto para a partida, sem passar pelo menu de novo.
    private static bool restartRequested;
    // O mundo só aceita clique em Playing. Sem GameManager na cena, nada é bloqueado.
    public static bool InputBlocked => inputBlocks > 0 || (Instance != null && Instance.State != GameState.Playing);

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.CallResolved += OnCallResolved;
        GameEvents.MoneyChanged += OnMoneyChanged;
    }

    private void OnDisable()
    {
        GameEvents.CallResolved -= OnCallResolved;
        GameEvents.MoneyChanged -= OnMoneyChanged;
    }

    private void Start()
    {
        // autoStart: a partida começa sozinha (sem o menu do Agente C). Depois de um Restart também.
        bool startNow = autoStart || restartRequested;
        restartRequested = false;
        if (startNow) StartGame();
        else GameEvents.RaiseGameStateChanged(State);
    }

    private void Update()
    {
        if (State == GameState.Playing) ElapsedTime += Time.deltaTime;
    }

    #region FLOW

    public void StartGame()
    {
        if (State != GameState.Menu) return;

        Time.timeScale = playTimeScale;
        SetState(GameState.Playing);
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;

        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;

        Time.timeScale = playTimeScale;
        SetState(GameState.Playing);
    }

    public void Win()
    {
        if (State != GameState.Playing) return;

        Time.timeScale = 1f;
        SetState(GameState.Won);
    }

    public void Lose()
    {
        if (State != GameState.Playing) return;

        Time.timeScale = 1f;
        SetState(GameState.Lost);
    }

    public void Restart()
    {
        DOTween.KillAll();
        Time.timeScale = 1f;
        inputBlocks = 0;
        restartRequested = true;
        GuardManager.selectBaseMode = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void SetState(GameState newState)
    {
        if (State == newState) return;

        State = newState;
        GameEvents.RaiseGameStateChanged(newState);
    }

    #endregion

    #region COUNTERS

    private void OnCallResolved(Call call, int moneyEarned)
    {
        if (call != null && call.type == CallType.Problem) HousesRepaired++;
    }

    private void OnMoneyChanged(int before, int after)
    {
        if (after > before) TotalMoneyEarned += after - before;
    }

    #endregion

    #region INPUT

    public static void BlockInput()
    {
        inputBlocks++;
    }

    public static void UnblockInput()
    {
        if (inputBlocks > 0) inputBlocks--;
    }

    #endregion

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        inputBlocks = 0;
        restartRequested = false;
    }

#if UNITY_EDITOR
    [Button("Alternar tempo acelerado")]
    private void ToggleFastTime()
    {
        playTimeScale = Mathf.Approximately(playTimeScale, 1f) ? debugTimeScale : 1f;
        if (State == GameState.Playing) Time.timeScale = playTimeScale;
    }
#endif
}
