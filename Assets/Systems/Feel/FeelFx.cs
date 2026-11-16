using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//Efeitos pequenos feitos só de Image e DOTween. Provisórios até a arte de VFX chegar (ART_REQUESTS.md).
public static class FeelFx
{
    //Estouro de quadradinhos (poeira, brilho) a partir de um ponto local do pai
    public static void Burst(Transform parent, Vector2 center, int count, Color color, float size, float distance, float duration, float upBias = 0f, Sprite sprite = null)
    {
        for (int i = 0; i < count; i++)
        {
            Image particle = FeelUI.CreateImage("Fx", parent, color, sprite);
            GameObject particleObject = particle.gameObject;
            RectTransform rect = particle.rectTransform;

            rect.sizeDelta = Vector2.one * size * Random.Range(.6f, 1.2f);
            rect.anchoredPosition = center;
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));

            Vector2 direction = Random.insideUnitCircle;
            direction.y = direction.y * (1f - upBias) + upBias;
            float time = duration * Random.Range(.7f, 1f);

            rect.DOAnchorPos(center + direction * distance, time).SetEase(Ease.OutCubic).SetLink(particleObject);
            rect.DOScale(.3f, time).SetEase(Ease.InQuad).SetLink(particleObject);
            particle.DOFade(0f, time).SetEase(Ease.InCubic).SetLink(particleObject).OnComplete(() => Object.Destroy(particleObject));
        }
    }
}
