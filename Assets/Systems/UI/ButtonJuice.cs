using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//Hover e clique de um botão de UI: escala pelo Tweener e som de hover. O som de clique é global (GameAudio).
[RequireComponent(typeof(Tweener))]
public class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("SOUND")]
    [SerializeField, Range(0f, 1f)] private float hoverVolume = .35f;
    [SerializeField] private float hoverPitchVariation = .05f;

    [Header("STATE")]
    [Tooltip("Cresce e afunda mesmo desabilitado: a linha de melhoria sem dinheiro também responde (o clique dá o tremor vermelho)")]
    [SerializeField] private bool reactWhenDisabled;

    private Tweener tweener;
    private Selectable selectable;

    private bool IsActive => reactWhenDisabled || selectable == null || selectable.IsInteractable();

    public bool ReactWhenDisabled
    {
        get => reactWhenDisabled;
        set => reactWhenDisabled = value;
    }

    private void Awake()
    {
        tweener = GetComponent<Tweener>();
        selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsActive) return;

        tweener.HoverEnter();
        AudioManager.PlaySfx(AudioManager.Sfx?.hover, hoverVolume, hoverPitchVariation);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tweener.HoverExit();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !IsActive) return;

        tweener.Hold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        tweener.Release();
    }
}
