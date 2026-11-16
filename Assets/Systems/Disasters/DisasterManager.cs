using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// O que vem aí. Sai junto do DisasterWarning, para a apresentação marcar o lugar antes de cair (GAMELOOP_REVISAO).
public class DisasterForecast
{
    public DisasterData disaster;
    public Vector3 position;    // epicentro do terremoto; meio da cidade na tempestade; primeira casa do incêndio
    public float radius;        // alcance do terremoto em unidades de mundo; 0 nos outros
    public int houses;          // casas que o desastre vai tentar atingir
    public bool isStrong;       // terremoto forte (aviso mais longo, mais casas)
    public float seconds;       // até começar
    public readonly List<House> targets = new(); // casas já escolhidas (terremoto e incêndio); vazia na tempestade
}

// Um desastre e as casas que ele quebrou. Encerrada quando todas foram consertadas: paga o bônus.
public class Occurrence
{
    public DisasterData disaster;
    public Vector3 position;
    public bool isStrong;
    public float startTime;     // GameManager.ElapsedTime do início
    public float clearTime;     // segundos do início até a última casa consertada
    public int bonus;
    public bool fast;
    public bool ended;          // o desastre terminou de cair
    public readonly List<House> houses = new();
    public readonly HashSet<House> repaired = new();
}

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
    // Desastres sorteados depois do primeiro: a cada strongDisasterEvery vem um terremoto forte.
    private int disasterCount;
    private float extraCalm;

    // Pausa pedida de fora (o tutorial): a contagem não anda e a rampa não envelhece.
    private int holds;
    private float heldTime;

    private readonly List<Occurrence> occurrences = new();

    public bool IsRunning => isRunning;
    public bool IsHeld => holds > 0;
    // Segundos até o próximo aviso (para a apresentação mostrar a contagem).
    public float Countdown => countdown;
    // Tempo de partida que conta para a rampa e para liberar tipos: não inclui o tempo em pausa pedida.
    private float RampTime => GameManager.Instance != null ? GameManager.Instance.ElapsedTime - heldTime : 0f;

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

    private void OnEnable()
    {
        GameEvents.HouseStateChanged += OnHouseStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.HouseStateChanged -= OnHouseStateChanged;
    }

    private void Update()
    {
        if (disasters.Length == 0 || isRunning || !IsPlaying()) return;

        if (holds > 0)
        {
            heldTime += Time.deltaTime;
            return;
        }

        countdown -= Time.deltaTime;
        if (countdown > 0f) return;

        DisasterData disaster = PickDisaster();
        if (disaster == null)
        {
            ScheduleNext();
            return;
        }

        bool strong = !isFirstDisaster && IsStrongTurn();
        if (strong) disaster = FindDisaster(DisasterType.Earthquake) ?? disaster;

        int houseCount = isFirstDisaster ? Stats.Balance.firstDisasterHouses : GetRampedHouseCount();
        if (strong) houseCount = Mathf.RoundToInt(houseCount * Stats.Balance.strongDisasterHouseMultiplier);

        StartCoroutine(RunDisaster(disaster, houseCount, isFirstDisaster, strong));
        isFirstDisaster = false;
    }

    // O tutorial segura os desastres enquanto ensina. Cada Hold pede um Release.
    public void Hold()
    {
        holds++;
    }

    public void Release()
    {
        if (holds > 0) holds--;
    }

    private bool IsStrongTurn()
    {
        disasterCount++;
        int every = Stats.Balance.strongDisasterEvery;
        return every > 0 && disasterCount % every == 0;
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
    private float GetRamp()
    {
        float duration = Stats.Balance.disasterRampDuration;
        if (duration <= 0f || GameManager.Instance == null) return 1f;

        return Mathf.Clamp01(RampTime / duration);
    }

    private float GetRampedInterval()
    {
        return Mathf.Lerp(Stats.Balance.disasterIntervalStart, Stats.Balance.disasterIntervalEnd, GetRamp());
    }

    private int GetRampedHouseCount()
    {
        return Mathf.RoundToInt(Mathf.Lerp(Stats.Balance.disasterHousesStart, Stats.Balance.disasterHousesEnd, GetRamp()));
    }

    // Depois do terremoto forte vem uma calma maior: o pico e o respiro fazem o ritmo (GAMELOOP_REVISAO).
    private void ScheduleNext()
    {
        countdown = Mathf.Max(0f, GetRampedInterval() - Stats.Balance.warningTime) + extraCalm;
        extraCalm = 0f;
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
    private float GetWeight(DisasterType type)
    {
        BalanceData balance = Stats.Balance;
        float elapsed = RampTime;

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

    // A previsão em curso (do aviso até o desastre começar a cair). Null fora do aviso.
    public DisasterForecast CurrentForecast { get; private set; }

    private IEnumerator RunDisaster(DisasterData disaster, int houseCount, bool nearGuard, bool strong)
    {
        isRunning = true;

        BalanceData balance = Stats.Balance;
        float warningTime = strong ? balance.strongDisasterWarningTime : balance.warningTime;
        DisasterForecast forecast = Forecast(disaster, houseCount, nearGuard, strong, warningTime);

        CurrentForecast = forecast;
        GameEvents.RaiseDisasterForecasted(forecast);
        GameEvents.RaiseDisasterWarning(disaster, warningTime);
        yield return new WaitForSeconds(warningTime);
        CurrentForecast = null;

        // A partida pode ter acabado durante o aviso.
        if (IsPlaying())
        {
            Occurrence occurrence = new()
            {
                disaster = disaster,
                position = forecast.position,
                isStrong = strong,
                startTime = GameManager.Instance != null ? GameManager.Instance.ElapsedTime : 0f,
            };
            occurrences.Add(occurrence);

            switch (disaster.type)
            {
                case DisasterType.Earthquake:
                    yield return Earthquake(disaster, forecast, occurrence);
                    break;
                case DisasterType.Storm:
                    yield return Storm(disaster, houseCount, occurrence);
                    break;
                case DisasterType.Fire:
                    IsolatedFire(disaster, forecast, occurrence);
                    break;
                default:
                    break;
            }

            occurrence.ended = true;
            if (occurrence.houses.Count == 0) occurrences.Remove(occurrence);
            CheckOccurrences();
        }

        if (strong) extraCalm = balance.strongDisasterCalm;
        ClearShoring(forecast);
        ScheduleNext();
        isRunning = false;
    }

    // Escolhe antes do aviso onde o desastre cai: o jogador vê o alvo e pode se preparar (escorar casas).
    private DisasterForecast Forecast(DisasterData disaster, int houseCount, bool nearGuard, bool strong, float warningTime)
    {
        DisasterForecast forecast = new()
        {
            disaster = disaster,
            houses = houseCount,
            isStrong = strong,
            seconds = warningTime,
        };

        LocationsManager locations = LocationsManager.Instance;
        if (locations == null || locations.Houses.Count == 0) return forecast;

        switch (disaster.type)
        {
            case DisasterType.Earthquake:
                forecast.position = PickEpicenter(nearGuard);
                forecast.targets.AddRange(locations.Houses);
                forecast.targets.Sort((a, b) => DistanceTo(a, forecast.position).CompareTo(DistanceTo(b, forecast.position)));
                if (forecast.targets.Count > houseCount) forecast.targets.RemoveRange(houseCount, forecast.targets.Count - houseCount);
                forecast.radius = forecast.targets.Count > 0 ? DistanceTo(forecast.targets[forecast.targets.Count - 1], forecast.position) : 0f;
                break;
            case DisasterType.Fire:
                List<House> candidates = locations.GetIntactHouses();
                int fireCount = Mathf.Min(Random.Range(Stats.Balance.fireMinHouses, Stats.Balance.fireMaxHouses + 1), candidates.Count);
                for (int i = 0; i < fireCount; i++)
                {
                    int index = Random.Range(0, candidates.Count);
                    forecast.targets.Add(candidates[index]);
                    candidates.RemoveAt(index);
                }
                forecast.houses = forecast.targets.Count;
                forecast.position = forecast.targets.Count > 0 ? forecast.targets[0].transform.position : GetCityCenter();
                break;
            default:
                forecast.position = GetCityCenter();
                break;
        }

        return forecast;
    }

    #endregion

    #region SHORING

    // Mini game 3 (GAMELOOP_REVISAO): durante o aviso de terremoto, casa intacta que está no alvo pode ser escorada.
    public bool CanShore(House house)
    {
        DisasterForecast forecast = CurrentForecast;
        return forecast != null && forecast.disaster.type == DisasterType.Earthquake && house != null
            && !house.IsShored && house.currentState == State.Intact && forecast.targets.Contains(house);
    }

    private static void ClearShoring(DisasterForecast forecast)
    {
        foreach (House house in forecast.targets)
        {
            if (house != null) house.ClearShoring();
        }
    }

    #endregion

    #region OCCURRENCES

    private void OnHouseStateChanged(House house, State before, State after)
    {
        if (after != State.Intact) return;

        bool any = false;
        foreach (Occurrence occurrence in occurrences)
        {
            if (occurrence.houses.Contains(house)) any |= occurrence.repaired.Add(house);
        }
        if (any) CheckOccurrences();
    }

    // Ocorrência encerrada: todas as casas que o desastre quebrou foram consertadas. Paga o bônus (rápido vale mais).
    private void CheckOccurrences()
    {
        if (!IsPlaying()) return;

        for (int i = occurrences.Count - 1; i >= 0; i--)
        {
            Occurrence occurrence = occurrences[i];
            if (!occurrence.ended || occurrence.repaired.Count < occurrence.houses.Count) continue;

            occurrences.RemoveAt(i);
            occurrence.clearTime = (GameManager.Instance != null ? GameManager.Instance.ElapsedTime : 0f) - occurrence.startTime;
            occurrence.fast = occurrence.clearTime <= Stats.Balance.fastClearTime;
            occurrence.bonus = Stats.GetClearBonus(occurrence.houses.Count, occurrence.fast);
            if (occurrence.bonus > 0) ResourcesManager.Money += occurrence.bonus;
            GameEvents.RaiseOccurrenceCleared(occurrence);
        }
    }

    // A casa conta para a ocorrência só se o desastre de fato a quebrou.
    private static void Track(Occurrence occurrence, House house, bool applied)
    {
        if (applied && !occurrence.houses.Contains(house)) occurrence.houses.Add(house);
    }

    #endregion

    #region EARTHQUAKE

    // Epicentro + raio: as casas mais próximas do epicentro quebram em onda, com dano maior perto do centro.
    // Casa escorada no aviso perde parte do dano; se o dano zera, ela fica de pé.
    private IEnumerator Earthquake(DisasterData disaster, DisasterForecast forecast, Occurrence occurrence)
    {
        if (forecast.targets.Count == 0) yield break;

        Vector3 epicenter = forecast.position;
        float radius = forecast.radius;

        GameEvents.RaiseDisasterStarted(disaster, epicenter);

        // A onda: cada casa quebra com um atraso proporcional à distância do epicentro.
        float delayPerUnit = Stats.Balance.earthquakeWaveDelayPerUnit;
        float elapsed = 0f;
        foreach (House house in forecast.targets)
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

            int damage = GetDamageByDistance(distance, radius);
            if (house.IsShored)
            {
                damage -= Stats.Balance.shoreDamageReduction;
                house.ResolveShoring(damage <= 0);
                if (damage <= 0) continue;
            }

            // Casa que já tem problema, Guarda e Armazém são ignorados dentro de ApplyProblem.
            Track(occurrence, house, house.ApplyProblem(State.Damaged, damage));
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
    private IEnumerator Storm(DisasterData disaster, int strikeCount, Occurrence occurrence)
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
            bool applied = Random.value < balance.lightningFireChance
                ? house.ApplyProblem(State.OnFire, 0)
                : house.ApplyProblem(State.Damaged, Random.Range(balance.stormMinDamage, balance.stormMaxDamage + 1));
            Track(occurrence, house, applied);
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

    // Incêndio isolado: as casas sorteadas no aviso pegam fogo de uma vez (as que ainda estiverem intactas).
    private static void IsolatedFire(DisasterData disaster, DisasterForecast forecast, Occurrence occurrence)
    {
        if (forecast.targets.Count == 0) return;

        GameEvents.RaiseDisasterStarted(disaster, forecast.position);
        foreach (House house in forecast.targets)
        {
            Track(occurrence, house, house.ApplyProblem(State.OnFire, 0));
        }

        GameEvents.RaiseDisasterEnded(disaster);
    }

    #endregion
}
