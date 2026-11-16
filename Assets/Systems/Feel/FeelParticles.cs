using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public enum FxKind { Dust, Debris, Sparks, Smoke, Embers, Steam, Confetti, Ash, Splash }
public enum FxShape { Square, Circle, Strip }

//Receita de um tipo de partícula. Tamanhos, velocidades e gravidade são frações do "scale" de quem emite
//(a altura da casa, a altura da tela), para o mesmo efeito servir no mundo e no HUD.
[System.Serializable]
public class FxPreset
{
    [Tooltip("Square = quadradinho liso; Circle = o Circle do jogo; Strip = tira que gira (confete)")]
    public FxShape shape = FxShape.Square;
    public Vector2Int count = new Vector2Int(6, 9);
    public Vector2 size = new Vector2(.06f, .1f);
    public Vector2 speed = new Vector2(.4f, .8f);
    public Vector2 lifetime = new Vector2(.5f, .8f);
    [Tooltip("Abertura do leque em graus (360 = para todo lado)"), Range(0f, 360f)]
    public float spread = 360f;
    [Tooltip("Direção do leque em graus: 90 = para cima, 270 = para baixo")]
    public float direction = 90f;
    public float spawnRadius;
    [Tooltip("Positivo puxa para baixo; negativo faz subir (fumaça)")]
    public float gravity;
    [Tooltip("Perda de velocidade por segundo")]
    public float drag = 2f;
    [Tooltip("Tamanho no fim da vida (1 = igual ao do começo)")]
    public float endSize = .3f;
    [Tooltip("Giro em graus por segundo, sorteado entre -spin e spin")]
    public float spin;
    [Tooltip("Balanço para os lados, fração do scale")]
    public float sway;
    public float swayFrequency = 3f;
    [Tooltip("Faísca: aponta e estica na direção em que voa")]
    public bool alignToVelocity;
    public float stretch = 2.5f;
    [Range(0f, 1f)] public float fadeIn;
    [Tooltip("A partir de que fração da vida começa a sumir"), Range(0f, 1f)]
    public float fadeOutFrom = .4f;
    public Color[] colors = { Color.white };
    public bool useEndColor;
    public Color endColor = Color.white;
    [Tooltip("Anda com o jogo pausado (telas finais)")]
    public bool unscaledTime;
}

//Partículas feitas de Image, para viver dentro dos Canvas do jogo (o mundo é UI). Um só na cena, no Feel.prefab;
//sem ele, o primeiro Emit cria um. As Images são recicladas: nada é criado nem destruído a cada efeito.
public class FeelParticles : MonoBehaviour
{
    public static FeelParticles Instance;

    [Header("LIMITS")]
    [SerializeField] private int maxParticles = 500;
    [SerializeField] private int prewarm = 60;

    [Header("PRESETS")]
    [SerializeField] private FxPreset dust = DustPreset();
    [SerializeField] private FxPreset debris = DebrisPreset();
    [SerializeField] private FxPreset sparks = SparksPreset();
    [SerializeField] private FxPreset smoke = SmokePreset();
    [SerializeField] private FxPreset embers = EmbersPreset();
    [SerializeField] private FxPreset steam = SteamPreset();
    [SerializeField] private FxPreset confetti = ConfettiPreset();
    [SerializeField] private FxPreset ash = AshPreset();
    [SerializeField] private FxPreset splash = SplashPreset();

    private class Particle
    {
        public Image image;
        public RectTransform rect;
        public FxPreset preset;
        public Vector2 position;
        public Vector2 velocity;
        public Color color;
        public float startSpeed;
        public float scale;
        public float size;
        public float age;
        public float life;
        public float rotation;
        public float spin;
        public float swayPhase;
        public float tumble;
    }

    private readonly List<Particle> live = new List<Particle>();
    private readonly Stack<Particle> idle = new Stack<Particle>();
    private Transform poolRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        //Imagens paradas ficam num filho desligado
        poolRoot = new GameObject("FxPool").transform;
        poolRoot.SetParent(transform, false);
        poolRoot.gameObject.SetActive(false);

        for (int i = 0; i < prewarm; i++) idle.Push(NewParticle());
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private static FeelParticles Runner
    {
        get
        {
            if (Instance == null) new GameObject("FeelParticles").AddComponent<FeelParticles>();
            return Instance;
        }
    }

    #region EMIT

    public static FxPreset Preset(FxKind kind)
    {
        return Runner.PresetFor(kind);
    }

