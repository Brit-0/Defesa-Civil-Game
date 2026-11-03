using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EletricPole : MonoBehaviour
{
    [SerializeField] private int range;
    [SerializeField] private List<EletricPole> connectedPoles = new();
    [SerializeField] private LineRenderer lr;
    [SerializeField] private GameObject areaDisplay;

    private void Update()
    {
        Build();
    }

    public void Build()
    {
        List<Collider2D> allHouses = Physics2D.OverlapCircleAll(transform.position, range, LayerMask.GetMask("Houses")).ToList();
        foreach (Collider2D house in allHouses)
        {
            house.gameObject.GetComponent<House>().Energize();
        }
    }

    public void ShowArea()
    {
        areaDisplay.SetActive(true);
        List<Collider2D> allHouses = Physics2D.OverlapCircleAll(transform.position, range, LayerMask.GetMask("Houses")).ToList();
        foreach (Collider2D house in allHouses)
        {
            house.gameObject.GetComponent<House>().ShowEnergyStatus();
        }
    }

    public void HideArea()
    {
        areaDisplay.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new(1f, 0f, 0f, .3f);
        Gizmos.DrawSphere(transform.position, range);
    }
}
