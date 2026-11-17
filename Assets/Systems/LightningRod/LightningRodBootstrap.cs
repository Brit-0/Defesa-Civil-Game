using UnityEngine;
using UnityEngine.SceneManagement;

// Liga o para-raio sem mexer no Gameplay.unity (como o ExpansionBootstrap): em toda cena com GameManager que ainda não
// tem uma LightningRodManager, cria o objeto "[Para-raio]" com ela e com a LightningRodFeel.
public static class LightningRodBootstrap
{
    public const string ObjectName = "[Para-raio]";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterFirstScene() => Ensure();

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    public static LightningRodManager Ensure()
    {
        LightningRodManager manager = Object.FindAnyObjectByType<LightningRodManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            if (Object.FindAnyObjectByType<GameManager>() == null) return null;
            manager = new GameObject(ObjectName).AddComponent<LightningRodManager>();
        }
        if (manager.GetComponent<LightningRodFeel>() == null) manager.gameObject.AddComponent<LightningRodFeel>();
        return manager;
    }
}
