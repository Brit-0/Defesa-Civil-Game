using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Linha do tempo dos desastres (GDD, seções 5 e 13). Só roda em GameState.Playing.
// Um desastre por vez: aviso, espera, execução, e só então a contagem do próximo.
public class DisasterManager : MonoBehaviour
{
    public static DisasterManager Instance;

    private const string DisastersPath = "Disasters";

    private static readonly DisasterType[] disasterTypes = { DisasterType.Earthquake, DisasterType.Storm, DisasterType.Fire };

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

    private static bool IsPaused()
    {
        return GameManager.Instance != null && GameManager.Instance.State == GameState.Paused;
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

    // O primeiro desastre é sempre um terremoto (o tutorial). Depois, sorteio por peso entre os tipos já liberados.
    private DisasterData PickDisaster()
    {
        if (isFirstDisaster)
        {
            DisasterData earthquake = FindDisaster(DisasterType.Earthquake);
            if (earthquake != null) return earthquake;
        }

        DisasterData picked = null;
        float totalWeight = 0f;
        foreach (DisasterType type in disasterTypes)
        {
            DisasterData disaster = FindDisaster(type);
            float weight = disaster != null ? GetWeight(type) : 0f;
            if (weight <= 0f) continue;

            // Sorteio por peso numa passada só: cada tipo toma o lugar com chance peso ÷ peso acumulado.
            totalWeight += weight;
            if (Random.value * totalWeight <= weight) picked = disaster;
        }
        return picked;
    }

    // Peso do tipo no sorteio. Vale 0 enquanto o tipo não foi liberado pelo tempo de partida.
    private static float GetWeight(DisasterType type)
    {
        BalanceData balance = Stats.Balance;
        float elapsed = GameManager.Instance != null ? GameManager.Instance.ElapsedTime : 0f;

        return type switch
        {
            DisasterType.Earthquake => balance.earthquakeWeight,
            DisasterType.Storm => elapsed >= balance.stormUnlockTime ? balance.stormWeight : 0f,
            DisasterType.Fire => elapsed >= balance.fireUnlockTime ? balance.fireWeight : 0f,
            _ => 0f,
        };
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
                case DisasterType.Storm:
                    yield return Storm(disaster, houseCount);
                    break;
                case DisasterType.Fire:
                    IsolatedFire(disaster);
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

            // Partida encerrada no meio da onda: as casas que faltam ficam de pé (a pausa já segura o WaitForSeconds).
            if (!IsPlaying() && !IsPaused()) break;

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

    #region STORM

    // Raios espalhados pela cidade ao longo da duração. Cada raio cai numa casa intacta sorteada na hora:
    // quebra a casa ou, com a chance do BalanceData, põe fogo nela.
    private IEnumerator Storm(DisasterData disaster, int strikeCount)
    {
        LocationsManager locations = LocationsManager.Instance;
        if (locations == null || locations.Houses.Count == 0 || strikeCount <= 0) yield break;

        BalanceData balance = Stats.Balance;
        float interval = balance.stormDuration / strikeCount;

        GameEvents.RaiseDisasterStarted(disaster, GetCityCenter());

        for (int i = 0; i < strikeCount; i++)
        {
            yield return new WaitForSeconds(interval);

            // A pausa segura o raio; partida encerrada acaba com a tempestade.
            while (IsPaused()) yield return null;
            if (!IsPlaying()) break;

            List<House> candidates = locations.GetIntactHouses();
            if (candidates.Count == 0) continue;

            House house = candidates[Random.Range(0, candidates.Count)];
            GameEvents.RaiseLightningStruck(house.transform.position);
            if (Random.value < balance.lightningFireChance) house.ApplyProblem(State.OnFire, 0);
            else house.ApplyProblem(State.Damaged, Random.Range(balance.stormMinDamage, balance.stormMaxDamage + 1));
        }

        GameEvents.RaiseDisasterEnded(disaster);
    }

    // A tempestade cobre a cidade inteira: o ponto do evento é o meio das casas.
    private static Vector3 GetCityCenter()
    {
        List<House> houses = LocationsManager.Instance.Houses;
        Vector3 sum = Vector3.zero;
        foreach (House house in houses)
        {
            sum += house.transform.position;
        }
        return sum / houses.Count;
    }

    #endregion

    #region FIRE

    // Incêndio isolado: poucas casas intactas sorteadas pegam fogo de uma vez.
    private static void IsolatedFire(DisasterData disaster)
    {
        if (LocationsManager.Instance == null) return;

        List<House> candidates = LocationsManager.Instance.GetIntactHouses();
        if (candidates.Count == 0) return;

        BalanceData balance = Stats.Balance;
        int fireCount = Mathf.Min(Random.Range(balance.fireMinHouses, balance.fireMaxHouses + 1), candidates.Count);

        for (int i = 0; i < fireCount; i++)
        {
            int index = Random.Range(0, candidates.Count);
            House house = candidates[index];
            candidates.RemoveAt(index);

            if (i == 0) GameEvents.RaiseDisasterStarted(disaster, house.transform.position);
            house.ApplyProblem(State.OnFire, 0);
        }

        GameEvents.RaiseDisasterEnded(disaster);
    }

    #endregion
}
