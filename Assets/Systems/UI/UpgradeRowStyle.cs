using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Medidas e montagem de uma linha do painel de melhorias (UpgradePanel), em unidades do HUD de 1080p.
//Reposiciona os filhos do UpgradeButton.prefab (Icon, Name, Price/MoneyIcon, Price/PriceLbl, LvlLbl) sem editar o prefab.
[Serializable]
public class UpgradeRowStyle
{
    public float height = 90f;
    public float iconSize = 66f;
    public float nameSize = 33f;
    public float priceSize = 30f;
    public float levelSize = 30f;
    [Tooltip("A linha larga cresce menos no hover que os outros botões, para não passar da borda do painel")]
    public float hoverScale = 1.05f;
    public float pressScale = .94f;

    //Linha maior e legível: ícone à esquerda, nome em cima ocupando a largura, preço e nível embaixo
    public void Apply(UpgradeButton row)
    {
        RectTransform r = (RectTransform)row.transform;
        r.localScale = Vector3.one;

        LayoutElement element = row.GetComponent<LayoutElement>();
        if (element == null) element = row.gameObject.AddComponent<LayoutElement>();
        element.minHeight = height;
        element.preferredHeight = height;

        //O texto fica no meio da face, acima do lábio de 2 pixels do botão
        float lift = FeelUI.Pixel != null ? FeelUI.PixelSize : 0f;
        float left = 18f;
        float textLeft = left + iconSize + 15f;
        float line = height * .22f;

        PlaceChild(r, "Icon", new Vector2(0f, .5f), new Vector2(.5f, .5f), new Vector2(left + iconSize * .5f, lift), Vector2.one * iconSize);

        RectTransform name = r.Find("Name") as RectTransform;
        if (name != null)
        {
            name.anchorMin = new Vector2(0f, .5f);
            name.anchorMax = new Vector2(1f, .5f);
            name.pivot = new Vector2(0f, .5f);
            name.anchoredPosition = new Vector2(textLeft, line + lift);
            name.sizeDelta = new Vector2(-(textLeft + left), nameSize * 1.25f);
            StyleText(name, nameSize, TextAlignmentOptions.Left, true);
        }

        RectTransform price = PlaceChild(r, "Price", new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(textLeft, -line + lift), new Vector2(260f, priceSize * 1.2f));
        if (price != null)
        {
            PlaceChild(price, "MoneyIcon", new Vector2(0f, .5f), new Vector2(0f, .5f), Vector2.zero, Vector2.one * priceSize);
            RectTransform priceLbl = PlaceChild(price, "PriceLbl", new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(priceSize + 9f, 0f), new Vector2(220f, priceSize * 1.2f));
            StyleText(priceLbl, priceSize, TextAlignmentOptions.Left, false);
        }

        RectTransform level = PlaceChild(r, "LvlLbl", new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(-left, -line + lift), new Vector2(150f, levelSize * 1.2f));
        StyleText(level, levelSize, TextAlignmentOptions.Right, false);

        //Mesmo hover e clique dos outros botões (escala, som, mãozinha), também sem dinheiro: a linha fica
        //desabilitada, mas responde, e o clique negado treme em vermelho (UpgradeButtonJuice). #531
        ButtonJuice juice = row.GetComponent<ButtonJuice>();
        if (juice == null) juice = row.gameObject.AddComponent<ButtonJuice>();
        juice.ReactWhenDisabled = true;
        row.GetComponent<Tweener>().SetScales(hoverScale, pressScale);
        if (row.GetComponent<UpgradeRowHover>() == null) row.gameObject.AddComponent<UpgradeRowHover>();
    }

    private static RectTransform PlaceChild(RectTransform parent, string childName, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        RectTransform child = parent.Find(childName) as RectTransform;
        if (child != null) FeelUI.Place(child, anchor, pivot, position, size);
        return child;
    }

    private static void StyleText(RectTransform target, float size, TextAlignmentOptions alignment, bool autoSize)
    {
        TMP_Text text = target != null ? target.GetComponent<TMP_Text>() : null;
        if (text == null) return;

        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.enableAutoSizing = autoSize;
        text.fontSizeMax = size;
        text.fontSizeMin = size * .7f;
        text.fontSize = size;
    }
}
