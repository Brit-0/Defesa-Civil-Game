using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLbl;
    [SerializeField] private TMP_Text descriptionLbl;
    [SerializeField] private TMP_Text specificLbl;

    //Aba no kit pixel do HUD (#531): janela Px_Window fatiada, fonte e cores do kit, margem interna.
    //Veste em runtime, sem editar House, Guard e Warehouse.prefab.
    [Header("PIXEL")]
    [SerializeField] private bool pixelSkin = true;
    [Tooltip("PPU da arte da cidade: 1 pixel da janela fica do tamanho de 1 pixel da casa")]
    [SerializeField] private float artPixelsPerUnit = 32f;
    [Tooltip("Margem interna além da moldura (que ocupa 6 pixels do sprite)")]
    [SerializeField] private float padding = 18f;
    [SerializeField] private float lineGap = 6f;

    private bool skinned;

    private void Awake()
    {
        Skin();
    }

    public void Set(LocationData data)
    {
        nameLbl.text = data.name;
        descriptionLbl.text = data.description;
        specificLbl.text = data.specific;
    }

    public void Show()
    {
        Skin();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Skin()
    {
        FeelUITheme px = FeelUI.Pixel;
        if (skinned || !pixelSkin || px == null || px.window == null) return;
        skinned = true;

        //Unidades deste Canvas por pixel da arte (na cidade de hoje, 3,375)
        float scale = Mathf.Abs(transform.lossyScale.x);
        float pixelUnits = scale > 0f ? 1f / (artPixelsPerUnit * scale) : 3f;

        Image tab = GetComponent<Image>();
        if (tab != null)
        {
            tab.sprite = px.window;
            tab.color = Color.white;
            tab.type = Image.Type.Sliced;
            float reference = tab.canvas != null ? tab.canvas.referencePixelsPerUnit : 100f;
            tab.pixelsPerUnitMultiplier = reference / (px.window.pixelsPerUnit * pixelUnits);
            //A moldura vem do sprite: o Outline do prefab desenharia uma segunda borda
            foreach (Shadow edge in GetComponents<Shadow>()) edge.enabled = false;
        }

        float frame = px.window.border.x * pixelUnits;
        float inset = frame + padding;
        Style(nameLbl, FeelUI.PixelGold);
        Style(descriptionLbl, FeelUI.PixelCream);
        Style(specificLbl, FeelUI.PixelLilac);
        Layout(inset);
    }

    private static void Style(TMP_Text label, Color color)
    {
        if (label == null) return;

        TMP_FontAsset font = FeelUI.Font;
        if (font != null && label.font != font)
        {
            label.font = font;
            label.fontSharedMaterial = font.material;
        }
        label.color = color;
    }

    //Nome em cima, descrição no meio e a linha específica embaixo, todos dentro da margem
    private void Layout(float inset)
    {
        RectTransform panel = (RectTransform)transform;
        float width = panel.rect.width - inset * 2f;
        float height = panel.rect.height - inset * 2f;
        if (width <= 0f || height <= 0f) return;

        float nameHeight = nameLbl != null ? nameLbl.fontSize * 1.2f : 0f;
        float specificHeight = specificLbl != null ? specificLbl.fontSize * 1.2f : 0f;

        Place(nameLbl, inset, nameHeight, TextAlignmentOptions.TopLeft);
        ShrinkToFit(nameLbl);

        if (specificLbl != null)
        {
            RectTransform rect = specificLbl.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, inset);
            rect.sizeDelta = new Vector2(-inset * 2f, specificHeight);
            specificLbl.alignment = TextAlignmentOptions.BottomLeft;
        }

        if (descriptionLbl != null)
        {
            RectTransform rect = descriptionLbl.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(inset, inset + specificHeight + lineGap);
            rect.offsetMax = new Vector2(-inset, -(inset + nameHeight + lineGap));
            descriptionLbl.alignment = TextAlignmentOptions.TopLeft;
            ShrinkToFit(descriptionLbl);
        }
    }

    //Texto comprido encolhe até 60% para caber na margem, nunca cresce além do tamanho do prefab
    private static void ShrinkToFit(TMP_Text label)
    {
        if (label == null) return;

        float size = label.enableAutoSizing ? label.fontSizeMax : label.fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMax = size;
        label.fontSizeMin = size * .6f;
    }

    private static void Place(TMP_Text label, float inset, float height, TextAlignmentOptions alignment)
    {
        if (label == null) return;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -inset);
        rect.sizeDelta = new Vector2(-inset * 2f, height);
        label.alignment = alignment;
    }
}
