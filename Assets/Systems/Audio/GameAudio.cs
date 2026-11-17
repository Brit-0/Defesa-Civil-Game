using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Liga os eventos do jogo aos sons do AudioManager. Só escuta GameEvents; não tem regra de jogo.
public class GameAudio : MonoBehaviour
{
    [Header("INTERFACE")]
    [SerializeField, Range(0f, 1f)] private float clickVolume = .7f;
    [SerializeField, Range(0f, 1f)] private float buyVolume = .9f;
    [SerializeField, Range(0f, 1f)] private float errorVolume = .8f;

    [Header("CALLS")]
    [SerializeField, Range(0f, 1f)] private float dispatchVolume = .8f;
    [SerializeField, Range(0f, 1f)] private float baseSelectVolume = .55f;
    [SerializeField, Range(0f, 1f)] private float readyVolume = .45f;
    [SerializeField, Range(0f, 1f)] private float coinVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float incomeVolume = .5f;

    [Header("HAMMER (enquanto houver equipe trabalhando)")]
    [SerializeField] private bool repeatHammer = true;
    [SerializeField, Range(0f, 1f)] private float hammerVolume = .4f;
    [SerializeField] private float hammerInterval = .55f;
    [SerializeField] private float hammerPitchVariation = .15f;

    [Header("DISASTERS")]
    [SerializeField, Range(0f, 1f)] private float sirenVolume = .7f;
    [SerializeField, Range(0f, 1f)] private float earthquakeVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float thunderVolume = .9f;
    [SerializeField, Range(0f, 1f)] private float rainVolume = .6f;
    [SerializeField, Range(0f, 1f)] private float fireVolume = .5f;
    [SerializeField, Range(0f, 1f)] private float extinguishVolume = .8f;
    [SerializeField] private float loopFadeTime = 1f;

    [Header("MUSIC (tensão pelo caos)")]
    [SerializeField, Range(0f, 1f)] private float tensionStartChaos = .35f;
    [SerializeField, Range(0f, 1f)] private float tensionFullChaos = .7f;
    [Tooltip("A camada de perigo (Music_Danger) sobe deste caos até 100%")]
    [SerializeField, Range(0f, 1f)] private float dangerLayerChaos = .7f;

    [Header("MUSIC LEVEL (por estado da partida)")]
    [SerializeField, Range(0f, 1f)] private float menuMusicLevel = .7f;
    [SerializeField, Range(0f, 1f)] private float pausedMusicLevel = .35f;
    [SerializeField, Range(0f, 1f)] private float endMusicLevel = .3f;
    [SerializeField] private float musicLevelFadeTime = .6f;

    [Header("DANGER (sirene ao fundo com o caos alto)")]
    [SerializeField, Range(0f, 1f)] private float dangerChaos = .6f;
    [SerializeField, Range(0f, 1f)] private float dangerSirenVolume = .2f;
    [SerializeField] private float dangerSirenInterval = 9f;

    [Header("START AND END")]
    [SerializeField, Range(0f, 1f)] private float startVolume = .8f;
    [SerializeField, Range(0f, 1f)] private float endVolume = 1f;

    private readonly HashSet<Call> working = new HashSet<Call>();
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private PointerEventData pointerData;
    private EventSystem pointerEventSystem;
    private float hammerTimer;
    private int housesOnFire;
    private float chaos;
    private float nextDangerSiren;
    private bool playing;
    private bool cameFromMenu;

    private static SfxLibrary Sfx => AudioManager.Sfx;

    //No reinício a cena traz outro AudioManager, que se destrói: o GameAudio dele não pode tocar em dobro
    private bool IsDuplicate => TryGetComponent(out AudioManager own) && AudioManager.Instance != null && AudioManager.Instance != own;

