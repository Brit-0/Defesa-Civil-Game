using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Liga o UpgradePanel na UpgradeTab do HUD em toda cena (o Reiniciar recarrega a cena), sem editar o Gameplay.unity.
//O botão de abrir e o X da cena ligam a aba por SetActive: passam a abrir e fechar com animação, e o de abrir
//vira liga e desliga. O Tab e a volta da aba depois de posicionar uma obra ficam no UpgradePanelShortcuts (v6A).
public static class UpgradePanelBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        UpgradePanel.Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterFirstScene() => Ensure();

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    //Pode rodar mais de uma vez
    public static UpgradePanel Ensure()
    {
        Canvas hud = FeelUI.HudCanvas;
        Transform tab = hud != null ? FeelUI.FindChild(hud.transform, UpgradePanel.TabObjectName) : null;
        if (tab == null) return null;

        UpgradePanel panel = tab.GetComponent<UpgradePanel>();
        if (panel != null) return panel;

        //A aba começa fechada: o Awake (e a montagem) só roda na primeira vez que ela abre
        panel = tab.gameObject.AddComponent<UpgradePanel>();
        UpgradePanel.Instance = panel;
        Button open = FindButton(hud.transform, "OpenUpgrades");
        Reroute(open, tab.gameObject, panel.Toggle);
        Reroute(FindButton(tab, "CloseBtn"), tab.gameObject, panel.Close);

        UpgradePanelShortcuts shortcuts = hud.GetComponent<UpgradePanelShortcuts>();
        if (shortcuts == null) shortcuts = hud.gameObject.AddComponent<UpgradePanelShortcuts>();
        shortcuts.Init(panel, open);
        return panel;
    }

    private static void Reroute(Button button, GameObject tab, UnityAction action)
    {
        if (button == null) return;

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == tab) button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }

        button.onClick.AddListener(action);
    }

    private static Button FindButton(Transform root, string childName)
    {
        Transform child = FeelUI.FindChild(root, childName);
        return child != null ? child.GetComponent<Button>() : null;
    }
}
