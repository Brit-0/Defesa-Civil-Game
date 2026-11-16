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

    [Header("SPLASHES (respingos do FeelParticles pela tela, escala = altura da tela)")]
    [SerializeField] private float splashesPerSecond = 30f;
    [SerializeField] private float splashScale = 1f;

    [Header("RAIN TILE (opcional: textura com Wrap Mode Repeat; troca as gotas)")]
    [SerializeField] private Sprite rainTile;
    [SerializeField] private float tileScale = 4f;
    [SerializeField] private Vector2 tileScroll = new Vector2(-.6f, 2.4f);

    [Header("GUSTS (a chuva entorta com o vento do AmbientFx)")]
    [SerializeField] private float gustAngle = 16f;
    [SerializeField] private float gustSpeedBoost = .35f;

    [Header("LIGHTNING")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float flashAlpha = .45f;
    [SerializeField] private float flashTime = .3f;
    [SerializeField] private Color boltColor = new Color(1f, .97f, .7f);
    [Tooltip("Contorno do raio: azul da tempestade")]
    [SerializeField] private Color boltGlowColor = new Color(.45f, .6f, 1f, .7f);
    [SerializeField] private int boltSegments = 7;
    [SerializeField] private int boltBranches = 2;
    [SerializeField] private float boltThickness = 7f;
    [SerializeField] private float boltJitter = 60f;
    [SerializeField] private float boltTime = .25f;
    [SerializeField] private int sparkCount = 12;
    [Tooltip("Escala das faíscas do FeelParticles, em pixels da tela de referência")]
    [SerializeField] private float sparkScale = 300f;

    private RectTransform layer;
    private CanvasGroup rainGroup;
    private float splashDebt;
    private RectTransform[] drops;
    private RawImage tile;
    private Image flash;
    private CanvasGroup boltGroup;
    private RectTransform[] bolt;
    private RectTransform[] boltGlow;
    private RectTransform[] branches;
    private Vector2 fall;
    private float fallAngle;
    private int activeDrops;
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
        //Contorno azul por baixo, galhos e o miolo claro por cima
        bolt = new RectTransform[Mathf.Max(1, boltSegments)];
        boltGlow = new RectTransform[bolt.Length];
        branches = new RectTransform[Mathf.Max(0, boltBranches) * 2];
        for (int i = 0; i < boltGlow.Length; i++) boltGlow[i] = FeelUI.CreateImage("Glow", boltGroup.transform, boltGlowColor).rectTransform;
        for (int i = 0; i < branches.Length; i++) branches[i] = FeelUI.CreateImage("Branch", boltGroup.transform, i % 2 == 0 ? boltGlowColor : boltColor).rectTransform;
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

    //Gotas de pixel: risco de 1 pixel do kit (3 px de tela em 1080p) que acende para baixo, comprimento inteiro
    private void BuildDrops(RectTransform rain)
    {
        SetFallAngle(dropAngle, 0f);

        bool pixel = FeelUI.Pixel != null;
        float unit = FeelUI.PixelSize;
        drops = new RectTransform[Mathf.Max(0, dropCount)];
        for (int i = 0; i < drops.Length; i++)
        {
            drops[i] = FeelUI.CreateImage("Drop", rain, dropColor, pixel ? PixelFx.Drop : null).rectTransform;
            Vector2 size = dropSize * Random.Range(.6f, 1.2f);
            if (pixel) size = new Vector2(unit, Mathf.Max(3f, Mathf.Round(size.y / unit)) * unit);
            drops[i].sizeDelta = size;
            drops[i].localRotation = Quaternion.Euler(0f, 0f, -fallAngle);
        }

        activeDrops = drops.Length;
    }

    private void SetFallAngle(float angle, float boost)
    {
        fallAngle = angle;
        fall = Quaternion.Euler(0f, 0f, -angle) * Vector2.down * dropSpeed * (1f + boost);
    }

    //Menos gotas nos níveis baixos do VfxQuality (as que sobram ficam desligadas)
    private void ApplyDensity()
    {
        if (drops == null) return;

        activeDrops = VfxQuality.Scale(drops.Length, 20);
        for (int i = 0; i < drops.Length; i++) drops[i].gameObject.SetActive(i < activeDrops);
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
            ApplyDensity();
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

        Splash(delta);

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

        //Rajada: a chuva entorta e acelera junto com o vento do AmbientFx
        float gust = AmbientFx.Gust;
        float angle = dropAngle + gust * gustAngle;
        bool turned = Mathf.Abs(angle - fallAngle) > .5f;
        SetFallAngle(turned ? angle : fallAngle, gust * gustSpeedBoost);
        Quaternion rotation = Quaternion.Euler(0f, 0f, -fallAngle);

        for (int i = 0; i < activeDrops; i++)
        {
            RectTransform drop = drops[i];
            if (turned) drop.localRotation = rotation;
            Vector2 position = drop.anchoredPosition + fall * delta;

            //Saiu por baixo ou pelo lado: volta por cima, em outro ponto
            if (position.y < -half.y - margin || Mathf.Abs(position.x) > half.x + margin)
            {
                position = new Vector2(Random.Range(-half.x, half.x) - fall.x * .3f, half.y + margin);
            }

            drop.anchoredPosition = position;
        }
    }

    //Respingos em pontos soltos da tela, dentro da chuva (somem com o fade dela)
    private void Splash(float delta)
    {
        if (!raining || splashesPerSecond <= 0f) return;

        splashDebt += splashesPerSecond * VfxQuality.Density * delta;
        if (splashDebt < 1f) return;

        Transform rain = rainGroup.transform;
        Vector2 half = layer.rect.size * .5f;
        float scale = layer.rect.height * splashScale;

        while (splashDebt >= 1f)
        {
            splashDebt -= 1f;
            Vector2 point = new Vector2(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y));
            FeelParticles.Emit(FxKind.Splash, rain, point, scale);
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

        if (sparkCount > 0) FeelParticles.Emit(FxKind.Sparks, layer, target, sparkScale, sparkCount);
    }

    //Raio em ziguezague do topo da tela até o ponto atingido, com contorno azul e galhos curtos.
    //Espessuras em pixels inteiros do kit, para o raio ser pixel art como o resto.
    private void DrawBolt(Vector2 target)
    {
        float top = layer.rect.size.y * .5f;
        float unit = FeelUI.Pixel != null ? FeelUI.PixelSize : 1f;
        Vector2 from = target;
        Vector2[] joints = new Vector2[bolt.Length + 1];
        joints[0] = target;

        for (int i = 0; i < bolt.Length; i++)
        {
            float t = (i + 1f) / bolt.Length;
            Vector2 to = new Vector2(target.x + Random.Range(-boltJitter, boltJitter) * t, Mathf.Lerp(target.y, top, t));
            float thickness = Mathf.Max(1f, Mathf.Round(boltThickness * Mathf.Lerp(.6f, 1.2f, t) / unit)) * unit;

            Line(bolt[i], from, to, thickness);
            Line(boltGlow[i], from, to, thickness + unit * 2f);

            joints[i + 1] = to;
            from = to;
        }

        //Galho: sai de uma junta do meio para o lado e para baixo
        for (int b = 0; b < branches.Length / 2; b++)
        {
            int joint = Mathf.Clamp(2 + b * 2, 1, joints.Length - 2);
            Vector2 start = joints[joint];
            float side = b % 2 == 0 ? 1f : -1f;
            Vector2 end = start + new Vector2(side * Random.Range(.6f, 1.1f) * boltJitter, -Random.Range(.5f, .9f) * boltJitter);

            Line(branches[b * 2], start, end, unit * 3f);
            Line(branches[b * 2 + 1], start, end, unit);
        }
    }

    private static void Line(RectTransform segment, Vector2 from, Vector2 to, float thickness)
    {
        Vector2 line = to - from;
        segment.anchoredPosition = (from + to) * .5f;
        segment.sizeDelta = new Vector2(thickness, line.magnitude + thickness * .5f);
        segment.localRotation = Quaternion.FromToRotation(Vector3.up, line);
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
