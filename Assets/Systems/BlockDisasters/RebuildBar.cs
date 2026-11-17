using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra da reconstrução do meteoro (docs/jam/ENCHENTE_METEORO.md): em cima do canteiro, larga, com a velocidade da
// obra ("x1,5") e setas que acendem com a aceleração. Cada acerto em sequência pisca a barra e soma uma seta; erro
// apaga as setas pela metade. Só lê o BlockIncident (Boost) e o House (WorkProgress01).
public class RebuildBar : MonoBehaviour
{
    [Header("BARRA")]
    [Tooltip("Tamanho em pixels da arte da cidade.")]
    [SerializeField] private Vector2 barSize = new(64f, 7f);
    [Tooltip("Tamanho de cada pixel da barra em relação à arte da cidade.")]
    [SerializeField] private float barPixels = 1.6f;
    [Tooltip("Altura da barra acima do meio da cratera, em pixels da arte.")]
    [SerializeField] private float barLift = 0f;
    [SerializeField] private Color barBack = new(0f, 0f, 0f, .75f);
    [SerializeField] private Color barSlow = new(.95f, .62f, .2f);
    [SerializeField] private Color barFast = new(1f, .9f, .35f);
    [SerializeField] private Color hitFlash = Color.white;

    [Header("VELOCIDADE")]
    [SerializeField] private string speedFormat = "OBRA x{0:0.0}";
    [SerializeField] private float labelSize = 11f;
    [Tooltip("Setas da aceleração: uma acende a cada acerto, até a aceleração máxima.")]
    [SerializeField] private int chevrons = 4;
    [SerializeField] private Color chevronOff = new(1f, 1f, 1f, .2f);
    [SerializeField] private Color chevronOn = new(1f, .85f, .2f, 1f);
    [SerializeField] private int order = 10010;

    private class View
    {
        public BlockIncident incident;
        public RectTransform root;
        public Image fill;
        public TextMeshProUGUI label;
        public readonly List<TextMeshProUGUI> arrows = new();
    }

    private readonly List<View> views = new();

    private void OnEnable()
    {
        GameEvents.WorkStarted += OnWorkStarted;
        GameEvents.SkillCheck += OnSkillCheck;
        GameEvents.BlockIncidentResolved += OnResolved;
    }

    private void OnDisable()
    {
        GameEvents.WorkStarted -= OnWorkStarted;
        GameEvents.SkillCheck -= OnSkillCheck;
        GameEvents.BlockIncidentResolved -= OnResolved;
    }

    private void OnWorkStarted(Call call)
    {
        if (call == null || call.rule is not BlockIncident incident || incident.kind != IncidentKind.Meteor) return;
        if (views.Exists(v => v.incident == incident)) return;
        RectTransform sky = PixelFx.Sky;
        if (sky == null) return;

        float pixel = PixelFx.ArtPixel * barPixels;
        View view = new() { incident = incident, root = FeelUI.CreateRect("Obra do meteoro", sky) };
        Canvas above = view.root.gameObject.AddComponent<Canvas>();
        above.overrideSorting = true;
        above.sortingLayerID = above.rootCanvas.sortingLayerID;
        above.sortingOrder = order;

        Image back = FeelUI.CreateImage("Barra", view.root, barBack);
        back.raycastTarget = false;
        back.rectTransform.sizeDelta = barSize * pixel;
        view.fill = FeelUI.CreateImage("Fill", back.transform, barSlow);
        view.fill.raycastTarget = false;
        RectTransform fill = view.fill.rectTransform;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.pivot = new Vector2(0f, .5f);
        fill.offsetMin = new Vector2(pixel, pixel);
        fill.offsetMax = new Vector2(-pixel, -pixel);
        fill.localScale = new Vector3(0f, 1f, 1f);

        view.label = FeelUI.CreateText("Velocidade", view.root, string.Format(speedFormat, 1f), labelSize * pixel, FeelUI.PixelCream, TextAlignmentOptions.Left);
        view.label.raycastTarget = false;
        FeelUI.Outline(view.label, .3f);
        RectTransform label = view.label.rectTransform;
        label.sizeDelta = new Vector2(barSize.x * .6f, labelSize + 2f) * pixel;
        label.anchoredPosition = new Vector2(-barSize.x * .2f, barSize.y * .5f + labelSize * .6f + 1f) * pixel;

        for (int i = 0; i < chevrons; i++)
        {
            TextMeshProUGUI arrow = FeelUI.CreateText("Seta", view.root, ">", labelSize * pixel, chevronOff);
            arrow.raycastTarget = false;
            FeelUI.Outline(arrow, .3f);
            arrow.rectTransform.sizeDelta = new Vector2(labelSize, labelSize + 2f) * pixel;
            arrow.rectTransform.anchoredPosition = new Vector2(barSize.x * .5f - (chevrons - i - .5f) * labelSize * .55f, barSize.y * .5f + labelSize * .6f + 1f) * pixel;
            view.arrows.Add(arrow);
        }

        view.root.localScale = Vector3.zero;
        view.root.DOScale(1f, .25f).SetEase(Ease.OutBack).SetLink(view.root.gameObject);
        views.Add(view);
        Place(view);
    }

