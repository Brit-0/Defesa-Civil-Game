using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Tweener : MonoBehaviour
{

    public void HoverEnter()
    {
        //CursorController.SetHover();
        DOTween.Complete("HoverExit");
        transform.DOScale(1.1f, .1f).SetId("HoverEnter");
    }

    public void HoverExit()
    {
        //CursorController.Reset();
        DOTween.Complete("HoverEnter");
        transform.DOScale(1f, .1f).SetId("HoverExit"); ;
    }

    public void Hold(float scale = .9f)
    {
        DOTween.Complete("TweenHover");
        DOTween.Complete("TweenHold");
        transform.DOScale(scale, .08f).SetId("TweenHold");
    }

    public void Release()
    {
        DOTween.Complete("TweenHold");
        //transform.DOShakePosition(.2f, new Vector3(5f, 5f, 0f)).SetId("TweenHold");
        transform.DOScale(1f, .15f).SetEase(Ease.OutBack, 5f).SetId("TweenHold");
    }
}
