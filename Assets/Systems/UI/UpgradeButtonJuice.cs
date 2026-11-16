using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//Resposta visual do botão de upgrade: brilha enquanto há dinheiro para comprar, pula na compra
//e treme com um clarão vermelho quando a compra é negada. Só escuta eventos; a compra é do UpgradeButton.
[RequireComponent(typeof(UpgradeButton))]
public class UpgradeButtonJuice : MonoBehaviour
{
    [Header("AFFORDABLE GLOW")]
    [SerializeField] private Color glowColor = new Color(1f, .93f, .5f);
    [SerializeField] private float glowMinAlpha = .06f;
    [SerializeField] private float glowMaxAlpha = .3f;
    [SerializeField] private float glowTime = .6f;
    [SerializeField] private float readyPunch = .08f;

    [Header("PURCHASE")]
    [SerializeField] private float buyPunch = .2f;
    [SerializeField] private float buyTime = .35f;
    [SerializeField] private Color buyFlashColor = Color.white;
    [SerializeField] private float buyFlashAlpha = .85f;

    [Header("DENIED")]
    [SerializeField] private float denyAngle = 4f;
    [SerializeField] private float denyTime = .3f;
    [SerializeField] private Color denyColor = new Color(1f, .3f, .25f);
    [SerializeField] private float denyAlpha = .55f;

    private UpgradeButton button;
    private Image glow;
    private Vector3 baseScale;
    private Quaternion baseRotation;
    private bool affordable;

    private void Awake()
    {
        button = GetComponent<UpgradeButton>();
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;

        BuildGlow();
    }

    //Uma Image por cima do fundo e por baixo do conteúdo, com a mesma forma do botão
    private void BuildGlow()
    {
        Image background = GetComponent<Image>();

        glow = FeelUI.CreateImage("Glow", transform, glowColor, background != null ? background.sprite : null);
        if (background != null) glow.type = background.type;
        glow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        glow.transform.SetAsFirstSibling();
        FeelUI.Stretch(glow.rectTransform);
        SetGlow(glowColor, 0f);
    }

    private void OnEnable()
    {
        GameEvents.MoneyChanged += OnMoneyChanged;
        GameEvents.UpgradePurchased += OnUpgradePurchased;
        GameEvents.ActionDenied += OnActionDenied;

        affordable = false;
        Refresh(false);
    }

    private void OnDisable()
    {
        GameEvents.MoneyChanged -= OnMoneyChanged;
        GameEvents.UpgradePurchased -= OnUpgradePurchased;
        GameEvents.ActionDenied -= OnActionDenied;

        ResetTransform();

        if (glow != null)
        {
            glow.DOKill();
            SetGlow(glowColor, 0f);
        }
    }

    //O UpgradesManager entrega o UpgradeData logo depois de instanciar
    private void Start()
    {
        Refresh(false);
    }

    private void OnMoneyChanged(int before, int after)
    {
        Refresh(true);
    }

    private void OnUpgradePurchased(UpgradeData upgrade, int newLevel)
    {
        if (upgrade != null && upgrade == button.Data)
        {
            ResetTransform();
            transform.DOPunchScale(baseScale * buyPunch, buyTime, 6, .6f).SetUpdate(true).SetLink(gameObject);
            FlashGlow(buyFlashColor, buyFlashAlpha, buyTime);
        }

        Refresh(false);
    }

    private void OnActionDenied(DeniedReason reason, Vector3 worldPosition)
    {
        //O UpgradeButton passa a própria posição no TryBuy
        if (reason != DeniedReason.NoMoney && reason != DeniedReason.MaxLevel) return;
        if (worldPosition != transform.position) return;

        ResetTransform();
        transform.DOPunchRotation(new Vector3(0f, 0f, denyAngle), denyTime, 14, 1f).SetUpdate(true).SetLink(gameObject);
        FlashGlow(denyColor, denyAlpha, denyTime);
    }

    private void Refresh(bool announce)
    {
        UpgradesManager manager = UpgradesManager.Instance;
        bool canBuy = manager != null && button.Data != null && manager.CanBuy(button.Data);
        if (canBuy == affordable) return;

        affordable = canBuy;

        if (affordable)
        {
            //Ficou comprável agora: um pulinho chama a atenção
            if (announce)
            {
                ResetTransform();
                transform.DOPunchScale(baseScale * readyPunch, buyTime, 4, .5f).SetUpdate(true).SetLink(gameObject);
            }

            StartGlow();
        }
        else
        {
            glow.DOKill();
            glow.DOFade(0f, glowTime * .5f).SetUpdate(true).SetLink(gameObject);
        }
    }

    private void StartGlow()
    {
        glow.DOKill();
        SetGlow(glowColor, glowMinAlpha);
        glow.DOFade(glowMaxAlpha, glowTime).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
    }

    //Clarão de uma vez (compra, erro); depois o brilho volta ao estado de "dá para comprar" ou apaga
    private void FlashGlow(Color color, float alpha, float time)
    {
        glow.DOKill();
        SetGlow(color, alpha);
        glow.DOFade(0f, time).SetUpdate(true).SetLink(gameObject).OnComplete(() =>
        {
            if (affordable) StartGlow();
        });
    }

    private void SetGlow(Color color, float alpha)
    {
        color.a = alpha;
        glow.color = color;
    }

    private void ResetTransform()
    {
        transform.DOKill();
        transform.localScale = baseScale;
        transform.localRotation = baseRotation;
    }
}
