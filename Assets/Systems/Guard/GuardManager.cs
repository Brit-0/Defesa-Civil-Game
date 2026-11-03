using System.Collections;
using TMPro;
using UnityEngine;

public class Call 
{
    public House house;
    public int workersNeeded;
    public CallType type;
}

public enum CallType
{
    Problem,
    Restock
}

public class GuardManager : MonoBehaviour
{
    public static GuardManager Instance;

    private CityGuard selectedGuard;
    private Call currentCall;
    
    public static bool selectBaseMode;

    [Header("WORKERS")]
    [SerializeField] private GameObject workerPf;
    [SerializeField] private Transform workersParent;
    [SerializeField] private TMP_Text teamSizeLbl;
    private int teamSize;
    public int TeamSize
    {
        get => teamSize;
        set
        {
            teamSize = value;
            teamSizeLbl.text = teamSize.ToString();
        }
    }

    [Header("CALL")]
    [SerializeField] private GameObject callTab;

    private void Awake()
    {
        Instance = this;
    }

    public void OpenCall(Call call)
    {
        currentCall = call;
        selectBaseMode = true;
        FeedbacksManager.ShowText("Escolha a base encarregada para o chamado");
    }

    public void SelectBase(CityGuard guard)
    {
        selectBaseMode = false;
        selectedGuard = guard;
        FeedbacksManager.HideText();
        TeamSize = 0;
        callTab.SetActive(true);
    }

    public void CallSendTeam()
    {
        StartCoroutine(SendTeam());
    }

    public IEnumerator SendTeam()
    {
        callTab.SetActive(false);
        currentCall.house.interactable = false;

        for (int i = 0; i < teamSize; i++)
        {
            Transform newWorker = Instantiate(workerPf, selectedGuard.entry.transform.position, Quaternion.identity, workersParent).transform;
            StartCoroutine(GPS.main.FollowPath(newWorker, GPS.main.FindShortestPath(selectedGuard.entry, currentCall.house.entry), i == teamSize - 1));
            yield return new WaitForSeconds(.8f);
        }
    }

    public void IncreaseTeamSize()
    {
        TeamSize++;
    }

    public void DecreaseTeamSize()
    {
        TeamSize--;
    }
}
