using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Fogo de uma casa em OnFire: chamas, fumaça e a barra do temporizador (House.FireTimeLeft01).
//Criado pelo HouseFeedback na primeira vez que a casa pega fogo; ligado e desligado por ele.
//Chamas e fumaça são quadradinhos até a arte chegar (Fire_6x32.png fatiado entra em fireFrames).
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
        public float flamesPerSecond = 16f;
        public float flameSize = .13f;
        public float flameWidth = .7f;
        public float flameBase = .05f;
        public float flameRise = .5f;
        public float flameTime = .55f;

        [Header("SMOKE")]
        public Color smokeColor = new Color(.22f, .22f, .26f, .65f);
        public float smokePerSecond = 4f;
        public float smokeSize = .16f;
        public float smokeRise = 1.1f;
        public float smokeTime = 1.5f;
        [Tooltip("Fumaça redonda (Circle do jogo) em vez de quadradinho")]
        public bool roundSmoke = true;

        [Header("EMBERS (brasas do FeelParticles subindo)")]
        public float embersPerSecond = 6f;
        public float emberScale = 1f;

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

    private class Particle
    {
        public RectTransform rect;
        public Image image;
        public Vector2 velocity;
        public Color from;
        public Color to;
        public float endScale;
        public float life;
        public float duration;
    }

    private House house;
    private Settings settings;
    private RectTransform root;
    private Vector2 size;
    private Image sprite;
    private CanvasGroup timerGroup;
    private RectTransform timerFill;
    private Image timerFillImage;
    private float flameDebt;
    private float smokeDebt;
    private float emberDebt;
    private float frameTime;

    private readonly List<Particle> live = new List<Particle>();
    private readonly Stack<Particle> idle = new Stack<Particle>();

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
        fire.Build();
        return fire;
    }

    private void Build()
    {
        Vector2 center = new Vector2(.5f, .5f);

        if (settings.fireFrames != null && settings.fireFrames.Length > 0)
        {
            sprite = FeelUI.CreateImage("Flame", root, Color.white, settings.fireFrames[0]);
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

    private void OnDisable()
    {
        foreach (Particle particle in live)
        {
            if (particle.rect != null) particle.rect.gameObject.SetActive(false);
            idle.Push(particle);
        }

        live.Clear();
        flameDebt = 0f;
        smokeDebt = 0f;
        emberDebt = 0f;
    }

    private void Update()
    {
        if (settings == null || house == null) return;

        float delta = Time.deltaTime;

        UpdateTimer();
        UpdateSprite(delta);
        Emit(delta);
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

    private void UpdateSprite(float delta)
    {
        if (sprite == null) return;

        frameTime += delta * settings.frameRate;
        sprite.sprite = settings.fireFrames[Mathf.FloorToInt(frameTime) % settings.fireFrames.Length];
    }

    private void Emit(float delta)
    {
        //Com a arte da chama, só a fumaça é de quadradinhos
        if (sprite == null)
        {
            flameDebt += settings.flamesPerSecond * delta;
            while (flameDebt >= 1f)
            {
                flameDebt -= 1f;
                Color from = Color.Lerp(settings.flameCoreColor, settings.flameColor, Random.value);
                Spawn(from, WithAlpha(settings.flameColor, 0f), settings.flameSize, settings.flameRise, settings.flameTime, .2f, null);
            }
        }

        Sprite smokeSprite = settings.roundSmoke && FeelUI.Theme != null ? FeelUI.Theme.circle : null;
        smokeDebt += settings.smokePerSecond * delta;
        while (smokeDebt >= 1f)
        {
            smokeDebt -= 1f;
            Spawn(settings.smokeColor, WithAlpha(settings.smokeColor, 0f), settings.smokeSize, settings.smokeRise, settings.smokeTime, 1.8f, smokeSprite);
        }

        emberDebt += settings.embersPerSecond * delta;
        while (emberDebt >= 1f)
        {
            emberDebt -= 1f;
            Vector2 spot = new Vector2(Random.Range(-.5f, .5f) * size.x * settings.flameWidth, size.y * settings.flameBase);
            FeelParticles.Emit(FxKind.Embers, root, spot, size.y * settings.emberScale);
        }
    }

    private void Spawn(Color from, Color to, float particleSize, float rise, float time, float endScale, Sprite shape)
    {
        Particle particle = idle.Count > 0 ? idle.Pop() : NewParticle();

        particle.image.sprite = shape;
        particle.rect.gameObject.SetActive(true);
        particle.rect.sizeDelta = Vector2.one * size.y * particleSize * Random.Range(.7f, 1.2f);
        particle.rect.anchoredPosition = new Vector2(
            Random.Range(-.5f, .5f) * size.x * settings.flameWidth,
            size.y * (settings.flameBase + Random.Range(-.15f, .15f)));
        particle.rect.localScale = Vector3.one;
        particle.velocity = new Vector2(Random.Range(-.2f, .2f), 1f) * (size.y * rise / Mathf.Max(.01f, time));
        particle.from = from;
        particle.to = to;
        particle.endScale = endScale;
        particle.life = 0f;
        particle.duration = Mathf.Max(.01f, time * Random.Range(.75f, 1f));
        particle.image.color = from;

        live.Add(particle);
    }

    private Particle NewParticle()
    {
        Image image = FeelUI.CreateImage("Fx", root, Color.white);
        //Atrás da barra do temporizador
        image.transform.SetAsFirstSibling();
        return new Particle { rect = image.rectTransform, image = image };
    }

    private void Move(float delta)
    {
        for (int i = live.Count - 1; i >= 0; i--)
        {
            Particle particle = live[i];
            particle.life += delta;
            float t = particle.life / particle.duration;

            if (t >= 1f)
            {
                if (particle.rect != null) particle.rect.gameObject.SetActive(false);
                idle.Push(particle);
                live.RemoveAt(i);
                continue;
            }

            particle.rect.anchoredPosition += particle.velocity * delta;
            particle.rect.localScale = Vector3.one * Mathf.Lerp(1f, particle.endScale, t);
            particle.image.color = Color.Lerp(particle.from, particle.to, t * t);
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
