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

    // O "1 por trabalhador" do asset é o material que cada um traz: acompanha o upgrade Carrinho (v5F).
    protected override string SpecificText => $"{Stats.MaterialPerWorker} por trabalhador";

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
