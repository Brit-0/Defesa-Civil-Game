using UnityEngine;
using UnityEngine.SceneManagement;

// Componente de ligação do fogo e da energia (docs/jam/ENERGIA_FOGO.md). O prefab FireEnergy (Resources/FireEnergy)
// já traz EnergyManager, EnergyHud, EnergyMarkers, FireMinigame e EnergyTutorial; o quadro de força (FuseBoxMinigame)
// entra pelo Ensure. Solto na cena, ele vale e nada é duplicado.
// Sem ele na cena, o bootstrap cria o prefab sozinho em toda cena com GameManager, como o GameloopBootstrap.
public class FireEnergyLink : MonoBehaviour
{
    public const string PrefabPath = "FireEnergy/FireEnergy";

    private void Awake()
    {
        Ensure<EnergyManager>();
        Ensure<EnergyHud>();
        Ensure<EnergyMarkers>();
        Ensure<FireMinigame>();
        Ensure<EnergyTutorial>();
        Ensure<FuseBoxMinigame>();
    }

    private void Ensure<T>() where T : Component
    {
        if (FindAnyObjectByType<T>(FindObjectsInactive.Include) != null) return;

        gameObject.AddComponent<T>();
    }

    #region BOOTSTRAP

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
        if (FindAnyObjectByType<GameManager>() == null) return;
        if (FindAnyObjectByType<FireEnergyLink>(FindObjectsInactive.Include) != null) return;

        GameObject prefab = Resources.Load<GameObject>(PrefabPath);
        GameObject link = prefab != null ? Instantiate(prefab) : new GameObject("FireEnergy", typeof(FireEnergyLink));
        link.name = "FireEnergy";
    }

    #endregion
}
