using UnityEngine;
using UnityEngine.SceneManagement;

// Liga por código o que a revisão do gameloop trouxe (GAMELOOP_REVISAO), sem editar a Gameplay.unity:
// em toda cena com GameManager cria MiniGameFeel, GoalTracker e Tutorial, se a cena ainda não tiver.
// Se o MEGABRAIN puser os componentes na cena, os da cena valem e nada é duplicado.
public static class GameloopBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Spawn();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Spawn();

    private static void Spawn()
    {
        if (Object.FindAnyObjectByType<GameManager>() == null) return;

        Ensure<MiniGameFeel>();
        Ensure<GoalTracker>();
        Ensure<Tutorial>();
    }

    private static void Ensure<T>() where T : Component
    {
        if (Object.FindAnyObjectByType<T>(FindObjectsInactive.Include) != null) return;

        new GameObject(typeof(T).Name).AddComponent<T>();
    }
}
