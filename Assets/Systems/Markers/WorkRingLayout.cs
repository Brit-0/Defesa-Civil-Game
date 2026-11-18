using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Círculo de progresso do conserto legível no zoom aberto: no Play o Progress do House.prefab ficava com 0,8 u no
//topo da casa (uns 50 px), e a faixa dourada do mini game (MiniGameFeel, filha dele) quase não aparecia.
//Quando o trabalho começa, centraliza o círculo no prédio, aumenta para uma fração da largura e põe uma trilha
//escura atrás. Só apresentação: escuta WorkStarted e lê o estado da casa. O MapMarkers adiciona este componente.
//Lista Para Arrumar, item 22: o anel liso e fino virou anel de pixel grosso (PixelFx.Ring, contorno escuro), verde
//vivo sobre trilha escura, começando no topo, maior e com o ícone do chamado no meio.
public class WorkRingLayout : MonoBehaviour
{
    [Header("SIZE (unidades da casa; o prédio médio tem 270 de largura)")]
    [SerializeField] private float widthFraction = .86f;
    [SerializeField] private float minSize = 210f;
    [SerializeField] private float maxSize = 300f;
    [SerializeField] private float trackPadding = 0f;
    [Tooltip("Ícone do chamado no meio do anel, em fração do anel")]
    [SerializeField] private float iconFraction = .46f;

    [Header("PIXEL RING (pixels da arte)")]
    [SerializeField] private int ringPixels = 46;
    [SerializeField] private int ringThickness = 9;

    [Header("COLORS")]
    [SerializeField] private Color trackColor = new Color(.08f, .07f, .1f, .85f);
    [SerializeField] private Color fillColor = new Color(.36f, .9f, .37f, 1f);

    private readonly Dictionary<House, Image> tracks = new Dictionary<House, Image>();

    private void OnEnable() => GameEvents.WorkStarted += OnWorkStarted;
    private void OnDisable() => GameEvents.WorkStarted -= OnWorkStarted;

    private void OnWorkStarted(Call call)
    {
        House house = call != null ? call.house : null;
        if (house == null || house.ProgressCircle == null) return;

        RectTransform ring = house.ProgressCircle.rectTransform;
        var parent = ring.parent as RectTransform;
        var visual = house.transform.Find("Visual") as RectTransform;
        if (parent == null || visual == null) return;

        Vector3 center = visual.TransformPoint(visual.rect.center);
        Vector2 local = (Vector2)parent.InverseTransformPoint(center) - parent.rect.center;
        float toParent = visual.lossyScale.x / Mathf.Max(1e-6f, parent.lossyScale.x);
        float size = Mathf.Clamp(visual.rect.width * toParent * widthFraction, minSize, maxSize);

        ring.anchorMin = ring.anchorMax = ring.pivot = new Vector2(.5f, .5f);
        ring.anchoredPosition = local;
        ring.sizeDelta = Vector2.one * size;
        StyleRing(house.ProgressCircle, size);

        Image track = Track(house, parent, ring);
        track.sprite = house.ProgressCircle.sprite;
        RectTransform t = track.rectTransform;
        t.anchorMin = t.anchorMax = t.pivot = new Vector2(.5f, .5f);
        t.anchoredPosition = local;
        t.sizeDelta = Vector2.one * (size + trackPadding);
        PlaceBehind(t, ring);
        track.gameObject.SetActive(true);
    }

    // Anel de pixel no lugar do sprite liso, enchendo a partir do topo. A janela do mini game (CritWindow, do
    // MiniGameFeel) copia o sprite e a origem do círculo: se ela já existe, recebe os novos aqui.
    private void StyleRing(Image circle, float size)
    {
        Sprite pixelRing = PixelFx.Ring(ringPixels, ringPixels, ringThickness, RingStyle.Solid);
        circle.sprite = pixelRing;
        circle.type = Image.Type.Filled;
        circle.fillMethod = Image.FillMethod.Radial360;
        circle.fillOrigin = (int)Image.Origin360.Top;
        circle.preserveAspect = true;
        circle.color = fillColor;

        Transform crit = circle.transform.Find("CritWindow");
        Image arc = crit != null ? crit.GetComponent<Image>() : null;
        if (arc != null)
        {
            arc.sprite = pixelRing;
            arc.fillOrigin = circle.fillOrigin;
            arc.preserveAspect = true;
        }

        Transform icon = circle.transform.Find("Icon");
        if (icon is RectTransform iconRect)
        {
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(.5f, .5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.one * (size * iconFraction);
            Image iconImage = icon.GetComponent<Image>();
            if (iconImage != null) iconImage.preserveAspect = true;
        }
    }

    // Trilha logo antes do anel (desenhada atrás). SetSiblingIndex(índice do anel) com a trilha já antes dele, a partir
    // da 2ª obra na mesma casa, a punha DEPOIS: a trilha escura (85%) cobria o anel e a janela dourada (v5F).
    public static void PlaceBehind(Transform track, Transform ring)
    {
        int ringIndex = ring.GetSiblingIndex();
        track.SetSiblingIndex(track.GetSiblingIndex() < ringIndex ? ringIndex - 1 : ringIndex);
    }

    private Image Track(House house, RectTransform parent, RectTransform ring)
    {
        if (tracks.TryGetValue(house, out Image track) && track != null) return track;

        var go = new GameObject("ProgressTrack", typeof(RectTransform));
        go.layer = ring.gameObject.layer;
        go.transform.SetParent(parent, false);
        track = go.AddComponent<Image>();
        track.sprite = house.ProgressCircle.sprite;
        track.preserveAspect = house.ProgressCircle.preserveAspect;
        track.color = trackColor;
        track.raycastTarget = false;
        tracks[house] = track;
        return track;
    }

    // A trilha some junto com o círculo (a House liga e desliga o Progress).
    private void LateUpdate()
    {
        foreach (KeyValuePair<House, Image> pair in tracks)
        {
            if (pair.Key == null || pair.Value == null) continue;
            bool on = pair.Key.ProgressCircle != null && pair.Key.ProgressCircle.gameObject.activeSelf && pair.Key.IsWorking;
            if (pair.Value.gameObject.activeSelf != on) pair.Value.gameObject.SetActive(on);
        }
    }
}
