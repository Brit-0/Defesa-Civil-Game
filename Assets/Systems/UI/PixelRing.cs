using UnityEngine;
using UnityEngine.UI;

//Aro fino do tutorial, das dicas e do escorar (v5A, pedido do Pietro: o Mk_Ring esticado ficava grosso e poluía).
//Troca o sprite pelo anel de 1 pixel do kit (PixelFx.ThinRing) do tamanho do retângulo: o traço fica com
//FeelUI.PixelSize unidades do HUD em qualquer lado, e a opacidade mais baixa já vem no sprite. Quem usa o aro só
//muda o sizeDelta e a cor; o desenho acompanha aqui
[RequireComponent(typeof(Image))]
public class PixelRing : MonoBehaviour
{
    private Image image;
    private int diameter = -1;

    private void Awake() => image = GetComponent<Image>();

    private void OnEnable() => Refresh();

    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (image == null) return;

        Rect rect = image.rectTransform.rect;
        int pixels = Mathf.Max(4, Mathf.RoundToInt(Mathf.Min(rect.width, rect.height) / FeelUI.PixelSize));
        if (pixels == diameter && image.sprite != null) return;

        diameter = pixels;
        image.sprite = PixelFx.ThinRing(pixels, FeelUI.RingAlpha);
    }
}
