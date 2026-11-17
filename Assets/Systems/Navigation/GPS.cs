using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GPS : MonoBehaviour
{
    public static GPS main;

    [Header("WAYPOINTS")]
    [SerializeField] private List<Waypoint> allWaypoints = new();

    [SerializeField] private LineRenderer pathDisplayer;


    [Header("PLAYER")]
    [SerializeField] private float arrivalDistance = 0.01f;
    [SerializeField] private float destroyDelay = .1f;
    [Header("LOCATIONS")]
    public List<House> allLocations = new();
    

    public bool isFollowingPath;


    void Awake()
    {
        main = this;
    }

    [Button]
    public IEnumerator FollowPath(Transform worker, List<Waypoint> path, Call call, bool isLastWorker = false, bool isReturning = false)
    { 
        isFollowingPath = true;
        House destination = isReturning ? call.guard : call.house;
        GameEvents.RaiseWorkerSpawned(worker, call);

        if (path == null)
        {
            // B10: sem caminho o trabalhador não anda; a chegada abaixo devolve ao estoque ou começa o trabalho.
            Debug.LogError($"Sem caminho até {destination.name}: trabalhador entregue direto no destino.");
        }
        else
        {
            foreach (Waypoint waypoint in path)
            {
                while (Vector3.Distance(worker.position, waypoint.transform.position) > arrivalDistance)
                {
                    worker.position = Vector3.MoveTowards(worker.position, waypoint.transform.position, Stats.WorkerSpeed * Time.deltaTime);

                    yield return null;
                }
            }
        }

        GameEvents.RaiseWorkerArrived(worker, call);
        Destroy(worker.gameObject, destroyDelay);
        if (isReturning) ResourcesManager.Workers++;

        if (isLastWorker && !isReturning)
        {
            destination.StartWorking();
        }
    }

    // Distância que o trabalhador anda no FollowPath: do ponto de saída até cada waypoint, em ordem (unidades de mundo).
    public static float PathLength(List<Waypoint> path, Vector3 start)
    {
        if (path == null) return 0f;

        float length = 0f;
        Vector3 from = start;
        foreach (Waypoint waypoint in path)
        {
            if (waypoint == null) continue;
            length += Vector3.Distance(from, waypoint.transform.position);
            from = waypoint.transform.position;
        }
        return length;
    }

    [Button]
    public List<Waypoint> FindShortestPath(Waypoint entryWaypoint, Waypoint destinationWaypoint)
    {
        return Pathfinder.FindPath(entryWaypoint, destinationWaypoint);
        //ShowPath();
    }   

    /*private void ShowPath()
    {
        if (path == null) return;
        
        Vector3[] pathPositions = new Vector3[99];
        pathDisplayer.positionCount = path.Count;
        // pathDisplayer.SetPositions(pathPositions);

        for (int i = 0; i < path.Count; i++)
        {
            pathPositions[i] = new(path[i].transform.position.x, path[i].transform.position.y, 0);
        }

        pathDisplayer.SetPositions(pathPositions);
    }*/
}
