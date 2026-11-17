using UnityEngine;
using UnityEngine.SceneManagement;

// Liga a enchente e o meteoro sem mexer no Gameplay.unity (como o LightningRodBootstrap): em toda cena com GameManager
// que ainda não tem uma BlockDisasterManager, cria o objeto "[Enchente e meteoro]" com a manager, a água, a válvula,
// o meteoro, a barra da obra, os sons e as dicas.
public static class BlockDisasterBootstrap
{
    public const string ObjectName = "[Enchente e meteoro]";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterFirstScene() => Ensure();

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    public static BlockDisasterManager Ensure()
    {
        BlockDisasterManager manager = Object.FindAnyObjectByType<BlockDisasterManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            if (Object.FindAnyObjectByType<GameManager>() == null) return null;
            manager = new GameObject(ObjectName).AddComponent<BlockDisasterManager>();
        }
        Add<FloodFx>(manager);
        Add<ValveMinigame>(manager);
        Add<MeteorFx>(manager);
        Add<RebuildBar>(manager);
        Add<BlockDisasterAudio>(manager);
        Add<BlockDisasterTips>(manager);
        return manager;
    }

    private static void Add<T>(Component owner) where T : Component
    {
        if (owner.GetComponent<T>() == null) owner.gameObject.AddComponent<T>();
    }
}