    private void Update()
    {
        BlockDisasterData data = BlockDisasterData.Instance;
        for (int i = views.Count - 1; i >= 0; i--)
        {
            View view = views[i];
            House leader = view.incident.leader;
            if (view.root == null || leader == null || view.incident.resolved || (!leader.IsWorking && !leader.IsComplete))
            {
                Close(view);
                continue;
            }

            Place(view);
            float progress = leader.IsComplete ? 1f : leader.WorkProgress01;
            float boost = view.incident.Boost;
            float speed = BlockDisasterRules.RebuildSpeed(boost);
            float k = data.rebuildMaxBoost > 0f ? Mathf.Clamp01(boost / data.rebuildMaxBoost) : 0f;
            view.fill.rectTransform.localScale = new Vector3(progress, 1f, 1f);
            if (!DOTween.IsTweening(view.fill)) view.fill.color = Color.Lerp(barSlow, barFast, k);
            view.label.text = string.Format(speedFormat, speed);

            int lit = Mathf.CeilToInt(k * view.arrows.Count - .01f);
            float wave = GameClock.Time * Mathf.Lerp(4f, 14f, k);
            for (int a = 0; a < view.arrows.Count; a++)
            {
                bool on = a < lit;
                Color color = on ? chevronOn : chevronOff;
                if (on) color.a = .65f + .35f * Mathf.Sin(wave - a * .9f);
                view.arrows[a].color = color;
            }
        }
    }

    // No meio da cratera, longe do círculo de progresso do chamado (que fica no canteiro).
    private void Place(View view)
    {
        if (view.root.parent is not RectTransform layer) return;

        view.root.anchoredPosition = PixelFx.ToLocal(layer, view.incident.Center) + Vector2.up * barLift * PixelFx.ArtPixel;
    }

    // Acerto: a barra pisca e dá um pulo; erro: treme.
    private void OnSkillCheck(House house, SkillResult result)
    {
        if (result != SkillResult.RebuildHit && result != SkillResult.RebuildMiss) return;

        View view = views.Find(v => v.incident.leader == house);
        if (view == null || view.root == null) return;

        view.root.DOKill(true);
        if (result == SkillResult.RebuildHit)
        {
            view.fill.DOKill();
            view.fill.color = hitFlash;
            view.fill.DOColor(barFast, .3f).SetLink(view.fill.gameObject);
            view.root.DOPunchScale(Vector3.one * .18f, .25f, 6).SetLink(view.root.gameObject);
        }
        else
        {
            view.root.DOShakeRotation(.25f, new Vector3(0f, 0f, 8f), 30).SetLink(view.root.gameObject);
        }
    }

    private void OnResolved(BlockIncident incident)
    {
        View view = views.Find(v => v.incident == incident);
        if (view != null) Close(view);
    }

    private void Close(View view)
    {
        views.Remove(view);
        if (view.root == null) return;

        GameObject root = view.root.gameObject;
        view.root.DOKill();
        view.root.DOScale(0f, .2f).SetEase(Ease.InBack).SetLink(root).OnComplete(() => Destroy(root));
    }
}
