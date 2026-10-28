using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class GPS : MonoBehaviour
{
    public static GPS main;

    [Header("WAYPOINTS")]
    [SerializeField] private List<Waypoint> allWaypoints = new();

    private List<Waypoint> path;

    [SerializeField] private LineRenderer pathDisplayer;

    [Header("ACTION POINTS")]
    [SerializeField] private TMP_Text pointsLbl;
    [SerializeField] private GameObject spentPointPf;
    [SerializeField] private Transform spentPointPos;
    [SerializeField] private Transform canvas;

    [Header("PLAYER")]
    [SerializeField] private Transform player;
    [SerializeField] private float speed;
    [Header("LOCATIONS")]
    public List<Location> allLocations = new();
    

    public bool isFollowingPath;


    void Awake()
    {
        main = this;
    }

    [Button]
    public IEnumerator FollowPath(Location destination)
    {
        if (isFollowingPath) yield break;

        GameManager.BlockInput();

        if (path == null) FindShortestPath(destination.entry);
        isFollowingPath = true;

        //CurrentLocation.GetComponent<Image>().raycastTarget = true;
        destination.GetComponent<Image>().raycastTarget = false;

        foreach (Waypoint waypoint in path)
        {
            while (Vector3.Distance(player.position, waypoint.transform.position) > 0.01f)
            {
                player.position = Vector3.MoveTowards(player.position, waypoint.transform.position, speed * Time.deltaTime);

                yield return null;
            }
        }

        //CurrentLocation = destination;
        isFollowingPath = false;
        GameManager.UnblockInput();
        ResetPath();
    }

    [Button]
    public void FindShortestPath(Waypoint destinationWaypoint)
    {
        //if (isFollowingPath || CurrentLocation.entry == destinationWaypoint) return 0;
        //path = Pathfinder.FindPath(CurrentLocation.entry, destinationWaypoint);
        ShowPath();
    }   

    public void ResetPath()
    {
        if (isFollowingPath) return;
        
        path = null;
        pathDisplayer.positionCount = 0;
    }

    private void ShowPath()
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
    }
}
