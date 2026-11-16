using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Microanimação de qualquer botão, sem mexer na escala nem na rotação dele (que podem ser do Tweener ou do
//UpgradeButtonJuice): o texto sobe um pouco no hover, afunda no aperto e o botão dá um clarão no clique.
//O ActionFeedback põe um em cada botão da cena em runtime; os botões do C já nascem com ele.
public class ButtonPress : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("LABEL (unidades do HUD)")]
    [SerializeField] private float hoverLift = 3f;
    [SerializeField] private float pressDepth = 5f;
    [SerializeField] private float moveTime = .08f;

    [Header("CLICK FLASH")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float flashAlpha = .35f;
    [SerializeField] private float flashTime = .25f;

    private Selectable selectable;
    private RectTransform label;
    private Vector2 labelPosition;
    private Image flash;
    private bool hovered;
    private bool pressed;

    private bool IsActive => selectable == null || selectable.IsInteractable();

    private void Awake()
    {
        selectable = GetComponent<Selectable>();

        //Só mexe no texto quando há um texto só (no botão de upgrade, nome e preço ficam onde estão)
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length == 1)
        {
            label = texts[0].rectTransform;
            labelPosition = label.anchoredPosition;
        }

        BuildFlash();
    }

    //Por cima do fundo, com o mesmo sprite e o mesmo corte, para o clarão ter o formato do botão
    private void BuildFlash()
    {
        Image background = selectable != null ? selectable.targetGraphic as Image : GetComponent<Image>();
        if (background == null) return;

        //O sprite pixel já vem colorido: o clarão usa a máscara branca do mesmo formato
        flash = FeelUI.CreateImage("ClickFlash", background.transform, flashColor, FeelUI.Silhouette(background.sprite));
        flash.type = background.type;
        flash.pixelsPerUnitMultiplier = background.pixelsPerUnitMultiplier;
        flash.preserveAspect = background.preserveAspect;
        FeelUI.Stretch(flash.rectTransform);
        //Logo acima do fundo: o texto continua por cima do clarão
        flash.transform.SetAsFirstSibling();
        flash.canvasRenderer.SetAlpha(0f);
    }

    private void OnDisable()
    {
        hovered = false;
        pressed = false;

        if (label != null)
        {
            label.DOKill();
            label.anchoredPosition = labelPosition;
        }

        if (flash != null) flash.canvasRenderer.SetAlpha(0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        if (IsActive) MoveLabel();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        MoveLabel();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !IsActive) return;

        pressed = true;
        MoveLabel();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        pressed = false;
        MoveLabel();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !IsActive || flash == null) return;

        flash.canvasRenderer.SetAlpha(flashAlpha);
        flash.CrossFadeAlpha(0f, flashTime, true);
    }

    private void MoveLabel()
    {
        if (label == null) return;

        float offset = pressed ? -pressDepth : hovered && IsActive ? hoverLift : 0f;
        label.DOKill();
        label.DOAnchorPos(labelPosition + Vector2.up * offset, moveTime).SetUpdate(true).SetLink(gameObject);
    }
}