    //Estouro a partir de um ponto do mundo, dentro do Canvas do pai (a casa, a rua)
    public static void EmitAt(FxKind kind, Transform parent, Vector3 worldPosition, float scale, int count = -1, float direction = float.NaN)
    {
        if (parent == null) return;

        Vector2 local = parent.InverseTransformPoint(worldPosition);
        if (parent is RectTransform rect) local -= rect.rect.center;

        Emit(kind, parent, local, scale, count, direction);
    }

    //Estouro a partir de um ponto local do pai (âncora no centro do pai). width espalha o nascimento numa faixa.
    public static void Emit(FxKind kind, Transform parent, Vector2 localPosition, float scale, int count = -1, float direction = float.NaN, float width = 0f)
    {
        if (parent == null || scale <= 0f) return;

        FeelParticles runner = Runner;
        FxPreset preset = runner.PresetFor(kind);
        if (preset == null) return;

        int amount = count >= 0 ? count : Random.Range(preset.count.x, preset.count.y + 1);
        float angle = float.IsNaN(direction) ? preset.direction : direction;

        for (int i = 0; i < amount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * preset.spawnRadius * scale;
            offset.x += Random.Range(-.5f, .5f) * width;
            runner.Spawn(preset, parent, localPosition + offset, scale, angle);
        }
    }

    private FxPreset PresetFor(FxKind kind)
    {
        switch (kind)
        {
            case FxKind.Dust: return dust;
            case FxKind.Debris: return debris;
            case FxKind.Sparks: return sparks;
            case FxKind.Smoke: return smoke;
            case FxKind.Embers: return embers;
            case FxKind.Steam: return steam;
            case FxKind.Confetti: return confetti;
            case FxKind.Ash: return ash;
            case FxKind.Splash: return splash;
            default: return dust;
        }
    }

    private void Spawn(FxPreset preset, Transform parent, Vector2 position, float scale, float angle)
    {
        if (live.Count >= maxParticles) return;

        Particle particle = idle.Count > 0 ? idle.Pop() : NewParticle();
        if (particle.image == null) particle = NewParticle();

        float size = Random.Range(preset.size.x, preset.size.y) * scale;
        float direction = (angle + Random.Range(-.5f, .5f) * preset.spread) * Mathf.Deg2Rad;
        float speed = Random.Range(preset.speed.x, preset.speed.y) * scale;

        particle.preset = preset;
        particle.position = position;
        particle.velocity = new Vector2(Mathf.Cos(direction), Mathf.Sin(direction)) * speed;
        particle.startSpeed = Mathf.Max(.001f, speed);
        particle.scale = scale;
        particle.size = size;
        particle.age = 0f;
        particle.life = Mathf.Max(.05f, Random.Range(preset.lifetime.x, preset.lifetime.y));
        particle.rotation = preset.shape == FxShape.Circle ? 0f : Random.Range(0f, 360f);
        particle.spin = Random.Range(-preset.spin, preset.spin);
        particle.swayPhase = Random.Range(0f, Mathf.PI * 2f);
        particle.tumble = Random.Range(4f, 10f);
        particle.color = preset.colors != null && preset.colors.Length > 0 ? preset.colors[Random.Range(0, preset.colors.Length)] : Color.white;

        Image image = particle.image;
        RectTransform rect = particle.rect;
        rect.SetParent(parent, false);
        rect.gameObject.layer = parent.gameObject.layer;
        rect.SetAsLastSibling();

        image.sprite = preset.shape == FxShape.Circle && FeelUI.Theme != null ? FeelUI.Theme.circle : null;
        image.color = particle.color;
        rect.sizeDelta = preset.shape == FxShape.Strip ? new Vector2(size, size * .5f)
            : preset.alignToVelocity ? new Vector2(size * preset.stretch, size * .6f)
            : Vector2.one * size;

        rect.gameObject.SetActive(true);
        Apply(particle, 0f);
        live.Add(particle);
    }

    private Particle NewParticle()
    {
        Image image = FeelUI.CreateImage("Fx", poolRoot, Color.white);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        image.gameObject.SetActive(false);
        return new Particle { image = image, rect = rect };
    }

    #endregion

    #region UPDATE

