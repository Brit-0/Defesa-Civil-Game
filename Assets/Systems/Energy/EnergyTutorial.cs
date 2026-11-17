using UnityEngine;

// Segundo tutorial, curto (docs/jam/ENERGIA_FOGO.md): quando a energia entra no jogo, 3 passos com o mesmo balão e a
// mesma seta do primeiro. 1) o que gasta energia e onde ver; 2) um apagão de verdade que o jogador religa;
// 3) gerador e sobrecarga. Desastres esperam enquanto ele roda. Uma vez só (PlayerPrefs); depois, só o aviso do HUD.
public class EnergyTutorial : MonoBehaviour
{
    public const string DoneKey = "DefesaCivil.EnergyTutorialDone";

    [Header("TEXTS")]
    [SerializeField, TextArea] private string introText = "A cidade agora usa ENERGIA. Bases, armazéns e a automação gastam. Aqui: o que gasta / o que o gerador dá.";
    [SerializeField, TextArea] private string blackoutText = "APAGÃO! Tempestade pode derrubar a rede e aí nada sai das bases. Clique em RELIGAR!";
    [SerializeField, TextArea] private string generatorText = "Base nova e automação gastam mais. Clique na ENERGIA e melhore o GERADOR. Passou do limite? A automação desliga primeiro.";
    [SerializeField] private string okLabel = "ENTENDI";
    [SerializeField] private string lastLabel = "VAMOS!";

    private const int StepCount = 3;

    private TutorialView view;
    private EnergyHud hud;
    private int step = -1;
    private bool okPressed;
    private bool holding;
    private bool ownBlackout;

    private void OnEnable()
    {
        GameEvents.EnergyUnlocked += OnUnlocked;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.EnergyUnlocked -= OnUnlocked;
        GameEvents.GameStateChanged -= OnGameStateChanged;
        if (view != null) view.SkipPressed -= Finish;
        SetHold(false);
    }

    private void OnUnlocked()
    {
        if (step >= 0 || PlayerPrefs.GetInt(DoneKey, 0) == 1) return;

        // O balão é o do primeiro tutorial quando ele está na cena; senão monta um igual.
        view = FindAnyObjectByType<TutorialView>();
        if (view == null)
        {
            view = gameObject.AddComponent<TutorialView>();
            view.Build();
        }
        view.SkipPressed += Finish;
        hud = FindAnyObjectByType<EnergyHud>();

        SetHold(true);
        Go(0);
    }

    // Com o balão na mão: o Tutorial não mostra nem esconde dica por cima (MEGABRAIN, 16/11).
    public bool Running => step >= 0 && step < StepCount;

    private void Go(int next)
    {
        // Depois do último passo: Finish antes de mexer no step. Com o step já em StepCount o Finish achava que tinha
        // acabado e saía sem soltar os desastres nem esconder o balão (o jogo parava no VAMOS, #532).
        if (next >= StepCount)
        {
            Finish();
            return;
        }

        step = next;
        okPressed = false;
        string header = $"ENERGIA {step + 1}/{StepCount}";

        switch (step)
        {
            case 0:
                view.ShowStep(header, introText, okLabel, () => okPressed = true, true);
                break;
            case 1:
                if (EnergyManager.Instance != null && !EnergyManager.Instance.Blackout)
                {
                    ownBlackout = true;
                    EnergyManager.Instance.StartBlackout(Camera.main != null ? Camera.main.transform.position : Vector3.zero);
                }
                view.ShowStep(header, blackoutText, null, null, true);
                break;
            case 2:
                view.ShowStep(header, generatorText, lastLabel, () => okPressed = true, true);
                break;
        }
    }

    private void Update()
    {
        if (step < 0 || step >= StepCount || view == null) return;

        view.SetTarget(Target(), null);
        bool done = step == 1 ? EnergyManager.Instance == null || !EnergyManager.Instance.Blackout : okPressed;
        if (done) Go(step + 1);
    }

    private RectTransform Target()
    {
        if (hud == null) return null;
        return step == 1 ? hud.RestoreButton : hud.Panel;
    }

    private void Finish()
    {
        if (step < 0 || step >= StepCount) return;

        step = StepCount;
        if (ownBlackout && EnergyManager.Instance != null && EnergyManager.Instance.Blackout) EnergyManager.Instance.Restore();
        ownBlackout = false;
        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();
        view.SkipPressed -= Finish;
        view.HideBubble();
        SetHold(false);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Won || state == GameState.Lost) Finish();
    }

    private void SetHold(bool hold)
    {
        if (hold == holding || DisasterManager.Instance == null) return;

        holding = hold;
        if (hold) DisasterManager.Instance.Hold();
        else DisasterManager.Instance.Release();
    }
}
