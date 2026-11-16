using System.Collections.Generic;
using UnityEngine;

public class LocationsManager : MonoBehaviour
{
    public static LocationsManager Instance;

    // Houses tem só as casas comuns: Guarda e Armazém herdam de House e ficam nas listas próprias.
    public List<House> Houses { get; private set; } = new();
    public List<CityGuard> Guards { get; private set; } = new();
    public List<Warehouse> Warehouses { get; private set; } = new();

    private void Awake()
    {
        Instance = this;
        Refresh();
    }

    public void Refresh()
    {
        Houses.Clear();
        Guards.Clear();
        Warehouses.Clear();

        foreach (House house in FindObjectsByType<House>())
        {
            if (house is CityGuard guard) Guards.Add(guard);
            else if (house is Warehouse warehouse) Warehouses.Add(warehouse);
            else if (house is Location) continue;
            else Houses.Add(house);
        }
    }

    #region QUERIES

    public List<House> GetHousesInRadius(Vector3 center, float radius)
    {
        List<House> result = new();
        foreach (House house in Houses)
        {
            if (Distance(house.transform.position, center) <= radius) result.Add(house);
        }
        return result;
    }

    public List<House> GetIntactHouses()
    {
        List<House> result = new();
        foreach (House house in Houses)
        {
            if (house.currentState == State.Intact) result.Add(house);
        }
        return result;
    }

    public List<House> GetTroubledHouses()
    {
        List<House> result = new();
        foreach (House house in Houses)
        {
            if (house.currentState != State.Intact) result.Add(house);
        }
        return result;
    }

    public CityGuard GetNearestGuard(Vector3 position)
    {
        CityGuard nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (CityGuard guard in Guards)
        {
            float distance = Distance(guard.transform.position, position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = guard;
            }
        }
        return nearest;
    }

    // A cidade é plana (Canvas World Space): a distância ignora o Z.
    public static float Distance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(a, b);
    }

    #endregion
}
