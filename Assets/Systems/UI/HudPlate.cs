using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Plaquinha pixel atrás de um contador do HUD que fica direto sobre a cidade (dinheiro, trabalhadores, material)
//ou do texto de instrução do topo. Fica como irmã, logo antes do alvo, e acompanha o ícone e a largura do número.
//Ignora a escala dos filhos (o pulo do HudJuice) e some junto com o alvo. Criada pelo PixelSkin.
public class HudPlate : MonoBehaviour
{
    public const string NamePrefix = "PxPlate_";

    [SerializeField] private RectTransform target;
    [SerializeField] private float padding = 9f;
    //Folga a mais dos lados: o número não encosta na borda da placa (lista Para Arrumar, item 7)
    [SerializeField] private float sidePadding = 15f;

    private RectTransform rect;
    private Image image;
    private TMP_Text alphaSource;

    public static HudPlate Create(RectTransform target, Sprite sprite)
    {
        if (target == null || target.parent == null || sprite == null) return null;

        Image plate = FeelUI.CreateSliced(NamePrefix + target.name, target.parent, sprite, Color.white);
        plate.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        plate.transform.SetSiblingIndex(target.GetSiblingIndex());

        HudPlate hudPlate = plate.gameObject.AddComponent<HudPlate>();
        hudPlate.target = target;
        hudPlate.padding = 3f * FeelUI.PixelSize;
        hudPlate.sidePadding = 5f * FeelUI.PixelSize;
        hudPlate.Init();
        return hudPlate;
    }

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        rect = (RectTransform)transform;
        image = GetComponent<Image>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        if (target != null) alphaSource = target.GetComponent<TMP_Text>();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Rect bounds = default;
        Rect textRect = default;
        bool found = target.gameObject.activeInHierarchy && TryGetBounds(out bounds, out textRect);
        image.enabled = found;
        if (!found) return;

        //A folga extra vai no lado em que o texto encosta na borda (número à direita do ícone, ou à esquerda da moeda)
        float left = padding + (textRect.width > 0f && textRect.xMin <= bounds.xMin + 1f ? sidePadding : 0f);
        float right = padding + (textRect.width > 0f && textRect.xMax >= bounds.xMax - 1f ? sidePadding : 0f);
        Rect plate = Rect.MinMaxRect(bounds.xMin - left, bounds.yMin - padding, bounds.xMax + right, bounds.yMax + padding);
        rect.localPosition = target.localPosition + (Vector3)plate.center;
        rect.sizeDelta = plate.size;

        //Texto que aparece e some por alfa (FeedbackText): a placa vai junto
        if (alphaSource != null) image.canvasRenderer.SetAlpha(alphaSource.alpha * alphaSource.canvasRenderer.GetAlpha());
    }

    //Retângulo dos gráficos visíveis do alvo, no espaço do alvo, sem escala
    private bool TryGetBounds(out Rect bounds, out Rect textRect)
    {
        bounds = default;
        textRect = default;
        bool any = false;
        bool anyText = false;

        foreach (Graphic graphic in target.GetComponentsInChildren<Graphic>())
        {
            if (!graphic.enabled) continue;

            Rect local;
            if (graphic is TMP_Text text)
            {
                if (string.IsNullOrEmpty(text.text)) continue;
                Bounds textBounds = text.textBounds;
                if (textBounds.size.x <= 0f) continue;
                local = new Rect(textBounds.min, textBounds.size);
            }
            else
            {
                local = graphic.rectTransform.rect;
            }

            Vector2 offset = Vector2.zero;
            for (Transform t = graphic.transform; t != target && t != null; t = t.parent)
            {
                offset += (Vector2)t.localPosition;
            }

            local.position += offset;
            bounds = any ? Union(bounds, local) : local;
            any = true;
            if (graphic is TMP_Text)
            {
                textRect = anyText ? Union(textRect, local) : local;
                anyText = true;
            }
        }

        return any;
    }

    private static Rect Union(Rect a, Rect b)
    {
        return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
    }
}
