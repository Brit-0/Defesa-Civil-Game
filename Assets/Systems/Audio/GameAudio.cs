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

    [Header("END")]
    [SerializeField, Range(0f, 1f)] private float endVolume = 1f;

    private readonly HashSet<Call> working = new HashSet<Call>();
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();
    private PointerEventData pointerData;
    private EventSystem pointerEventSystem;
    private float hammerTimer;
    private int housesOnFire;

    private static SfxLibrary Sfx => AudioManager.Sfx;

    private void OnEnable()
    {
        GameEvents.CallOpened += OnCallOpened;
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
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && IsPointerOverButton())
        {
            AudioManager.PlaySfx(Sfx?.click, clickVolume, .05f);
        }

        if (!repeatHammer || working.Count == 0) return;

        hammerTimer -= Time.deltaTime;
        if (hammerTimer > 0f) return;

        hammerTimer = hammerInterval;
        AudioManager.PlaySfx(Sfx?.hammer, hammerVolume, hammerPitchVariation);
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

    private void OnChaosChanged(float chaos)
    {
        AudioManager.SetTension(Mathf.InverseLerp(tensionStartChaos, tensionFullChaos, chaos));
    }

    private void OnGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Won:
                working.Clear();
                AudioManager.SetTension(0f);
                AudioManager.PlaySfx(Sfx?.victory, endVolume);
                break;
            case GameState.Lost:
                working.Clear();
                AudioManager.PlaySfx(Sfx?.defeat, endVolume);
                break;
        }
    }

    #endregion
}
