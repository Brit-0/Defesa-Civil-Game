using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

//Texto "+N" que nasce na casa quando o chamado é coletado e voa até o contador de dinheiro do HUD
public class MoneyPopups : MonoBehaviour
{
    [Header("REFERENCES (vazio = monta sozinho)")]
    [SerializeField] private TMP_Text popupPf;
    [SerializeField] private Transform moneyTarget;

    [Header("LOOK")]
    [SerializeField] private float fontSize = 46f;
    [SerializeField] private Color moneyColor = new Color(1f, .87f, .25f);
    [SerializeField] private Color incomeColor = new Color(.55f, .95f, 1f);
    [SerializeField] private float outlineWidth = .25f;

    [Header("TIMING")]
    [SerializeField] private float popTime = .25f;
    [SerializeField] private float holdTime = .3f;
    [SerializeField] private float flyTime = .45f;
    [SerializeField] private float riseDistance = 60f;
    [SerializeField] private float endScale = .5f;

    [Header("PASSIVE INCOME")]
    [SerializeField] private string incomeFormat = "+{0} Repasse";
    [SerializeField] private Vector2 incomeOffset = new Vector2(-40f, -130f);

    private const string LayerName = "FeelPopups";

    private readonly Stack<TMP_Text> pool = new Stack<TMP_Text>();

    private void OnEnable()
    {
        GameEvents.CallResolved += OnCallResolved;
        GameEvents.PassiveIncomePaid += OnPassiveIncomePaid;
    }

    private void OnDisable()
    {
        GameEvents.CallResolved -= OnCallResolved;
        GameEvents.PassiveIncomePaid -= OnPassiveIncomePaid;
    }

    private void OnCallResolved(Call call, int moneyEarned)
    {
        if (moneyEarned <= 0 || call == null || call.house == null) return;

        Spawn("+" + moneyEarned, FeelUI.WorldToHud(call.house.transform.position), moneyColor);
    }

    //O Repasse da Prefeitura não vem de uma casa: nasce perto do contador e sobe até ele
    private void OnPassiveIncomePaid(int amount)
    {
        if (amount <= 0) return;

        Transform target = GetTarget();
        if (target == null) return;

        Spawn(string.Format(incomeFormat, amount), FeelUI.HudPosition(target) + incomeOffset, incomeColor);
    }

    private Transform GetTarget()
    {
        if (moneyTarget != null) return moneyTarget;

        return HudJuice.Instance != null ? HudJuice.Instance.MoneyTarget : null;
    }

    private void Spawn(string text, Vector2 from, Color color)
    {
        RectTransform layer = FeelUI.HudLayer(LayerName);
        if (layer == null) return;

        TMP_Text popup = GetPopup(layer);
        popup.text = text;
        popup.color = color;

        Transform target = GetTarget();
        Vector2 top = from + Vector2.up * riseDistance;
        Vector2 to = target != null ? FeelUI.HudPosition(target) : top + Vector2.up * riseDistance;

        RectTransform rect = popup.rectTransform;
        rect.anchoredPosition = from;
        rect.localScale = Vector3.zero;

        Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(popup.gameObject);
        sequence.Append(rect.DOScale(1f, popTime).SetEase(Ease.OutBack));
        sequence.Join(rect.DOAnchorPos(top, popTime).SetEase(Ease.OutCubic));
        sequence.AppendInterval(holdTime);
        sequence.Append(rect.DOAnchorPos(to, flyTime).SetEase(Ease.InBack));
        sequence.Join(rect.DOScale(endScale, flyTime).SetEase(Ease.InQuad));
        sequence.OnComplete(() =>
        {
            Release(popup);
            if (HudJuice.Instance != null) HudJuice.Instance.PunchMoney();
        });
    }

    private TMP_Text GetPopup(RectTransform layer)
    {
        while (pool.Count > 0)
        {
            TMP_Text pooled = pool.Pop();
            if (pooled == null) continue;

            pooled.gameObject.SetActive(true);
            pooled.transform.SetAsLastSibling();
            return pooled;
        }

        TMP_Text popup;

        if (popupPf != null)
        {
            popup = Instantiate(popupPf, layer);
        }
        else
        {
            popup = FeelUI.CreateText("MoneyPopup", layer, "", fontSize, moneyColor);
            popup.fontStyle = FontStyles.Bold;
            popup.outlineColor = Color.black;
            popup.outlineWidth = outlineWidth;
        }

        popup.rectTransform.anchorMin = popup.rectTransform.anchorMax = new Vector2(.5f, .5f);
        return popup;
    }

    private void Release(TMP_Text popup)
    {
        if (popup == null) return;

        popup.gameObject.SetActive(false);
        pool.Push(popup);
    }
}
