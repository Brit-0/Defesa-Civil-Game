using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Base ou armazém sem energia (docs/jam/ENERGIA_FOGO.md): tomada riscada piscando em cima do prédio, sobre um círculo
// escuro como os ícones de estado das casas. Camada no fundo do HUD, embaixo das placas. Só escuta PowerChanged.
public class EnergyMarkers : MonoBehaviour
{
    [SerializeField] private float markerSize = 60f;
    [SerializeField] private Color markerBack = new Color(.08f, .07f, .1f, .85f);
    [SerializeField] private Color fallbackColor = new Color(1f, .32f, .25f, 1f);
    [SerializeField] private float blinkSpeed = 6f;

    private const string LayerName = "EnergyMarkers";

    private RectTransform layer;
    private readonly Dictionary<House, Image> markers = new();
    private readonly List<House> scratch = new();

    private void OnEnable()
    {
        GameEvents.PowerChanged += OnPowerChanged;
    }

    private void OnDisable()
    {
        GameEvents.PowerChanged -= OnPowerChanged;
    }

    private void OnPowerChanged(House location, bool powered)
    {
        if (location == null) return;

        if (powered)
        {
            if (markers.TryGetValue(location, out Image marker) && marker != null) Destroy(marker.gameObject);
            markers.Remove(location);
            return;
        }

        if (markers.ContainsKey(location) || !EnsureLayer()) return;

        Image back = FeelUI.CreateCircle("NoPower_" + location.name, layer, markerBack);
        back.rectTransform.sizeDelta = Vector2.one * markerSize;
        back.gameObject.AddComponent<CanvasGroup>();

        Sprite off = FireEnergyArt.Load(FireEnergyArt.Off);
        Image icon = FeelUI.CreateImage("Icon", back.transform, off != null ? Color.white : fallbackColor, off != null ? off : FeelUI.Theme?.iconClose);
        icon.preserveAspect = true;
        icon.rectTransform.sizeDelta = Vector2.one * markerSize * .8f;

        back.rectTransform.localScale = Vector3.zero;
        back.rectTransform.DOScale(1f, .3f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(back.gameObject);
        markers[location] = back;
    }

    private bool EnsureLayer()
    {
        if (layer != null) return true;

        layer = FeelUI.HudLayer(LayerName);
        if (layer == null) return false;

        layer.SetAsFirstSibling();
        return true;
    }

    private void Update()
    {
        if (markers.Count == 0) return;

        float blink = .65f + .35f * Mathf.Sin(GameClock.Time * blinkSpeed);
        scratch.Clear();
        scratch.AddRange(markers.Keys);
        foreach (House location in scratch)
        {
            Image marker = markers[location];
            if (location == null || marker == null)
            {
                if (marker != null) Destroy(marker.gameObject);
                markers.Remove(location);
                continue;
            }

            Vector2 center = MiniGameFeel.HudCenter(location.transform, out Vector2 size);
            marker.rectTransform.anchoredPosition = center + new Vector2(size.x * .3f, size.y * .5f);
            if (marker.TryGetComponent(out CanvasGroup group)) group.alpha = blink;
        }
    }
}
