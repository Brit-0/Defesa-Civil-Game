using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Ambiente da cidade, discreto de propósito: decora sem esconder casa, ícone ou HUD.
//- Vento: Wind e Gust são lidos pela fumaça do fogo, pelo vento e pela chuva da tempestade (DisasterFx chama SetStorm).
//- Folhas: pixels verdes voando com o vento, piscando de lado (espelho, sem girar, para não sair da grade).
//- Poeira de luz: pontinhos creme e dourados no chão, embaixo das casas; somem na tempestade.
//- Nuvens: sombra bem clara passando por cima e o corpo mais claro ainda, deslocado; a sombra escurece na tempestade.
//Tudo acima dos marcadores fica de fora (camada Sky abaixo deles); no zoom afastado o ambiente esmaece.
//Um na cena (Feel.prefab). Quantidade pela VfxQuality: no Baixo só fica uma nuvem.
public class AmbientFx : MonoBehaviour
{
    //x é o sentido e a força do vento: calmo perto de 0,3, tempestade perto de 1
    public static Vector2 Wind { get; private set; } = new Vector2(.3f, -.03f);
    //Rajada da tempestade (0 a 1)
    public static float Gust { get; private set; }
    public static bool Storm { get; private set; }

    public static void SetStorm(bool on) => Storm = on;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Wind = new Vector2(.3f, -.03f);
        Gust = 0f;
        Storm = false;
    }

    [Header("WIND")]
    [SerializeField] private float calmWind = .3f;
    [SerializeField] private float stormWind = 1.1f;
    [Tooltip("Inclinação do vento em graus (negativo desce)")]
    [SerializeField] private float windAngle = -6f;
    [SerializeField] private float windBlend = .6f;
    [SerializeField] private float gustFrequency = .35f;
    [SerializeField, Range(0f, 1f)] private float gustThreshold = .35f;

    [Header("LEAVES (velocidades em casas por segundo)")]
    [SerializeField] private int leaves = 8;
    [SerializeField] private int stormLeaves = 16;
    [SerializeField] private Color[] leafColors = { PixelFx.Green, PixelFx.GreenDark, PixelFx.GoldDark };
    [SerializeField, Range(0f, 1f)] private float leafAlpha = .85f;
    [SerializeField] private float leafSpeed = .8f;
    [SerializeField] private float leafFall = .12f;
    [SerializeField] private float leafSway = .25f;
    [SerializeField] private float leafFlutter = 5f;

    [Header("LIGHT MOTES")]
    [SerializeField] private int motes = 12;
    [SerializeField] private Color[] moteColors = { PixelFx.Cream, PixelFx.Gold };
    [SerializeField, Range(0f, 1f)] private float moteAlpha = .6f;
    [SerializeField] private Vector2 moteLife = new Vector2(3f, 6f);
    [SerializeField] private float moteRise = .08f;
    [SerializeField] private float moteTwinkle = 3f;
    [Tooltip("Fração dos pontinhos que são vaga-lumes (cruz dourada) em vez de poeira (1 pixel)")]
    [SerializeField, Range(0f, 1f)] private float fireflyShare = .3f;

    [Header("CLOUDS (1 pixel da nuvem = cloudPixels pixels da arte)")]
    [SerializeField] private int clouds = 3;
    [SerializeField] private float cloudPixels = 3f;
    [SerializeField] private Color shadowColor = new Color(.08f, .07f, .1f, .09f);
    [SerializeField, Range(0f, 1f)] private float stormShadowAlpha = .16f;
    [SerializeField] private Color bodyColor = new Color(.95f, .92f, .87f, .12f);
    [Tooltip("Corpo da nuvem em relação à sombra, em casas")]
    [SerializeField] private Vector2 bodyOffset = new Vector2(-.6f, 1.4f);
    [SerializeField] private float cloudSpeed = .35f;

    [Header("ZOOM (afastado, o ambiente esmaece para não virar ruído)")]
    [SerializeField] private float farZoomPixels = 1.3f;
    [SerializeField, Range(0f, 1f)] private float farAlpha = .45f;

    private class Bit
    {
        public RectTransform rect;
        public Image image;
        public Vector2 position;
        public float phase;
        public float speed;
        public float age;
        public float life;
        public bool firefly;
        public RectTransform body;
    }

    private readonly List<Bit> leafBits = new List<Bit>();
    private readonly List<Bit> moteBits = new List<Bit>();
    private readonly List<Bit> cloudBits = new List<Bit>();
    private RectTransform skyRoot;
    private RectTransform groundRoot;
    private CanvasGroup skyGroup;
    private CanvasGroup groundGroup;
    private float stormBlend;
    private float moteFade = 1f;

    private float House => PixelFx.HouseUnit;
    private float Pixel => PixelFx.ArtPixel;

    private void OnEnable()
    {
        VfxQuality.LevelChanged += OnLevelChanged;
    }

    private void OnDisable()
    {
        VfxQuality.LevelChanged -= OnLevelChanged;
    }

    private void OnDestroy()
    {
        ResetStatics();
    }

    private void Start()
    {
        RectTransform sky = PixelFx.Sky;
        RectTransform ground = PixelFx.Ground;
        if (sky == null || ground == null) return;

        skyRoot = Layer("Ambient", sky, out skyGroup);
        groundRoot = Layer("AmbientGround", ground, out groundGroup);
        Build();
    }

    private RectTransform Layer(string objectName, RectTransform parent, out CanvasGroup group)
    {
        RectTransform root = FeelUI.CreateRect(objectName, parent);
        FeelUI.Stretch(root);
        root.SetAsFirstSibling();
        group = root.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        return root;
    }

    private void OnLevelChanged(VfxLevel level)
    {
        if (skyRoot != null) Build();
    }

    private int Count(int count)
    {
        return VfxQuality.Level == VfxLevel.Low ? 0 : VfxQuality.Scale(count, 0);
    }

    //Monta (ou remonta, quando a qualidade muda) os pools: quem sobra fica desligado
    private void Build()
    {
        Rect skyView = PixelFx.ViewRect(skyRoot);
        Rect groundView = PixelFx.ViewRect(groundRoot);

        Fill(leafBits, Mathf.Max(Count(leaves), Count(stormLeaves)), () => NewLeaf());
        Fill(moteBits, Count(motes), () => NewMote());
        Fill(cloudBits, VfxQuality.Level == VfxLevel.Low ? 1 : VfxQuality.Scale(clouds, 1), () => NewCloud());

        //As folhas a mais da tempestade começam guardadas (transparentes)
        int calm = Count(leaves);
        for (int i = 0; i < leafBits.Count; i++)
        {
            if (i < calm) RespawnLeaf(leafBits[i], skyView, true);
            else leafBits[i].image.color = Color.clear;
        }

        foreach (Bit mote in moteBits) RespawnMote(mote, groundView, Random.value);
        foreach (Bit cloud in cloudBits) RespawnCloud(cloud, skyView, true);
    }

    private static void Fill(List<Bit> bits, int count, System.Func<Bit> create)
    {
        while (bits.Count < count) bits.Add(create());
        for (int i = 0; i < bits.Count; i++) bits[i].rect.gameObject.SetActive(i < count);
    }

    private Bit NewLeaf()
    {
        Image image = PixelFx.CreateSprite("Leaf", skyRoot, PixelFx.Leaf, Color.white);
        return new Bit { rect = image.rectTransform, image = image };
    }

    private Bit NewMote()
    {
        bool firefly = Random.value < fireflyShare;
        Image image = PixelFx.CreateSprite(firefly ? "Firefly" : "Mote", groundRoot, firefly ? PixelFx.Plus : null, Color.clear);
        return new Bit { rect = image.rectTransform, image = image, firefly = firefly };
    }

    private Bit NewCloud()
    {
        Image shadow = PixelFx.CreateSprite("CloudShadow", skyRoot, PixelFx.Cloud, shadowColor, cloudPixels);
        Image body = PixelFx.CreateSprite("Cloud", shadow.rectTransform, PixelFx.Cloud, bodyColor);
        body.rectTransform.anchorMin = body.rectTransform.anchorMax = new Vector2(.5f, .5f);
        body.rectTransform.sizeDelta = shadow.rectTransform.sizeDelta;
        return new Bit { rect = shadow.rectTransform, image = shadow, body = body.rectTransform };
    }

    private void RespawnLeaf(Bit leaf, Rect view, bool anywhere)
    {
        float x = anywhere ? Random.Range(view.xMin, view.xMax) : (Wind.x >= 0f ? view.xMin : view.xMax);
        leaf.position = new Vector2(x, Random.Range(view.yMin, view.yMax));
        leaf.phase = Random.value * Mathf.PI * 2f;
        leaf.speed = Random.Range(.7f, 1.3f);
        Color color = leafColors.Length > 0 ? leafColors[Random.Range(0, leafColors.Length)] : PixelFx.Green;
        leaf.image.color = PixelFx.WithAlpha(color, leafAlpha);
    }

    private void RespawnMote(Bit mote, Rect view, float startAge)
    {
        mote.position = new Vector2(Random.Range(view.xMin, view.xMax), Random.Range(view.yMin, view.yMax));
        mote.life = Random.Range(moteLife.x, moteLife.y);
        mote.age = startAge * mote.life;
        mote.phase = Random.value * Mathf.PI * 2f;
        Color color = mote.firefly ? PixelFx.Gold : moteColors[Random.Range(0, moteColors.Length)];
        mote.image.color = PixelFx.WithAlpha(color, 1f);
    }

    private void RespawnCloud(Bit cloud, Rect view, bool anywhere)
    {
        float width = cloud.rect.sizeDelta.x;
        float x = anywhere ? Random.Range(view.xMin, view.xMax) : (Wind.x >= 0f ? view.xMin - width : view.xMax + width);
        cloud.position = new Vector2(x, Random.Range(view.yMin, view.yMax));
        cloud.speed = Random.Range(.8f, 1.2f);
        cloud.body.anchoredPosition = Snap(bodyOffset * House);
    }

    private Vector2 Snap(Vector2 position)
    {
        float pixel = Pixel;
        return pixel > 0f ? new Vector2(Mathf.Round(position.x / pixel) * pixel, Mathf.Round(position.y / pixel) * pixel) : position;
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        UpdateWind(delta);
        if (skyRoot == null) return;

        //Zoom afastado: o ambiente esmaece e não disputa com casas e ícones
        float zoom = PixelFx.ScreenPixelsPerArtPixel(skyRoot);
        float alpha = zoom < farZoomPixels ? farAlpha : 1f;
        skyGroup.alpha = Mathf.MoveTowards(skyGroup.alpha, alpha, delta * 2f);
        groundGroup.alpha = skyGroup.alpha;

        Rect skyView = PixelFx.ViewRect(skyRoot, House);
        UpdateLeaves(skyView, delta);
        UpdateClouds(skyView, delta);
        UpdateMotes(PixelFx.ViewRect(groundRoot), delta);
    }

    private void UpdateWind(float delta)
    {
        stormBlend = Mathf.MoveTowards(stormBlend, Storm ? 1f : 0f, windBlend * delta);

        float noise = Mathf.PerlinNoise(Time.time * gustFrequency, .37f);
        Gust = Mathf.Clamp01((noise - gustThreshold) / Mathf.Max(.01f, 1f - gustThreshold)) * stormBlend;

        float strength = Mathf.Lerp(calmWind, stormWind, stormBlend) * (1f + Gust * .6f);
        float angle = windAngle * Mathf.Deg2Rad;
        Wind = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * strength;
    }

    private void UpdateLeaves(Rect view, float delta)
    {
        int wanted = Mathf.RoundToInt(Mathf.Lerp(Count(leaves), Count(stormLeaves), stormBlend));
        float house = House;

        for (int i = 0; i < leafBits.Count; i++)
        {
            Bit leaf = leafBits[i];
            if (!leaf.rect.gameObject.activeSelf) continue;

            //Folha a mais da tempestade: só some quando sai da tela
            bool extra = i >= wanted;
            leaf.phase += delta * leafFlutter * leaf.speed;
            Vector2 velocity = new Vector2(Wind.x * leafSpeed, Wind.y - leafFall) * house * leaf.speed;
            velocity.y += Mathf.Sin(leaf.phase * .5f) * leafSway * house;
            leaf.position += velocity * delta;

            leaf.rect.anchoredPosition = Snap(leaf.position);
            float flip = Mathf.Sin(leaf.phase);
            leaf.rect.localScale = new Vector3(flip >= 0f ? 1f : -1f, Mathf.Abs(flip) > .3f ? 1f : -1f, 1f);

            if (view.Contains(leaf.position)) continue;
            if (extra) leaf.image.color = Color.clear;
            else RespawnLeaf(leaf, view, false);
        }

        //Folhas que voltaram a ser pedidas aparecem de novo pela borda
        for (int i = 0; i < Mathf.Min(wanted, leafBits.Count); i++)
        {
            if (leafBits[i].image.color.a <= 0f) RespawnLeaf(leafBits[i], view, false);
        }
    }

    private void UpdateMotes(Rect view, float delta)
    {
        moteFade = Mathf.MoveTowards(moteFade, Storm ? 0f : 1f, delta);
        float house = House;

        foreach (Bit mote in moteBits)
        {
            if (!mote.rect.gameObject.activeSelf) continue;

            mote.age += delta;
            if (mote.age >= mote.life)
            {
                RespawnMote(mote, view, 0f);
                continue;
            }

            mote.phase += delta * moteTwinkle;
            mote.position += new Vector2(Mathf.Sin(mote.phase * .37f) * .5f, 1f) * moteRise * house * delta;
            mote.rect.anchoredPosition = Snap(mote.position);

            //Nasce e some devagar; o vaga-lume pisca, a poeira só cintila
            float t = mote.age / mote.life;
            float envelope = Mathf.Clamp01(Mathf.Min(t, 1f - t) * 4f);
            float twinkle = mote.firefly ? Mathf.Clamp01(Mathf.Sin(mote.phase) * 1.5f) : .7f + .3f * Mathf.Sin(mote.phase * 1.7f);
            mote.image.canvasRenderer.SetAlpha(moteAlpha * envelope * twinkle * moteFade);
        }
    }

    private void UpdateClouds(Rect view, float delta)
    {
        float house = House;
        float shadowAlpha = Mathf.Lerp(shadowColor.a, stormShadowAlpha, stormBlend);

        foreach (Bit cloud in cloudBits)
        {
            if (!cloud.rect.gameObject.activeSelf) continue;

            cloud.position += Wind * cloudSpeed * house * cloud.speed * delta;
            cloud.rect.anchoredPosition = Snap(cloud.position);
            cloud.image.color = PixelFx.WithAlpha(shadowColor, shadowAlpha);

            float width = cloud.rect.sizeDelta.x;
            bool gone = Wind.x >= 0f ? cloud.position.x > view.xMax + width : cloud.position.x < view.xMin - width;
            if (gone) RespawnCloud(cloud, view, false);
        }
    }
}
