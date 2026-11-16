using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

//Vida dos trabalhadores na rua. Um só na cena (Feel.prefab): anima todos os que nascem em WorkerSpawned,
//sem componente no Worker.prefab. Não mexe na posição, que é do GPS: balanço por rotação, escala e pivô.
public class WorkerFeel : MonoBehaviour
{
    [Header("WALK")]
    [SerializeField] private float stepsPerSecond = 4.5f;
    [SerializeField] private float tiltAngle = 7f;
    [SerializeField] private float squash = .07f;
    [SerializeField] private float hopHeight = .14f;
    [SerializeField] private float blendSpeed = 10f;

    [Header("FACING")]
    [SerializeField] private bool spriteFacesRight = true;
    [SerializeField, Range(0f, 1f)] private float turnThreshold = .25f;

    [Header("SPAWN POP")]
    [SerializeField] private float spawnPopTime = .25f;

    [Header("ARRIVE POP (o GPS destrói o trabalhador 0,1 s depois)")]
    [SerializeField] private float arrivePopTime = .1f;
    [SerializeField] private float arrivePopScale = 1.35f;

    [Header("PUFFS (poeira do FeelParticles; escala sobre a altura do trabalhador)")]
    [SerializeField] private int puffCount = 6;
    [SerializeField] private int spawnPuffCount = 3;
    [SerializeField] private float puffScale = 1.2f;

    private class Walker
    {
        public Transform transform;
        public RectTransform rect;
        public Vector3 baseScale;
        public Vector2 basePivot;
        public Vector3 lastPosition;
        public float phase;
        public float walk;
        public float facing = 1f;
        public float age;
        public bool leaving;
        public float leaveAge;
    }

    private readonly List<Walker> walkers = new List<Walker>();

    private void OnEnable()
    {
        GameEvents.WorkerSpawned += OnWorkerSpawned;
        GameEvents.WorkerArrived += OnWorkerArrived;
    }

    private void OnDisable()
    {
        GameEvents.WorkerSpawned -= OnWorkerSpawned;
        GameEvents.WorkerArrived -= OnWorkerArrived;

        foreach (Walker walker in walkers) Restore(walker);
        walkers.Clear();
    }

    private void OnWorkerSpawned(Transform worker, Call call)
    {
        if (worker == null || Find(worker) != null) return;

        Walker walker = new Walker
        {
            transform = worker,
            rect = worker as RectTransform,
            baseScale = worker.localScale,
            lastPosition = worker.position,
            phase = Random.value * Mathf.PI * 2f,
        };
        if (walker.rect != null) walker.basePivot = walker.rect.pivot;

        walkers.Add(walker);
        Apply(walker, 0f);
        Puff(walker, spawnPuffCount);
    }

    private void OnWorkerArrived(Transform worker, Call call)
    {
        Walker walker = Find(worker);
        if (walker == null || walker.leaving) return;

        walker.leaving = true;

        Puff(walker, puffCount);
    }

    private void Puff(Walker walker, int count)
    {
        if (walker.rect == null || walker.transform.parent == null || count <= 0) return;

        FeelParticles.EmitAt(FxKind.Dust, walker.transform.parent, walker.transform.position, walker.rect.rect.height * puffScale, count);
    }

    private Walker Find(Transform worker)
    {
        foreach (Walker walker in walkers)
        {
            if (walker.transform == worker) return walker;
        }

        return null;
    }

    private void LateUpdate()
    {
        float delta = Time.deltaTime;

        for (int i = walkers.Count - 1; i >= 0; i--)
        {
            Walker walker = walkers[i];

            //O GPS já destruiu
            if (walker.transform == null)
            {
                walkers.RemoveAt(i);
                continue;
            }

            Apply(walker, delta);
        }
    }

    private void Apply(Walker walker, float delta)
    {
        Vector3 position = walker.transform.position;
        Vector3 move = position - walker.lastPosition;
        walker.lastPosition = position;

        bool moving = delta > 0f && move.sqrMagnitude > 0f;
        if (moving && Mathf.Abs(move.x) > move.magnitude * turnThreshold) walker.facing = Mathf.Sign(move.x);

        //O balanço entra e sai aos poucos quando o trabalhador anda ou para
        walker.walk = Mathf.MoveTowards(walker.walk, moving ? 1f : 0f, blendSpeed * delta);
        if (moving) walker.phase += stepsPerSecond * Mathf.PI * delta;

        float swing = Mathf.Sin(walker.phase) * walker.walk;
        float step = Mathf.Abs(swing);

        float pop = PopScale(walker, delta);
        float side = spriteFacesRight ? walker.facing : -walker.facing;

        walker.transform.localScale = new Vector3(
            walker.baseScale.x * side * pop * (1f + squash * (1f - step) * walker.walk),
            walker.baseScale.y * pop * (1f - squash * (1f - step) * walker.walk),
            walker.baseScale.z);
        walker.transform.localRotation = Quaternion.Euler(0f, 0f, swing * tiltAngle);

        //Pulinho: baixar o pivô sobe o desenho sem tocar na posição
        if (walker.rect != null) walker.rect.pivot = walker.basePivot - new Vector2(0f, hopHeight * step);
    }

    private float PopScale(Walker walker, float delta)
    {
        if (walker.leaving)
        {
            walker.leaveAge += delta;
            float t = arrivePopTime > 0f ? Mathf.Clamp01(walker.leaveAge / arrivePopTime) : 1f;
            //Incha na primeira metade e some na segunda
            return t < .5f ? Mathf.Lerp(1f, arrivePopScale, t * 2f) : Mathf.Lerp(arrivePopScale, 0f, (t - .5f) * 2f);
        }

        walker.age += delta;
        float life = spawnPopTime > 0f ? Mathf.Clamp01(walker.age / spawnPopTime) : 1f;
        return DOVirtual.EasedValue(0f, 1f, life, Ease.OutBack);
    }

    private void Restore(Walker walker)
    {
        if (walker.transform == null) return;

        walker.transform.localScale = walker.baseScale;
        walker.transform.localRotation = Quaternion.identity;
        if (walker.rect != null) walker.rect.pivot = walker.basePivot;
    }
}
