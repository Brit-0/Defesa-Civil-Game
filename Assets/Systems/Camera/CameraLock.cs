using UnityEngine;
using UnityEngine.SceneManagement;

//Câmera parada na tela inicial e na tela de fim (lista Para Arrumar, item 2): sem arrastar e sem zoom.
//Desliga o CameraController fora da partida, sem mexer nele (regra 7 do CLAUDE.md). Só escuta GameStateChanged.
//Liga sozinho em toda cena com CameraController, sem mexer no Gameplay.unity.
public class CameraLock : MonoBehaviour
{
    private CameraController controller;
    private bool started;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterFirstScene() => Ensure();

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    private static void Ensure()
    {
        CameraController camera = FindAnyObjectByType<CameraController>();
        if (camera != null && camera.GetComponent<CameraLock>() == null) camera.gameObject.AddComponent<CameraLock>();
    }

    //A câmera trava nas telas por cima da cidade: menu inicial, vitória e derrota
    public static bool LockedIn(GameState state)
    {
        return state == GameState.Menu || state == GameState.Won || state == GameState.Lost;
    }

    private void Awake()
    {
        controller = GetComponent<CameraController>();
    }

    private void OnEnable()
    {
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    //Espera um quadro: o Start do CameraController precisa rodar antes (ele pega a câmera e põe o zoom inicial)
    private System.Collections.IEnumerator Start()
    {
        yield return null;

        started = true;
        if (GameManager.Instance != null) OnGameStateChanged(GameManager.Instance.State);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (started && controller != null) controller.enabled = !LockedIn(state);
    }
}
