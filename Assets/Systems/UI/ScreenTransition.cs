using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Transição de tela: uma onda de discos (o Circle do jogo) cobre a tela na diagonal e depois abre do mesmo lado.
//Vive fora da cena (DontDestroyOnLoad), por isso atravessa o Reiniciar: cobre, recarrega e abre na cena nova.
//Sem prefab na cena, o primeiro uso cria uma com os valores padrão; com o prefab, valem os do Inspector.
public class ScreenTransition : MonoBehaviour
{
    public static ScreenTransition Instance;

    [Header("GRID (tela de referência 1920x1080)")]
    [SerializeField] private float cellSize = 120f;
    [Tooltip("Diâmetro do disco cheio em células (1,5 cobre os cantos)")]
    [SerializeField] private float coverScale = 1.5f;
    [SerializeField] private int sortingOrder = 30000;

    [Header("COLORS")]
    [SerializeField] private Color discColor = FeelUI.PanelColor;
    [SerializeField] private Color leadColor = FeelUI.ButtonColor;

    [Header("TIMING")]
    [SerializeField] private float sweepTime = .35f;
    [SerializeField] private float discTime = .22f;
    [SerializeField] private float holdTime = .12f;
    [Tooltip("A cena abre coberta e a onda revela a cidade")]
    [SerializeField] private bool revealOnFirstLoad = true;
    [Tooltip("Se ninguém mandar abrir, abre sozinha depois disso (nunca prende a tela)")]
    [SerializeField] private float maxCoverTime = 4f;

    [Header("SOUND")]
    [SerializeField, Range(0f, 1f)] private float whooshVolume = .5f;

    private Canvas canvas;
    private Image blocker;
    private RectTransform[] discs;
    private Image[] discImages;
    private float[] delays;
    private Sequence sequence;
    private bool covered;
    private bool revealOnLoad;
    private float coveredTime;

    public static bool Busy => Instance != null && (Instance.covered || (Instance.sequence != null && Instance.sequence.IsActive()));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private static ScreenTransition Runner
    {
        get
        {
            if (Instance == null) new GameObject("ScreenTransition", typeof(RectTransform)).AddComponent<ScreenTransition>();
            return Instance;
        }
    }

    //Sem prefab na cena, a abertura acontece do mesmo jeito
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateOnBoot()
    {
        if (Instance == null) _ = Runner;
    }

    private void Awake()
    {
        //A cópia da cena recarregada some: a primeira continua viva
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        Build();
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (revealOnFirstLoad)
        {
            SetCovered();
            Reveal();
        }
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    private void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform root = (RectTransform)transform;

        //Segura os cliques enquanto a tela está coberta ou mudando
        blocker = FeelUI.CreateImage("Blocker", root, Color.clear);
        blocker.raycastTarget = true;
        FeelUI.Stretch(blocker.rectTransform);

        //Sobra uma célula em cada borda, para telas mais largas que 16:9
        int columns = Mathf.CeilToInt(1920f / cellSize) + 2;
        int rows = Mathf.CeilToInt(1080f / cellSize) + 2;
        discs = new RectTransform[columns * rows];
        discImages = new Image[discs.Length];
        delays = new float[discs.Length];

        Vector2 center = new Vector2(.5f, .5f);
        Vector2 origin = new Vector2(-(columns - 1) * cellSize * .5f, -(rows - 1) * cellSize * .5f);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int i = y * columns + x;
                //Kit pixel: disco de pixels grossos (o Circle liso destoa da UI pixel)
                Image disc = FeelUI.Pixel != null && FeelUI.Pixel.disc != null ? FeelUI.CreateImage("Disc", root, discColor, FeelUI.Pixel.disc) : FeelUI.CreateCircle("Disc", root, discColor);
                discImages[i] = disc;
                discs[i] = disc.rectTransform;
                FeelUI.Place(discs[i], center, center, origin + new Vector2(x, y) * cellSize, Vector2.one * cellSize * coverScale);

                //A onda anda do canto de baixo à esquerda para o de cima à direita
                delays[i] = ((float)x / (columns - 1) + (float)y / (rows - 1)) * .5f * sweepTime;
            }
        }

        SetDiscs(0f, discColor);
        canvas.enabled = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!revealOnLoad) return;

        revealOnLoad = false;
        //Um respiro para a cena nova montar o HUD por baixo
        DOVirtual.DelayedCall(holdTime, Reveal, true).SetLink(gameObject);
    }

    private void Update()
    {
        if (!covered || sequence != null && sequence.IsActive()) return;

        coveredTime += Time.unscaledDeltaTime;
        if (coveredTime > maxCoverTime) Reveal();
    }

    #region API

    //Cobre a tela, chama a ação no escuro e abre de novo
    public static void Play(Action whileCovered)
    {
        Runner.Cover(() =>
        {
            whileCovered?.Invoke();
            DOVirtual.DelayedCall(Instance.holdTime, Instance.Reveal, true).SetLink(Instance.gameObject);
        });
    }

    //Cobre, recarrega a cena pelo GameManager e a cena nova abre com a onda (toMenu: abre no menu inicial)
    public static void Restart(bool toMenu = false)
    {
        ScreenTransition runner = Runner;
        runner.Cover(() =>
        {
            runner.revealOnLoad = true;
            FeelParticles.Clear();

            if (GameManager.Instance != null) GameManager.Instance.Restart(toMenu);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });
    }

    #endregion

    #region ANIMATION

    private void Cover(Action onCovered)
    {
        sequence?.Kill();
        canvas.enabled = true;
        blocker.raycastTarget = true;
        covered = false;

        PlayWhoosh();
        sequence = Wave(true).OnComplete(() =>
        {
            SetCovered();
            onCovered?.Invoke();
        });
    }

    private void Reveal()
    {
        sequence?.Kill();
        canvas.enabled = true;
        covered = false;
        coveredTime = 0f;
        //A cidade já pode receber clique enquanto a onda abre
        blocker.raycastTarget = false;

        sequence = Wave(false).OnComplete(() => canvas.enabled = false);
    }

    private Sequence Wave(bool cover)
    {
        Sequence wave = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        for (int i = 0; i < discs.Length; i++)
        {
            RectTransform disc = discs[i];
            Image image = discImages[i];
            disc.DOKill();
            image.DOKill();

            if (cover)
            {
                //Entra na cor de destaque e escurece até a cor do painel
                disc.localScale = Vector3.zero;
                image.color = leadColor;
                wave.Insert(delays[i], disc.DOScale(1f, discTime).SetEase(Ease.OutBack));
                wave.Insert(delays[i], image.DOColor(discColor, discTime * 1.5f));
            }
            else
            {
                disc.localScale = Vector3.one;
                image.color = discColor;
                wave.Insert(delays[i], disc.DOScale(0f, discTime).SetEase(Ease.InBack));
            }
        }

        return wave;
    }

    private void SetCovered()
    {
        canvas.enabled = true;
        blocker.raycastTarget = true;
        covered = true;
        coveredTime = 0f;
        SetDiscs(1f, discColor);
    }

    private void SetDiscs(float scale, Color color)
    {
        for (int i = 0; i < discs.Length; i++)
        {
            discs[i].localScale = Vector3.one * scale;
            discImages[i].color = color;
        }
    }

    private void PlayWhoosh()
    {
        SfxLibrary sfx = AudioManager.Sfx;
        if (sfx != null) AudioManager.PlaySfx(sfx.whoosh != null ? sfx.whoosh : sfx.dispatch, whooshVolume, .05f);
    }

    #endregion

    [Button]
    private void DebugPlay()
    {
        Play(null);
    }
}
