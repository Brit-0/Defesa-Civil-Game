using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

//Resposta às ações do jogador que não dependem de um objeto só. Um na cena (Feel.prefab):
//em ActionDenied mostra um texto curto no lugar do clique (o som de erro é do GameAudio) e
//garante o UpgradeButtonJuice em todo botão de upgrade, sem precisar mexer no UpgradeButton.prefab.
public class ActionFeedback : MonoBehaviour
{
    [Header("DENIED TEXTS")]
    [SerializeField] private string noWorkersText = "Sem trabalhadores!";
    [SerializeField] private string noMaterialText = "Sem material!";
    [SerializeField] private string noMoneyText = "Sem dinheiro!";
    [SerializeField] private string maxLevelText = "Nível máximo!";

    [Header("DENIED POPUP (unidades do HUD, tela de referência 1920x1080)")]
    [SerializeField] private float fontSize = 38f;
    [SerializeField] private float outlineWidth = .3f;
    [SerializeField] private Color textColor = new Color(1f, .42f, .36f);
    [SerializeField] private Vector2 offset = new Vector2(0f, 46f);
    [SerializeField] private Vector2 screenMargin = new Vector2(190f, 40f);
    [SerializeField] private float popTime = .12f;
    [SerializeField] private float shakeStrength = 14f;
    [SerializeField] private float shakeTime = .25f;
    [SerializeField] private float riseDistance = 50f;
    [SerializeField] private float riseTime = .55f;

    private readonly Stack<TMP_Text> pool = new Stack<TMP_Text>();
    private RectTransform layer;

    private void OnEnable()
    {
        GameEvents.ActionDenied += OnActionDenied;
    }

    private void OnDisable()
    {
        GameEvents.ActionDenied -= OnActionDenied;
    }

    //Os botões de upgrade nascem no Start do UpgradesManager: espera um frame
    private IEnumerator Start()
    {
        yield return null;

        foreach (UpgradeButton button in FindObjectsByType<UpgradeButton>(FindObjectsInactive.Include))
        {
            if (button.GetComponent<UpgradeButtonJuice>() == null) button.gameObject.AddComponent<UpgradeButtonJuice>();
        }
    }

    private void OnActionDenied(DeniedReason reason, Vector3 worldPosition)
    {
        string text = TextFor(reason);
        if (string.IsNullOrEmpty(text)) return;

        if (layer == null) layer = FeelUI.HudLayer("FeelPopups");
        if (layer == null) return;

        TMP_Text label = pool.Count > 0 ? pool.Pop() : NewLabel();
        RectTransform rect = label.rectTransform;

        //Dentro da tela, mesmo com o clique na borda (aba de upgrades)
        Vector2 half = layer.rect.size * .5f - screenMargin;
        Vector2 position = FeelUI.WorldToHud(worldPosition) + offset;
        position.x = Mathf.Clamp(position.x, -half.x, half.x);
        position.y = Mathf.Clamp(position.y, -half.y, half.y);

        label.text = text;
        label.color = textColor;
        label.gameObject.SetActive(true);
        rect.SetAsLastSibling();
        rect.anchoredPosition = position;
        rect.localScale = Vector3.zero;

        Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(label.gameObject);
        sequence.Append(rect.DOScale(1f, popTime).SetEase(Ease.OutBack));
        sequence.Append(rect.DOShakeAnchorPos(shakeTime, new Vector2(shakeStrength, 0f), 20, 0f));
        sequence.Append(rect.DOAnchorPosY(position.y + riseDistance, riseTime).SetEase(Ease.OutQuad));
        sequence.Join(label.DOFade(0f, riseTime).SetEase(Ease.InQuad));
        sequence.OnComplete(() =>
        {
            label.gameObject.SetActive(false);
            pool.Push(label);
        });
    }

    private TMP_Text NewLabel()
    {
        TMP_Text label = FeelUI.CreateText("Denied", layer, "", fontSize, textColor);
        FeelUI.Outline(label, outlineWidth);
        return label;
    }

    private string TextFor(DeniedReason reason)
    {
        switch (reason)
        {
            case DeniedReason.NoWorkers: return noWorkersText;
            case DeniedReason.NoMaterial: return noMaterialText;
            case DeniedReason.NoMoney: return noMoneyText;
            case DeniedReason.MaxLevel: return maxLevelText;
            default: return null;
        }
    }
}
