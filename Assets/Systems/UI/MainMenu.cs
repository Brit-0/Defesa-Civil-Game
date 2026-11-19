using System.Collections;
using DG.Tweening;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Menu inicial por cima da cidade, na mesma cena. Aparece enquanto o GameManager está em Menu
//(precisa do autoStart desligado) e cada botão de dificuldade guarda a escolha (Difficulty) e chama StartGame.
//Monta a própria UI.
public class MainMenu : MonoBehaviour
{
    [Header("TEXTS (o título aceita tags do Text Animator)")]
    [SerializeField] private string title = "<wave a=0.45>DEFESA CIVIL</wave>";
    [SerializeField] private string subtitle = "Mantenha a cidade de pé.";
    [SerializeField, TextArea(3, 6)] private string howToPlay =
        "1. Clique numa casa com problema para abrir um chamado\n" +
        "2. Escolha a base, monte a equipe e envie\n" +
        "3. Receba o pagamento e compre melhorias antes que o caos tome a cidade";
    [SerializeField] private string chooseText = "Escolha a dificuldade";

    [Header("DIFICULDADES (nome no botão e subtítulo curto embaixo)")]
    [SerializeField] private string easyName = "GAROA";
    [SerializeField] private string easyHint = "Fácil · desastres mais calmos";
    [SerializeField] private string normalName = "TEMPORAL";
    [SerializeField] private string normalHint = "Normal · o jogo como ele é";
    [SerializeField] private string hardName = "APOCALIPSE";
    [SerializeField] private string hardHint = "Difícil · caos sem trégua";
    //Tinta por cima do botão dourado: verde no fácil, vermelho no difícil
    [SerializeField] private Color easyTint = new Color(.62f, 1f, .62f);
    [SerializeField] private Color hardTint = new Color(1f, .5f, .45f);

    [Header("LAYOUT (tela de referência 1920x1080)")]
    [SerializeField] private float bandHeight = 300f;
    [SerializeField] private float bandY = 200f;
    [SerializeField] private float bandMargin = 90f;
    [SerializeField] private float titleSize = 150f;
    [SerializeField] private float subtitleSize = 40f;
    [SerializeField] private float howToPlaySize = 30f;
    [SerializeField] private Vector2 difficultyButtonSize = new Vector2(460f, 150f);
    [SerializeField] private float buttonSpacing = 50f;
    [SerializeField] private float buttonFontSize = 56f;
    [Tooltip("Tamanho do subtítulo do botão, em fração da fonte do nome")]
    [SerializeField] private float hintScale = .46f;
    [SerializeField] private float chooseSize = 34f;
    //Emblema pixel (32x32) dos dois lados do título: escala sobre os 3 px por pixel e distância do centro
    [SerializeField] private float emblemScale = 1.25f;
    [SerializeField] private float emblemOffset = 600f;
    [SerializeField] private float buttonY = -150f;
    [SerializeField] private float howToPlayY = -340f;