    private void Update()
    {
        float scaled = Time.deltaTime;
        float unscaled = Time.unscaledDeltaTime;

        for (int i = live.Count - 1; i >= 0; i--)
        {
            Particle particle = live[i];

            //O pai foi destruído (cena recarregada, casa apagada): a Image foi junto
            if (particle.image == null)
            {
                live.RemoveAt(i);
                continue;
            }

            FxPreset preset = particle.preset;
            float delta = preset.unscaledTime ? unscaled : scaled;
            if (delta <= 0f) continue;

            particle.age += delta;
            if (particle.age >= particle.life)
            {
                Recycle(particle);
                live.RemoveAt(i);
                continue;
            }

            particle.velocity.y -= preset.gravity * particle.scale * delta;
            particle.velocity *= 1f / (1f + preset.drag * delta);
            particle.position += particle.velocity * delta;
            particle.rotation += particle.spin * delta;

            Apply(particle, particle.age / particle.life);
        }
    }

    private void Apply(Particle particle, float t)
    {
        FxPreset preset = particle.preset;
        RectTransform rect = particle.rect;

        Vector2 position = particle.position;
        if (preset.sway > 0f) position.x += Mathf.Sin(particle.swayPhase + particle.age * preset.swayFrequency) * preset.sway * particle.scale;
        rect.anchoredPosition = position;

        float size = Mathf.Lerp(1f, preset.endSize, t);
        Vector3 scale = new Vector3(size, size, 1f);

        if (preset.alignToVelocity)
        {
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(particle.velocity.y, particle.velocity.x) * Mathf.Rad2Deg);
            scale.x *= Mathf.Lerp(.4f, 1f, particle.velocity.magnitude / particle.startSpeed);
        }
        else
        {
            rect.localRotation = Quaternion.Euler(0f, 0f, particle.rotation);
        }

        //Confete: a tira vira de lado e volta, como papel caindo
        if (preset.shape == FxShape.Strip) scale.y *= Mathf.Max(.15f, Mathf.Abs(Mathf.Cos(particle.swayPhase + particle.age * particle.tumble)));

        rect.localScale = scale;

        if (preset.useEndColor) particle.image.color = Color.Lerp(particle.color, preset.endColor, t);

