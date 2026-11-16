using UnityEngine;

public class Warehouse : Location
{
    protected override void Awake()
    {
        base.Awake();
        defaultCall = CreateRestockCall();
    }

    protected override void Start()
    {
        base.Start();
        currentCall = defaultCall;
    }

    public override void ExecuteLocationAction()
    {
        if (HasTeam) return;

        GuardManager.Instance.OpenCall(currentCall);
    }

    // Cada ida ao Armazém tem o seu chamado: a equipe que volta não divide dados com a próxima.
    protected override void OnCallResolved()
    {
        defaultCall = CreateRestockCall();
        currentCall = defaultCall;
    }

    private Call CreateRestockCall()
    {
        return new()
        {
            house = this,
            type = CallType.Restock
        };
    }
}
