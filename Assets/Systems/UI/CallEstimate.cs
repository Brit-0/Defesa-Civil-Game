using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Estimativa na aba de chamado (GuardTab): quanto a equipe leva para chegar e quanto leva o conserto, com o número de
//trabalhadores escolhido no + e no - (pedido do Pietro 17/11, #531). A conta é a do jogo: caminho do GPS ÷ velocidade
//mais o intervalo entre as saídas (Stats.GetTravelTime) e Stats.GetWorkTime com a equipe escolhida.
//No Armazém, uma terceira linha diz quanto material a equipe traz (Stats.GetRestockAmount, #535).
//A aba cresce para cima para caber a faixa; montada em runtime em toda cena, sem editar o Gameplay.unity.
public class CallEstimate : MonoBehaviour
{
    public const string TabObjectName = "GuardTab";

    [Header("TEXTS")]
    [SerializeField] private string arriveLabel = "Chegada";
    [SerializeField] private string repairLabel = "Conserto";
    [SerializeField] private string fireLabel = "Apagar";
    [SerializeField] private string restockLabel = "Coleta";
    [SerializeField] private string materialLabel = "Material";
    [SerializeField] private string materialFormat = "+{0}";
    [SerializeField] private string emptyHint = "Escolha a equipe no +";
    [SerializeField] private string noValue = "--";

    [Header("LAYOUT (tela de referência 1920x1080)")]
    [Tooltip("Da borda da aba até a faixa funda")]
    [SerializeField] private float sideMargin = 27f;
    [SerializeField] private float topMargin = 24f;
    [Tooltip("Quanto o pé da faixa entra na margem de cima que a aba já tinha")]
    [SerializeField] private float overlap = 9f;
    [Tooltip("Folga dentro da faixa: o texto não encosta na moldura (#535; era 15 e a linha 25)")]
    [SerializeField] private float padX = 24f;
    [SerializeField] private float padY = 16f;
    [SerializeField] private float lineHeight = 32f;
    [SerializeField] private float lineGap = 8f;
    [SerializeField] private float textSize = 26f;
    [Tooltip("Rótulo da barra de CAOS (de pé, na direita): a aba mais alta não entra embaixo dele")]
    [SerializeField] private string chaosLabelText = "CAOS";
    [SerializeField] private float chaosGap = 24f;

    [Header("JUICE")]
    [SerializeField] private float valuePunch = .18f;
    [SerializeField] private float hintPunch = .2f;
    [SerializeField] private float punchTime = .25f;

    private RectTransform tab;
    private RectTransform rows;
    private TMP_Text arriveLbl;
    private TMP_Text arriveValue;
    private TMP_Text workLbl;
    private TMP_Text workValue;
    private TMP_Text materialLbl;
    private TMP_Text materialValue;
    private TMP_Text hintLbl;
    private Transform plusButton;
    //Altura que a aba ganhou em cima, linhas da faixa agora (2, ou 3 no Armazém) e o x de antes do desvio do CAOS
    private float grown;
    private int lines;
    private float baseX;

    private Call pathCall;
    private CityGuard pathGuard;
    private float pathLength = -1f;
    private bool shown;

    #region BOOT

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterFirstScene() => Ensure();

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Ensure();

    //Pode rodar mais de uma vez. A aba começa fechada: a montagem roda na primeira vez que ela abre.
    public static CallEstimate Ensure()
    {
        Canvas hud = FeelUI.HudCanvas;
        Transform tab = hud != null ? FeelUI.FindChild(hud.transform, TabObjectName) : null;
        if (tab == null) return null;

        CallEstimate estimate = tab.GetComponent<CallEstimate>();
        return estimate != null ? estimate : tab.gameObject.AddComponent<CallEstimate>();
    }

    #endregion

