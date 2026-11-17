using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ciclo de dia e noite, só iluminação (docs/jam/ENERGIA_DIA_NOITE.md). Anda com o tempo da partida (para na pausa):
// a luz global (ambiente) e o "sol" da cena seguem as fases do DayNightData; à noite os postes, a Guarda, os armazéns
// e as janelas das casas acendem, cada um no seu momento. No apagão tudo pisca e apaga; quando a rede volta, a cidade
// reacende numa onda que sai da Guarda. Não mexe em nada da jogabilidade. As luzes são as que o CityBuilder já pôs
// na cena ([CG] Lights); as janelas são um SpriteRenderer "[W]" (shader DefesaCivil/Window Glow) criado no Play.
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    [Header("TECH")]
    [Tooltip("Segundos entre as buscas por base ou armazém novo (expansão) e por sprite de casa trocado.")]
    [SerializeField] private float scanInterval = 2f;
    [Tooltip("Velocidade com que uma luz acende ou apaga pela energia (fração por segundo).")]
    [SerializeField] private float fadeSpeed = 6f;

    private const string LightsRoot = "[CG] Lights";
    private const string LightPrefix = "[CG] Light ";
    private const string WindowName = "[W]";
    private const string GlowName = "[G]";
    private const string MaterialPath = "DayNight/WindowGlowMAT";

    // Uma luz da cidade: Light2D (poste, base) ou brilho de sprite (lâmpada do poste, janela).
    private class Lamp
    {
        public Light2D light;
        public SpriteRenderer glow;
        public float baseIntensity;
        public Color baseColor;
        public float slot;
        public bool isBase;
        public bool windowLit;
        public Location owner;
        public Image visual;
        public Canvas canvas;
        public Vector2 position;
        public float power = 1f;
        public float offAt = -1f;
        public float onAt = -1f;
        public float shown = -1f;
    }

    private readonly List<Lamp> lamps = new();
    private readonly List<Lamp> windows = new();
    private readonly HashSet<House> windowHouses = new();
    private Light2D global;
    private Light2D sun;
    private Material windowMaterial;
    private Light2D baseTemplate;
    private Vector3 baseTemplateOffset;
    private float elapsed;
    private float scanTimer;
    private bool blackout;
    private bool ready;

    private static DayNightData Data => DayNightData.Instance;

    public float Phase { get; private set; }
    public float Night { get; private set; }
    public int WindowCount => windows.Count;
    public int LampCount => lamps.Count;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.BlackoutChanged += OnBlackoutChanged;
    }

    private void OnDisable()
    {
        GameEvents.BlackoutChanged -= OnBlackoutChanged;
    }

    private void Start()
    {
        // Cópia do material: a cor e o brilho vêm do DayNightData sem mexer no asset.
        Material shared = Resources.Load<Material>(MaterialPath);
        if (shared != null)
        {
            windowMaterial = new Material(shared) { name = shared.name + " (DayNight)" };
            windowMaterial.SetColor("_GlowColor", Data.windowColor);
            windowMaterial.SetFloat("_Intensity", Data.windowIntensity);
        }
        FindLights();
        Scan();
        ready = true;
        Apply(0f);
    }

    private void Update()
    {
        if (!ready) return;

        if (GameManager.Instance == null || GameManager.Instance.State == GameState.Playing) elapsed += Time.deltaTime;

        scanTimer -= Time.unscaledDeltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            Scan();
        }

        Apply(Time.deltaTime);
    }

    #region DEBUG

    // Inspector e prints: pula para a fase dada (0 a 1) sem esperar o relógio.
    [Sirenix.OdinInspector.Button]
    public void SetPhase(float phase)
    {
        float cycle = Mathf.Max(1f, Data.cycleSeconds);
        elapsed = (Mathf.Repeat(phase, 1f) - Data.startPhase) * cycle;
        if (elapsed < 0f) elapsed += cycle;
        foreach (Lamp lamp in AllLamps()) lamp.shown = -1f;
        Apply(0f);
    }

    // Inspector e prints: pula para a fase com esse nome ("Amanhecer", "Dia", "Entardecer", "Noite"...).
    [Sirenix.OdinInspector.Button]
    public bool JumpTo(string keyName)
    {
        float phase = DayNightModel.PhaseOf(Data.keys, keyName);
        if (phase < 0f) return false;

        SetPhase(phase + .001f);
        return true;
    }

    #endregion

    #region FIND

    private void FindLights()
    {
        foreach (Light2D light in FindObjectsByType<Light2D>(FindObjectsInactive.Include))
        {
            if (light.lightType == Light2D.LightType.Global)
            {
                if (global == null && !light.name.StartsWith("[CG]")) global = light;
                continue;
            }

            if (light.transform.parent == null || light.transform.parent.name != LightsRoot) continue;

            string kind = light.name.StartsWith(LightPrefix) ? light.name.Substring(LightPrefix.Length) : light.name;
            if (kind.StartsWith("Sun"))
            {
                sun = light;
                continue;
            }

            Lamp lamp = new Lamp { light = light, baseIntensity = light.intensity, baseColor = light.color, position = light.transform.position };
            if (kind.StartsWith("Lamp")) lamp.slot = IdSlot(kind);
            else
            {
                lamp.isBase = true;
                lamp.owner = NearestLocation(light.transform.position);
                lamp.slot = DayNightModel.Slot(lamp.position) * .3f;
                if (baseTemplate == null && lamp.owner != null)
                {
                    baseTemplate = light;
                    baseTemplateOffset = light.transform.position - lamp.owner.transform.position;
                }
            }
            lamps.Add(lamp);
        }

        // Lâmpada acesa do poste ([G] com sprite Poste...): acende junto com a luz do mesmo poste (mesmo id).
        foreach (SpriteRenderer renderer in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
        {
            if (renderer.name != GlowName || renderer.sprite == null || !renderer.sprite.name.StartsWith("Poste")) continue;

            Transform owner = renderer.transform.parent;
            lamps.Add(new Lamp { glow = renderer, baseColor = renderer.color, slot = IdSlot(owner != null ? owner.name : renderer.name), position = renderer.transform.position });
        }
    }

    // Casas sem janela ainda (início e casas que mudaram) e bases novas da expansão sem luz.
    private void Scan()
    {
        if (LocationsManager.Instance != null)
        {
            foreach (House house in LocationsManager.Instance.Houses) AddWindow(house);
            foreach (CityGuard guard in LocationsManager.Instance.Guards) { AddWindow(guard); AddBaseLight(guard); }
            foreach (Warehouse depot in LocationsManager.Instance.Warehouses) { AddWindow(depot); AddBaseLight(depot); }
        }
        else
        {
            foreach (House house in FindObjectsByType<House>()) AddWindow(house);
        }

        foreach (Lamp window in windows) SyncWindow(window);
    }

    private void AddWindow(House house)
    {
        if (house == null || windowMaterial == null || windowHouses.Contains(house)) return;

        Transform visualTransform = house.transform.Find("Visual");
        Image visual = visualTransform != null ? visualTransform.GetComponent<Image>() : null;
        Canvas canvas = house.GetComponent<Canvas>();
        if (visual == null || visual.sprite == null || canvas == null) return;

        windowHouses.Add(house);
        Transform existing = visualTransform.Find(WindowName);
        GameObject go = existing != null ? existing.gameObject : new GameObject(WindowName, typeof(SpriteRenderer));
        go.layer = visual.gameObject.layer;
        go.transform.SetParent(visualTransform, false);

        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        renderer.sharedMaterial = windowMaterial;
        renderer.enabled = false;

        Vector2 position = house.transform.position;
        Lamp window = new Lamp
        {
            glow = renderer,
            visual = visual,
            canvas = canvas,
            position = position,
            slot = DayNightModel.Slot(position),
            windowLit = house is Location || DayNightModel.Slot(position + new Vector2(7.31f, 1.17f)) < Data.windowsLitShare,
        };
        windows.Add(window);
        SyncWindow(window);
    }

    // O [W] acompanha a Image Visual: mesmo sprite, mesmo lugar e ordem logo acima do Canvas da casa.
    private void SyncWindow(Lamp window)
    {
        if (window.visual == null || window.glow == null) return;

        SpriteRenderer renderer = window.glow;
        Sprite sprite = window.visual.sprite;
        if (renderer.sprite != sprite)
        {
            renderer.sprite = sprite;
            window.shown = -1f;
            if (sprite != null) Fit(renderer.transform, window.visual, sprite);
        }
        if (renderer.sortingLayerID != window.canvas.sortingLayerID) renderer.sortingLayerID = window.canvas.sortingLayerID;
        if (renderer.sortingOrder != window.canvas.sortingOrder + 1) renderer.sortingOrder = window.canvas.sortingOrder + 1;
    }

    // Mesma conta do espelho [N] do CityBuilder: o sprite do SpriteRenderer cobre exatamente o retângulo da Image.
    private static void Fit(Transform target, Image image, Sprite sprite)
    {
        Vector3[] corners = new Vector3[4];
        image.rectTransform.GetWorldCorners(corners);
        Bounds bounds = sprite.bounds;
        Vector3 lossy = image.transform.lossyScale;
        float sx = (corners[2].x - corners[0].x) / bounds.size.x;
        float sy = (corners[2].y - corners[0].y) / bounds.size.y;
        if (Mathf.Abs(lossy.x) < 1e-6f || Mathf.Abs(lossy.y) < 1e-6f) return;

        Vector3 center = (corners[0] + corners[2]) * .5f;
        target.localScale = new Vector3(sx / lossy.x, sy / lossy.y, 1f);
        target.position = new Vector3(center.x - bounds.center.x * sx, center.y - bounds.center.y * sy, image.transform.position.z);
    }

    // Base ou armazém construído depois (expansão) ganha a mesma luz das outras bases.
    private void AddBaseLight(Location location)
    {
        if (location == null || baseTemplate == null || !location.isActiveAndEnabled) return;

        foreach (Lamp lamp in lamps)
        {
            if (lamp.isBase && lamp.owner == location) return;
        }

        Light2D light = Instantiate(baseTemplate, location.transform.position + baseTemplateOffset, Quaternion.identity, baseTemplate.transform.parent);
        light.name = LightPrefix + "Built " + location.name;
        Lamp template = lamps.Find(l => l.light == baseTemplate);
        lamps.Add(new Lamp
        {
            light = light,
            baseIntensity = template != null ? template.baseIntensity : light.intensity,
            baseColor = template != null ? template.baseColor : light.color,
            isBase = true,
            owner = location,
            position = light.transform.position,
            slot = DayNightModel.Slot(light.transform.position) * .3f,
            onAt = Time.time + .2f,
            power = 0f,
        });
    }

    private Location NearestLocation(Vector3 position)
    {
        Location nearest = null;
        float best = 9f;
        foreach (Location location in FindObjectsByType<Location>())
        {
            float distance = ((Vector2)(location.transform.position - position)).sqrMagnitude;
            if (distance < best)
            {
                best = distance;
                nearest = location;
            }
        }
        return nearest;
    }

    // Postes: o lugar na fila de acender vem do id do enfeite ("Lamp 209" e "[CG] Tree 209"), igual para a luz e a lâmpada.
    private static float IdSlot(string name)
    {
        int end = name.Length;
        int start = end;
        while (start > 0 && char.IsDigit(name[start - 1])) start--;
        int id = start < end && int.TryParse(name.Substring(start, end - start), out int parsed) ? parsed : name.Length;
        return DayNightModel.Slot(new Vector2(id * .731f, id * .277f));
    }

    #endregion

    #region APPLY

    private void Apply(float deltaTime)
    {
        Phase = DayNightModel.PhaseAt(Data.startPhase, elapsed, Data.cycleSeconds);
        DayNightSample sample = DayNightModel.Evaluate(Data.keys, Phase);
        Night = sample.night;

        if (global != null)
        {
            global.color = sample.ambient;
            global.intensity = sample.ambientIntensity;
        }
        if (sun != null)
        {
            sun.color = sample.sun;
            sun.intensity = sample.sunIntensity;
            sun.enabled = sample.sunIntensity > .01f;
        }

        float now = Time.time;
        float spread = Data.lightsSpread;
        foreach (Lamp lamp in lamps)
        {
            bool powered = lamp.isBase ? lamp.owner == null || EnergyManager.IsPowered(lamp.owner) : !blackout;
            float power = Power(lamp, powered, now, deltaTime);
            float on = DayNightModel.LightOn(Night, lamp.slot, spread);

            if (lamp.light != null)
            {
                float day = lamp.isBase ? Data.baseDay : Data.lampDay;
                float night = lamp.isBase ? Data.baseNight : Data.lampNight;
                float intensity = lamp.baseIntensity * Mathf.Lerp(day, night, on) * power;
                Show(lamp, intensity);
            }
            else if (lamp.glow != null)
            {
                Show(lamp, on * power);
            }
        }

        foreach (Lamp window in windows)
        {
            float power = Power(window, !blackout, now, deltaTime);
            float on = window.windowLit ? DayNightModel.LightOn(Night, window.slot, spread) : 0f;
            Show(window, on * power);
        }
    }

    // Energia de uma luz (0 a 1): no apagão pisca até o seu offAt e apaga; na volta espera a onda (onAt) e acende.
    private float Power(Lamp lamp, bool powered, float now, float deltaTime)
    {
        if (!powered)
        {
            if (now < lamp.offAt) return Mathf.PerlinNoise(now * 25f, lamp.slot * 40f) > .45f ? 1f : .15f;
            lamp.power = Mathf.MoveTowards(lamp.power, 0f, fadeSpeed * 2f * deltaTime);
            return lamp.power;
        }

        if (now < lamp.onAt) return lamp.power;
        lamp.power = Mathf.MoveTowards(lamp.power, 1f, fadeSpeed * deltaTime);
        return lamp.power;
    }

    // Só mexe no componente quando o valor muda; apagado de vez = desligado (sem custo de desenho).
    private void Show(Lamp lamp, float value)
    {
        if (Mathf.Abs(value - lamp.shown) < .004f) return;
        lamp.shown = value;

        if (lamp.light != null)
        {
            lamp.light.intensity = value;
            lamp.light.enabled = value > .01f;
            return;
        }

        SpriteRenderer renderer = lamp.glow;
        if (lamp.visual != null)
        {
            bool warm = WarmKeyOn(renderer.sprite);
            renderer.color = new Color(warm ? 1f : 0f, 1f, 1f, value);
        }
        else
        {
            Color color = lamp.baseColor;
            color.a = lamp.baseColor.a * value;
            renderer.color = color;
        }
        renderer.enabled = value > .004f;
    }

    private static bool WarmKeyOn(Sprite sprite)
    {
        if (sprite == null || Data.warmKeyOff == null) return true;
        foreach (string prefix in Data.warmKeyOff)
        {
            if (!string.IsNullOrEmpty(prefix) && sprite.name.StartsWith(prefix)) return false;
        }
        return true;
    }

    private IEnumerable<Lamp> AllLamps()
    {
        foreach (Lamp lamp in lamps) yield return lamp;
        foreach (Lamp window in windows) yield return window;
    }

    #endregion

    #region EVENTS

    // Apagão: cada luz pisca um instante e apaga. Volta: a onda sai da Guarda (ou do centro da câmera) e reacende tudo.
    private void OnBlackoutChanged(bool active, Vector3 worldPosition)
    {
        blackout = active;
        float now = Time.time;
        Vector2 origin = WaveOrigin();
        foreach (Lamp lamp in AllLamps())
        {
            if (active)
            {
                lamp.offAt = now + DayNightModel.Slot(lamp.position + Vector2.one) * Data.blackoutFlicker;
                continue;
            }

            float distance = Vector2.Distance(origin, lamp.position);
            lamp.onAt = now + (Data.restoreWaveSpeed > 0f ? distance / Data.restoreWaveSpeed : 0f);
            lamp.power = 0f;
        }
    }

    private static Vector2 WaveOrigin()
    {
        if (LocationsManager.Instance != null && LocationsManager.Instance.Guards.Count > 0 && LocationsManager.Instance.Guards[0] != null)
            return LocationsManager.Instance.Guards[0].transform.position;
        return Camera.main != null ? (Vector2)Camera.main.transform.position : Vector2.zero;
    }

    #endregion

    #region BOOTSTRAP

    // Entra sozinho em toda cena com GameManager, como o FireEnergyLink: a Gameplay.unity não precisa mudar.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Spawn();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Spawn();

    private static void Spawn()
    {
        if (FindAnyObjectByType<GameManager>() == null) return;
        if (FindAnyObjectByType<DayNightCycle>(FindObjectsInactive.Include) != null) return;

        new GameObject("DayNight", typeof(DayNightCycle));
    }

    #endregion
}
