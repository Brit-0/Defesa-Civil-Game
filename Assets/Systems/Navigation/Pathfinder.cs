using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Pathfinder
{
    public static List<Waypoint> FindPath(Waypoint start, Waypoint goal)
    {
        List<PathNode> openList = new();
        HashSet<Waypoint> closedList = new();

        Dictionary<Waypoint, PathNode> nodes = new();

        PathNode startNode = new(start)
        {
            gCost = 0,
            hCost = Heuristic(start, goal)
        };

        nodes[start] = startNode;
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            // Escolhe o nó com menor F
            PathNode current = openList
                .OrderBy(n => n.FCost)
                .ThenBy(n => n.hCost)
                .First();

            if (current.waypoint == goal)
                return BuildPath(current);

            openList.Remove(current);
            closedList.Add(current.waypoint);

            foreach (Waypoint neighbor in current.waypoint.neighbors)
            {
                if (closedList.Contains(neighbor))
                    continue;

                float newCost =
                    current.gCost +
                    Distance(current.waypoint, neighbor);

                if (!nodes.TryGetValue(neighbor, out PathNode neighborNode))
                {
                    neighborNode = new PathNode(neighbor);
                    nodes[neighbor] = neighborNode;
                }

                if (!openList.Contains(neighborNode) || newCost < neighborNode.gCost)
                {
                    neighborNode.gCost = newCost;
                    neighborNode.hCost = Heuristic(neighbor, goal);
                    neighborNode.parent = current;

                    if (!openList.Contains(neighborNode))
                        openList.Add(neighborNode);
                }
            }
        }

        return null;
    }

    static List<Waypoint> BuildPath(PathNode endNode)
    {
        List<Waypoint> path = new();

        PathNode current = endNode;

        while (current != null)
        {
            path.Add(current.waypoint);
            current = current.parent;
        }

        path.Reverse();

        return path;
    }

    static float Heuristic(Waypoint a, Waypoint b)
    {
        return Vector3.Distance(
            a.transform.position,
            b.transform.position);
    }

    static float Distance(Waypoint a, Waypoint b)
    {
        return Vector3.Distance(
            a.transform.position,
            b.transform.position);
    }
}