        //Alpha pelo CanvasRenderer: não refaz a malha da Image a cada quadro
        float alpha = 1f;
        if (preset.fadeIn > 0f && t < preset.fadeIn) alpha = t / preset.fadeIn;
        if (t > preset.fadeOutFrom) alpha *= 1f - (t - preset.fadeOutFrom) / Mathf.Max(.001f, 1f - preset.fadeOutFrom);
        particle.image.canvasRenderer.SetAlpha(alpha);
    }

    private void Recycle(Particle particle)
    {
        particle.rect.gameObject.SetActive(false);
        particle.rect.SetParent(poolRoot, false);
        idle.Push(particle);
    }

    //Some com tudo o que está no ar (troca de tela, reinício)
    public static void Clear()
    {
        if (Instance == null) return;

        for (int i = Instance.live.Count - 1; i >= 0; i--)
        {
            if (Instance.live[i].image != null) Instance.Recycle(Instance.live[i]);
        }

        Instance.live.Clear();
    }

    #endregion

    #region DEFAULT PRESETS

    private static FxPreset DustPreset() => new FxPreset
    {
        shape = FxShape.Circle, count = new Vector2Int(9, 12), size = new Vector2(.08f, .15f), speed = new Vector2(.5f, 1.1f),
        lifetime = new Vector2(.6f, 1f), spread = 200f, spawnRadius = .12f, gravity = -.2f, drag = 3.5f, endSize = 1.8f,
        fadeOutFrom = .25f,
        colors = new[] { new Color(.8f, .74f, .64f, .9f), new Color(.7f, .64f, .55f, .85f), new Color(.88f, .84f, .76f, .8f) }
    };

    private static FxPreset DebrisPreset() => new FxPreset
    {
        count = new Vector2Int(5, 8), size = new Vector2(.03f, .06f), speed = new Vector2(.9f, 1.5f), lifetime = new Vector2(.55f, .8f),
        spread = 110f, gravity = 5f, drag = .4f, endSize = .8f, spin = 540f, fadeOutFrom = .7f,
        colors = new[] { new Color(.45f, .36f, .3f), new Color(.6f, .5f, .42f), new Color(.35f, .33f, .36f) }
    };

    private static FxPreset SparksPreset() => new FxPreset
    {
        count = new Vector2Int(10, 14), size = new Vector2(.025f, .04f), speed = new Vector2(1.3f, 2.2f), lifetime = new Vector2(.35f, .6f),
        gravity = 1.6f, drag = 4f, endSize = .3f, alignToVelocity = true, stretch = 3f, fadeOutFrom = .5f,
        colors = new[] { new Color(1f, .95f, .6f), Color.white, new Color(1f, .82f, .35f) }
    };

    private static FxPreset SmokePreset() => new FxPreset
    {
        shape = FxShape.Circle, count = new Vector2Int(6, 9), size = new Vector2(.12f, .2f), speed = new Vector2(.25f, .5f),
        lifetime = new Vector2(1.2f, 1.8f), spread = 70f, spawnRadius = .15f, gravity = -.3f, drag = 1f, endSize = 2.2f,
        sway = .05f, swayFrequency = 2f, fadeIn = .1f, fadeOutFrom = .3f,
        colors = new[] { new Color(.2f, .2f, .23f, .75f), new Color(.3f, .29f, .32f, .7f) }
    };

    private static FxPreset EmbersPreset() => new FxPreset
    {
        count = new Vector2Int(1, 1), size = new Vector2(.015f, .03f), speed = new Vector2(.5f, .9f), lifetime = new Vector2(.7f, 1.1f),
        spread = 50f, gravity = -.6f, drag = .5f, endSize = .4f, sway = .06f, swayFrequency = 6f, fadeOutFrom = .5f,
        colors = new[] { new Color(1f, .85f, .3f), new Color(1f, .6f, .15f) }, useEndColor = true, endColor = new Color(.9f, .2f, .1f)
    };

    private static FxPreset SteamPreset() => new FxPreset
    {
        shape = FxShape.Circle, count = new Vector2Int(8, 12), size = new Vector2(.1f, .16f), speed = new Vector2(.4f, .8f),
        lifetime = new Vector2(.8f, 1.2f), spread = 100f, spawnRadius = .15f, gravity = -.5f, drag = 2f, endSize = 2f, fadeOutFrom = .2f,
        colors = new[] { new Color(1f, 1f, 1f, .8f), new Color(.85f, .9f, .95f, .75f) }
    };

    //Confete e cinza são do HUD: scale = altura da tela de referência (1080)
    private static FxPreset ConfettiPreset() => new FxPreset
    {
        shape = FxShape.Strip, count = new Vector2Int(60, 80), size = new Vector2(.012f, .02f), speed = new Vector2(.9f, 1.6f),
        lifetime = new Vector2(2.5f, 4f), spread = 40f, gravity = .9f, drag = 1.5f, endSize = 1f, spin = 360f,
        sway = .02f, swayFrequency = 4f, fadeOutFrom = .8f, unscaledTime = true,
        colors = new[]
        {
            new Color(.95f, .3f, .25f), new Color(1f, .82f, .25f), new Color(.45f, .9f, .5f),
            new Color(.35f, .65f, 1f), new Color(.95f, .5f, .8f), Color.white
        }
    };

    private static FxPreset AshPreset() => new FxPreset
    {
        count = new Vector2Int(1, 1), size = new Vector2(.005f, .01f), speed = new Vector2(.03f, .08f), lifetime = new Vector2(4f, 6f),
        spread = 60f, direction = 270f, gravity = .02f, drag = 0f, endSize = .6f, spin = 90f, sway = .02f, swayFrequency = 1.5f,
        fadeIn = .2f, fadeOutFrom = .7f, unscaledTime = true,
        colors = new[] { new Color(.55f, .53f, .56f, .8f), new Color(.35f, .33f, .36f, .8f) }
    };

    private static FxPreset SplashPreset() => new FxPreset
    {
        count = new Vector2Int(3, 4), size = new Vector2(.004f, .007f), speed = new Vector2(.05f, .1f), lifetime = new Vector2(.25f, .35f),
        spread = 120f, gravity = .4f, drag = 1f, endSize = .5f, fadeOutFrom = .3f,
        colors = new[] { new Color(.75f, .85f, 1f, .7f) }
    };

    #endregion

    #region DEBUG

    [Button]
    private void DebugConfetti()
    {
        RectTransform layer = FeelUI.HudLayer("FeelParticlesTest");
        if (layer == null) return;

        float height = layer.rect.height;
        Emit(FxKind.Confetti, layer, new Vector2(-layer.rect.width * .45f, -height * .5f), height, -1, 65f);
        Emit(FxKind.Confetti, layer, new Vector2(layer.rect.width * .45f, -height * .5f), height, -1, 115f);
    }

    [Button]
    private void DebugAtScreenCenter(FxKind kind = FxKind.Dust, float scale = 160f)
    {
        RectTransform layer = FeelUI.HudLayer("FeelParticlesTest");
        if (layer != null) Emit(kind, layer, Vector2.zero, scale);
    }

    #endregion
}
