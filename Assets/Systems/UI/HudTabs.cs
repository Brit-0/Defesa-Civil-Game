using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Abas do HUD sempre por cima dos efeitos do mapa (v7B-2): seta e aro do tutorial numa casa, pinos, números e
// partículas que as camadas do HUD criam depois das abas. Cada aba ganha um Canvas próprio acima do HUD (0) e da
// camada de feedback (100), abaixo da engrenagem (14000), da pausa (15000), das configurações (15100) e do fim (20000).
public static class HudTabs
{
    public const int TabOrder = 12000;       // aba de melhorias, painel da equipe, meta, energia
    public const int TutorialOrder = 12010;  // balão e cartão do tutorial: por cima das outras abas, como antes
    public const int PointerOrder = 12020;   // seta e aro do tutorial num botão do HUD (dentro de uma aba)
    public const int MapEffectOrder = 200;   // seta e aro do tutorial numa casa: efeito do mapa, abaixo das abas
    public const int FeedbackOrder = 12030;  // resposta ao clique na aba: confete da compra, "Sem dinheiro!"

    // As duas abas que já vêm na cena (UpgradeTab e GuardTab)
    public static readonly string[] SceneTabs = { UpgradePanel.TabObjectName, CallEstimate.TabObjectName };

    // Seta e aro apontando um elemento do HUD ficam acima das abas; apontando o mundo, abaixo delas.
    public static int PointerOrderFor(bool hudTarget) => hudTarget ? PointerOrder : MapEffectOrder;

    // Canvas próprio na ordem dada, na camada do HUD; com raycaster, os botões da aba continuam clicáveis.
    public static Canvas Raise(GameObject tab, int order, bool clickable = true)
    {
        if (tab == null) return null;

        Canvas canvas = tab.GetComponent<Canvas>();
        if (canvas == null) canvas = tab.AddComponent<Canvas>();
        if (clickable && tab.GetComponent<GraphicRaycaster>() == null) tab.AddComponent<GraphicRaycaster>();

        canvas.overrideSorting = true;
        Canvas hud = FeelUI.HudCanvas;
        if (hud != null) canvas.sortingLayerID = hud.sortingLayerID;
        canvas.sortingOrder = order;
        return canvas;
    }

    // Camada do HUD para a resposta a um clique que pode cair numa aba (confete da compra, aviso de compra negada):
    // fica acima das abas, sem pegar clique. Os efeitos do mapa seguem nas camadas comuns, abaixo delas.
    public static RectTransform FeedbackLayer(string layerName)
    {
        RectTransform layer = FeelUI.HudLayer(layerName);
        if (layer != null) Raise(layer.gameObject, FeedbackOrder, false);
        return layer;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterFirstScene() => RaiseSceneTabs();

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RaiseSceneTabs();

    private static void RaiseSceneTabs()
    {
        Canvas hud = FeelUI.HudCanvas;
        if (hud == null) return;

        foreach (string tabName in SceneTabs)
        {
            Transform tab = FeelUI.FindChild(hud.transform, tabName);
            if (tab != null && tab.GetComponent<HudTabSorting>() == null) tab.gameObject.AddComponent<HudTabSorting>();
        }
    }
}
