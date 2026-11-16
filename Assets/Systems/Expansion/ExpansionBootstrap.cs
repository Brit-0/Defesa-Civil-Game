using UnityEngine;
using UnityEngine.SceneManagement;

// Liga a expansão sem mexer no Gameplay.unity: em toda cena com GameManager que ainda não tem uma ExpansionManager,
// cria o objeto "[Expansão]" com ela e com a ExpansionFeel. Se o MEGABRAIN puser a ExpansionManager na cena, esta
// ligação só acrescenta a ExpansionFeel (se faltar).
public static class ExpansionBootstrap
{
    public const string ObjectName = "[Expansão]";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterFirstScene() => Ensure();

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    public static ExpansionManager Ensure()
    {
        ExpansionManager manager = Object.FindAnyObjectByType<ExpansionManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            if (Object.FindAnyObjectByType<GameManager>() == null) return null;
            manager = new GameObject(ObjectName).AddComponent<ExpansionManager>();
        }
        if (manager.GetComponent<ExpansionFeel>() == null) manager.gameObject.AddComponent<ExpansionFeel>();
        return manager;
    }
}
