using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

//Fogo de uma casa em OnFire: halo de luz que tremula, chamas em 3 camadas (fundo vermelho, meio laranja, frente
//amarela), fumaça, brasas e a barra do temporizador (House.FireTimeLeft01). Quanto menos tempo resta, mais alto,
//mais vermelho e mais rápido o fogo tremula: a urgência se lê sem a barra. Criado pelo HouseFeedback na primeira vez
//que a casa pega fogo; ligado e desligado por ele. Fire_6x32.png fatiado em fireFrames troca as chamas de quadradinhos.
public class HouseFire : MonoBehaviour
{
    [System.Serializable]
    public class Settings
    {
        [Header("FLAMES (frações do tamanho da casa)")]
        public Sprite[] fireFrames;
        public float frameRate = 10f;
        public float spriteSize = .8f;
        public Color flameColor = new Color(1f, .45f, .1f);
        public Color flameCoreColor = new Color(1f, .9f, .35f);
        public Color flameBackColor = new Color(.85f, .25f, .18f);
        public float flamesPerSecond = 22f;
        public float flameSize = .13f;
        public float flameWidth = .7f;
        public float flameBase = .05f;
        public float flameRise = .5f;
        public float flameTime = .55f;
        [Tooltip("Lado das chamas em pixels inteiros da arte (quadradinhos nítidos)")]
        public bool snapToArtPixel = true;

        [Header("SMOKE")]
        public Color smokeColor = new Color(.22f, .22f, .26f, .65f);
        public Color urgentSmokeColor = new Color(.1f, .09f, .12f, .8f);
        public float smokePerSecond = 4f;
        public float smokeSize = .16f;
        public float smokeRise = 1.1f;
        public float smokeTime = 1.5f;
        [Tooltip("Fumaça redonda (bolha de pixel ou Circle do jogo) em vez de quadradinho")]
        public bool roundSmoke = true;

        [Header("EMBERS (brasas do FeelParticles subindo)")]
        public float embersPerSecond = 6f;
        public float emberScale = 1f;

        [Header("GLOW (halo em faixas: a luz que aparece no mundo feito de UI)")]
        public bool glow = true;
        public Color glowColor = new Color(1f, .55f, .16f, .38f);
        public Color urgentGlowColor = new Color(.95f, .25f, .15f, .5f);
        public Vector2 glowSize = new Vector2(1.4f, 1.1f);
        [Range(0f, 1f)] public float glowFlicker = .25f;
        public float glowFlickerSpeed = 7f;

        [Header("LIGHT 2D (só acende sprites com material Sprite-Lit; casas e chão são UI sem luz)")]
        public bool useLight2D = true;
        public float lightIntensity = 1.3f;
        [Tooltip("Raio em frações do tamanho da casa")]
        public float lightRadius = 1.6f;
        public int maxLights = 6;

        [Header("URGENCY (1 = o fogo está para virar dano)")]
        [Tooltip("Quanto as chamas crescem e aceleram no fim")]
        public float urgentGrow = .6f;
        public float urgentFlickerSpeed = 2.2f;

        [Header("TIMER")]
        public float timerWidth = .7f;
        public float timerHeight = .07f;
        public float timerY = -.6f;
        public float timerBorder = 2f;
        public Color timerBackColor = FeelUI.PanelColor;
        public Color timerFullColor = new Color(1f, .85f, .3f);
        public Color timerEmptyColor = new Color(.95f, .2f, .15f);
        [Range(0f, 1f)] public float blinkBelow = .3f;
        public float blinkSpeed = 12f;
    }

    private enum Layer { Back, Mid, Front, Smoke }

    private class Particle
    {
        public RectTransform rect;
        public Image image;
        public Layer layer;
        public Vector2 velocity;
        public Color from;
        public Color middle;
        public Color to;
        public float endScale;
        public float life;
        public float duration;
        public float wobble;
        public float phase;
    }

    private static int lightsOn;

    private House house;
    private Settings settings;
    private RectTransform root;
    private Vector2 size;
    private Image sprite;
    private Image glow;
    private Light2D light2D;
    private bool lightCounted;
    private RectTransform[] layers;
    private CanvasGroup timerGroup;
    private RectTransform timerFill;
    private Image timerFillImage;
    private float flameDebt;
    private float smokeDebt;
    private float emberDebt;
    private float frameTime;
    private float noiseSeed;

