using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

//Tempestade na tela: chuva em overlay enquanto o desastre Storm dura e clarão com raio em LightningStruck.
//Um na cena (Feel.prefab). Só escuta eventos; desenha numa camada do HUD atrás dos contadores.
//Chuva e raio são feitos de Image até a arte chegar (Rain_Tile.png entra no campo rainTile).
public class WeatherFx : MonoBehaviour
{
    [Header("RAIN")]
    [SerializeField] private Color tintColor = new Color(.08f, .13f, .3f, .22f);
    [SerializeField] private Color dropColor = new Color(.75f, .85f, 1f, .55f);
    [SerializeField] private int dropCount = 80;
    [SerializeField] private Vector2 dropSize = new Vector2(3f, 30f);
    [SerializeField] private float dropAngle = 14f;
    [SerializeField] private float dropSpeed = 1500f;
    [SerializeField] private float rainFadeTime = 1f;

    [Header("RAIN TILE (opcional: textura com Wrap Mode Repeat; troca as gotas)")]
    [SerializeField] private Sprite rainTile;
    [SerializeField] private float tileScale = 4f;
    [SerializeField] private Vector2 tileScroll = new Vector2(-.6f, 2.4f);

    [Header("LIGHTNING")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float flashAlpha = .45f;
    [SerializeField] private float flashTime = .3f;
    [SerializeField] private Color boltColor = new Color(1f, .97f, .7f);
    [SerializeField] private int boltSegments = 7;
    [SerializeField] private float boltThickness = 7f;
    [SerializeField] private float boltJitter = 60f;
    [SerializeField] private float boltTime = .25f;
    [SerializeField] private int sparkCount = 8;
    [SerializeField] private float sparkSize = 10f;
    [SerializeField] private float sparkDistance = 70f;

    private RectTransform layer;
    private CanvasGroup rainGroup;
    private RectTransform[] drops;
    private RawImage tile;
    private Image flash;
    private CanvasGroup boltGroup;
    private RectTransform[] bolt;
    private Vector2 fall;
    private bool raining;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        layer = FeelUI.HudLayer("Weather");
        if (layer == null) return;

        //Atrás do HUD e num Canvas próprio: as gotas se mexem todo frame sem refazer o HUD
        layer.SetAsFirstSibling();
        layer.gameObject.AddComponent<Canvas>();

        RectTransform rain = FeelUI.CreateRect("Rain", layer);
        FeelUI.Stretch(rain);
        rainGroup = rain.gameObject.AddComponent<CanvasGroup>();
        rainGroup.alpha = 0f;
        rainGroup.blocksRaycasts = false;

        FeelUI.Stretch(FeelUI.CreateImage("Tint", rain, tintColor).rectTransform);

        if (rainTile != null) BuildTile(rain);
        else BuildDrops(rain);

        rain.gameObject.SetActive(false);

        boltGroup = FeelUI.CreateRect("Bolt", layer).gameObject.AddComponent<CanvasGroup>();
        boltGroup.alpha = 0f;
        boltGroup.blocksRaycasts = false;
        bolt = new RectTransform[Mathf.Max(1, boltSegments)];
        for (int i = 0; i < bolt.Length; i++) bolt[i] = FeelUI.CreateImage("Segment", boltGroup.transform, boltColor).rectTransform;

        flash = FeelUI.CreateImage("Flash", layer, flashColor);
        FeelUI.Stretch(flash.rectTransform);
        SetFlash(0f);
    }

    private void BuildTile(RectTransform rain)
    {
        tile = FeelUI.CreateRect("Tile", rain).gameObject.AddComponent<RawImage>();
        tile.texture = rainTile.texture;
        tile.color = dropColor;
        tile.raycastTarget = false;
        FeelUI.Stretch(tile.rectTransform);
    }

    private void BuildDrops(RectTransform rain)
    {
        Quaternion rotation = Quaternion.Euler(0f, 0f, -dropAngle);
        fall = rotation * Vector2.down * dropSpeed;

        drops = new RectTransform[Mathf.Max(0, dropCount)];
        for (int i = 0; i < drops.Length; i++)
        {
            drops[i] = FeelUI.CreateImage("Drop", rain, dropColor).rectTransform;
            drops[i].sizeDelta = dropSize * Random.Range(.6f, 1.2f);
            drops[i].localRotation = rotation;
        }
    }

