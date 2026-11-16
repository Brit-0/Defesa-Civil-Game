using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//Vinheta vermelha e cor lavada conforme o caos sobe, no Global Volume que já está na cena.
//Um na cena (Feel.prefab). Só escuta ChaosChanged. Mexe na cópia do perfil em runtime, nunca no asset.
public class ChaosVignette : MonoBehaviour
{
    [Header("REFERENCES (vazio = acha o Volume global da cena)")]
    [SerializeField] private Volume volume;

    [Header("RANGE (caos de 0 a 1)")]
    [SerializeField, Range(0f, 1f)] private float startChaos = .4f;
    [SerializeField, Range(0f, 1f)] private float fullChaos = .95f;

    [Header("VIGNETTE")]
    [SerializeField] private Color color = new Color(.75f, .05f, .05f);
    [SerializeField, Range(0f, 1f)] private float maxIntensity = .45f;
    [SerializeField, Range(0f, 1f)] private float smoothness = .5f;

    [Header("PULSE (acima do limite de perigo)")]
    [SerializeField, Range(0f, 1f)] private float pulseChaos = .6f;
    [SerializeField, Range(0f, 1f)] private float pulseAmount = .06f;
    [SerializeField] private float pulseSpeed = 4f;

    [Header("COLOR")]
    [SerializeField, Range(-100f, 0f)] private float maxDesaturation = -30f;

    [Header("SMOOTHING")]
    [SerializeField] private float changeSpeed = 1.5f;

    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private float chaos;
    private float amount;

    private void Start()
    {
        if (volume == null) volume = FindGlobalVolume();
        if (volume == null) return;

        //volume.profile devolve uma cópia só desta cena: o asset do perfil fica intacto
        VolumeProfile profile = volume.profile;

        if (!profile.TryGet(out vignette)) vignette = profile.Add<Vignette>();
        if (!profile.TryGet(out colorAdjustments)) colorAdjustments = profile.Add<ColorAdjustments>();

        vignette.active = true;
        vignette.color.Override(color);
        vignette.smoothness.Override(smoothness);
        colorAdjustments.active = true;

        if (ChaosManager.Instance != null) chaos = ChaosManager.Instance.Chaos;
        Apply(0f);
    }

    private static Volume FindGlobalVolume()
    {
        foreach (Volume candidate in FindObjectsByType<Volume>())
        {
            if (candidate.isGlobal) return candidate;
        }

        return null;
    }

    private void OnEnable()
    {
        GameEvents.ChaosChanged += OnChaosChanged;
    }

    private void OnDisable()
    {
        GameEvents.ChaosChanged -= OnChaosChanged;

        Apply(0f);
    }

    private void OnChaosChanged(float value)
    {
        chaos = value;
    }

    private void Update()
    {
        if (vignette == null) return;

        float target = Mathf.InverseLerp(startChaos, fullChaos, chaos);
        amount = Mathf.MoveTowards(amount, target, changeSpeed * Time.unscaledDeltaTime);

        float pulse = chaos >= pulseChaos ? Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount : 0f;
        Apply(amount, pulse);
    }

    private void Apply(float value, float pulse = 0f)
    {
        if (vignette == null) return;

        vignette.intensity.Override(Mathf.Clamp01(value * maxIntensity + pulse * value));
        colorAdjustments.saturation.Override(value * maxDesaturation);
    }
}
