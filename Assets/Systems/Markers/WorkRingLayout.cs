using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Círculo de progresso do conserto legível no zoom aberto: no Play o Progress do House.prefab ficava com 0,8 u no
//topo da casa (uns 50 px), e a faixa dourada do mini game (MiniGameFeel, filha dele) quase não aparecia.
//Quando o trabalho começa, centraliza o círculo no prédio, aumenta para uma fração da largura e põe uma trilha
//escura atrás. Só apresentação: escuta WorkStarted e lê o estado da casa. O MapMarkers adiciona este componente.
public class WorkRingLayout : MonoBehaviour
{
    [Header("SIZE (unidades da casa; o prédio médio tem 270 de largura)")]
    [SerializeField] private float widthFraction = .72f;
    [SerializeField] private float minSize = 170f;
    [SerializeField] private float maxSize = 260f;
    [SerializeField] private float trackPadding = 18f;

    [Header("COLORS")]
    [SerializeField] private Color trackColor = new Color(.08f, .07f, .1f, .6f);

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

        Image track = Track(house, parent, ring);
        RectTransform t = track.rectTransform;
        t.anchorMin = t.anchorMax = t.pivot = new Vector2(.5f, .5f);
        t.anchoredPosition = local;
        t.sizeDelta = Vector2.one * (size + trackPadding);
        t.SetSiblingIndex(ring.GetSiblingIndex());
        track.gameObject.SetActive(true);
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