    private void Awake()
    {
        tab = (RectTransform)transform;
        baseX = tab.anchoredPosition.x;
        Build();
        SetLines(2);

        plusButton = FeelUI.FindChild(transform, "PlusButton");
        Transform send = FeelUI.FindChild(transform, "SendButton");
        Button sendButton = send != null ? send.GetComponent<Button>() : null;
        if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);
    }

    //A aba ganha altura em cima e o pé fica onde estava: os filhos andam o tanto que a âncora deles subiu
    private static void GrowUp(RectTransform tab, float height)
    {
        foreach (Transform child in tab)
        {
            if (!(child is RectTransform rect) || !Mathf.Approximately(rect.anchorMin.y, rect.anchorMax.y)) continue;
            rect.anchoredPosition -= new Vector2(0f, height * rect.anchorMin.y);
        }

        tab.sizeDelta += new Vector2(0f, height);
        tab.anchoredPosition += new Vector2(0f, height * tab.pivot.y);
    }

    //Com a altura nova, o alto da aba chega na altura do rótulo CAOS: se ficar por baixo dele, a aba anda para a
    //esquerda só o necessário (a placa do rótulo cobria a dica e o tempo de chegada)
    private void ClearChaosLabel(RectTransform tab)
    {
        Canvas hud = FeelUI.HudCanvas;
        if (hud == null) return;

        RectTransform root = (RectTransform)hud.transform;
        TMP_Text label = null;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.text == chaosLabelText && !text.transform.IsChildOf(tab))
            {
                label = text;
                break;
            }
        }
        if (label == null) return;

        Bounds tabBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, tab);
        Bounds labelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, label.rectTransform);
        bool below = tabBounds.max.y > labelBounds.min.y - chaosGap && tabBounds.min.y < labelBounds.max.y;
        float overlap = tabBounds.max.x - (labelBounds.min.x - chaosGap);
        if (below && overlap > 0f) tab.anchoredPosition -= new Vector2(overlap, 0f);
    }

    //Faixa funda no alto da aba: "Chegada" e "Conserto" (e "Material" no Armazém) à esquerda, os valores à direita,
    //em dourado. SetLines dá o tamanho e o lugar de cada linha.
    private void Build()
    {
        Image inset = FeelUI.CreateInset("Estimate", transform, new Color(0f, 0f, 0f, .35f));
        rows = inset.rectTransform;
        rows.anchorMin = new Vector2(0f, 1f);
        rows.anchorMax = new Vector2(1f, 1f);
        rows.pivot = new Vector2(.5f, 1f);

        arriveLbl = Line("ArriveLbl", arriveLabel, FeelUI.PixelCream, TextAlignmentOptions.MidlineLeft);
        arriveValue = Line("ArriveValue", noValue, FeelUI.PixelGold, TextAlignmentOptions.MidlineRight);
        workLbl = Line("WorkLbl", repairLabel, FeelUI.PixelCream, TextAlignmentOptions.MidlineLeft);
        workValue = Line("WorkValue", noValue, FeelUI.PixelGold, TextAlignmentOptions.MidlineRight);
        materialLbl = Line("MaterialLbl", materialLabel, FeelUI.PixelCream, TextAlignmentOptions.MidlineLeft);
        materialValue = Line("MaterialValue", noValue, FeelUI.PixelGold, TextAlignmentOptions.MidlineRight);

        hintLbl = FeelUI.CreateText("Hint", rows, emptyHint, textSize, FeelUI.PixelGold);
        FeelUI.Stretch(hintLbl.rectTransform, padX * .5f);
        hintLbl.enableAutoSizing = true;
        hintLbl.fontSizeMin = textSize * .6f;
        hintLbl.fontSizeMax = textSize;
    }

    private TMP_Text Line(string lineName, string text, Color color, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI label = FeelUI.CreateText(lineName, rows, text, textSize, color, alignment);
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        return label;
    }

    private void PlaceLine(TMP_Text label, int index)
    {
        float top = padY + (lineHeight + lineGap) * index;
        label.rectTransform.offsetMin = new Vector2(padX, -(top + lineHeight));
        label.rectTransform.offsetMax = new Vector2(-padX, -top);
    }

    public float BlockHeight(int count) => padY * 2f + lineHeight * count + lineGap * (count - 1);

    //A aba cresce (ou volta) em cima para a faixa de 2 ou 3 linhas e sai de baixo do rótulo CAOS de novo
    private void SetLines(int count)
    {
        if (count == lines) return;
        lines = count;

        float block = BlockHeight(count);
        float extra = topMargin + block - overlap;
        GrowUp(tab, extra - grown);
        grown = extra;

        rows.offsetMin = new Vector2(sideMargin, -(topMargin + block));
        rows.offsetMax = new Vector2(-sideMargin, -topMargin);
        PlaceLine(arriveLbl, 0);
        PlaceLine(arriveValue, 0);
        PlaceLine(workLbl, 1);
        PlaceLine(workValue, 1);
        PlaceLine(materialLbl, 2);
        PlaceLine(materialValue, 2);

        tab.anchoredPosition = new Vector2(baseX, tab.anchoredPosition.y);
        ClearChaosLabel(tab);
    }

    private void OnEnable()
    {
        shown = false;
        pathCall = null;
        pathGuard = null;
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        GuardManager guards = GuardManager.Instance;
        Call call = guards != null ? guards.CurrentCall : null;
        CityGuard guard = guards != null ? guards.SelectedGuard : null;
        if (call == null || call.house == null || guard == null || arriveValue == null) return;

        int team = guards.TeamSize;
        bool restock = call.type == CallType.Restock;
        SetLines(restock ? 3 : 2);

        bool empty = team < Mathf.Max(1, call.workersNeeded);
        SetVisible(arriveLbl, !empty);
        SetVisible(arriveValue, !empty);
        SetVisible(workLbl, !empty);
        SetVisible(workValue, !empty);
        SetVisible(materialLbl, restock && !empty);
        SetVisible(materialValue, restock && !empty);
        SetVisible(hintLbl, empty);
        if (empty)
        {
            shown = false;
            return;
        }

        float length = PathLength(call, guard);
        workLbl.text = WorkLabel(call);
        bool changed = SetValue(arriveValue, length >= 0f ? FormatTime(Stats.GetTravelTime(length, team)) : noValue);
        changed |= SetValue(workValue, FormatTime(Stats.GetWorkTime(call, team)));
        if (restock) changed |= SetValue(materialValue, string.Format(materialFormat, Stats.GetRestockAmount(team)));

        //Resposta ao + e ao -: os valores pulam quando mudam (não na abertura da aba)
        if (changed && shown)
        {
            Punch(arriveValue.transform, valuePunch);
            Punch(workValue.transform, valuePunch);
            if (restock) Punch(materialValue.transform, valuePunch);
        }
        shown = true;
    }

    //O caminho só muda com outro chamado ou outra base: calculado uma vez por par
    private float PathLength(Call call, CityGuard guard)
    {
        if (call == pathCall && guard == pathGuard) return pathLength;

        pathCall = call;
        pathGuard = guard;
        pathLength = -1f;
        if (GPS.main == null || guard.entry == null || call.house.entry == null) return pathLength;

        List<Waypoint> path = GPS.main.FindShortestPath(guard.entry, call.house.entry);
        if (path != null) pathLength = GPS.PathLength(path, guard.entry.transform.position);
        return pathLength;
    }

    private string WorkLabel(Call call)
    {
        if (call.type == CallType.Restock) return restockLabel;
        return call.house.currentState == State.OnFire ? fireLabel : repairLabel;
    }

    //Segundos inteiros para cima ("12 s"); de um minuto em diante, "1:05"
    public static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds - .001f));
        return total < 60 ? total + " s" : $"{total / 60}:{total % 60:00}";
    }

    private static bool SetValue(TMP_Text label, string text)
    {
        if (label.text == text) return false;

        label.text = text;
        return true;
    }

    private static void SetVisible(Component target, bool visible)
    {
        if (target.gameObject.activeSelf != visible) target.gameObject.SetActive(visible);
    }

    //Enviar com a equipe vazia: o + e a dica pulam, com o som de erro, em vez de "sem trabalhadores"
    private void OnSendClicked()
    {
        GuardManager guards = GuardManager.Instance;
        Call call = guards != null ? guards.CurrentCall : null;
        if (call == null || guards.TeamSize >= call.workersNeeded) return;

        AudioManager.PlaySfx(AudioManager.Sfx?.error);
        Punch(hintLbl.transform, hintPunch);
        if (plusButton != null) Punch(plusButton, hintPunch);
    }

    private void Punch(Transform target, float amount)
    {
        target.DOComplete();
        target.localScale = Vector3.one;
        target.DOPunchScale(Vector3.one * amount, punchTime, 6, .6f).SetUpdate(true).SetLink(target.gameObject);
    }
}
