using UnityEngine;
using UnityEngine.EventSystems;

//Painel de informação do chamado do quarteirão (v6G): passar o mouse em qualquer casa alagada ou destruída abre a janela
//pixel da própria casa (o InfoPanel "Tab" do House.prefab, que a casa comum não usa) com o nome do desastre, o que fazer
//e "Exige N trabalhadores". N é o Call.workersNeeded do líder. Posto em runtime nas casas do quarteirão atingido, sem
//editar a cena nem o prefab; some quando a equipe sai ou o quarteirão volta.
public class CallCrewInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("TEXTS")]
    [SerializeField] private string floodDescription = "Quarteirão alagado. Desentupa o bueiro.";
    [SerializeField] private string meteorDescription = "Quarteirão destruído. Reconstrua a quadra.";
    [SerializeField] private string otherDescription = "Mande a equipe inteira de uma vez.";

    [Header("SORTING")]
    [Tooltip("Acima da água, da cratera e da fumaça do incêndio (10000): a janela não fica atrás do quarteirão")]
    [SerializeField] private int sortingOrder = 10050;

    private House house;
    private InfoPanel panel;
    private bool open;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        GameEvents.BlockIncidentStarted -= OnIncidentStarted;
        GameEvents.BlockIncidentStarted += OnIncidentStarted;
    }

    private static void OnIncidentStarted(BlockIncident incident)
    {
        if (incident == null) return;

        foreach (House member in incident.houses)
        {
            if (member != null && member.GetComponent<CallCrewInfo>() == null) member.gameObject.AddComponent<CallCrewInfo>();
        }
    }

    private void Awake()
    {
        house = GetComponent<House>();
        panel = GetComponentInChildren<InfoPanel>(true);
    }

    private Call WaitingCall()
    {
        House leader = house != null ? house.IncidentLeader : null;
        if (leader == null || leader.HasTeam || leader.IsComplete) return null;

        Call call = leader.currentCall;
        return call != null && call.house == leader && CallCrew.Shows(call) ? call : null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Call call = WaitingCall();
        if (call == null || panel == null || GameManager.InputBlocked) return;

        BlockIncident incident = call.rule as BlockIncident;
        string title = incident != null && incident.disaster != null ? incident.disaster.displayName : "";
        string description = incident == null ? otherDescription
            : incident.kind == IncidentKind.Flood ? floodDescription : meteorDescription;

        panel.SetTexts(title, description, CallCrew.Requirement(CallCrew.Minimum(call)));
        panel.Show();
        panel.HighlightSpecific(FeelUI.PixelGold);
        RaiseAbove();
        open = true;
    }

    public void OnPointerExit(PointerEventData eventData) => Close();

    //Equipe enviada, quarteirão coletado ou fim de partida com o mouse parado em cima: a janela fecha sozinha
    private void Update()
    {
        if (open && (WaitingCall() == null || GameManager.InputBlocked)) Close();
    }

    private void OnDisable() => Close();

    private void Close()
    {
        if (!open) return;

        open = false;
        if (panel != null) panel.Hide();
    }

    //Canvas próprio por cima de tudo no mundo, na mesma camada de ordenação do Canvas da casa
    private void RaiseAbove()
    {
        Canvas own = panel.GetComponent<Canvas>();
        if (own == null)
        {
            Canvas parent = panel.transform.parent != null ? panel.transform.parent.GetComponentInParent<Canvas>() : null;
            own = panel.gameObject.AddComponent<Canvas>();
            if (parent != null) own.sortingLayerID = parent.sortingLayerID;
        }
        own.overrideSorting = true;
        own.sortingOrder = sortingOrder;
    }
}
