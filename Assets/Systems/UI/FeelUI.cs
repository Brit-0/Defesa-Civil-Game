using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Atalhos para montar e achar UI por código: os componentes do Agente C funcionam sem nada arrastado na cena
public static class FeelUI
{
    //Ordem acima de qualquer casa do Canvas World Space (casas em 5000 por Y, overlays em 10000)
    public const int OverlaySortingOrder = 10000;

    private static TMP_FontAsset font;
    private static Canvas hudCanvas;
    private static Canvas worldCanvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        font = null;
        hudCanvas = null;
        worldCanvas = null;
    }

    #region FIND

    //O UICanvas (Screen Space - Camera), onde fica o HUD
    public static Canvas HudCanvas
    {
        get
        {
            if (hudCanvas == null) FindCanvases();
            return hudCanvas;
        }
    }

    //O Canvas World Space raiz, onde ficam casas, árvores e trabalhadores
    public static Canvas WorldCanvas
    {
        get
        {
            if (worldCanvas == null) FindCanvases();
            return worldCanvas;
        }
    }

    private static void FindCanvases()
    {
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>())
        {
            if (!canvas.isRootCanvas) continue;

            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                if (worldCanvas == null) worldCanvas = canvas;
            }
            else if (hudCanvas == null || canvas.name == "UICanvas")
            {
                hudCanvas = canvas;
            }
        }
    }

    //Fonte do jogo (ModernDOS), lida de um texto que já existe no HUD
    public static TMP_FontAsset Font
    {
        get
        {
            if (font != null) return font;

            TMP_Text sample = HudCanvas != null ? HudCanvas.GetComponentInChildren<TMP_Text>(true) : null;
            if (sample == null) sample = Object.FindAnyObjectByType<TMP_Text>();

            font = sample != null ? sample.font : TMP_Settings.defaultFontAsset;
            return font;
        }
        set => font = value;
    }

    //Desce pela hierarquia nome a nome, incluindo objetos desligados. Ex.: FindChild(hud, "Money", "Lbl")
    public static Transform FindChild(Transform root, params string[] path)
    {
        Transform current = root;

        foreach (string childName in path)
        {
            if (current == null) return null;
            current = FindDeep(current, childName);
        }

        return current;
    }

    private static Transform FindDeep(Transform root, string childName)
    {
        foreach (Transform child in root)
        {
            if (child.name == childName) return child;
        }

        foreach (Transform child in root)
        {
            Transform found = FindDeep(child, childName);
            if (found != null) return found;
        }

        return null;
    }

    #endregion

    #region HUD SPACE

    //Camada esticada no HUD para popups, faixas e telas. Criada na primeira vez, sempre por cima do resto.
    public static RectTransform HudLayer(string layerName)
    {
        Canvas canvas = HudCanvas;
        if (canvas == null) return null;

        Transform existing = canvas.transform.Find(layerName);
        if (existing != null) return (RectTransform)existing;

        RectTransform layer = CreateRect(layerName, canvas.transform);
        Stretch(layer);
        return layer;
    }

    //Posição de um ponto do mundo em coordenadas locais do HUD (âncora e pivô no centro)
    public static Vector2 WorldToHud(Vector3 worldPosition)
    {
        Canvas canvas = HudCanvas;
        Camera cam = Camera.main;
        if (canvas == null || cam == null) return Vector2.zero;

        Vector2 screenPoint = cam.WorldToScreenPoint(worldPosition);
        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screenPoint, canvasCamera, out Vector2 local);
        return local;
    }

    //Posição de um elemento do HUD nas mesmas coordenadas de WorldToHud
    public static Vector2 HudPosition(Transform hudElement)
    {
        Canvas canvas = HudCanvas;
        if (canvas == null || hudElement == null) return Vector2.zero;

        return canvas.transform.InverseTransformPoint(hudElement.position);
    }

    #endregion

    #region CREATE

    public static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        if (parent != null) go.layer = parent.gameObject.layer;

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    public static Image CreateImage(string objectName, Transform parent, Color color, Sprite sprite = null)
    {
        Image image = CreateRect(objectName, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        TextMeshProUGUI label = CreateRect(objectName, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = Font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    public static Button CreateButton(string objectName, Transform parent, string text, Vector2 size, Color color, float fontSize = 40f)
    {
        Image background = CreateImage(objectName, parent, color);
        background.raycastTarget = true;
        background.rectTransform.sizeDelta = size;

        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        TextMeshProUGUI label = CreateText("Lbl", background.transform, text, fontSize, Color.white);
        Stretch(label.rectTransform);

        background.gameObject.AddComponent<Tweener>();
        background.gameObject.AddComponent<ButtonJuice>();
        return button;
    }

    public static void Stretch(RectTransform rect, float margin = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margin, margin);
        rect.offsetMax = new Vector2(-margin, -margin);
    }

    public static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    #endregion
}
