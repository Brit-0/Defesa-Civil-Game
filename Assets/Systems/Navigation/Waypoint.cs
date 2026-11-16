using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public List<Waypoint> neighbors = new();

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, .1f);

        foreach (var neighbor in neighbors)
        {
            //Gizmos.DrawLine(transform.position, neighbor.transform.position);
            Handles.DrawBezier(transform.position, neighbor.transform.position, transform.position, neighbor.transform.position, Color.red, null, 10f);
        }
    }
#endif
}

public class PathNode
{
    public Waypoint waypoint;

    public PathNode parent;

    public float gCost;   // custo desde o início
    public float hCost;   // estimativa até o destino

    public float FCost => gCost + hCost;

    public PathNode(Waypoint waypoint)
    {
        this.waypoint = waypoint;
    }
}