    private readonly List<Particle> live = new List<Particle>();
    private readonly Stack<Particle>[] idle = { new Stack<Particle>(), new Stack<Particle>(), new Stack<Particle>(), new Stack<Particle>() };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        lightsOn = 0;
    }

    public static HouseFire Create(House house, Settings settings)
    {
        RectTransform houseRect = house.transform as RectTransform;

        //No Canvas da casa (o dos marcadores), para ficar por cima do desenho e atrás do chamado
        Transform parent = house.transform.Find("Canvas");
        if (parent == null) parent = house.transform;

        RectTransform rect = FeelUI.CreateRect("Fire", parent);
        rect.SetAsFirstSibling();
        rect.sizeDelta = houseRect != null ? houseRect.rect.size : Vector2.one * 100f;
        rect.position = houseRect != null ? houseRect.TransformPoint(houseRect.rect.center) : house.transform.position;

        HouseFire fire = rect.gameObject.AddComponent<HouseFire>();
        fire.house = house;
        fire.settings = settings;
        fire.root = rect;
        fire.size = rect.sizeDelta;
        fire.noiseSeed = Random.Range(0f, 100f);
        fire.Build();
        //O OnEnable do AddComponent veio antes da luz existir
        fire.TakeLight();
        return fire;
    }

    //Urgência 0..1: 0 = acabou de pegar fogo, 1 = vai virar dano
    public float Urgency => house != null && house.currentState == State.OnFire ? 1f - house.FireTimeLeft01 : 0f;

    private void Build()
    {
        Vector2 center = new Vector2(.5f, .5f);

        if (settings.glow)
        {
            glow = FeelUI.CreateImage("Glow", root, settings.glowColor, PixelFx.Glow(32, 22, 4));
            FeelUI.Place(glow.rectTransform, center, center, new Vector2(0f, size.y * settings.flameBase), Vector2.Scale(settings.glowSize, Vector2.one * size.y));
        }

        if (settings.useLight2D) BuildLight();

        layers = new RectTransform[4];
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i] = FeelUI.CreateRect(((Layer)i).ToString(), root);
            FeelUI.Place(layers[i], center, center, Vector2.zero, size);
        }

        if (settings.fireFrames != null && settings.fireFrames.Length > 0)
        {
            sprite = FeelUI.CreateImage("Flame", layers[(int)Layer.Mid], Color.white, settings.fireFrames[0]);
            sprite.preserveAspect = true;
            FeelUI.Place(sprite.rectTransform, center, center, new Vector2(0f, size.y * settings.flameBase), Vector2.one * size.y * settings.spriteSize);
        }

        Vector2 timerSize = new Vector2(size.x * settings.timerWidth, size.y * settings.timerHeight);
        bool pixel = FeelUI.Pixel != null;

        //Kit pixel: barra reta com contorno de 1 pixel (a barra tem 5 pixels de altura). A casa fica no Canvas do
        //mundo, onde a moldura de 3 pixels do HUD não cabe; quadrado puro fica nítido em qualquer zoom
        Image back = pixel ? FeelUI.CreateImage("Timer", root, FeelUI.PixelInk) : FeelUI.CreatePanel("Timer", root, settings.timerBackColor);
        FeelUI.Place(back.rectTransform, center, center, new Vector2(0f, size.y * settings.timerY), timerSize);
        timerGroup = back.gameObject.AddComponent<CanvasGroup>();

        timerFillImage = pixel ? FeelUI.CreateImage("Fill", back.transform, settings.timerFullColor) : FeelUI.CreatePanel("Fill", back.transform, settings.timerFullColor);
        timerFill = timerFillImage.rectTransform;
        timerFill.anchorMin = Vector2.zero;
        timerFill.anchorMax = Vector2.one;
        float inset = pixel ? timerSize.y / 5f : settings.timerBorder;
        timerFill.offsetMin = Vector2.one * inset;
        timerFill.offsetMax = -Vector2.one * inset;
    }

    private void BuildLight()
    {
        GameObject go = new GameObject("Light2D");
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(0f, size.y * settings.flameBase, 0f);

        light2D = go.AddComponent<Light2D>();
        light2D.lightType = Light2D.LightType.Point;
        light2D.color = settings.glowColor;
        light2D.intensity = 0f;
        light2D.falloffIntensity = .7f;

        //Raio em unidades de mundo: a casa tem escala do Canvas
        float worldHeight = size.y * Mathf.Abs(root.lossyScale.y);
        light2D.pointLightOuterRadius = worldHeight * settings.lightRadius;
        light2D.pointLightInnerRadius = worldHeight * settings.lightRadius * .2f;
        go.SetActive(false);
    }

    private void OnEnable()
    {
        TakeLight();
    }

    //Poucas luzes ao mesmo tempo (WebGL): as casas que pegam fogo depois ficam só com o halo
    private void TakeLight()
    {
        if (light2D == null || lightCounted || settings == null || lightsOn >= settings.maxLights) return;

        lightsOn++;
        lightCounted = true;
        light2D.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        foreach (Particle particle in live)
        {
            if (particle.rect != null) particle.rect.gameObject.SetActive(false);
            idle[(int)particle.layer].Push(particle);
        }

        live.Clear();
        flameDebt = 0f;
        smokeDebt = 0f;
        emberDebt = 0f;

        if (lightCounted)
        {
            lightsOn = Mathf.Max(0, lightsOn - 1);
            lightCounted = false;
            if (light2D != null) light2D.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (settings == null || house == null) return;

        float delta = Time.deltaTime;
        float urgency = Urgency;

        UpdateTimer();
        UpdateGlow(urgency);
        UpdateSprite(delta);
        Emit(delta, urgency);
        Move(delta);
    }

    private void UpdateTimer()
    {
        float left = house.FireTimeLeft01;

        timerFill.anchorMax = new Vector2(left, 1f);
        timerFillImage.color = Color.Lerp(settings.timerEmptyColor, settings.timerFullColor, left);
        //Perto de acabar, a barra pisca
        timerGroup.alpha = left < settings.blinkBelow ? .55f + .45f * Mathf.Sin(Time.time * settings.blinkSpeed) : 1f;
    }

    //Tremor da luz: ruído suave, mais rápido e mais vermelho com a urgência
    private void UpdateGlow(float urgency)
    {
        if (glow == null && light2D == null) return;

        float speed = settings.glowFlickerSpeed * Mathf.Lerp(1f, settings.urgentFlickerSpeed, urgency);
        float noise = Mathf.PerlinNoise(noiseSeed, Time.time * speed);
        float flicker = 1f - settings.glowFlicker + settings.glowFlicker * 2f * noise;
        Color color = Color.Lerp(settings.glowColor, settings.urgentGlowColor, urgency);

        if (glow != null)
        {
            glow.color = PixelFx.WithAlpha(color, color.a * flicker);
            glow.rectTransform.localScale = Vector3.one * (1f + urgency * .25f) * Mathf.Lerp(.96f, 1.04f, noise);
        }

        if (light2D != null && light2D.gameObject.activeSelf)
        {
            light2D.color = PixelFx.WithAlpha(color, 1f);
            light2D.intensity = settings.lightIntensity * flicker * (1f + urgency * .4f);
        }
    }

    private void UpdateSprite(float delta)
    {
        if (sprite == null) return;

        frameTime += delta * settings.frameRate;
        sprite.sprite = settings.fireFrames[Mathf.FloorToInt(frameTime) % settings.fireFrames.Length];
    }

    private void Emit(float delta, float urgency)
    {
        float density = VfxQuality.Density;
        float grow = 1f + urgency * settings.urgentGrow;

        //Com a arte da chama, só fumaça e brasas são de quadradinhos
        if (sprite == null)
        {
            flameDebt += settings.flamesPerSecond * grow * density * delta;
            while (flameDebt >= 1f)
            {
                flameDebt -= 1f;
                SpawnFlame(grow);
            }
        }

        Color smoke = Color.Lerp(settings.smokeColor, settings.urgentSmokeColor, urgency);
        Sprite smokeSprite = !settings.roundSmoke ? null : FeelUI.Pixel != null ? PixelFx.Puff : FeelUI.Theme != null ? FeelUI.Theme.circle : null;
        smokeDebt += settings.smokePerSecond * grow * density * delta;
        while (smokeDebt >= 1f)
        {
            smokeDebt -= 1f;
            Vector2 at = new Vector2(Random.Range(-.5f, .5f) * size.x * settings.flameWidth, size.y * (settings.flameBase + .25f));
            Particle particle = Spawn(Layer.Smoke, at, settings.smokeSize * Random.Range(.8f, 1.2f), smokeSprite);
            particle.velocity = new Vector2(Random.Range(-.15f, .15f) + AmbientFx.Wind.x * .3f, 1f) * (size.y * settings.smokeRise / settings.smokeTime);
            particle.from = PixelFx.WithAlpha(smoke, 0f);
            particle.middle = smoke;
            particle.to = PixelFx.WithAlpha(smoke, 0f);
            particle.endScale = 2f;
            particle.duration = settings.smokeTime * Random.Range(.8f, 1.1f);
            particle.wobble = size.x * .03f;
        }

        emberDebt += settings.embersPerSecond * grow * density * delta;
        while (emberDebt >= 1f)
        {
            emberDebt -= 1f;
            Vector2 spot = new Vector2(Random.Range(-.5f, .5f) * size.x * settings.flameWidth, size.y * settings.flameBase);
            FeelParticles.Emit(FxKind.Embers, root, spot, size.y * settings.emberScale, 1);
        }
    }

    //Três camadas dão volume: as de trás são maiores, mais escuras e sobem mais; a frente é o miolo amarelo, baixo
    private void SpawnFlame(float grow)
    {
        float pick = Random.value;
        Layer layer = pick < .3f ? Layer.Back : pick < .7f ? Layer.Mid : Layer.Front;

        float sizeMul = layer == Layer.Back ? 1.35f : layer == Layer.Mid ? 1f : .7f;
        float riseMul = layer == Layer.Back ? 1.2f : layer == Layer.Mid ? 1f : .55f;
        float widthMul = layer == Layer.Front ? .55f : 1f;

        Vector2 at = new Vector2(Random.Range(-.5f, .5f) * size.x * settings.flameWidth * widthMul,
            size.y * (settings.flameBase + Random.Range(-.12f, .08f)));
        Particle particle = Spawn(layer, at, settings.flameSize * sizeMul * grow, PixelFx.Flame);

        float time = settings.flameTime * Random.Range(.75f, 1f) * (layer == Layer.Back ? 1.1f : 1f);
        //Chama mais perto do meio sobe mais: forma de língua, não de muro
        float middle = 1f - Mathf.Abs(at.x) / Mathf.Max(1f, size.x * settings.flameWidth * .5f);
        particle.velocity = new Vector2(Random.Range(-.08f, .08f) * size.y, size.y * settings.flameRise * riseMul * grow * Mathf.Lerp(.6f, 1.2f, middle) / time);
        particle.duration = time;
        particle.endScale = .25f;
        particle.wobble = size.x * .025f;

        switch (layer)
        {
            case Layer.Back:
                particle.from = settings.flameColor;
                particle.middle = settings.flameBackColor;
                particle.to = PixelFx.WithAlpha(PixelFx.RedDark, 0f);
                break;
            case Layer.Mid:
                particle.from = Color.Lerp(settings.flameCoreColor, settings.flameColor, Random.value);
                particle.middle = settings.flameColor;
                particle.to = PixelFx.WithAlpha(settings.flameBackColor, 0f);
                break;
            default:
                particle.from = PixelFx.Cream;
                particle.middle = settings.flameCoreColor;
                particle.to = PixelFx.WithAlpha(settings.flameColor, 0f);
                break;
        }

        particle.image.color = particle.from;
    }

    private Particle Spawn(Layer layer, Vector2 position, float particleSize, Sprite shape)
    {
        Stack<Particle> stack = idle[(int)layer];
        Particle particle = stack.Count > 0 ? stack.Pop() : NewParticle(layer);

        float side = size.y * particleSize;
        if (settings.snapToArtPixel)
        {
            float pixel = PixelFx.ArtPixel;
            side = Mathf.Max(1f, Mathf.Round(side / pixel)) * pixel;
        }

        particle.image.sprite = shape;
        particle.rect.gameObject.SetActive(true);
        //A chama em gota é mais alta que larga; a fumaça redonda continua quadrada
        particle.rect.sizeDelta = shape != null ? new Vector2(side * shape.rect.width / shape.rect.height, side) : Vector2.one * side;
        particle.rect.anchoredPosition = position;
        particle.rect.localScale = Vector3.one;
        particle.life = 0f;
        particle.phase = Random.Range(0f, Mathf.PI * 2f);

        live.Add(particle);
        return particle;
    }

    private Particle NewParticle(Layer layer)
    {
        Image image = FeelUI.CreateImage("Fx", layers[(int)layer], Color.white);
        return new Particle { rect = image.rectTransform, image = image, layer = layer };
    }

    private void Move(float delta)
    {
        for (int i = live.Count - 1; i >= 0; i--)
        {
            Particle particle = live[i];
            particle.life += delta;
            float t = particle.life / Mathf.Max(.01f, particle.duration);

            if (t >= 1f)
            {
                if (particle.rect != null) particle.rect.gameObject.SetActive(false);
                idle[(int)particle.layer].Push(particle);
                live.RemoveAt(i);
                continue;
            }

            Vector2 step = particle.velocity * delta;
            step.x += Mathf.Cos(particle.phase + particle.life * 14f) * particle.wobble * delta * 6f;
            particle.rect.anchoredPosition += step;
            particle.rect.localScale = Vector3.one * Mathf.Lerp(1f, particle.endScale, t);
            particle.image.color = t < .45f ? Color.Lerp(particle.from, particle.middle, t / .45f) : Color.Lerp(particle.middle, particle.to, (t - .45f) / .55f);
        }
    }
}
