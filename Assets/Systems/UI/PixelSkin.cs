using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//Veste em runtime, com o kit pixel, a UI que mora na cena e nos prefabs do MEGABRAIN (HUD, aba de melhorias,
//aba da guarda, botões), sem editar a cena. A regra sai do sprite que cada Image já usa:
//RoundRectangle/RoundSquare vira janela (ou botão pixel com estados, se for o fundo de um Button);
//Circle dourado de ícone vira moeda; Circle ou Button2 de botão vira botão redondo; Arrow vira a seta pixel.
//Contador solto sobre a cidade (dinheiro, trabalhadores, material) ganha uma plaquinha atrás (HudPlate).
//Liga e desliga em FeelUITheme.skinSceneHud. Roda a cada cena carregada (o Reiniciar recarrega a cena).
public static class PixelSkin
{
    private static bool subscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        subscribed = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (!subscribed)
        {
            subscribed = true;
            SceneManager.sceneLoaded += (scene, mode) => SkinScene();
        }

        SkinScene();
    }

    //Pode rodar mais de uma vez: o que já é pixel fica como está
    public static void SkinScene()
    {
        FeelUITheme px = FeelUI.Pixel;
        if (px == null || !px.skinSceneHud) return;

        Canvas hud = FeelUI.HudCanvas;
        if (hud == null) return;

        SkinTree(hud.transform);
        AddPlates(hud.transform, px);
    }

    //Aplica as regras a cada Image da árvore, inclusive as desligadas (aba de melhorias fechada)
    public static void SkinTree(Transform root)
    {
        FeelUITheme px = FeelUI.Pixel;
        if (px == null || root == null) return;

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            SkinImage(image, px);
        }
    }

    private static void SkinImage(Image image, FeelUITheme px)
    {
        Sprite sprite = image.sprite;
        if (sprite == null || FeelUI.IsPixelSprite(sprite)) return;

        Selectable selectable = image.GetComponent<Selectable>();
        bool isButton = selectable != null && (selectable.targetGraphic == image || selectable.targetGraphic == null);

        switch (sprite.name)
        {
            case "RoundRectangle":
            case "RoundSquare":
            case "SmallRoundSquare":
                if (isButton)
                {
                    FeelUI.SkinButton(selectable);
                }
                else
                {
                    image.sprite = px.window;
                    image.color = Color.white;
                    FeelUI.Slice(image);
                }
                //A moldura agora vem do sprite: o Outline da cena desenharia uma segunda borda
                foreach (Shadow edge in image.GetComponents<Shadow>()) edge.enabled = false;
                break;

            case "Circle":
                if (isButton)
                {
                    FeelUI.SkinRoundButton(selectable);
                    SwapCloseLabel(image, px);
                }
                else if (image.name.Contains("Icon") && IsGold(image.color))
                {
                    SetIcon(image, px.iconCoin);
                }
                break;

            case "Button_0":
            case "Button_1":
            case "Button2_0":
            case "Button2_1":
                if (isButton) FeelUI.SkinRoundButton(selectable);
                break;

            case "Arrow":
                SetIcon(image, px.iconArrowUp);
                break;
        }
    }

    private static void SetIcon(Image image, Sprite icon)
    {
        if (icon == null) return;

        image.sprite = icon;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
    }

    private static bool IsGold(Color color)
    {
        return color.r > .8f && color.g > .55f && color.b < .5f;
    }

    //Botão de fechar com "X" escrito: o X vira o ícone pixel
    private static void SwapCloseLabel(Image button, FeelUITheme px)
    {
        if (px.iconClose == null) return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null || label.text.Trim().ToUpperInvariant() != "X") return;

        label.enabled = false;
        Image icon = FeelUI.CreateIcon("Icon", button.transform, px.iconClose);
        RectTransform rect = button.rectTransform;
        float side = Mathf.Min(rect.rect.width, rect.rect.height) * (2f / 3f);
        icon.rectTransform.sizeDelta = new Vector2(side, side);
        icon.rectTransform.anchoredPosition = new Vector2(0f, side / 16f);
    }

    //Contador solto no HUD (um Icon e um texto, sem painel em volta) e texto solto no topo (FeedbackText)
    private static void AddPlates(Transform hud, FeelUITheme px)
    {
        foreach (Transform child in hud.GetComponentsInChildren<Transform>(true))
        {
            if (!(child is RectTransform rect) || child == hud) continue;
            if (child.name.StartsWith(HudPlate.NamePrefix) || HasPlate(child)) continue;
            if (InsidePanel(child, hud)) continue;

            Transform icon = child.Find("Icon");
            bool counter = child.GetComponent<Graphic>() == null && icon != null && icon.GetComponent<Image>() != null
                && DirectTexts(child) == 1;
            bool looseText = child.parent == hud && child.GetComponent<TMP_Text>() != null && child.name == "FeedbackText";

            if (counter || looseText) HudPlate.Create(rect, px.inset);
        }
    }

    private static int DirectTexts(Transform parent)
    {
        int count = 0;
        foreach (Transform child in parent)
        {
            if (child.GetComponent<TMP_Text>() != null) count++;
        }

        return count;
    }

    private static bool HasPlate(Transform target)
    {
        return target.parent != null && target.parent.Find(HudPlate.NamePrefix + target.name) != null;
    }

    //Algum pai, até o Canvas, já é um painel com Image (a aba da guarda, a aba de melhorias, as telas do C)
    private static bool InsidePanel(Transform child, Transform hud)
    {
        for (Transform t = child.parent; t != null && t != hud; t = t.parent)
        {
            if (t.GetComponent<Image>() != null) return true;
            if (t.GetComponent<Canvas>() != null) return true;
        }

        return false;
    }
}
