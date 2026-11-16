using DG.Tweening;
using UnityEngine;

//Marcador que nasce com um pop e fica pulsando enquanto estiver ligado (chamado, pronto para coletar)
public class MarkerPulse : MonoBehaviour
{
    [Header("POP")]
    public float popTime = .25f;

    [Header("PULSE")]
    public float pulseScale = 1.15f;
    public float pulseTime = .45f;

    private Vector3 baseScale;
    private bool hasBaseScale;
    private Tween popTween;
    private Tween pulseTween;

    private void OnEnable()
    {
        if (!hasBaseScale)
        {
            baseScale = transform.localScale;
            hasBaseScale = true;
        }

        transform.localScale = Vector3.zero;
        popTween = transform.DOScale(baseScale, popTime).SetEase(Ease.OutBack).SetLink(gameObject).OnComplete(StartPulse);
    }

    private void StartPulse()
    {
        pulseTween = transform.DOScale(baseScale * pulseScale, pulseTime).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
    }

    private void OnDisable()
    {
        popTween?.Kill();
        pulseTween?.Kill();
        if (hasBaseScale) transform.localScale = baseScale;
    }
}
