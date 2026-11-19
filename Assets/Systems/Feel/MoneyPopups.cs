using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//Coleta: várias moedinhas pulam da casa e voam em arco até o contador de dinheiro do HUD, sem número
//(lista Para Arrumar, item 21). A Coleta de Impostos, que não vem de uma casa, continua com o texto "+N Impostos".
public class MoneyPopups : MonoBehaviour
{
    [Header("REFERENCES (vazio = monta sozinho)")]
    [SerializeField] private TMP_Text popupPf;
    [SerializeField] private Transform moneyTarget;

    [Header("COINS (coleta)")]
    [SerializeField] private Vector2Int coinCount = new Vector2Int(4, 12);
    [Tooltip("Moedas por raiz quadrada do valor: 25 dá 6 moedas com 1,2")]
    [SerializeField] private float coinsPerSqrtMoney = 1.2f;
    [Tooltip("Unidades do HUD por pixel da moeda (2 = moeda de 16 px com 32 unidades, menor que a do contador)")]
    [SerializeField] private float coinPixel = 2f;
    [SerializeField] private Vector2 burstRadius = new Vector2(40f, 95f);
    [SerializeField] private float burstTime = .28f;
    [SerializeField] private float coinStagger = .06f;
    [SerializeField] private float coinFlyTime = .55f;
    [SerializeField, Range(0f, 1f)] private float coinTickVolume = .22f;

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
    [SerializeField] private string incomeFormat = "+{0} Impostos";
    [SerializeField] private Vector2 incomeOffset = new Vector2(-40f, -130f);

    private const string LayerName = "FeelPopups";

    private readonly Stack<TMP_Text> pool = new Stack<TMP_Text>();
    private readonly Stack<Image> coinPool = new Stack<Image>();

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

        Vector2 from = FeelUI.WorldToHud(call.house.transform.position);
        Sprite coin = FeelUI.Pixel != null ? FeelUI.Pixel.iconCoin : null;
        if (coin == null)
        {
            Spawn("+" + moneyEarned, from, moneyColor);
            return;
        }

        int count = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(moneyEarned) * coinsPerSqrtMoney), coinCount.x, coinCount.y);
        for (int i = 0; i < count; i++) SpawnCoin(coin, from, i, count);
    }

    //Moeda pula para um ponto em volta da casa e depois voa até o contador; x e y com curvas diferentes fazem o arco
    private void SpawnCoin(Sprite sprite, Vector2 from, int index, int count)
    {
        RectTransform layer = FeelUI.HudLayer(LayerName);
        Transform target = GetTarget();
        if (layer == null || target == null) return;

        Image coin = GetCoin(layer, sprite);
        RectTransform rect = coin.rectTransform;
        rect.anchoredPosition = from;
        rect.localScale = Vector3.zero;

        float angle = (index / (float)count) * Mathf.PI * 2f + Random.Range(-.3f, .3f);
        Vector2 burst = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .7f + .35f) * Random.Range(burstRadius.x, burstRadius.y);
        Vector2 to = FeelUI.HudPosition(target);
        bool last = index == count - 1;

        Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(coin.gameObject);
        sequence.Append(rect.DOAnchorPos(burst, burstTime).SetEase(Ease.OutCubic));
        sequence.Join(rect.DOScale(1f, burstTime).SetEase(Ease.OutBack));
        sequence.AppendInterval(coinStagger * index);
        sequence.Append(rect.DOAnchorPosX(to.x, coinFlyTime).SetEase(Ease.InSine));
        sequence.Join(rect.DOAnchorPosY(to.y, coinFlyTime).SetEase(Ease.InQuad));
        sequence.Join(rect.DOScale(.8f, coinFlyTime).SetEase(Ease.InQuad));
        sequence.OnComplete(() =>
        {
            ReleaseCoin(coin);
            if (HudJuice.Instance != null) HudJuice.Instance.PunchMoney();
            //Tique de moeda a cada duas que chegam, mais a última: chuva de "plins" sem embolar
            if (index % 2 == 0 || last) AudioManager.PlaySfx(AudioManager.Sfx?.coin, coinTickVolume, .12f);
        });
    }

    private Image GetCoin(RectTransform layer, Sprite sprite)
    {
        while (coinPool.Count > 0)
        {
            Image pooled = coinPool.Pop();
            if (pooled == null) continue;

            pooled.gameObject.SetActive(true);
            pooled.transform.SetAsLastSibling();
            return pooled;
        }

        Image coin = FeelUI.CreateImage("Coin", layer, Color.white, sprite);
        coin.raycastTarget = false;
        coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = new Vector2(.5f, .5f);
        coin.rectTransform.sizeDelta = sprite.rect.size * coinPixel;
        return coin;
    }

    private void ReleaseCoin(Image coin)
    {
        if (coin == null) return;

        coin.gameObject.SetActive(false);
        coinPool.Push(coin);
    }

    //A Coleta de Impostos não vem de uma casa: nasce perto do contador e sobe até ele
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
