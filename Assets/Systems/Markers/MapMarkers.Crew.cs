using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Etiqueta do mínimo de trabalhadores no pino (v6G): bonequinho de trabalhador e o número que o chamado exige, na placa
//escura do kit dos pinos, embaixo à esquerda do disco (o selo do dano fica à direita). Só em chamado que pede mais de
//um (enchente 2, meteoro 8, ou outro com mínimo) e só enquanto ninguém foi enviado. O número é o Call.workersNeeded.
public partial class MapMarkers
{
    [Header("CREW TAG (v6G: mínimo de trabalhadores)")]
    [Tooltip("Altura do bonequinho em px de 1080p; o sprite do HUD é escalado para ela")]
    [SerializeField] private float crewIconHeight = 30f;
    [SerializeField] private float crewFontSize = 26f;
    [SerializeField] private Color crewTextColor = new Color32(0xFF, 0xD1, 0x4D, 0xFF);

    private class CrewTag
    {
        public RectTransform root;
        public Image icon;
        public TMP_Text amount;
        public int shown = -1;
    }

    private readonly Dictionary<Pin, CrewTag> crewTags = new Dictionary<Pin, CrewTag>();
    private Sprite crewIcon;

    //Mesmo bonequinho do contador de trabalhadores do HUD (Resources/Workers/Icon); sem ele, o ícone da melhoria
    private Sprite CrewIcon()
    {
        if (crewIcon != null) return crewIcon;

        Transform icon = canvas != null ? FeelUI.FindChild(canvas.transform, "Resources", "Workers", "Icon") : null;
        Image image = icon != null ? icon.GetComponent<Image>() : null;
        crewIcon = image != null ? image.sprite : null;
        if (crewIcon == null && FeelUI.Theme != null) crewIcon = FeelUI.Theme.LabIconFor("upgrade_workers");
        return crewIcon;
    }

    private void MakeCrew(Pin p, float s)
    {
        Image plate = NewImage("Crew", p.body, plateSpr, s);
        plate.type = Image.Type.Sliced;
        plate.pixelsPerUnitMultiplier = 1f / s;
        RectTransform root = plate.rectTransform;
        root.pivot = new Vector2(1f, .5f);
        root.anchoredPosition = new Vector2(-4f, -10f) * s;

        Image icon = NewImage("Icon", root, null, 1f);
        icon.preserveAspect = true;
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, .5f);
        icon.rectTransform.pivot = new Vector2(0f, .5f);

        TMP_Text amount = NewText("Amount", root, crewFontSize);
        amount.color = crewTextColor;
        amount.alignment = TextAlignmentOptions.MidlineLeft;
        amount.rectTransform.anchorMin = amount.rectTransform.anchorMax = new Vector2(0f, .5f);
        amount.rectTransform.pivot = new Vector2(0f, .5f);

        root.gameObject.SetActive(false);
        crewTags[p] = new CrewTag { root = root, icon = icon, amount = amount };
    }

    //Chamado pelo Animate a cada quadro em que o pino aparece
    private void PaintCrew(Pin p)
    {
        if (!crewTags.TryGetValue(p, out CrewTag tag)) return;

        Call call = p.house.currentCall;
        bool waiting = p.kind != Kind.None && p.kind != Kind.Done && !p.house.HasTeam && call != null && call.house == p.house;
        int minimum = waiting && CallCrew.Shows(call) ? CallCrew.Minimum(call) : 0;
        if (minimum == tag.shown) return;

        bool appearing = tag.shown <= 0 && minimum > 0;
        tag.shown = minimum;
        tag.root.gameObject.SetActive(minimum > 0);
        if (minimum <= 0) return;

        Layout(tag, minimum);
        if (!appearing) return;

        //Entra com um pulo, como o selo do dano quando a chuva sobe o número
        tag.root.DOKill(true);
        tag.root.localScale = Vector3.one;
        tag.root.DOPunchScale(Vector3.one * .35f, .35f, 6).SetUpdate(true).SetLink(tag.root.gameObject);
    }

    private void Layout(CrewTag tag, int minimum)
    {
        const float pad = 8f, gap = 4f;
        Sprite sprite = CrewIcon();
        float iconWidth = 0f;
        tag.icon.enabled = sprite != null;
        if (sprite != null)
        {
            tag.icon.sprite = sprite;
            float scale = crewIconHeight / Mathf.Max(1f, sprite.rect.height);
            tag.icon.rectTransform.sizeDelta = sprite.rect.size * scale;
            tag.icon.rectTransform.anchoredPosition = new Vector2(pad, 0f);
            iconWidth = tag.icon.rectTransform.sizeDelta.x + gap;
        }

        tag.amount.text = CallCrew.PinLabel(minimum);
        float textWidth = tag.amount.GetPreferredValues(tag.amount.text).x;
        tag.amount.rectTransform.sizeDelta = new Vector2(textWidth + 2f, crewFontSize * 1.4f);
        tag.amount.rectTransform.anchoredPosition = new Vector2(pad + iconWidth, 1f);

        float height = Mathf.Max(crewIconHeight, crewFontSize) + 10f;
        tag.root.sizeDelta = new Vector2(pad * 2f + iconWidth + textWidth + 2f, height);
    }
}