    private void OnEnable()
    {
        if (IsDuplicate) return;

        GameEvents.CallOpened += OnCallOpened;
        GameEvents.BaseSelected += OnBaseSelected;
        GameEvents.TeamDispatched += OnTeamDispatched;
        GameEvents.WorkStarted += OnWorkStarted;
        GameEvents.WorkCompleted += OnWorkCompleted;
        GameEvents.CallResolved += OnCallResolved;
        GameEvents.PassiveIncomePaid += OnPassiveIncomePaid;
        GameEvents.UpgradePurchased += OnUpgradePurchased;
        GameEvents.ActionDenied += OnActionDenied;
        GameEvents.HouseStateChanged += OnHouseStateChanged;
        GameEvents.DisasterWarning += OnDisasterWarning;
        GameEvents.DisasterStarted += OnDisasterStarted;
        GameEvents.DisasterEnded += OnDisasterEnded;
        GameEvents.LightningStruck += OnLightningStruck;
        GameEvents.ChaosChanged += OnChaosChanged;
        GameEvents.GameStateChanged += OnGameStateChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameEvents.CallOpened -= OnCallOpened;
        GameEvents.BaseSelected -= OnBaseSelected;
        GameEvents.TeamDispatched -= OnTeamDispatched;
        GameEvents.WorkStarted -= OnWorkStarted;
        GameEvents.WorkCompleted -= OnWorkCompleted;
        GameEvents.CallResolved -= OnCallResolved;
        GameEvents.PassiveIncomePaid -= OnPassiveIncomePaid;
        GameEvents.UpgradePurchased -= OnUpgradePurchased;
        GameEvents.ActionDenied -= OnActionDenied;
        GameEvents.HouseStateChanged -= OnHouseStateChanged;
        GameEvents.DisasterWarning -= OnDisasterWarning;
        GameEvents.DisasterStarted -= OnDisasterStarted;
        GameEvents.DisasterEnded -= OnDisasterEnded;
        GameEvents.LightningStruck -= OnLightningStruck;
        GameEvents.ChaosChanged -= OnChaosChanged;
        GameEvents.GameStateChanged -= OnGameStateChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    //Este objeto atravessa o reinício junto com o AudioManager: o que era da partida anterior zera aqui
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        working.Clear();
        housesOnFire = 0;
        AudioManager.SetLoop(Sfx?.fireLoop, 0f, loopFadeTime);
        AudioManager.SetLoop(Sfx?.rainLoop, 0f, loopFadeTime);
        AudioManager.SetTension(0f);
        AudioManager.SetDanger(0f);
        AudioManager.SetMusicLevel(1f, musicLevelFadeTime);
        chaos = 0f;
        playing = false;
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && IsPointerOverButton())
        {
            AudioManager.PlaySfx(Sfx?.click, clickVolume, .05f);
        }

