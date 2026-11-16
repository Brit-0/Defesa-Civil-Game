using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

//Desastres no chão da cidade. Um na cena (Feel.prefab); só escuta GameEvents.
//- Terremoto: rachadura que abre no epicentro e ondas de choque cor de poeira que correm pela cidade no tempo da onda
//  do DisasterManager (earthquakeWaveDelayPerUnit), levantando poeira por cima das casas na frente da onda.
//- Tempestade: rajadas de vento (riscos claros que cruzam a tela, por cima das casas) e, em cada raio, um clarão azul
//  no chão do ponto atingido. A chuva e o raio da tela continuam no WeatherFx.
//Current diz o desastre em andamento (o HouseStatusFx usa para saber se o dano é de terremoto ou de tempestade).
public class DisasterFx : MonoBehaviour
{
    public static DisasterType? Current { get; private set; }

    [Header("EARTHQUAKE (tamanhos em pixels da arte)")]
    [SerializeField] private Color quakeColor = new Color(.88f, .8f, .66f, .9f);
    [SerializeField] private int waveCount = 3;
    [SerializeField] private float waveGap = .35f;
    [Tooltip("Raio final da onda em tamanhos de casa")]
    [SerializeField] private float waveRadius = 9f;
    [SerializeField] private float fallbackWaveSpeed = 6f;
    [SerializeField] private float dustPerSecond = 26f;
    [SerializeField] private float dustScale = .8f;
    [SerializeField] private int epicenterCrackPixels = 44;
    [SerializeField] private float epicenterCrackTime = 8f;

    [Header("STORM WIND (riscos por cima das casas)")]
    [SerializeField] private Color gustColor = new Color(.85f, .92f, 1f, .35f);
    [SerializeField] private int gustStreaks = 14;
    [SerializeField] private Vector2 gustLength = new Vector2(10f, 22f);
    [Tooltip("Velocidade em tamanhos de casa por segundo")]
    [SerializeField] private float gustSpeed = 9f;

    [Header("LIGHTNING (no chão)")]
    [SerializeField] private Color strikeColor = new Color(.62f, .78f, 1f, .9f);
    [SerializeField] private float strikeTime = .5f;
    [SerializeField] private float strikeScale = 2.4f;

    //Onda feita de traços de pixel em volta de uma elipse: continua nítida em qualquer raio
    private class Wave
    {
        public RectTransform root;
        public CanvasGroup group;
        public List<RectTransform> dashes = new List<RectTransform>();
        public float[] angles;
        public Vector2 center;
        public float age;
        public float delay;
        public float speed;
        public float maxRadius;
        public float dustDebt;
        public bool dusty;
    }

    private class Streak
    {
        public RectTransform rect;
        public Image image;
        public float speed;
    }

    [SerializeField] private int dashesPerWave = 56;

    private readonly List<Wave> waves = new List<Wave>();
    private readonly Stack<RectTransform> dashIdle = new Stack<RectTransform>();
    private readonly List<Streak> streaks = new List<Streak>();
    private RectTransform windRoot;
    private CanvasGroup windGroup;
    private bool windy;

