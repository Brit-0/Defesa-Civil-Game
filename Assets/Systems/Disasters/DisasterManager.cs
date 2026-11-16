using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Linha do tempo dos desastres (GDD, seções 5 e 13). Só roda em GameState.Playing.
// Um desastre por vez: aviso, espera, execução, e só então a contagem do próximo.
public class DisasterManager : MonoBehaviour
{
    public static DisasterManager Instance;

    private const string DisastersPath = "Disasters";

    private DisasterData[] disasters;
    private bool isFirstDisaster = true;
    private bool isRunning;
    // Segundos de partida até o próximo aviso.
    private float countdown;

    public bool IsRunning => isRunning;

    private void Awake()
    {
        Instance = this;

        disasters = Resources.LoadAll<DisasterData>(DisastersPath);
        if (disasters.Length == 0)
        {
            Debug.LogWarning($"Nenhum DisasterData em Resources/{DisastersPath}: sem desastres. Rode Tools/Jam/Create Default Data.");
        }

        BalanceData balance = Stats.Balance;
        countdown = Mathf.Max(0f, balance.firstDisasterDelay - balance.warningTime);
    }

    private void Update()
    {
        if (disasters.Length == 0 || isRunning || !IsPlaying()) return;

        countdown -= Time.deltaTime;
        if (countdown > 0f) return;

        DisasterData disaster = PickDisaster();
        if (disaster == null)
        {
            ScheduleNext();
            return;
        }

        int houseCount = isFirstDisaster ? Stats.Balance.firstDisasterHouses : GetRampedHouseCount();
        StartCoroutine(RunDisaster(disaster, houseCount, isFirstDisaster));
        isFirstDisaster = false;
    }

    private static bool IsPlaying()
    {
        return GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
    }

    #region TIMELINE

    // 0 no começo da partida, 1 quando a rampa termina.
    private static float GetRamp()
    {
        float duration = Stats.Balance.disasterRampDuration;
        if (duration <= 0f || GameManager.Instance == null) return 1f;

        return Mathf.Clamp01(GameManager.Instance.ElapsedTime / duration);
    }

    private static float GetRampedInterval()
    {
        return Mathf.Lerp(Stats.Balance.disasterIntervalStart, Stats.Balance.disasterIntervalEnd, GetRamp());
    }

    private static int GetRampedHouseCount()
    {
        return Mathf.RoundToInt(Mathf.Lerp(Stats.Balance.disasterHousesStart, Stats.Balance.disasterHousesEnd, GetRamp()));
    }

    private void ScheduleNext()
    {
        countdown = Mathf.Max(0f, GetRampedInterval() - Stats.Balance.warningTime);
    }

    // O primeiro desastre é sempre um terremoto (o tutorial). Na v1 só existe terremoto.
    private DisasterData PickDisaster()
    {
        return FindDisaster(DisasterType.Earthquake);
    }

    private DisasterData FindDisaster(DisasterType type)
    {
        foreach (DisasterData disaster in disasters)
        {
            if (disaster.type == type) return disaster;
        }
        return null;
    }

    private IEnumerator RunDisaster(DisasterData disaster, int houseCount, bool nearGuard)
    {
        isRunning = true;

        float warningTime = Stats.Balance.warningTime;
        GameEvents.RaiseDisasterWarning(disaster, warningTime);
        yield return new WaitForSeconds(warningTime);

        // A partida pode ter acabado durante o aviso.
        if (IsPlaying())
        {
            switch (disaster.type)
            {
                case DisasterType.Earthquake:
                    yield return Earthquake(disaster, houseCount, nearGuard);
                    break;
                default:
                    break;
            }
        }

        ScheduleNext();
        isRunning = false;
    }

    #endregion

    #region EARTHQUAKE

    // Epicentro + raio: as casas mais próximas do epicentro quebram em onda, com dano maior perto do centro.
    private IEnumerator Earthquake(DisasterData disaster, int houseCount, bool nearGuard)
    {
        if (LocationsManager.Instance == null || LocationsManager.Instance.Houses.Count == 0 || houseCount <= 0) yield break;

        Vector3 epicenter = PickEpicenter(nearGuard);

        List<House> targets = new(LocationsManager.Instance.Houses);
        targets.Sort((a, b) => DistanceTo(a, epicenter).CompareTo(DistanceTo(b, epicenter)));
        if (targets.Count > houseCount) targets.RemoveRange(houseCount, targets.Count - houseCount);
        float radius = DistanceTo(targets[targets.Count - 1], epicenter);

        GameEvents.RaiseDisasterStarted(disaster, epicenter);

        // A onda: cada casa quebra com um atraso proporcional à distância do epicentro.
        float delayPerUnit = Stats.Balance.earthquakeWaveDelayPerUnit;
        float elapsed = 0f;
        foreach (House house in targets)
        {
            float distance = DistanceTo(house, epicenter);
            float delay = distance * delayPerUnit;
            if (delay > elapsed)
            {
                yield return new WaitForSeconds(delay - elapsed);
                elapsed = delay;
            }

            // Casa que já tem problema, Guarda e Armazém são ignorados dentro de ApplyProblem.
            house.ApplyProblem(State.Damaged, GetDamageByDistance(distance, radius));
        }

        GameEvents.RaiseDisasterEnded(disaster);
    }

    // Primeiro desastre: na Guarda. Depois: numa casa intacta sorteada, para o desastre nunca cair no vazio.
    private static Vector3 PickEpicenter(bool nearGuard)
    {
        LocationsManager locations = LocationsManager.Instance;

        if (nearGuard && locations.Guards.Count > 0) return locations.Guards[0].transform.position;

        List<House> candidates = locations.GetIntactHouses();
        if (candidates.Count == 0) candidates = locations.Houses;
        return candidates[Random.Range(0, candidates.Count)].transform.position;
    }

    // Dano máximo no centro, mínimo na borda, em faixas iguais do raio.
    private static int GetDamageByDistance(float distance, float radius)
    {
        int max = Stats.MaxDamage;
        int min = Stats.MinDamage;
        if (radius <= 0f) return max;

        int steps = max - min + 1;
        int drop = Mathf.FloorToInt(distance / radius * steps);
        return Mathf.Clamp(max - drop, min, max);
    }

    private static float DistanceTo(House house, Vector3 point)
    {
        return LocationsManager.Distance(house.transform.position, point);
    }

    #endregion
}
