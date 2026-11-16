using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Tela de fim de partida. Só escuta GameStateChanged: em Won ou Lost mostra tempo, casas consertadas e dinheiro,
//lidos do GameManager, e o botão Reiniciar. Monta a própria UI: basta soltar o prefab no UICanvas.
public class EndScreen : MonoBehaviour
{
    [Header("TEXTS")]
    [SerializeField] private string wonTitle = "CIDADE SALVA!";
    [SerializeField] private string wonSubtitle = "A Defesa Civil deu conta do recado.";
    [SerializeField] private string lostTitle = "O CAOS VENCEU";
    [SerializeField] private string lostSubtitle = "A cidade ficou tempo demais sem socorro.";
    [SerializeField] private string timeFormat = "Tempo: {0:00}:{1:00}";
    [SerializeField] private string housesFormat = "Casas consertadas: {0}";
    [SerializeField] private string moneyFormat = "Dinheiro arrecadado: {0}";
    [SerializeField] private string restartText = "REINICIAR";

    [Header("LAYOUT (tela de referência 1920x1080)")]
    [SerializeField] private Vector2 panelSize = new Vector2(860f, 600f);
    [SerializeField] private float border = 6f;
    [SerializeField] private float titleSize = 84f;
    [SerializeField] private float subtitleSize = 30f;
    [SerializeField] private float statSize = 44f;
    [SerializeField] private float statSpacing = 62f;
    [SerializeField] private Vector2 buttonSize = new Vector2(340f, 88f);

    [Header("COLORS")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, .72f);
    [SerializeField] private Color frameColor = new Color(.07f, .07f, .1f);
    [SerializeField] private Color panelColor = new Color(.16f, .17f, .24f);
    [SerializeField] private Color wonColor = new Color(.45f, .9f, .5f);
    [SerializeField] private Color lostColor = new Color(.95f, .3f, .25f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color subtitleColor = new Color(.75f, .78f, .85f);
    [SerializeField] private Color buttonColor = new Color(.25f, .5f, .85f);

    [Header("TIMING")]
    [SerializeField] private float showDelay = .9f;
    [SerializeField] private float fadeTime = .35f;
    [SerializeField] private float popTime = .4f;
    [SerializeField] private float statInterval = .15f;
    [SerializeField] private float countTime = .6f;
    [SerializeField] private float statSoundVolume = .5f;

    private RectTransform screen;
    private RectTransform panel;
    private CanvasGroup group;
    private TMP_Text titleLbl;
    private TMP_Text subtitleLbl;
    private TMP_Text[] statLbls;
    private RectTransform buttonHolder;
    private Sequence sequence;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        RectTransform parent = FeelUI.ViewRoot(this, "EndScreen");
        if (parent == null) return;

        Vector2 center = new Vector2(.5f, .5f);
        Vector2 top = new Vector2(.5f, 1f);

        screen = FeelUI.CreateRect("Screen", parent);
        FeelUI.Stretch(screen);
        group = screen.gameObject.AddComponent<CanvasGroup>();

        //O fundo escuro segura os cliques que iriam para o HUD
        Image dim = FeelUI.CreateImage("Dim", screen, dimColor);
        dim.raycastTarget = true;
        FeelUI.Stretch(dim.rectTransform);

        panel = FeelUI.CreateImage("Panel", screen, frameColor).rectTransform;
        FeelUI.Place(panel, center, center, Vector2.zero, panelSize);
        FeelUI.Stretch(FeelUI.CreateImage("Back", panel, panelColor).rectTransform, border);

        titleLbl = FeelUI.CreateText("Title", panel, wonTitle, titleSize, wonColor);
        FeelUI.Place(titleLbl.rectTransform, top, center, new Vector2(0f, -100f), new Vector2(panelSize.x, titleSize * 1.2f));

        subtitleLbl = FeelUI.CreateText("Subtitle", panel, wonSubtitle, subtitleSize, subtitleColor);
        FeelUI.Place(subtitleLbl.rectTransform, top, center, new Vector2(0f, -172f), new Vector2(panelSize.x, subtitleSize * 1.4f));

        statLbls = new TMP_Text[3];
        for (int i = 0; i < statLbls.Length; i++)
        {
            statLbls[i] = FeelUI.CreateText("Stat" + i, panel, "", statSize, textColor);
            FeelUI.Place(statLbls[i].rectTransform, top, center, new Vector2(0f, -260f - statSpacing * i), new Vector2(panelSize.x, statSize * 1.3f));
        }

        //O botão fica num suporte: a escala do botão é do Tweener (hover), a do suporte é da entrada
        buttonHolder = FeelUI.CreateRect("ButtonHolder", panel);
        FeelUI.Place(buttonHolder, new Vector2(.5f, 0f), center, new Vector2(0f, 40f + buttonSize.y * .5f), buttonSize);

        Button restart = FeelUI.CreateButton("RestartBtn", buttonHolder, restartText, buttonSize, buttonColor);
        restart.onClick.AddListener(Restart);

        screen.gameObject.SetActive(false);
    }