        UpdateHammer();
        UpdateDangerSiren();
    }

    private void UpdateHammer()
    {
        if (!repeatHammer || working.Count == 0) return;

        hammerTimer -= Time.deltaTime;
        if (hammerTimer > 0f) return;

        hammerTimer = hammerInterval;
        AudioManager.PlaySfx(Sfx?.hammer, hammerVolume, hammerPitchVariation);
    }

    //Com o caos acima do limite de perigo, uma sirene baixa de tempos em tempos (GDD, seção 6)
    private void UpdateDangerSiren()
    {
        if (!playing || chaos < dangerChaos || Time.time < nextDangerSiren) return;

        nextDangerSiren = Time.time + dangerSirenInterval;
        AudioManager.PlaySfx(Sfx?.siren, dangerSirenVolume);
    }

    //Clique em qualquer botão de UI ganha som sem precisar de componente no botão
    private bool IsPointerOverButton()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        if (pointerData == null || pointerEventSystem != eventSystem)
        {
            pointerData = new PointerEventData(eventSystem);
            pointerEventSystem = eventSystem;
        }

        pointerData.position = Mouse.current.position.ReadValue();
        raycastResults.Clear();
        eventSystem.RaycastAll(pointerData, raycastResults);
        if (raycastResults.Count == 0) return false;

        Selectable selectable = raycastResults[0].gameObject.GetComponentInParent<Selectable>();
        return selectable != null && selectable.IsInteractable();
    }

    #region CALLS

    private void OnCallOpened(Call call)
    {
        AudioManager.PlaySfx(Sfx?.click, clickVolume, .05f);
    }

    //Escolher a base tem o mesmo clique das outras ações e o estalo da aba da equipe abrindo
    private void OnBaseSelected(Call call, House guard)
    {
        AudioManager.PlaySfx(Sfx?.click, clickVolume, .05f);
        AudioManager.PlaySfx(Sfx?.popup, baseSelectVolume, .08f);
    }

    private void OnTeamDispatched(Call call)
    {
        AudioManager.PlaySfx(Sfx?.dispatch, dispatchVolume, .05f);
    }

    private void OnWorkStarted(Call call)
    {
        if (call == null) return;

        if (working.Count == 0) hammerTimer = 0f;
        working.Add(call);
    }

    private void OnWorkCompleted(Call call)
    {
        if (call != null) working.Remove(call);

        AudioManager.PlaySfx(Sfx?.repaired, readyVolume, .05f);
    }

    private void OnCallResolved(Call call, int moneyEarned)
    {
        if (call != null) working.Remove(call);

        if (moneyEarned > 0) AudioManager.PlaySfx(Sfx?.coin, coinVolume, .08f);
        else AudioManager.PlaySfx(Sfx?.click, clickVolume, .05f);
    }

    private void OnPassiveIncomePaid(int amount)
    {
        if (amount > 0) AudioManager.PlaySfx(Sfx?.coin, incomeVolume, .08f);
    }

    private void OnUpgradePurchased(UpgradeData upgrade, int newLevel)
    {
        AudioManager.PlaySfx(Sfx?.buy, buyVolume);
    }

    private void OnActionDenied(DeniedReason reason, Vector3 worldPosition)
    {
        AudioManager.PlaySfx(Sfx?.error, errorVolume);
    }

    #endregion

    #region DISASTERS

    private void OnHouseStateChanged(House house, State before, State after)
    {
        if (before == after) return;

        if (after == State.OnFire) housesOnFire++;
        if (before == State.OnFire)
        {
            housesOnFire = Mathf.Max(0, housesOnFire - 1);
            if (after == State.Intact) AudioManager.PlaySfx(Sfx?.extinguish, extinguishVolume, .05f);
        }

        AudioManager.SetLoop(Sfx?.fireLoop, housesOnFire > 0 ? fireVolume : 0f, loopFadeTime);
    }

    private void OnDisasterWarning(DisasterData disaster, float secondsUntilStart)
    {
        AudioManager.PlaySfx(Sfx?.siren, sirenVolume);
    }

    private void OnDisasterStarted(DisasterData disaster, Vector3 epicenter)
    {
        if (disaster == null) return;

        switch (disaster.type)
        {
            case DisasterType.Earthquake:
                AudioManager.PlaySfx(Sfx?.earthquake, earthquakeVolume, .05f);
                break;
            case DisasterType.Storm:
                AudioManager.SetLoop(Sfx?.rainLoop, rainVolume, loopFadeTime);
                break;
            case DisasterType.Fire:
                break;
        }
    }

    private void OnDisasterEnded(DisasterData disaster)
    {
        if (disaster != null && disaster.type == DisasterType.Storm) AudioManager.SetLoop(Sfx?.rainLoop, 0f, loopFadeTime);
    }

    private void OnLightningStruck(Vector3 worldPosition)
    {
        AudioManager.PlaySfx(Sfx?.thunder, thunderVolume, .1f);
    }

    #endregion

    #region PROGRESSION

    private void OnChaosChanged(float value)
    {
        chaos = value;
        AudioManager.SetTension(Mathf.InverseLerp(tensionStartChaos, tensionFullChaos, value));
        AudioManager.SetDanger(Mathf.InverseLerp(dangerLayerChaos, 1f, value));
    }

    private void OnGameStateChanged(GameState state)
    {
        playing = state == GameState.Playing;

        switch (state)
        {
            case GameState.Menu:
                cameFromMenu = true;
                AudioManager.SetTension(0f);
                AudioManager.SetDanger(0f);
                AudioManager.SetMusicLevel(menuMusicLevel, musicLevelFadeTime);
                break;
            case GameState.Playing:
                AudioManager.SetMusicLevel(1f, musicLevelFadeTime);
                //Só quando o jogador clicou em Jogar, não a cada volta da pausa
                if (cameFromMenu) AudioManager.PlaySfx(Sfx?.dispatch, startVolume);
                cameFromMenu = false;
                break;
            case GameState.Paused:
                AudioManager.SetMusicLevel(pausedMusicLevel, musicLevelFadeTime);
                break;
            case GameState.Won:
                EndMatch();
                AudioManager.SetTension(0f);
                AudioManager.SetDanger(0f);
                AudioManager.PlaySfx(Sfx?.victory, endVolume);
                break;
            case GameState.Lost:
                EndMatch();
                AudioManager.PlaySfx(Sfx?.defeat, endVolume);
                break;
        }
    }

    //Fim de partida: os loops param e a música abaixa para a vinheta de vitória ou derrota aparecer
    private void EndMatch()
    {
        working.Clear();
        AudioManager.SetLoop(Sfx?.fireLoop, 0f, loopFadeTime);
        AudioManager.SetLoop(Sfx?.rainLoop, 0f, loopFadeTime);
        AudioManager.SetMusicLevel(endMusicLevel, musicLevelFadeTime);
    }

    #endregion
}