    private void OnEnable()
    {
        GameEvents.DisasterStarted += OnDisasterStarted;
        GameEvents.DisasterEnded += OnDisasterEnded;
        GameEvents.LightningStruck += OnLightningStruck;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.DisasterStarted -= OnDisasterStarted;
        GameEvents.DisasterEnded -= OnDisasterEnded;
        GameEvents.LightningStruck -= OnLightningStruck;
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    private void OnDisasterStarted(DisasterData disaster, Vector3 epicenter)
    {
        if (disaster != null && disaster.type == DisasterType.Storm) SetRain(true);
    }

    private void OnDisasterEnded(DisasterData disaster)
    {
        if (disaster != null && disaster.type == DisasterType.Storm) SetRain(false);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.Playing && state != GameState.Paused) SetRain(false);
    }

    private void SetRain(bool on)
    {
        if (rainGroup == null || raining == on) return;

        raining = on;
        GameObject rain = rainGroup.gameObject;
        rainGroup.DOKill();

        if (on)
        {
            rain.SetActive(true);
            Scatter();
            rainGroup.DOFade(1f, rainFadeTime).SetLink(rain);
        }
        else
        {
            rainGroup.DOFade(0f, rainFadeTime).SetLink(rain).OnComplete(() => rain.SetActive(false));
        }
    }

    //Espalha as gotas pela tela inteira no começo da chuva
    private void Scatter()
    {
        if (drops == null) return;

        Vector2 half = layer.rect.size * .5f;
        foreach (RectTransform drop in drops)
        {
            drop.anchoredPosition = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
        }
    }

    private void Update()
    {
        if (rainGroup == null || !rainGroup.gameObject.activeSelf) return;

        float delta = Time.deltaTime;

        if (tile != null)
        {
            Vector2 size = layer.rect.size;
            float pixels = Mathf.Max(1f, rainTile.texture.width * tileScale);
            Rect uv = tile.uvRect;
            uv.size = size / pixels;
            uv.position += tileScroll * delta;
            tile.uvRect = uv;
            return;
        }

        Vector2 half = layer.rect.size * .5f;
        float margin = dropSize.y;

        foreach (RectTransform drop in drops)
        {
            Vector2 position = drop.anchoredPosition + fall * delta;

            //Saiu por baixo ou pelo lado: volta por cima, em outro ponto
            if (position.y < -half.y - margin || Mathf.Abs(position.x) > half.x + margin)
            {
                position = new Vector2(Random.Range(-half.x, half.x) - fall.x * .3f, half.y + margin);
            }

            drop.anchoredPosition = position;
        }
    }

    private void OnLightningStruck(Vector3 worldPosition)
    {
        if (layer == null) return;

        Vector2 target = FeelUI.WorldToHud(worldPosition);

        //Clarão em dois tempos
        flash.DOKill();
        SetFlash(flashAlpha);
        DOTween.Sequence().SetTarget(flash).SetLink(flash.gameObject)
            .Append(flash.DOFade(flashAlpha * .2f, flashTime * .2f))
            .Append(flash.DOFade(flashAlpha * .7f, flashTime * .1f))
            .Append(flash.DOFade(0f, flashTime * .7f));

        DrawBolt(target);
        boltGroup.DOKill();
        boltGroup.alpha = 1f;
        boltGroup.DOFade(0f, boltTime).SetEase(Ease.InQuad).SetLink(boltGroup.gameObject);

        if (sparkCount > 0) FeelFx.Burst(layer, target, sparkCount, boltColor, sparkSize, sparkDistance, boltTime * 2f, .4f);
    }

    //Raio em ziguezague do topo da tela até o ponto atingido
    private void DrawBolt(Vector2 target)
    {
        float top = layer.rect.size.y * .5f;
        Vector2 from = target;

        for (int i = 0; i < bolt.Length; i++)
        {
            float t = (i + 1f) / bolt.Length;
            Vector2 to = new Vector2(target.x + Random.Range(-boltJitter, boltJitter) * t, Mathf.Lerp(target.y, top, t));
            Vector2 line = to - from;

            bolt[i].anchoredPosition = (from + to) * .5f;
            bolt[i].sizeDelta = new Vector2(boltThickness * Mathf.Lerp(.6f, 1.2f, t), line.magnitude + boltThickness * .5f);
            bolt[i].localRotation = Quaternion.FromToRotation(Vector3.up, line);

            from = to;
        }
    }

    private void SetFlash(float alpha)
    {
        Color color = flashColor;
        color.a = alpha;
        flash.color = color;
    }

    [Button]
    private void DebugRain(bool on = true)
    {
        SetRain(on);
    }

    [Button]
    private void DebugLightning()
    {
        OnLightningStruck(Camera.main != null ? Camera.main.transform.position + Vector3.down : Vector3.zero);
    }
}
