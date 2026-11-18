using UnityEngine;

//Tremor da Main Camera por um deslocamento somado no LateUpdate e desfeito no frame seguinte.
//Não altera o CameraController: se ele (ou um tween) mover a câmera, o tremor recomeça da posição nova.
public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance;

    [Header("EARTHQUAKE (força = fração da altura da tela)")]
    [SerializeField] private float earthquakeStrength = .035f;
    [SerializeField] private float earthquakeTime = 1.6f;

    [Header("HOUSE BREAK")]
    [SerializeField] private float breakStrength = .008f;
    [SerializeField] private float breakTime = .25f;

    [Header("LIGHTNING")]
    [SerializeField] private float lightningStrength = .015f;
    [SerializeField] private float lightningTime = .35f;

    [Header("NOISE")]
    [SerializeField] private float frequency = 28f;
    [SerializeField] private float maxStrength = .06f;

    private Camera cam;
    private Vector3 lastOffset;
    private Vector3 appliedPosition;
    private bool applied;
    private float strength;
    private float duration;
    private float timeLeft;
    private float seed;

    private void Awake()
    {
        Instance = this;
        seed = Random.value * 100f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        GameEvents.DisasterStarted += OnDisasterStarted;
        GameEvents.LightningStruck += OnLightningStruck;
        GameEvents.HouseStateChanged += OnHouseStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.DisasterStarted -= OnDisasterStarted;
        GameEvents.LightningStruck -= OnLightningStruck;
        GameEvents.HouseStateChanged -= OnHouseStateChanged;

        Restore();
    }

    private void OnDisasterStarted(DisasterData disaster, Vector3 epicenter)
    {
        if (disaster != null && disaster.type == DisasterType.Earthquake) Shake(earthquakeStrength, earthquakeTime);
    }

    private void OnLightningStruck(Vector3 worldPosition)
    {
        Shake(lightningStrength, lightningTime);
    }

    //Cada casa que quebra dá um tranco: é a onda do terremoto chegando
    private void OnHouseStateChanged(House house, State before, State after)
    {
        if (after == State.Damaged && before != State.Damaged) Shake(breakStrength, breakTime);
    }

    public void Shake(float shakeStrength, float time)
    {
        //Configurações (v6D): o controle vibra mesmo com o tremor de tela desligado; a intensidade escala o tremor
        GameHaptics.Shake(shakeStrength, time);
        shakeStrength *= SettingsStore.Current.ShakeMultiplier;
        if (shakeStrength <= 0f) return;

        float current = timeLeft > 0f ? strength * (timeLeft / duration) : 0f;

        strength = Mathf.Min(maxStrength, Mathf.Max(current, shakeStrength));
        timeLeft = Mathf.Max(timeLeft, time);
        duration = timeLeft;
    }

    private void LateUpdate()
    {
        if (timeLeft <= 0f && !applied) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Transform camTransform = cam.transform;

        //Se ninguém mexeu na câmera desde o último frame, o deslocamento anterior ainda está nela
        Vector3 basePosition = camTransform.position;
        if (applied && basePosition == appliedPosition) basePosition -= lastOffset;

        timeLeft -= Time.deltaTime;

        Vector3 offset = Vector3.zero;
        if (timeLeft > 0f)
        {
            float amplitude = strength * (timeLeft / duration) * cam.orthographicSize * 2f;
            float t = Time.time * frequency;
            offset = new Vector3(Mathf.PerlinNoise(seed, t) - .5f, Mathf.PerlinNoise(t, seed) - .5f, 0f) * (2f * amplitude);
        }

        camTransform.position = basePosition + offset;
        lastOffset = offset;
        appliedPosition = camTransform.position;
        applied = timeLeft > 0f;
    }

    private void Restore()
    {
        if (applied && cam != null && cam.transform.position == appliedPosition) cam.transform.position -= lastOffset;

        applied = false;
        timeLeft = 0f;
        lastOffset = Vector3.zero;
    }
}
