using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//DashedOdd acende os gomos que o Dashed apaga: trocar um pelo outro faz o tracejado andar.
//Segmented: 8 gomos cheios com um vão pequeno (progresso do conserto, 1 gomo por oitavo)
public enum RingStyle { Solid, Dashed, Spiky, Broken, Zigzag, DashedOdd, Segmented }

//Sprites de pixel art desenhados por código (Point, sem arquivo nem import) e as camadas do mundo onde os VFX moram.
//Cores da paleta do kit pixel (docs/jam/ICONES_FALTANDO.md). O branco do sprite é tingido pela cor da Image; o contorno
//sai escuro em qualquer cor. 1 pixel do sprite é desenhado com o tamanho de 1 pixel da arte da cidade (ArtPixel).
public static partial class PixelFx
{
    #region PALETTE

    public static readonly Color Ink = Hex(0x141218);
    public static readonly Color Cream = Hex(0xF2EBDD);
    public static readonly Color Gold = Hex(0xFFD14D);
    public static readonly Color GoldDark = Hex(0xC98A2B);
    public static readonly Color Orange = Hex(0xF28C28);
    public static readonly Color Red = Hex(0xD9412F);
    public static readonly Color RedDark = Hex(0x8A2424);
    public static readonly Color Green = Hex(0x5CC45E);
    public static readonly Color GreenDark = Hex(0x2E7A3C);
    public static readonly Color Blue = Hex(0x3F7FE0);
    public static readonly Color BlueDark = Hex(0x24448A);
    public static readonly Color Lilac = Hex(0xA895B5);
    public static readonly Color Brown = Hex(0x7A4A22);
    public static readonly Color Gray = Hex(0x9A9A9A);
    public static readonly Color LightGray = Hex(0xD0D0D0);

