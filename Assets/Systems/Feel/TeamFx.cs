using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

//Equipe a caminho: na saída (TeamDispatched) acende no chão uma trilha de pontos da Base até o destino, pelo mesmo
//caminho do GPS, com setas ">" e uma onda de brilho correndo na direção do destino. Laranja da Defesa Civil para
//conserto, dourado para buscar material no Depósito. Some quando o trabalho começa ou o chamado acaba.
//Um na cena (Feel.prefab). Pontos em pool, no chão (embaixo das casas, em cima das ruas).
public class TeamFx : MonoBehaviour
{
    [Header("ROUTE (tamanhos em pixels da arte)")]
    [SerializeField] private Color problemColor = PixelFx.Orange;
    [SerializeField] private Color restockColor = PixelFx.Gold;
    [SerializeField] private float spacingPixels = 8f;
    [SerializeField] private int chevronEvery = 4;
    [SerializeField] private int maxDotsPerRoute = 140;

    [Header("WAVE (pontos por segundo e largura da onda em pontos)")]
    [SerializeField] private float waveSpeed = 9f;
    [SerializeField] private float waveLength = 6f;
    [SerializeField, Range(0f, 1f)] private float baseAlpha = .45f;

    [Header("FADE")]
    [SerializeField] private float drawTime = .5f;
    [SerializeField] private float fadeTime = .35f;

    private class Route
    {
        public Call call;
        public RectTransform root;
        public CanvasGroup group;
        public readonly List<Image> dots = new List<Image>();
        public float age;
        public bool leaving;
    }

    private readonly List<Route> routes = new List<Route>();
    private readonly Stack<Image> idleDots = new Stack<Image>();
    private readonly Stack<Image> idleChevrons = new Stack<Image>();

    private void OnEnable()
    {
        GameEvents.TeamDispatched += OnTeamDispatched;
        GameEvents.WorkStarted += OnCallMoved;
        GameEvents.CallResolved += OnCallResolved;
        GameEvents.GameStateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.TeamDispatched -= OnTeamDispatched;
        GameEvents.WorkStarted -= OnCallMoved;
        GameEvents.CallResolved -= OnCallResolved;
        GameEvents.GameStateChanged -= OnGameStateChanged;
    }

    private void OnTeamDispatched(Call call)
    {
        if (call == null || call.house == null || call.guard == null || GPS.main == null) return;

        RectTransform ground = PixelFx.Ground;
        if (ground == null) return;

        //Mesmo cálculo do GuardManager: só leitura, nada muda no jogo
        List<Waypoint> path = GPS.main.FindShortestPath(call.guard.entry, call.house.entry);
        if (path == null || path.Count == 0) return;

        List<Vector2> points = new List<Vector2> { PixelFx.ToLocal(ground, call.guard.entry.transform.position) };
        foreach (Waypoint waypoint in path)
        {
            if (waypoint != null) points.Add(PixelFx.ToLocal(ground, waypoint.transform.position));
        }

        Remove(call);
        routes.Add(Build(ground, call, points));
    }

    private void OnCallMoved(Call call)
    {
        Remove(call);
    }

    private void OnCallResolved(Call call, int money)
    {
        Remove(call);
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Playing || state == GameState.Paused) return;

        for (int i = routes.Count - 1; i >= 0; i--) Release(routes[i]);
        routes.Clear();
    }

    private Route Build(RectTransform ground, Call call, List<Vector2> points)
    {
        Route route = new Route { call = call };
        route.root = FeelUI.CreateRect("Route", ground);
        FeelUI.Place(route.root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        route.group = route.root.gameObject.AddComponent<CanvasGroup>();
        route.group.blocksRaycasts = false;

        Color color = call.type == CallType.Restock ? restockColor : problemColor;
        float pixel = PixelFx.ArtPixel;
        float spacing = Mathf.Max(2f, spacingPixels) * pixel;

        //Pontos a cada "spacing" ao longo da linha quebrada, contando a distância que sobra de um trecho para o outro
        float carry = spacing * .5f;
        int index = 0;
        for (int s = 0; s < points.Count - 1 && route.dots.Count < maxDotsPerRoute; s++)
        {
            Vector2 a = points[s];
            Vector2 b = points[s + 1];
            float length = Vector2.Distance(a, b);
            if (length < .01f) continue;

            Vector2 direction = (b - a) / length;
            float d = carry;
            for (; d < length && route.dots.Count < maxDotsPerRoute; d += spacing)
            {
                bool chevron = chevronEvery > 0 && index % chevronEvery == chevronEvery - 1;
                Image dot = Take(chevron, route.root, color);
                dot.rectTransform.anchoredPosition = a + direction * d;
                dot.rectTransform.localRotation = chevron ? Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) : Quaternion.identity;
                route.dots.Add(dot);
                index++;
            }

            carry = d - length;
        }

        //A trilha se desenha da Base para o destino
        for (int i = 0; i < route.dots.Count; i++) route.dots[i].rectTransform.localScale = Vector3.zero;
        return route;
    }

    private Image Take(bool chevron, RectTransform parent, Color color)
    {
        Stack<Image> stack = chevron ? idleChevrons : idleDots;
        Image image = null;
        while (stack.Count > 0 && image == null) image = stack.Pop();
        if (image == null) image = PixelFx.CreateSprite(chevron ? "Chevron" : "Dot", parent, chevron ? PixelFx.Chevron : PixelFx.Dot, color);

        image.rectTransform.SetParent(parent, false);
        image.color = color;
        image.gameObject.SetActive(true);
        return image;
    }

    private void Remove(Call call)
    {
        for (int i = routes.Count - 1; i >= 0; i--)
        {
            Route route = routes[i];
            if (route.call != call || route.leaving) continue;

            route.leaving = true;
            route.group.DOFade(0f, fadeTime).SetLink(route.root.gameObject).OnComplete(() =>
            {
                Release(route);
                routes.Remove(route);
            });
        }
    }

    private void Release(Route route)
    {
        foreach (Image dot in route.dots)
        {
            if (dot == null) continue;
            dot.gameObject.SetActive(false);
            dot.rectTransform.SetParent(transform, false);
            (dot.sprite == PixelFx.Chevron ? idleChevrons : idleDots).Push(dot);
        }

        route.dots.Clear();
        if (route.root != null)
        {
            route.root.DOKill();
            Destroy(route.root.gameObject);
        }
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        for (int r = routes.Count - 1; r >= 0; r--)
        {
            Route route = routes[r];
            if (route.root == null)
            {
                routes.RemoveAt(r);
                continue;
            }

            route.age += delta;
            int count = route.dots.Count;
            float drawn = drawTime > 0f ? route.age / drawTime * count : count;
            float head = Mathf.Repeat(route.age * waveSpeed, count + waveLength * 2f) - waveLength;

            for (int i = 0; i < count; i++)
            {
                Image dot = route.dots[i];
                if (dot == null) continue;

                //Desenho: cada ponto nasce com um pulinho na sua vez
                float grow = Mathf.Clamp01(drawn - i);
                dot.rectTransform.localScale = Vector3.one * (grow < 1f ? DOVirtual.EasedValue(0f, 1f, grow, Ease.OutBack) : 1f);

                //Onda de brilho andando para o destino
                float wave = 1f - Mathf.Clamp01(Mathf.Abs(i - head) / Mathf.Max(1f, waveLength * .5f));
                dot.canvasRenderer.SetAlpha(Mathf.Lerp(baseAlpha, 1f, wave));
            }
        }
    }
}
