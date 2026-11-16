using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Atalhos para montar e achar UI por código: os componentes do Agente C funcionam sem nada arrastado na cena
public static class FeelUI
{
    //Ordem acima de qualquer casa do Canvas World Space (casas em 5000 por Y, overlays em 10000)
    public const int OverlaySortingOrder = 10000;

    //Cores dos painéis e dos botões que já existem no jogo (UpgradeTab, GuardTab e UpgradeButton)
    public static readonly Color PanelColor = new Color32(0x29, 0x26, 0x2F, 0xFF);
    public static readonly Color ButtonColor = new Color32(0x59, 0x4C, 0x63, 0xFF);

    //Cores da paleta do kit pixel (Tools/PixelUI/pixel_kit.py) que o código usa em texto
    public static readonly Color PixelCream = new Color32(0xF2, 0xEB, 0xDD, 0xFF);
    public static readonly Color PixelGold = new Color32(0xFF, 0xD1, 0x4D, 0xFF);
    public static readonly Color PixelLilac = new Color32(0xA8, 0x95, 0xB5, 0xFF);
    public static readonly Color PixelInk = new Color32(0x14, 0x12, 0x18, 0xFF);

    private static TMP_FontAsset font;
    private static Canvas hudCanvas;
    private static Canvas worldCanvas;
    private static FeelUITheme theme;
    private static bool themeMissing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        font = null;
        hudCanvas = null;
        worldCanvas = null;
        theme = null;
        themeMissing = false;
    }

    //Os sprites de UI do jogo (RoundRectangle, Circle, Button e Button2). Sem o asset os painéis saem lisos.
    public static FeelUITheme Theme
    {
        get
        {
            if (theme != null || themeMissing) return theme;

            theme = Resources.Load<FeelUITheme>(FeelUITheme.ResourcePath);
            themeMissing = theme == null;
            if (themeMissing) Debug.LogWarning("FeelUI: não achei o FeelUITheme em Resources (rodar Tools/Jam/Create Feel Prefabs); os painéis ficam sem sprite");

            return theme;
        }
    }

    //O tema com a pixel art ligada e o kit importado; null volta tudo aos sprites antigos
    public static FeelUITheme Pixel => Theme != null && Theme.Pixel ? Theme : null;

    //Unidades do Canvas por pixel do sprite (3 na tela de referência de 1080p)
    public static float PixelSize => Theme != null && Theme.pixelScale > 0f ? Theme.pixelScale : 3f;

    //Branco com o formato do sprite, para clarão e brilho (o sprite pixel já vem colorido e não clareia por tint)
    public static Sprite Silhouette(Sprite sprite)
    {
        return Theme != null ? Theme.SilhouetteOf(sprite) : sprite;
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

    //Onde um componente monta a própria UI: nele mesmo, esticado, se foi solto dentro de um Canvas;
    //senão numa camada do HUD com o nome dado. Assim o prefab funciona em qualquer lugar da cena.
    public static RectTransform ViewRoot(Component owner, string layerName)
    {
        if (owner.transform is RectTransform rect && owner.GetComponentInParent<Canvas>() != null)
        {
            Stretch(rect);
            return rect;
        }

        return HudLayer(layerName);
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

    //Image lisa (sem sprite) ou com o sprite dado. Painel, caixa e barra usam CreatePanel; disco usa CreateCircle.
    public static Image CreateImage(string objectName, Transform parent, Color color, Sprite sprite = null)
    {
        Image image = CreateRect(objectName, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    //Painel, caixa ou barra com o RoundRectangle do jogo, fatiado (o canto não estica); a cor entra por tint
    public static Image CreatePanel(string objectName, Transform parent, Color color)
    {
        Image image = CreateImage(objectName, parent, color, Theme != null ? Theme.roundRectangle : null);
        Slice(image);
        return image;
    }

    //Disco com o Circle do jogo, inteiro (Simple), como os marcadores de chamado das casas
    public static Image CreateCircle(string objectName, Transform parent, Color color)
    {
        return CreateImage(objectName, parent, color, Theme != null ? Theme.circle : null);
    }

    //Sliced quando o sprite tem borda; o tamanho do canto vem do tema
    //(no kit pixel, 1 pixel do sprite = pixelScale unidades do Canvas, para o pixel ficar inteiro)
    public static void Slice(Image image)
    {
        if (image.sprite == null) return;

        image.type = image.sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        if (Theme == null) return;

        image.pixelsPerUnitMultiplier = IsPixelSprite(image.sprite) ? Theme.MultiplierFor(image.sprite) : Theme.pixelsPerUnitMultiplier;
    }

    public static bool IsPixelSprite(Sprite sprite)
    {
        return sprite != null && sprite.name.StartsWith("Px_");
    }

    //Image fatiada com o sprite dado (painel pixel, moldura, barra)
    public static Image CreateSliced(string objectName, Transform parent, Sprite sprite, Color color)
    {
        Image image = CreateImage(objectName, parent, color, sprite);
        Slice(image);
        return image;
    }

    //Janela: moldura pixel com rebites (sem o kit, o RoundRectangle com moldura na cor de botão, como antes)
    public static Image CreateWindow(string objectName, Transform parent)
    {
        FeelUITheme px = Pixel;
        if (px != null) return CreateSliced(objectName, parent, px.window, Color.white);

        Image frame = CreatePanel(objectName, parent, ButtonColor);
        Stretch(CreatePanel("Back", frame.transform, PanelColor).rectTransform, 6f);
        return frame;
    }

    //Área funda dentro de uma janela (linha de estatística, trilho)
    public static Image CreateInset(string objectName, Transform parent, Color legacyColor)
    {
        FeelUITheme px = Pixel;
        return px != null ? CreateSliced(objectName, parent, px.inset, Color.white) : CreatePanel(objectName, parent, legacyColor);
    }

    //Ícone pixel inteiro, no tamanho do sprite vezes a escala do pixel (16x16 vira 48x48 em 1080p)
    public static Image CreateIcon(string objectName, Transform parent, Sprite icon, float scale = 1f)
    {
        Image image = CreateImage(objectName, parent, Color.white, icon);
        image.preserveAspect = true;
        if (icon != null) image.rectTransform.sizeDelta = icon.rect.size * PixelSize * scale;
        return image;
    }

    //Veste um botão com o kit pixel: sprite por estado (hover, apertado, desativado) e cor branca.
    //primary = o dourado, para a ação principal da tela (Jogar, Reiniciar no fim).
    public static void SkinButton(Selectable selectable, bool primary = false)
    {
        FeelUITheme px = Pixel;
        if (px == null || selectable == null) return;

        Image background = selectable.targetGraphic as Image;
        if (background == null) background = selectable.GetComponent<Image>();
        if (background == null) return;

        background.sprite = primary ? px.primaryNormal : px.buttonNormal;
        background.overrideSprite = null;
        background.color = Color.white;
        Slice(background);

        selectable.targetGraphic = background;
        selectable.transition = Selectable.Transition.SpriteSwap;
        selectable.spriteState = new SpriteState
        {
            highlightedSprite = primary ? px.primaryHover : px.buttonHover,
            pressedSprite = primary ? px.primaryPressed : px.buttonPressed,
            disabledSprite = px.buttonDisabled
        };
    }

    //Botão redondo pixel (pausa, fechar, + e -): disco com lábio que afunda no clique
    public static void SkinRoundButton(Selectable selectable)
    {
        FeelUITheme px = Pixel;
        if (px == null || selectable == null) return;

        Image background = selectable.targetGraphic as Image;
        if (background == null) background = selectable.GetComponent<Image>();
        if (background == null) return;

        background.sprite = px.roundNormal;
        background.overrideSprite = null;
        background.type = Image.Type.Simple;
        background.preserveAspect = true;
        background.color = Color.white;

        selectable.targetGraphic = background;
        selectable.transition = Selectable.Transition.SpriteSwap;
        selectable.spriteState = new SpriteState
        {
            highlightedSprite = px.roundHover,
            pressedSprite = px.roundPressed,
            disabledSprite = px.roundPressed
        };
    }

    //O texto do botão pixel fica no meio da face, acima do lábio de 2 pixels
    public static void LiftLabel(RectTransform label)
    {
        if (Pixel == null || label == null) return;
        label.offsetMin = new Vector2(label.offsetMin.x, label.offsetMin.y + 2f * PixelSize);
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

    //Contorno preto para texto que fica direto por cima da cidade (a fonte do jogo é SDF)
    public static void Outline(TMP_Text label, float width = .25f)
    {
        label.outlineColor = Color.black;
        label.outlineWidth = width;
    }

    //Botão retangular. Com o kit pixel: sprite por estado e a cor vem do sprite (color fica para o modo antigo).
    public static Button CreateButton(string objectName, Transform parent, string text, Vector2 size, Color color, float fontSize = 40f, bool primary = false)
    {
        Image background = CreatePanel(objectName, parent, color);
        background.raycastTarget = true;
        background.rectTransform.sizeDelta = size;

        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        SkinButton(button, primary);

        //No dourado o texto é escuro, para ler bem
        Color textColor = Pixel != null && primary ? PanelColor : Color.white;
        TextMeshProUGUI label = CreateText("Lbl", background.transform, text, fontSize, textColor);
        Stretch(label.rectTransform);
        LiftLabel(label.rectTransform);

        background.gameObject.AddComponent<Tweener>();
        background.gameObject.AddComponent<ButtonJuice>();
        background.gameObject.AddComponent<ButtonPress>();
        return button;
    }

    //Botão redondo como o + e o - da cena: aro do Button2, que ganha fundo no hover e vira disco cheio no clique.
    //O disco de trás segura o clique e deixa o texto legível por cima da cidade.
    //Com o kit pixel e um ícone, o ícone entra no lugar do texto.
    public static Button CreateRoundButton(string objectName, Transform parent, string text, Vector2 size, Color discColor, float fontSize = 40f, Sprite icon = null)
    {
        Image disc = CreateCircle(objectName, parent, discColor);
        disc.raycastTarget = true;
        disc.rectTransform.sizeDelta = size;

        Button button = disc.gameObject.AddComponent<Button>();
        button.targetGraphic = disc;

        if (Pixel != null)
        {
            SkinRoundButton(button);

            if (icon != null)
            {
                //Ícone 16 px num disco de 24 px: 2/3 do botão, acima do lábio
                Image iconImage = CreateIcon("Icon", disc.transform, icon);
                iconImage.rectTransform.sizeDelta = size * (2f / 3f);
                iconImage.rectTransform.anchoredPosition = new Vector2(0f, size.y / 24f);
            }
            else
            {
                TextMeshProUGUI pixelLabel = CreateText("Lbl", disc.transform, text, fontSize, Color.white);
                Stretch(pixelLabel.rectTransform);
                LiftLabel(pixelLabel.rectTransform);
            }

            disc.gameObject.AddComponent<Tweener>();
            disc.gameObject.AddComponent<ButtonJuice>();
            disc.gameObject.AddComponent<ButtonPress>();
            return button;
        }

        if (Theme != null && Theme.button2 != null)
        {
            Image ring = CreateImage("Ring", disc.transform, Color.white, Theme.button2);
            Stretch(ring.rectTransform);

            button.targetGraphic = ring;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { highlightedSprite = Theme.button2Filled, pressedSprite = Theme.circle };
        }

        TextMeshProUGUI label = CreateText("Lbl", disc.transform, text, fontSize, Color.white);
        Stretch(label.rectTransform);

        disc.gameObject.AddComponent<Tweener>();
        disc.gameObject.AddComponent<ButtonJuice>();
        disc.gameObject.AddComponent<ButtonPress>();
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
