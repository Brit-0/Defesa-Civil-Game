using UnityEngine;
#if !UNITY_WEBGL || UNITY_EDITOR
using Lofelt.NiceVibrations;
using UnityEngine.InputSystem;
#endif

// Vibração do controle (v6D) pelo Nice Vibrations: tranco no terremoto, no raio e no meteoro, junto com o tremor de tela.
// Só no PC: o rumble do Nice Vibrations conta o tempo com System.Timers (thread), que o WebGL não tem.
public static class GameHaptics
{
    // Tremor abaixo disto não vibra (cada casa que quebra dá um tranco pequeno; vibrar em todos cansa)
    private const float MinStrength = .012f;
    // Tremor que vale a vibração cheia (o terremoto é 0,035)
    private const float FullStrength = .04f;

    public static bool Supported => Application.platform != RuntimePlatform.WebGLPlayer;

    // strength e time nas unidades do CameraShaker.Shake
    public static void Shake(float strength, float time)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (!Supported || strength < MinStrength || !SettingsStore.Current.vibration || Gamepad.current == null) return;

        float amplitude = Mathf.Clamp01(strength / FullStrength);
        HapticPatterns.PlayConstant(amplitude, .5f, Mathf.Clamp(time, .1f, 2f));
#endif
    }

    // Desligou no meio de um tranco: o controle para na hora (o hapticsEnabled do Nice Vibrations fica intocado:
    // mexer nele inicia o plugin mesmo sem controle)
    public static void Apply(GameSettings s)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (Supported && !s.vibration && Gamepad.current != null) HapticController.Stop();
#endif
    }
}
