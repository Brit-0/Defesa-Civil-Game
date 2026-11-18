using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

//Atalhos da aba de melhorias (v6A). Tab abre e fecha a aba como um clique no botão dela: mesmo aperto do botão, mesmo
//som de clique e só quando o mouse também alcançaria o botão (fora da pausa, do fim e de janela por cima). O Esc
//continua só com a pausa. Depois de posicionar a guarita, o para-raio ou a obra da expansão comprados pela aba (a
//compra fecha a aba para o jogador escolher o lugar), a aba abre de novo sozinha.
//Fica no HUD, que está sempre ligado: a UpgradeTab fica desligada enquanto fechada e não recebe Update.
public class UpgradePanelShortcuts : MonoBehaviour
{
    [Header("REABRIR")]
    [Tooltip("Segundos (GameClock) entre posicionar a obra e a aba abrir de novo: dá para ver a obra começar")]
    [SerializeField] private float reopenDelay = .45f;

    [Header("TAB")]
    [Tooltip("Segundos com o botão afundado antes de soltar, como um clique rápido do mouse")]
    [SerializeField] private float pressTime = .09f;
    [SerializeField, Range(0f, 1f)] private float clickVolume = .7f;

    private UpgradePanel panel;
    private Button openButton;
    private bool placingFromTab;
    private float reopenAt = -1f;
    private readonly List<RaycastResult> hits = new();

    public bool ReopenPending => reopenAt >= 0f;

    public void Init(UpgradePanel upgradePanel, Button button)
    {
        panel = upgradePanel;
        openButton = button;
    }

    private void OnEnable()
    {
        OutpostEvents.PlacementChanged += OnPlacementChanged;
        LightningRodEvents.PlacementChanged += OnPlacementChanged;
        ExpansionEvents.PlacementChanged += OnLotPlacementChanged;
    }

    private void OnDisable()
    {
        OutpostEvents.PlacementChanged -= OnPlacementChanged;
        LightningRodEvents.PlacementChanged -= OnPlacementChanged;
        ExpansionEvents.PlacementChanged -= OnLotPlacementChanged;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame) PressTab();

        if (ReopenPending && GameClock.Time >= reopenAt) Reopen();
    }

    #region TAB

    private void PressTab()
    {
        GameManager game = GameManager.Instance;
        if (panel == null || game == null || game.State != GameState.Playing || !Clickable()) return;

        //Os mesmos eventos de um clique: o ButtonJuice afunda e solta o botão, o Button chama o Toggle da aba
        var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(openButton.gameObject, data, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(openButton.gameObject, data, ExecuteEvents.pointerClickHandler);
        StartCoroutine(Release(data));
        AudioManager.Play("ui_click", null, clickVolume);
    }

    private IEnumerator Release(PointerEventData data)
    {
        yield return new WaitForSecondsRealtime(pressTime);
        if (openButton != null) ExecuteEvents.Execute(openButton.gameObject, data, ExecuteEvents.pointerUpHandler);
    }

    //O mouse clicaria no botão agora? Ligado, habilitado e por cima de tudo no ponto do meio dele.
    private bool Clickable()
    {
        if (openButton == null || !openButton.isActiveAndEnabled || !openButton.IsInteractable()) return false;

        EventSystem events = EventSystem.current;
        if (events == null) return true;

        Canvas canvas = openButton.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        var probe = new PointerEventData(events) { position = RectTransformUtility.WorldToScreenPoint(cam, openButton.transform.position) };
        events.RaycastAll(probe, hits);
        return hits.Count == 0 || hits[0].gameObject.transform.IsChildOf(openButton.transform);
    }

    #endregion

    #region REOPEN

    //Só a compra pela aba põe o jogo no modo de escolher o lugar (a compra pelo mapa já começa a obra)
    private void OnPlacementChanged(bool placing)
    {
        if (placing)
        {
            placingFromTab = true;
            reopenAt = -1f;
            return;
        }

        if (!placingFromTab || AnyPlacing()) return;
        placingFromTab = false;
        reopenAt = GameClock.Time + reopenDelay;
    }

    private void OnLotPlacementChanged(LotKind? kind) => OnPlacementChanged(kind.HasValue);

    private static bool AnyPlacing()
    {
        return (OutpostManager.Instance != null && OutpostManager.Instance.Placing)
            || (LightningRodManager.Instance != null && LightningRodManager.Instance.Placing)
            || (ExpansionManager.Instance != null && ExpansionManager.Instance.Placing.HasValue);
    }

    private void Reopen()
    {
        GameManager game = GameManager.Instance;
        if (game == null || panel == null) { reopenAt = -1f; return; }
        //Na pausa espera; no fim da partida (o fim também tira do modo de escolher) não abre
        if (game.State == GameState.Paused) return;

        reopenAt = -1f;
        if (game.State == GameState.Playing && !panel.IsOpen) panel.Open();
    }

    #endregion
}