    [Header("COLORS")]
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, .4f);
    [SerializeField] private Color bandColor = FeelUI.PanelColor;
    [SerializeField] private Color titleColor = new Color(1f, .82f, .3f);
    [SerializeField] private Color subtitleColor = Color.white;
    [SerializeField] private Color howToPlayColor = new Color(.86f, .88f, .93f);
    [SerializeField] private Color buttonColor = FeelUI.ButtonColor;

    [Header("TIMING")]
    [SerializeField] private float fadeTime = .35f;
    [SerializeField] private float dropTime = .6f;
    [SerializeField] private float buttonPulseScale = 1.06f;
    [SerializeField] private float buttonPulseTime = .6f;
    [Tooltip("Jogar passa pela onda de discos do ScreenTransition")]
    [SerializeField] private bool useTransition = true;

    private RectTransform screen;
    private RectTransform band;
    private readonly RectTransform[] buttonHolders = new RectTransform[3];
    //O Normal pulsa: é a sugestão para quem chega
    private RectTransform buttonHolder;
    private CanvasGroup group;
    private TextAnimator_TMP titleAnimator;
    private bool shown;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        RectTransform parent = FeelUI.ViewRoot(this, "MainMenu");
        if (parent == null) return;

        Vector2 center = new Vector2(.5f, .5f);

        screen = FeelUI.CreateRect("Screen", parent);
        FeelUI.Stretch(screen);
        group = screen.gameObject.AddComponent<CanvasGroup>();

        //Escurece pouco, para a cidade aparecer, e segura os cliques do HUD
        Image dim = FeelUI.CreateImage("Dim", screen, dimColor);
        dim.raycastTarget = true;
        FeelUI.Stretch(dim.rectTransform);

        //Kit pixel: janela com moldura e rebites (sem o kit, a faixa lisa na cor do painel)
        FeelUITheme px = FeelUI.Pixel;
        band = (px != null ? FeelUI.CreateWindow("Band", screen) : FeelUI.CreatePanel("Band", screen, bandColor)).rectTransform;
        band.anchorMin = new Vector2(0f, .5f);
        band.anchorMax = new Vector2(1f, .5f);
        band.pivot = center;
        band.sizeDelta = new Vector2(-bandMargin * 2f, bandHeight);
        band.anchoredPosition = new Vector2(0f, bandY);

        TMP_Text titleLbl = FeelUI.CreateText("Title", band, "", titleSize, titleColor);
        FeelUI.Place(titleLbl.rectTransform, center, center, new Vector2(0f, 30f), new Vector2(1600f, titleSize * 1.2f));
        titleAnimator = titleLbl.gameObject.AddComponent<TextAnimator_TMP>();

        //Emblema da Defesa Civil dos dois lados do título
        if (px != null && px.emblem != null)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Image emblem = FeelUI.CreateIcon("Emblem", band, px.emblem, emblemScale);
                emblem.rectTransform.anchoredPosition = new Vector2(side * emblemOffset, 30f);
            }
        }

        TMP_Text subtitleLbl = FeelUI.CreateText("Subtitle", band, subtitle, subtitleSize, subtitleColor);
        FeelUI.Place(subtitleLbl.rectTransform, center, center, new Vector2(0f, -bandHeight * .5f + subtitleSize), new Vector2(1600f, subtitleSize * 1.4f));

        TMP_Text chooseLbl = FeelUI.CreateText("Choose", screen, chooseText, chooseSize, subtitleColor);
        FeelUI.Outline(chooseLbl);
        FeelUI.Place(chooseLbl.rectTransform, center, center, new Vector2(0f, buttonY + difficultyButtonSize.y * .5f + chooseSize), new Vector2(1200f, chooseSize * 1.4f));

        //Três botões lado a lado, do mais calmo ao pior. Cada um fica num suporte: a escala do botão é do Tweener
        //(hover), a do suporte é do pulso
        AddDifficultyButton(0, DifficultyLevel.Easy, easyName, easyHint, easyTint);
        AddDifficultyButton(1, DifficultyLevel.Normal, normalName, normalHint, Color.white);
        AddDifficultyButton(2, DifficultyLevel.Hard, hardName, hardHint, hardTint);
        buttonHolder = buttonHolders[1];

        //Faixa escura também atrás dos passos: direto por cima da cidade o texto pequeno some
        RectTransform howBand = (px != null ? FeelUI.CreateWindow("HowBand", screen) : FeelUI.CreatePanel("HowBand", screen, bandColor)).rectTransform;
        howBand.anchorMin = new Vector2(0f, .5f);
        howBand.anchorMax = new Vector2(1f, .5f);
        howBand.pivot = center;
        howBand.sizeDelta = new Vector2(-bandMargin * 2f, howToPlaySize * 5.6f);
        howBand.anchoredPosition = new Vector2(0f, howToPlayY);

        TMP_Text howLbl = FeelUI.CreateText("HowToPlay", screen, howToPlay, howToPlaySize, howToPlayColor);
        howLbl.lineSpacing = 20f;
        FeelUI.Place(howLbl.rectTransform, center, center, new Vector2(0f, howToPlayY), new Vector2(1700f, howToPlaySize * 5f));

        screen.gameObject.SetActive(false);
    }

    private void AddDifficultyButton(int index, DifficultyLevel level, string levelName, string hint, Color tint)
    {
        Vector2 center = new Vector2(.5f, .5f);
        float x = (index - 1) * (difficultyButtonSize.x + buttonSpacing);

        RectTransform holder = FeelUI.CreateRect("ButtonHolder" + level, screen);
        FeelUI.Place(holder, center, center, new Vector2(x, buttonY), difficultyButtonSize);
        buttonHolders[index] = holder;

        string text = $"{levelName}\n<size={Mathf.RoundToInt(hintScale * 100f)}%>{hint}</size>";
        Button button = FeelUI.CreateButton("PlayBtn" + level, holder, text, difficultyButtonSize, buttonColor, buttonFontSize, true);
        //A tinta multiplica o sprite dourado e fica no hover e no clique (o botão troca só o sprite)
        if (button.targetGraphic != null) button.targetGraphic.color = tint;
        button.onClick.AddListener(() => Play(level));
    }

    //Espera o Start do GameManager: com autoStart ligado a partida já começou e o menu nem pisca
    private IEnumerator Start()
    {
        yield return null;

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
        if (state == GameState.Menu) Show();
        else Hide();
    }

    private void Show()
    {
        if (screen == null || shown) return;

        shown = true;
        screen.gameObject.SetActive(true);
        screen.parent.SetAsLastSibling();
        titleAnimator.SetText(title);

        group.DOKill();
        group.alpha = 0f;
        group.interactable = true;
        group.blocksRaycasts = true;
        group.DOFade(1f, fadeTime).SetUpdate(true).SetLink(screen.gameObject);

        //O título cai de cima e o botão fica pulsando
        band.DOKill();
        band.anchoredPosition = new Vector2(0f, bandY + 500f);
        band.DOAnchorPosY(bandY, dropTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(screen.gameObject);

        buttonHolder.DOKill();
        buttonHolder.localScale = Vector3.one;
        buttonHolder.DOScale(buttonPulseScale, buttonPulseTime).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true).SetLink(screen.gameObject);
    }

    private void Hide()
    {
        if (screen == null || !shown) return;

        shown = false;
        group.interactable = false;
        group.blocksRaycasts = false;

        group.DOKill();
        band.DOKill();
        foreach (RectTransform holder in buttonHolders) holder.DOKill();
        group.DOFade(0f, fadeTime).SetUpdate(true).SetLink(screen.gameObject).OnComplete(() => screen.gameObject.SetActive(false));
    }

    private void Play(DifficultyLevel level)
    {
        if (GameManager.Instance == null || ScreenTransition.Busy) return;

        Difficulty.Set(level);

        //Some o botão para não aceitar o segundo clique durante a onda
        group.interactable = false;
        if (useTransition) ScreenTransition.Play(() => { if (GameManager.Instance != null) GameManager.Instance.StartGame(); });
        else GameManager.Instance.StartGame();
    }
}
