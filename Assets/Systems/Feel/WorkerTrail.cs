using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Rastro bem suave atrás de cada trabalhador, só para achá-los na rua (lista nova do Pietro, #532): uma fita clara no
//chão que afina e some em menos de 1 s. Um Graphic só, esticado no chão (PixelFx.Ground), desenha todos numa chamada,
//embaixo das casas como as pegadas. O WorkerFeel cria e alimenta; aqui não há regra nem evento.
public class WorkerTrail : MaskableGraphic
{
    private class Point
    {
        public Vector2 at;
        public float time;
    }

    private class Trail
    {
        public Transform worker;
        public float width;
        public float spacing;
        public readonly List<Point> points = new List<Point>();
    }

    private readonly List<Trail> trails = new List<Trail>();
    private float life = .7f;
    private float alpha = .3f;

    public static WorkerTrail Create(RectTransform ground, Color tint, float lifeSeconds, float alpha01)
    {
        var go = new GameObject("WorkerTrails", typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(ground, false);
        FeelUI.Stretch(rect);
        WorkerTrail trail = go.AddComponent<WorkerTrail>();
        trail.raycastTarget = false;
        trail.color = tint;
        trail.life = Mathf.Max(.05f, lifeSeconds);
        trail.alpha = Mathf.Clamp01(alpha01);
        return trail;
    }

    //Largura e espaço entre pontos nas unidades do chão (o WorkerFeel passa frações da altura do trabalhador)
    public void Track(Transform worker, float width, float spacing)
    {
        if (worker == null || Find(worker) != null) return;

        var trail = new Trail { worker = worker, width = width, spacing = Mathf.Max(.01f, spacing) };
        trail.points.Add(new Point { at = Local(worker.position), time = Time.time });
        trails.Add(trail);
    }

    //Chegou: para de crescer, a fita que ficou some sozinha
    public void Release(Transform worker)
    {
        Trail trail = Find(worker);
        if (trail != null) trail.worker = null;
    }

    private Trail Find(Transform worker)
    {
        foreach (Trail trail in trails)
        {
            if (trail.worker == worker) return trail;
        }
        return null;
    }

    private Vector2 Local(Vector3 world) => rectTransform.InverseTransformPoint(world);

    private void LateUpdate()
    {
        if (trails.Count == 0) return;

        float now = Time.time;
        for (int i = trails.Count - 1; i >= 0; i--)
        {
            Trail trail = trails[i];
            //Destruído pelo GPS sem passar pela chegada: vira fita solta
            if (trail.worker != null && !trail.worker.gameObject.activeInHierarchy) trail.worker = null;

            if (trail.worker != null)
            {
                Vector2 at = Local(trail.worker.position);
                if (Vector2.Distance(at, trail.points[trail.points.Count - 1].at) >= trail.spacing)
                    trail.points.Add(new Point { at = at, time = now });
            }

            while (trail.points.Count > 0 && now - trail.points[0].time > life) trail.points.RemoveAt(0);
            if (trail.points.Count == 0 && trail.worker == null) trails.RemoveAt(i);
        }

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        float now = Application.isPlaying ? Time.time : 0f;
        var strip = new List<Vector2>();
        var ages = new List<float>();

        foreach (Trail trail in trails)
        {
            strip.Clear();
            ages.Clear();
            foreach (Point point in trail.points)
            {
                strip.Add(point.at);
                ages.Add(now - point.time);
            }
            //A ponta vai colada no trabalhador, sem esperar o próximo ponto
            if (trail.worker != null)
            {
                strip.Add(Local(trail.worker.position));
                ages.Add(0f);
            }
            if (strip.Count < 2) continue;

            int first = vh.currentVertCount;
            for (int i = 0; i < strip.Count; i++)
            {
                Vector2 previous = strip[Mathf.Max(0, i - 1)];
                Vector2 next = strip[Mathf.Min(strip.Count - 1, i + 1)];
                Vector2 direction = next - previous;
                Vector2 side = direction.sqrMagnitude > 1e-8f ? new Vector2(-direction.y, direction.x).normalized : Vector2.up;

                //Some do rabo para a ponta: fina e transparente no fim, mais larga perto do trabalhador
                float fresh = 1f - Mathf.Clamp01(ages[i] / life);
                float half = trail.width * .5f * Mathf.Lerp(.25f, 1f, fresh);
                Color32 tint = color;
                tint.a = (byte)Mathf.RoundToInt(255f * alpha * fresh * fresh);

                vh.AddVert(strip[i] + side * half, tint, Vector4.zero);
                vh.AddVert(strip[i] - side * half, tint, Vector4.zero);
            }

            for (int i = 0; i < strip.Count - 1; i++)
            {
                int a = first + i * 2;
                vh.AddTriangle(a, a + 1, a + 3);
                vh.AddTriangle(a, a + 3, a + 2);
            }
        }
    }
}