    private static float HouseSize => PixelFx.HouseUnit;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Current = null;
    }

    //O GameEvents limpa os assinantes no SubsystemRegistration; este vem depois e vale para todas as cenas
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void TrackDisasters()
    {
        GameEvents.DisasterStarted += (disaster, epicenter) => Current = disaster != null ? disaster.type : (DisasterType?)null;
        GameEvents.DisasterEnded += disaster => Current = null;
        GameEvents.GameStateChanged += state => { if (state == GameState.Menu) Current = null; };
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
        if (disaster == null) return;

        if (disaster.type == DisasterType.Earthquake) Quake(epicenter);
        if (disaster.type == DisasterType.Storm) SetWind(true);
    }

    private void OnDisasterEnded(DisasterData disaster)
    {
        if (disaster != null && disaster.type == DisasterType.Storm) SetWind(false);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.Playing && state != GameState.Paused) SetWind(false);
    }

    #region EARTHQUAKE

    private void Quake(Vector3 worldEpicenter)
    {
        RectTransform ground = PixelFx.Ground;
        if (ground == null) return;

        Vector2 center = PixelFx.ToLocal(ground, worldEpicenter);
        float house = HouseSize;

        //A onda anda na mesma velocidade das casas quebrando (DisasterManager)
        float delayPerUnit = Stats.Balance != null ? Stats.Balance.earthquakeWaveDelayPerUnit : 0f;
        float worldPerLocal = Mathf.Abs(ground.lossyScale.x);
        float speed = delayPerUnit > 0f ? 1f / (delayPerUnit * Mathf.Max(1e-6f, worldPerLocal)) : fallbackWaveSpeed * house;

        for (int i = 0; i < Mathf.Max(1, waveCount); i++)
        {
            Wave wave = new Wave
            {
                center = center,
                delay = i * waveGap,
                speed = speed * (1f - i * .08f),
                maxRadius = waveRadius * house * (1f - i * .15f),
                dusty = i == 0,
            };
            BuildWave(ground, wave);
            waves.Add(wave);
        }

        EpicenterCrack(ground, center);
    }

    private void BuildWave(RectTransform ground, Wave wave)
    {
        wave.root = FeelUI.CreateRect("QuakeWave", ground);
        FeelUI.Place(wave.root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), wave.center, Vector2.zero);
        wave.group = wave.root.gameObject.AddComponent<CanvasGroup>();
        wave.group.alpha = 0f;

        float pixel = PixelFx.ArtPixel;
        int count = VfxQuality.Scale(dashesPerWave, 16);
        wave.angles = new float[count];

        for (int i = 0; i < count; i++)
        {
            //Falhas na onda: um traço em cada 7 fica de fora
            float angle = (i + Random.Range(-.25f, .25f)) / count * Mathf.PI * 2f;
            wave.angles[i] = angle;

            RectTransform dash = dashIdle.Count > 0 ? dashIdle.Pop() : null;
            if (dash == null) dash = FeelUI.CreateImage("Dash", wave.root, Color.white).rectTransform;
            dash.SetParent(wave.root, false);
            dash.anchorMin = dash.anchorMax = new Vector2(.5f, .5f);
            dash.gameObject.SetActive(i % 7 != 3);
            //Traço com contorno escuro: aparece na praça clara e no asfalto
            Image image = dash.GetComponent<Image>();
            image.sprite = PixelFx.Dash(Random.Range(3, 6));
            image.color = i % 2 == 0 ? quakeColor : PixelFx.WithAlpha(PixelFx.Cream, quakeColor.a);
            dash.sizeDelta = image.sprite.rect.size * pixel;
            //Traço deitado na tangente da elipse
            dash.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(Mathf.Cos(angle) * .62f, -Mathf.Sin(angle)) * Mathf.Rad2Deg);
            wave.dashes.Add(dash);
        }
    }

    private void ReleaseWave(Wave wave)
    {
        foreach (RectTransform dash in wave.dashes)
        {
            if (dash == null) continue;
            dash.gameObject.SetActive(false);
            dash.SetParent(transform, false);
            dashIdle.Push(dash);
        }

        if (wave.root != null) Destroy(wave.root.gameObject);
    }

    private void EpicenterCrack(RectTransform ground, Vector2 center)
    {
        Image crack = PixelFx.CreateSprite("EpicenterCrack", ground, PixelFx.Crack(epicenterCrackPixels, Random.Range(0, 9)), Color.white);
        crack.rectTransform.anchoredPosition = center;
        crack.rectTransform.localScale = Vector3.zero;

        DOTween.Sequence().SetLink(crack.gameObject)
            .Append(crack.rectTransform.DOScale(1f, .35f).SetEase(Ease.OutBack))
            .AppendInterval(epicenterCrackTime)
            .Append(crack.DOFade(0f, 1f))
            .OnComplete(() => Destroy(crack.gameObject));
    }

    private void UpdateWaves(float delta)
    {
        RectTransform sky = waves.Count > 0 ? PixelFx.Sky : null;
        float house = HouseSize;

        for (int i = waves.Count - 1; i >= 0; i--)
        {
            Wave wave = waves[i];
            if (wave.root == null)
            {
                waves.RemoveAt(i);
                continue;
            }

            wave.age += delta;
            float time = wave.age - wave.delay;
            if (time < 0f) continue;

            float radius = time * wave.speed;
            float t = radius / Mathf.Max(1f, wave.maxRadius);
            if (t >= 1f)
            {
                ReleaseWave(wave);
                waves.RemoveAt(i);
                continue;
            }

            //Elipse achatada: o chão é visto de cima e um pouco de frente
            for (int d = 0; d < wave.dashes.Count; d++)
            {
                float angle = wave.angles[d];
                wave.dashes[d].anchoredPosition = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * .62f);
            }
            wave.group.alpha = Mathf.Min(1f, time * 8f) * (1f - t * t);

            //Poeira na frente da onda, por cima das casas (só a primeira onda levanta poeira)
            if (sky == null || !wave.dusty) continue;

            wave.dustDebt += dustPerSecond * VfxQuality.Density * delta;
            while (wave.dustDebt >= 1f)
            {
                wave.dustDebt -= 1f;
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 point = wave.center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * .62f);
                FeelParticles.Emit(FxKind.Dust, sky, point, house * dustScale, 1);
            }
        }
    }

    #endregion

    #region STORM

    private void SetWind(bool on)
    {
        if (windy == on) return;

        windy = on;
        AmbientFx.SetStorm(on);

        if (on && windRoot == null) BuildWind();
        if (windGroup == null) return;

        windGroup.DOKill();
        if (on)
        {
            windRoot.gameObject.SetActive(true);
            Scatter();
            windGroup.DOFade(1f, 1f).SetLink(windRoot.gameObject);
        }
        else
        {
            windGroup.DOFade(0f, 1f).SetLink(windRoot.gameObject).OnComplete(() => windRoot.gameObject.SetActive(false));
        }
    }

    private void BuildWind()
    {
        RectTransform sky = PixelFx.Sky;
        if (sky == null) return;

        windRoot = FeelUI.CreateRect("Wind", sky);
        FeelUI.Stretch(windRoot);
        windGroup = windRoot.gameObject.AddComponent<CanvasGroup>();
        windGroup.alpha = 0f;
        windGroup.blocksRaycasts = false;

        int count = VfxQuality.Scale(gustStreaks, 4);
        for (int i = 0; i < count; i++)
        {
            Image image = FeelUI.CreateImage("Gust", windRoot, gustColor);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
            streaks.Add(new Streak { rect = image.rectTransform, image = image });
        }
    }

    private void Scatter()
    {
        Rect view = PixelFx.ViewRect(windRoot);
        foreach (Streak streak in streaks) Respawn(streak, view, true);
    }

    //Risco de 1 pixel de altura, comprido, em linha com o vento
    private void Respawn(Streak streak, Rect view, bool anywhere)
    {
        float pixel = PixelFx.ArtPixel;
        Vector2 wind = AmbientFx.Wind;
        float length = Mathf.Round(Random.Range(gustLength.x, gustLength.y)) * pixel;

        streak.rect.sizeDelta = new Vector2(length, pixel);
        streak.rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(wind.y, wind.x) * Mathf.Rad2Deg);
        streak.speed = gustSpeed * HouseSize * Random.Range(.7f, 1.3f);

        float x = anywhere ? Random.Range(view.xMin, view.xMax) : (wind.x >= 0f ? view.xMin - length : view.xMax + length);
        streak.rect.anchoredPosition = new Vector2(x, Random.Range(view.yMin, view.yMax));
        streak.image.color = PixelFx.WithAlpha(gustColor, gustColor.a * Random.Range(.5f, 1f));
    }

    private void UpdateWind(float delta)
    {
        if (windRoot == null || !windRoot.gameObject.activeSelf) return;

        Rect view = PixelFx.ViewRect(windRoot, HouseSize);
        Vector2 direction = AmbientFx.Wind.normalized;
        if (direction == Vector2.zero) direction = Vector2.right;

        foreach (Streak streak in streaks)
        {
            Vector2 position = streak.rect.anchoredPosition + direction * streak.speed * delta;
            streak.rect.anchoredPosition = position;
            if (!view.Contains(position)) Respawn(streak, view, false);
        }
    }

    private void OnLightningStruck(Vector3 worldPosition)
    {
        RectTransform ground = PixelFx.Ground;
        if (ground == null) return;

        Image glow = FeelUI.CreateImage("Strike", ground, strikeColor, PixelFx.Glow(28, 18, 4));
        glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(.5f, .5f);
        glow.rectTransform.anchoredPosition = PixelFx.ToLocal(ground, worldPosition);
        glow.rectTransform.sizeDelta = new Vector2(HouseSize * strikeScale, HouseSize * strikeScale * .64f);
        glow.rectTransform.localScale = Vector3.one * .4f;

        DOTween.Sequence().SetLink(glow.gameObject)
            .Append(glow.rectTransform.DOScale(1f, strikeTime * .3f).SetEase(Ease.OutQuad))
            .Join(glow.DOFade(0f, strikeTime).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(glow.gameObject));
    }

    #endregion

    private void Update()
    {
        float delta = Time.deltaTime;
        if (waves.Count > 0) UpdateWaves(delta);
        UpdateWind(delta);
    }

    [Button]
    private void DebugQuake()
    {
        Camera cam = Camera.main;
        Quake(cam != null ? cam.transform.position : Vector3.zero);
    }

    [Button]
    private void DebugWind(bool on = true)
    {
        SetWind(on);
    }
}
