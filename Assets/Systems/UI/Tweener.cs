using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Tweener : MonoBehaviour
{
    private bool isTweening;
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color highlightColor;
    [ColorUsage(true, true)]
    [SerializeField] private Color glowColor;
    public bool isHighlightEnabled = true;
    [SerializeField] private float tiltMult = 1f;

    public void HoverEnter()
    {
        CursorController.SetHover();
        if (isTweening) return;
        isTweening = true;
        DOTween.Complete(this);
        var seq = DOTween.Sequence();
        seq.Append(transform.DOPunchRotation(new Vector3(0f, 0f, -25f * tiltMult), .1f)).SetId("TweenHover");
        seq.Append(transform.DOPunchRotation(new Vector3(0f, 0f, 15f * tiltMult), .3f)).SetId("TweenHover").OnComplete(() => { isTweening = false; });
    }

    public void HoverExit()
    {
        CursorController.Reset();
    }

    public void Hold(float scale = .8f)
    {
        DOTween.Complete("TweenHover");
        DOTween.Complete("TweenHold");
        transform.DOScale(scale, .08f).SetId("TweenHold").OnComplete(() => { isTweening = false; });
    }

    public void Release()
    {
        DOTween.Complete("TweenHold");
        transform.DOShakePosition(.2f, new Vector3(5f, 5f, 0f)).SetId("TweenHold");
        transform.DOScale(1f, .15f).SetEase(Ease.OutBack, 12f).SetId("TweenHold").OnComplete(() => { isTweening = false; });
    }

    public void HighlightEnter()
    {
        if (!isHighlightEnabled) return;

        GetComponent<Image>().color = highlightColor;
    }

    public void HighlightExit()
    {
        if (!isHighlightEnabled) return;

        GetComponent<Image>().color = defaultColor;
    }

    public void DisableHighlight()
    {
        HighlightExit();
        isHighlightEnabled = false;
    }

    public void Glow(float time = 1f)
    {
        GetComponent<Image>().material.DOColor(glowColor, time).SetId("glow");
    }

    public void StopGlowing(float time = .5f)
    {
        DOTween.Kill("glow");
        GetComponent<Image>().material.DOColor(defaultColor, time);
    }
}
