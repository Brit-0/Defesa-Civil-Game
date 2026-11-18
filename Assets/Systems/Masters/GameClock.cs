using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Relógio das animações que rodam fora do timeScale (pulsos, setas, chamas do jato), parado na pausa (Esc ou P, #535).
// Time.unscaledTime segue andando com o jogo pausado; GameClock.Time fica parado enquanto a pausa durar.
// O GameManager congela e solta junto com o Time.timeScale: os tweens que estavam tocando param (os da tela de pausa
// nascem depois e tocam) e os sons do jogo param pelo AudioListener (música e cliques da interface ignoram a pausa).
public static class GameClock
{
    private static float pausedTotal;
    private static float pausedSince = -1f;
    private static readonly List<Tween> frozen = new();

    public static bool Frozen => pausedSince >= 0f;

    public static float Time => (Frozen ? pausedSince : UnityEngine.Time.unscaledTime) - pausedTotal;
    // Com o mesmo teto do Time.deltaTime (Maximum Allowed Timestep): depois de um engasgo do Editor o quadro seguinte
    // vinha com segundos de uma vez e os laços de efeito por tempo (ondas da enchente, respingos) criavam centenas de
    // objetos num quadro só, o que segurava o quadro seguinte também (v5F).
    public static float DeltaTime => Frozen ? 0f : Mathf.Min(UnityEngine.Time.unscaledDeltaTime, UnityEngine.Time.maximumDeltaTime);

    public static void Freeze()
    {
        if (Frozen) return;

        pausedSince = UnityEngine.Time.unscaledTime;
        frozen.Clear();
        List<Tween> playing = DOTween.PlayingTweens();
        if (playing != null)
        {
            foreach (Tween tween in playing)
            {
                tween.Pause();
                frozen.Add(tween);
            }
        }
        AudioListener.pause = true;
    }

    public static void Thaw()
    {
        if (!Frozen) return;

        pausedTotal += UnityEngine.Time.unscaledTime - pausedSince;
        pausedSince = -1f;
        foreach (Tween tween in frozen)
        {
            if (tween.IsActive()) tween.Play();
        }
        frozen.Clear();
        AudioListener.pause = false;
    }

    // Reiniciar pela pausa: a cena nova começa com o relógio e o som soltos
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        pausedTotal = 0f;
        pausedSince = -1f;
        frozen.Clear();
        AudioListener.pause = false;
    }
}
