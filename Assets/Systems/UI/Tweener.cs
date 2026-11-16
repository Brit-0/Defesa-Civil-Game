using DG.Tweening;
using UnityEngine;

public class Tweener : MonoBehaviour
{
    [Header("HOVER")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverTime = .1f;

    [Header("PRESS")]
    [SerializeField] private float holdTime = .08f;
    [SerializeField] private float releaseTime = .15f;
    [SerializeField] private float releaseOvershoot = 5f;

    [Header("UPDATE")]
    [SerializeField] private bool ignoreTimeScale = true;

    //Um tween de escala por instância: o novo mata só o anterior deste transform
    private Tween scaleTween;

    public void HoverEnter()
    {
        //CursorController.SetHover();
        ScaleTo(hoverScale, hoverTime);
    }

    public void HoverExit()
    {
        //CursorController.Reset();
        ScaleTo(1f, hoverTime);
    }

    public void Hold(float scale = .9f)
    {
        ScaleTo(scale, holdTime);
    }

    public void Release()
    {
        ScaleTo(1f, releaseTime).SetEase(Ease.OutBack, releaseOvershoot);
    }

    private Tween ScaleTo(float scale, float duration)
    {
        scaleTween?.Kill();

        Tween tween = transform.DOScale(scale, duration).SetUpdate(ignoreTimeScale).SetLink(gameObject);
        //Solta a referência quando o tween morre, para nunca matar um tween reciclado de outro objeto
        tween.OnKill(() => { if (scaleTween == tween) scaleTween = null; });
        scaleTween = tween;
        return tween;
    }

    private void OnDisable()
    {
        if (scaleTween == null) return;

        scaleTween.Kill();
        scaleTween = null;
        transform.localScale = Vector3.one;
    }
}