    public static Color Hex(int rgb, float alpha = 1f)
    {
        return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);
    }

    public static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    #endregion

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    private static RectTransform container;
    private static RectTransform ground;
    private static RectTransform sky;
    private static float artPixel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        cache.Clear();
        container = ground = sky = null;
        artPixel = houseUnit = 0f;
    }

    #region WORLD LAYERS

    //O UIContainer do Canvas do mundo (pai de Houses, Trees e Workers)
    public static RectTransform Container
    {
        get
        {
            if (container != null) return container;

            House any = Object.FindAnyObjectByType<House>();
            Transform houses = any != null ? any.transform.parent : null;
            if (houses != null && houses.parent is RectTransform found) container = found;
            return container;
        }
    }

    //Chão: logo acima do chão assado da cidade ([CG] Ground; sem ele, o PolygonsDisplay), embaixo de toda casa, árvore e
    //trabalhador (eles têm Canvas com ordem própria). Acima só do PolygonsDisplay o [CG] Ground cobria pegadas, a trilha
    //da equipe e o rastro dos trabalhadores (#532).
    public static RectTransform Ground
    {
        get
        {
            if (ground == null && Container != null)
            {
                ground = Layer("FxGround", container);
                Transform floor = container.Find("[CG] Ground");
                if (floor == null) floor = container.Find("PolygonsDisplay");
                ground.SetSiblingIndex(floor != null ? floor.GetSiblingIndex() + 1 : 0);
            }
            return ground;
        }
    }

    //Céu: acima das casas (ordem até ~9900) e abaixo dos marcadores e painéis delas (10000)
    public static RectTransform Sky
    {
        get
        {
            if (sky == null && Container != null)
            {
                sky = Layer("FxSky", container);
                Canvas canvas = sky.GetComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = SkyOrder;
            }
            return sky;
        }
    }

    public const int SkyOrder = 9990;

    //Camada esticada com Canvas próprio: o que se mexe nela não refaz a malha do resto do mundo
    private static RectTransform Layer(string layerName, RectTransform parent)
    {
        Transform existing = parent.Find(layerName);
        if (existing != null) return (RectTransform)existing;

        RectTransform layer = FeelUI.CreateRect(layerName, parent);
        FeelUI.Stretch(layer);
        Canvas canvas = layer.gameObject.AddComponent<Canvas>();
        Canvas parentCanvas = parent.GetComponentInParent<Canvas>();
        if (parentCanvas != null) canvas.sortingLayerID = parentCanvas.sortingLayerID;
        return layer;
    }

    //Ponto do mundo nas coordenadas de um filho com âncora no centro da camada
    public static Vector2 ToLocal(RectTransform layer, Vector3 worldPosition)
    {
        return (Vector2)layer.InverseTransformPoint(worldPosition) - layer.rect.center;
    }

    //Tamanho de 1 pixel da arte da cidade, em unidades do UIContainer (mediana das primeiras casas)
    public static float ArtPixel
    {
        get
        {
            if (artPixel <= 0f) Measure();
            return artPixel;
        }
    }

    //Altura típica de uma casa, em unidades do UIContainer: a régua dos efeitos que não nascem numa casa
    public static float HouseUnit
    {
        get
        {
            if (houseUnit <= 0f) Measure();
            return houseUnit;
        }
    }

    private static float houseUnit;

    private static void Measure()
    {
        List<float> pixels = new List<float>();
        List<float> heights = new List<float>();

        foreach (House house in Object.FindObjectsByType<House>())
        {
            Transform visual = house.transform.Find("Visual");
            Image image = visual != null ? visual.GetComponent<Image>() : null;
            if (image == null || image.sprite == null || image.sprite.rect.height <= 0f) continue;

            float scale = Container != null ? image.rectTransform.lossyScale.y / Mathf.Max(1e-6f, container.lossyScale.y) : 1f;
            pixels.Add(image.rectTransform.rect.height * scale / image.sprite.rect.height);
            if (house.transform is RectTransform rect) heights.Add(rect.rect.height * scale);
            if (pixels.Count >= 9) break;
        }

        pixels.Sort();
        heights.Sort();
        //Sem casas na cena ainda: valores do House.prefab (218 de altura, sprite de 64 pixels)
        artPixel = pixels.Count > 0 ? pixels[pixels.Count / 2] : 3.4f;
        houseUnit = heights.Count > 0 ? heights[heights.Count / 2] : 218f;
    }

    //Quantos pixels da tela tem 1 pixel da arte agora (zoom da câmera): perto de 1, o efeito precisa engrossar
    public static float ScreenPixelsPerArtPixel(RectTransform space)
    {
        Camera cam = Camera.main;
        if (cam == null || space == null) return 3f;

        Vector3 a = cam.WorldToScreenPoint(space.TransformPoint(Vector3.zero));
        Vector3 b = cam.WorldToScreenPoint(space.TransformPoint(Vector3.right * ArtPixel));
        return Vector2.Distance(a, b);
    }

    //O que a câmera vê, em coordenadas locais da camada (com margem)
    public static Rect ViewRect(RectTransform layer, float margin = 0f)
    {
        Camera cam = Camera.main;
        if (cam == null || layer == null) return new Rect(-500f, -500f, 1000f, 1000f);

        Plane plane = new Plane(layer.forward, layer.position);
        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(i % 2, i / 2, 0f));
            if (!plane.Raycast(ray, out float distance)) continue;

            Vector2 local = ToLocal(layer, ray.GetPoint(distance));
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        if (float.IsInfinity(min.x)) return new Rect(-500f, -500f, 1000f, 1000f);
        return Rect.MinMaxRect(min.x - margin, min.y - margin, max.x + margin, max.y + margin);
    }

    //Image de efeito: sem clique, com o sprite pixel, do tamanho em pixels da arte
    public static Image CreateSprite(string objectName, Transform parent, Sprite sprite, Color color, float pixels = 1f)
    {
        Image image = FeelUI.CreateImage(objectName, parent, color, sprite);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = sprite != null ? sprite.pivot / sprite.rect.size : new Vector2(.5f, .5f);
        rect.sizeDelta = sprite != null ? sprite.rect.size * ArtPixel * pixels : Vector2.one * ArtPixel * pixels;
        return image;
    }

    #endregion
}
