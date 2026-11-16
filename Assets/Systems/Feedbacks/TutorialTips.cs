using DG.Tweening;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using TMPro;
using UnityEngine;

//Dicas dos primeiros passos, numa faixa embaixo do HUD. Só escuta eventos e só na primeira volta da sessão:
//casa com problema -> chamado aberto -> conserto pronto -> pagamento recebido. Monta a própria UI.
public class TutorialTips : MonoBehaviour
{
    [Header("TEXTS (aceitam tags do Text Animator)")]
    [SerializeField] private string openCallTip = "Uma casa precisa de ajuda! <bounce>Clique nela</bounce> para abrir um chamado.";
    [SerializeField] private string sendTeamTip = "Escolha a <bounce>base</bounce>, monte a equipe e clique em <bounce>Enviar</bounce>.";
    [SerializeField] private string collectTip = "Conserto pronto! <bounce>Clique na casa</bounce> para receber o pagamento.";
    [SerializeField] private string doneTip = "Boa! Use o dinheiro para comprar <bounce>melhorias</bounce>.";

    [Header("LAYOUT (tela de referência 1920x1080)")]
    [SerializeField] private Vector2 size = new Vector2(1240f, 84f);
    [SerializeField] private float bottomMargin = 110f;
    [SerializeField] private float border = 4f;
    [SerializeField] private float fontSize = 34f;

    [Header("COLORS")]
    [SerializeField] private Color frameColor = new Color(1f, .82f, .3f);
    [SerializeField] private Color backColor = FeelUI.PanelColor;
    [SerializeField] private Color textColor = Color.white;

    [Header("TIMING")]
    [SerializeField] private float slideTime = .35f;
    [SerializeField] private float changePunch = .08f;
    [SerializeField] private float doneTime = 5f;

    private enum Step { WaitProblem, OpenCall, SendTeam, WaitWork, Collect, Done }

    //Vale pela sessão: depois do Reiniciar as dicas não voltam (sem disco, por causa do WebGL)
    private static bool completed;

    private RectTransform bar;
    private TextAnimator_TMP label;
    private Tween hideTween;
    private Step step;

    private float ShownY => bottomMargin + size.y * .5f;
    private float HiddenY => -size.y;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        completed = false;
    }

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        RectTransform parent = FeelUI.ViewRoot(this, "TutorialTips");
        if (parent == null) return;

        Vector2 center = new Vector2(.5f, .5f);

        bar = FeelUI.CreatePanel("Tip", parent, frameColor).rectTransform;
        FeelUI.Place(bar, new Vector2(.5f, 0f), center, new Vector2(0f, HiddenY), size);
        FeelUI.Stretch(FeelUI.CreatePanel("Back", bar, backColor).rectTransform, border);

        TMP_Text text = FeelUI.CreateText("Lbl", bar, "", fontSize, textColor);
        FeelUI.Stretch(text.rectTransform, border * 4f);
        //Dica comprida encolhe para caber na faixa
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * .5f;
        text.fontSizeMax = fontSize;
        label =text.gameObject.AddComponent<TextAnimator_TMP>();

        bar.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.HouseStateChanged += OnHouseStateChanged;
        GameEvents.CallOpened += OnCallOpened;
        GameEvents.TeamDispatched += OnTeamDispatched;
        GameEvents.WorkCompleted += OnWorkCompleted;
        GameEvents.CallResolved += OnCallResolved;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.HouseStateChanged -= OnHouseStateChanged;
        GameEvents.CallOpened -= OnCallOpened;
        GameEvents.TeamDispatched -= OnTeamDispatched;
        GameEvents.WorkCompleted -= OnWorkCompleted;
        GameEvents.CallResolved -= OnCallResolved;
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    private void OnHouseStateChanged(House house, State before, State after)
    {
        if (completed || step != Step.WaitProblem) return;
        if (after != State.Damaged && after != State.OnFire) return;

        Show(Step.OpenCall, openCallTip);
    }

    private void OnCallOpened(Call call)
    {
        if (completed || step > Step.OpenCall) return;

        Show(Step.SendTeam, sendTeamTip);
    }

    private void OnTeamDispatched(Call call)
    {
        if (completed || step > Step.SendTeam) return;

        step = Step.WaitWork;
        Hide();
    }

    private void OnWorkCompleted(Call call)
    {
        if (completed || step > Step.WaitWork) return;

        Show(Step.Collect, collectTip);
    }

    private void OnCallResolved(Call call, int moneyEarned)
    {
        if (completed) return;

        //A primeira volta fechou: a última dica fica um tempo e as dicas acabam
        completed = true;
        Show(Step.Done, doneTip);
        if (bar != null) hideTween = DOVirtual.DelayedCall(doneTime, Hide, true).SetLink(bar.gameObject);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Won || state == GameState.Lost || state == GameState.Menu) Hide();
    }

    private void Show(Step newStep, string text)
    {
        step = newStep;
        if (bar == null) return;

        hideTween?.Kill();
        hideTween = null;

        bool wasHidden = !bar.gameObject.activeSelf;
        bar.gameObject.SetActive(true);
        label.SetText(text);

        bar.DOKill();
        bar.localScale = Vector3.one;

        if (wasHidden) bar.anchoredPosition = new Vector2(0f, HiddenY);
        else bar.DOPunchScale(Vector3.one * changePunch, slideTime, 4, .5f).SetUpdate(true).SetLink(bar.gameObject);

        bar.DOAnchorPosY(ShownY, slideTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(bar.gameObject);
    }

    private void Hide()
    {
        hideTween?.Kill();
        hideTween = null;

        if (bar == null || !bar.gameObject.activeSelf) return;

        bar.DOKill();
        bar.DOAnchorPosY(HiddenY, slideTime).SetEase(Ease.InBack).SetUpdate(true).SetLink(bar.gameObject)
            .OnComplete(() => bar.gameObject.SetActive(false));
    }
}
