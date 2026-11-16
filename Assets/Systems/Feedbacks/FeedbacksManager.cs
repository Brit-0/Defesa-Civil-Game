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

    [Header("FADE")]
    [SerializeField] private float fadeTime = .2f;

    private CanvasGroup group;

    private void Awake()
    {
        Instance = this;

        if (feedbackText != null)
        {
            group = feedbackText.GetComponent<CanvasGroup>();
            if (group == null) group = feedbackText.gameObject.AddComponent<CanvasGroup>();
        }
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
