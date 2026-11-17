using DG.Tweening;
using TMPro;
using UnityEngine;

//Texto de instrução no topo do HUD. Escuta CallOpened para pedir a base do chamado e some quando a equipe sai
//ou a partida acaba. ShowText e HideText continuam públicos para quem ainda chama direto (GuardManager).
public class FeedbacksManager : MonoBehaviour
{
    public static FeedbacksManager Instance;

    [SerializeField] private TMP_Text feedbackText;

    [Header("TEXTS")]
    [SerializeField] private string chooseBaseText = "Escolha a base encarregada para o chamado";

    [Header("LAYOUT (tela de referência 1920x1080)")]
    [Tooltip("Do topo da tela até o centro do texto: abaixo da faixa de desastre, para nada cobrir a instrução")]
    [SerializeField] private float textTop = 180f;

    [Tooltip("Ordem da camada do texto: acima de todo o HUD (setas, placas e contadores), abaixo da tela de fim (20000)")]
    [SerializeField] private int sortingOrder = 100;

    [Header("FADE")]
    [SerializeField] private float fadeTime = .2f;

    private CanvasGroup group;
    private Canvas layer;

    private void Awake()
    {
        Instance = this;

        if (feedbackText != null)
        {
            group = feedbackText.GetComponent<CanvasGroup>();
            if (group == null) group = feedbackText.gameObject.AddComponent<CanvasGroup>();

            //Preso ao topo: em qualquer proporção de tela fica logo abaixo da faixa de desastre
            RectTransform rect = feedbackText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -textTop);

            RaiseAboveHud();
        }
    }

    //O texto (e a placa que o PixelSkin põe atrás dele) vai para uma camada com Canvas próprio, desenhada depois de
    //todo o HUD: nada passa por cima da instrução (pedido do Pietro 17/11, #531). Copia a camada de ordenação do
    //UICanvas, senão cai na Default, abaixo da cidade.
    private void RaiseAboveHud()
    {
        Transform parent = feedbackText.transform.parent;
        Canvas root = parent != null ? parent.GetComponentInParent<Canvas>(true) : null;
        if (root == null) return;

        RectTransform holder = FeelUI.CreateRect("FeedbackLayer", parent);
        FeelUI.Stretch(holder);
        holder.SetSiblingIndex(feedbackText.transform.GetSiblingIndex());
        feedbackText.transform.SetParent(holder, false);

        layer = holder.gameObject.AddComponent<Canvas>();
        layer.overrideSorting = true;
        layer.sortingLayerID = root.rootCanvas.sortingLayerID;
        layer.sortingOrder = sortingOrder;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        GameEvents.CallOpened += OnCallOpened;
        GameEvents.TeamDispatched += OnTeamDispatched;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.CallOpened -= OnCallOpened;
        GameEvents.TeamDispatched -= OnTeamDispatched;
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    //Com a automação da base, o GuardManager esconde o texto logo em seguida, no SelectBase
    private void OnCallOpened(Call call)
    {
        ShowText(chooseBaseText);
    }

    private void OnTeamDispatched(Call call)
    {
        HideText();
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.Playing && state != GameState.Paused) HideText();
        //A camada fica acima da tela de pausa (que é HUD comum): some enquanto ela está aberta e volta depois
        if (layer != null) layer.enabled = state != GameState.Paused;
    }

    public static void ShowText(string text)
    {
        if (Instance == null || Instance.feedbackText == null) return;

        Instance.Show(text);
    }

    public static void HideText()
    {
        if (Instance == null || Instance.feedbackText == null) return;

        Instance.Hide();
    }

    private void Show(string text)
    {
        GameObject textObject = feedbackText.gameObject;

        feedbackText.text = text;
        group.DOKill();

        if (!textObject.activeSelf)
        {
            group.alpha = 0f;
            textObject.SetActive(true);
        }

        group.DOFade(1f, fadeTime).SetUpdate(true).SetLink(textObject);
    }

    private void Hide()
    {
        GameObject textObject = feedbackText.gameObject;
        if (!textObject.activeSelf) return;

        group.DOKill();
        group.DOFade(0f, fadeTime).SetUpdate(true).SetLink(textObject).OnComplete(() => textObject.SetActive(false));
    }
}
