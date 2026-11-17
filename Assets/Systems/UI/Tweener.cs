using DG.Tweening;
using UnityEngine;

public class Tweener : MonoBehaviour
{
    [Header("HOVER")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverTime = .1f;

    [Header("PRESS")]
    [SerializeField] private float pressScale = .9f;
    [SerializeField] private float holdTime = .08f;
    [SerializeField] private float releaseTime = .15f;
    [SerializeField] private float releaseOvershoot = 5f;

    [Header("UPDATE")]
    [SerializeField] private bool ignoreTimeScale = true;

    //Um tween de escala por instância: o novo mata só o anterior deste transform
    private Tween scaleTween;
    //Mão do cursor ligada por este objeto (casa ou botão): some junto se ele desligar com o mouse em cima
    private bool hovered;

    //Botão largo (linha de upgrade) cresce menos que o padrão, para não passar da borda do painel
    public void SetScales(float hover, float press)
    {
        hoverScale = hover;
        pressScale = press;
    }

    public void HoverEnter()
    {
        hovered = true;
        CursorController.SetHover();
        ScaleTo(hoverScale, hoverTime);
    }

    public void HoverExit()
    {
        hovered = false;
        CursorController.Reset();
        ScaleTo(1f, hoverTime);
    }

    public void Hold()
    {
        ScaleTo(pressScale, holdTime);
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
        if (hovered)
        {
            hovered = false;
            CursorController.Reset();
        }

        if (scaleTween == null) return;

        scaleTween.Kill();
        scaleTween = null;
        transform.localScale = Vector3.one;
    }
}
