using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class WaypointCreator : MonoBehaviour
{
    [SerializeField] private Waypoint selectedWp;
    [SerializeField] private GameObject wpPf;
    [SerializeField] private Transform waypointsParent;

#if UNITY_EDITOR

    [Button]
    public void CreateLinked()
    {
        if (!selectedWp)
        {
            print("Nenhum waypoint selecionado!");
            return;
        }

        Waypoint newWP = Instantiate(wpPf, selectedWp.transform.position, Quaternion.identity, waypointsParent).GetComponent<Waypoint>();
        newWP.neighbors.Add(selectedWp);
        selectedWp.neighbors.Add(newWP);
        selectedWp = newWP;
        Selection.activeGameObject = newWP.gameObject;
    }

    [Button]
    public void CreateUnlinked()
    {
        Waypoint newWP = Instantiate(wpPf, selectedWp.transform.position, Quaternion.identity, waypointsParent).GetComponent<Waypoint>();
        selectedWp = newWP;
        Selection.activeGameObject = newWP.gameObject;
    }

    [Button]
    public void LinkWaypoints(Waypoint wp)
    {
        wp.neighbors.Add(selectedWp);
        selectedWp.neighbors.Add(wp);
        //selectedWp = wp;
        Selection.activeGameObject = wp.gameObject;
    }

    [Button]
    public void SelectWaypoint(Waypoint wp)
    {
        selectedWp = wp;
    }

#endif
}
