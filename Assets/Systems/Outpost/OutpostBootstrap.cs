using UnityEngine;
using UnityEngine.SceneManagement;

// Liga a guarita sem mexer no Gameplay.unity (como o LightningRodBootstrap): em toda cena com GameManager que ainda não
// tem uma OutpostManager, cria o objeto "[Guaritas]" com ela e com a OutpostFeel.
public static class OutpostBootstrap
{
    public const string ObjectName = "[Guaritas]";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterFirstScene() => Ensure();

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    public static OutpostManager Ensure()
    {
        OutpostManager manager = Object.FindAnyObjectByType<OutpostManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            if (Object.FindAnyObjectByType<GameManager>() == null) return null;
            manager = new GameObject(ObjectName).AddComponent<OutpostManager>();
        }
        if (manager.GetComponent<OutpostFeel>() == null) manager.gameObject.AddComponent<OutpostFeel>();
        return manager;
    }
}