    private void Start()
    {
        //Caso a tela entre na cena com a partida já encerrada
        if (GameManager.Instance != null) OnGameStateChanged(GameManager.Instance.State);
    }

    private void OnEnable()
    {
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Won) Show(true);
        else if (state == GameState.Lost) Show(false);
        else Hide();
    }

    private void Show(bool won)
    {
        if (screen == null || screen.gameObject.activeSelf) return;

        GameManager game = GameManager.Instance;
        float elapsed = game != null ? game.ElapsedTime : 0f;
        int houses = game != null ? game.HousesRepaired : 0;
        int money = game != null ? game.TotalMoneyEarned : 0;

        titleLbl.text = won ? wonTitle : lostTitle;
        titleLbl.color = won ? wonColor : lostColor;
        subtitleLbl.text = won ? wonSubtitle : lostSubtitle;

        foreach (TMP_Text stat in statLbls) stat.alpha = 0f;

        screen.gameObject.SetActive(true);
        screen.SetAsLastSibling();
        group.alpha = 0f;
        group.interactable = false;
        panel.localScale = Vector3.one * .8f;
        buttonHolder.localScale = Vector3.zero;

        //A partida acabou: tudo aqui roda fora do timeScale
        sequence?.Kill();
        sequence = DOTween.Sequence().SetUpdate(true).SetLink(screen.gameObject);
        sequence.AppendInterval(showDelay);
        sequence.Append(group.DOFade(1f, fadeTime));
        sequence.Join(panel.DOScale(1f, popTime).SetEase(Ease.OutBack));

        AppendStat(statLbls[0], elapsed, value => string.Format(timeFormat, Mathf.FloorToInt(value / 60f), Mathf.FloorToInt(value % 60f)));
        AppendStat(statLbls[1], houses, value => string.Format(housesFormat, Mathf.RoundToInt(value)));
        AppendStat(statLbls[2], money, value => string.Format(moneyFormat, Mathf.RoundToInt(value)));

        sequence.AppendInterval(statInterval);
        sequence.AppendCallback(() => group.interactable = true);
        sequence.Append(buttonHolder.DOScale(1f, popTime).SetEase(Ease.OutBack));
    }

    //Cada linha aparece e conta de zero até o valor
    private void AppendStat(TMP_Text label, float target, Func<float, string> format)
    {
        float shown = 0f;
        label.text = format(0f);

        sequence.AppendInterval(statInterval);
        sequence.AppendCallback(() =>
        {
            label.alpha = 1f;
            if (AudioManager.Sfx != null) AudioManager.PlaySfx(AudioManager.Sfx.click, statSoundVolume);
        });
        sequence.Append(DOTween.To(() => shown, value => { shown = value; label.text = format(value); }, target, countTime).SetEase(Ease.OutQuad));
        sequence.AppendCallback(() => label.text = format(target));
    }

    private void Hide()
    {
        if (screen == null || !screen.gameObject.activeSelf) return;

        sequence?.Kill();
        screen.gameObject.SetActive(false);
    }

    private void Restart()
    {
        if (GameManager.Instance != null) GameManager.Instance.Restart();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    [Button]
    private void DebugShow(bool won = true)
    {
        Hide();
        Show(won);
    }
}
