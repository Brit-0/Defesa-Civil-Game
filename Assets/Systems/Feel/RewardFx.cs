using Sirenix.OdinInspector;
using UnityEngine;

//Recompensa em dourado, para o jogador sentir e entender o que ganhou:
//- Chamado resolvido: brilho dourado na casa (as moedas que voam ao contador são do MoneyPopups); reposição traz brilhos.
//- Dinheiro entrando: brilhos no contador do HUD (a contagem e o pulo do número são do HudJuice).
//- Upgrade comprado: brilho e confete pequeno no botão; no nível máximo, confete grande.
//- Marco a cada N casas consertadas: confete dos dois cantos e brilhos no topo da tela.
//Só escuta GameEvents (MoneyPopups faz as moedas voando; isto é só o brilho). Um na cena (Feel.prefab).
public class RewardFx : MonoBehaviour
{
    private const string LayerName = "RewardFx";

    [Header("COLLECT (no mundo; escala em tamanhos da casa)")]
    [SerializeField] private float coinScale = 1f;
    [SerializeField] private int restockGlints = 6;

    [Header("COUNTER")]
    [SerializeField] private int counterGlints = 4;
    [SerializeField] private float counterScale = 120f;

    [Header("UPGRADE")]
    [SerializeField] private int upgradeGlints = 6;
    [SerializeField] private float upgradeGlintScale = 120f;
    [SerializeField] private int upgradeConfetti = 16;
    [Tooltip("Escala do confete em fração da altura do HUD (a receita do Confetti é para a tela inteira)")]
    [SerializeField] private float upgradeConfettiScale = .5f;
    [SerializeField] private float maxLevelMultiplier = 2f;

    [Header("MILESTONE")]
    [Tooltip("A cada quantas casas consertadas comemora (0 = nunca)")]
    [SerializeField] private int milestoneEvery = 5;
    [SerializeField, Range(0f, 1f)] private float milestoneConfettiShare = .6f;
    [SerializeField] private float cannonTilt = 25f;
    [SerializeField] private int milestoneGlints = 10;

    private int milestonesSeen = -1;
    private bool moneySeen;

    private void OnEnable()
    {
        GameEvents.CallResolved += OnCallResolved;
        GameEvents.MoneyChanged += OnMoneyChanged;
        GameEvents.UpgradePurchased += OnUpgradePurchased;
    }

    private void OnDisable()
    {
        GameEvents.CallResolved -= OnCallResolved;
        GameEvents.MoneyChanged -= OnMoneyChanged;
        GameEvents.UpgradePurchased -= OnUpgradePurchased;
    }

    private void OnCallResolved(Call call, int money)
    {
        if (call == null || call.house == null) return;

        RectTransform sky = PixelFx.Sky;
        if (sky == null) return;

        Vector3 at = call.house.transform.position;
        float scale = PixelFx.HouseUnit * coinScale;

        if (call.type == CallType.Restock)
        {
            FeelParticles.EmitAt(FxKind.Glint, sky, at, scale, VfxQuality.Scale(restockGlints));
            FeelParticles.EmitColored(FxKind.Burst, sky, PixelFx.ToLocal(sky, at), scale, PixelFx.Gold, VfxQuality.Scale(8));
        }

        if (money <= 0) return;

        //As moedas que voam até o contador são do MoneyPopups; aqui fica só o brilho na casa
        FeelParticles.EmitAt(FxKind.Glint, sky, at, scale, VfxQuality.Scale(restockGlints));
    }

    private void OnMoneyChanged(int before, int after)
    {
        //O primeiro valor é o dinheiro inicial, não um ganho (o HudJuice também o mostra sem contagem)
        if (!moneySeen)
        {
            moneySeen = true;
            return;
        }

        if (after <= before) return;

        Transform target = HudJuice.Instance != null ? HudJuice.Instance.MoneyTarget : null;
        RectTransform layer = FeelUI.HudLayer(LayerName);
        if (target == null || layer == null) return;

        FeelParticles.Emit(FxKind.Glint, layer, FeelUI.HudPosition(target), counterScale, VfxQuality.Scale(counterGlints));
    }

    private void OnUpgradePurchased(UpgradeData upgrade, int level)
    {
        RectTransform layer = FeelUI.HudLayer(LayerName);
        if (layer == null) return;

        Vector2 at = ButtonPosition(upgrade, layer);
        bool maxed = upgrade != null && level >= upgrade.maxLevel;
        float boost = maxed ? maxLevelMultiplier : 1f;

        FeelParticles.Emit(FxKind.Glint, layer, at, upgradeGlintScale * boost, VfxQuality.Scale(upgradeGlints));
        FeelParticles.Emit(FxKind.Confetti, layer, at, layer.rect.height * upgradeConfettiScale * boost, VfxQuality.Scale(Mathf.RoundToInt(upgradeConfetti * boost)), 90f);
        if (maxed) FeelParticles.EmitColored(FxKind.Burst, layer, at, upgradeGlintScale * boost, PixelFx.Gold);
    }

    //Onde está o botão desse upgrade no HUD; sem botão, embaixo no meio da tela
    private static Vector2 ButtonPosition(UpgradeData upgrade, RectTransform layer)
    {
        foreach (UpgradeButton button in FindObjectsByType<UpgradeButton>())
        {
            if (button != null && button.Data == upgrade && button.isActiveAndEnabled) return FeelUI.HudPosition(button.transform);
        }

        return new Vector2(0f, -layer.rect.height * .3f);
    }

    //O contador de casas é do GameManager: confere aqui, sem depender da ordem de quem escuta o CallResolved
    private void Update()
    {
        GameManager game = GameManager.Instance;
        if (game == null || milestoneEvery <= 0) return;

        int reached = game.HousesRepaired / milestoneEvery;
        if (milestonesSeen < 0 || reached < milestonesSeen)
        {
            milestonesSeen = reached;
            return;
        }

        if (reached == milestonesSeen) return;

        milestonesSeen = reached;
        Celebrate();
    }

    private void Celebrate()
    {
        RectTransform layer = FeelUI.HudLayer(LayerName);
        if (layer == null) return;

        float width = layer.rect.width;
        float height = layer.rect.height;
        int confetti = Mathf.RoundToInt(FxPresets.Confetti().count.y * milestoneConfettiShare);

        FeelParticles.Emit(FxKind.Confetti, layer, new Vector2(-width * .45f, -height * .5f), height, VfxQuality.Scale(confetti), 90f - cannonTilt);
        FeelParticles.Emit(FxKind.Confetti, layer, new Vector2(width * .45f, -height * .5f), height, VfxQuality.Scale(confetti), 90f + cannonTilt);
        FeelParticles.Emit(FxKind.Glint, layer, new Vector2(0f, height * .35f), height * .5f, VfxQuality.Scale(milestoneGlints), float.NaN, width * .6f);
    }

    [Button]
    private void DebugMilestone() => Celebrate();
}
