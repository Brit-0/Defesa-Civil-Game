using DG.Tweening;
using TMPro;
using UnityEngine;

public class FeedbacksManager : MonoBehaviour
{
    public static FeedbacksManager Instance;

    [SerializeField] private TMP_Text feedbackText;

    private void Awake()
    {
        Instance = this;
    }

    public static void ShowText(string text)
    {
        Instance.feedbackText.text = text;
        if (!Instance.feedbackText.gameObject.activeInHierarchy)
        {
            Instance.feedbackText.GetComponent<CanvasGroup>().alpha = 0f;
            Instance.feedbackText.gameObject.SetActive(true);
            Instance.feedbackText.GetComponent<CanvasGroup>().DOFade(1f, .2f);
        }
    }

    public static void HideText()
    {
        Instance.feedbackText.GetComponent<CanvasGroup>().DOFade(0f, .2f).OnComplete(() => Instance.feedbackText.gameObject.SetActive(false));
    }
}
