using System.Collections.Generic;
using UnityEngine;

public class EletricityManager : MonoBehaviour, IAnalyzable
{
    public static EletricityManager main;
    public List<EletricPole> allPoles = new();

    private void Awake()
    {
        main = this;
    }

    public void EnterViewMode()
    {
        foreach (EletricPole pole in allPoles)
        {
            pole.ShowArea();
        }
    }

    public void ExitViewMode()
    {
        foreach (EletricPole pole in allPoles)
        {
            pole.HideArea();
        }
    }
}

