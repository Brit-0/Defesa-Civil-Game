using UnityEngine;

//Mínimo de trabalhadores do chamado (v6G): a enchente pede 2 e o meteoro 8, e o jogador não tinha como saber. O número
//vem sempre do Call.workersNeeded (que o BlockDisasterManager lê do BlockDisasterData), nunca do texto: outro chamado
//com mínimo ganha o mesmo aviso no pino, no painel de informação e na aba de chamado. Regra pura, testada no
//CallCrewTests.
public static class CallCrew
{
    public const string RequirementOne = "Exige 1 trabalhador";
    public const string RequirementMany = "Exige {0} trabalhadores";
    public const string CounterFormat = "{0}/{1}";
    public const string PinFormat = "{0}";
    public const string MissingOne = "Falta 1 trabalhador";
    public const string MissingMany = "Faltam {0} trabalhadores";
    public const string WarningOne = "Exige {0}: falta 1!";
    public const string WarningMany = "Exige {0}: faltam {1}!";
    public const string NotEnoughWorkers = "Exige {0} trabalhadores!";

    //Chamado comum vale 1 (workersNeeded 0 ou chamado nulo também)
    public static int Minimum(Call call) => call != null ? Mathf.Max(1, call.workersNeeded) : 1;

    //Só o chamado que pede mais de um trabalhador ganha o aviso (o conserto comum continua como era)
    public static bool Shows(Call call) => Minimum(call) > 1;

    public static int Missing(int team, int minimum) => Mathf.Max(0, Mathf.Max(1, minimum) - Mathf.Max(0, team));

    public static bool Enough(int team, int minimum) => Missing(team, minimum) == 0;

    //"Exige 2 trabalhadores" (painel de informação e aba)
    public static string Requirement(int minimum)
    {
        minimum = Mathf.Max(1, minimum);
        return minimum == 1 ? RequirementOne : string.Format(RequirementMany, minimum);
    }

    //"1/2": equipe escolhida no + e no - contra o mínimo
    public static string Counter(int team, int minimum) => string.Format(CounterFormat, Mathf.Max(0, team), Mathf.Max(1, minimum));

    //Número da etiqueta do pino, ao lado do bonequinho de trabalhador
    public static string PinLabel(int minimum) => string.Format(PinFormat, Mathf.Max(1, minimum));

    //Dica da aba com a equipe curta: "Falta 1 trabalhador" / "Faltam 6 trabalhadores"; vazio com a equipe completa
    public static string Shortfall(int team, int minimum)
    {
        int missing = Missing(team, minimum);
        if (missing == 0) return "";
        return missing == 1 ? MissingOne : string.Format(MissingMany, missing);
    }

    //Texto curto que sobe ao clicar em ENVIAR com menos que o mínimo: "Exige 8: faltam 3!"; vazio com a equipe completa
    public static string Warning(int team, int minimum)
    {
        int missing = Missing(team, minimum);
        if (missing == 0) return "";
        minimum = Mathf.Max(1, minimum);
        return missing == 1 ? string.Format(WarningOne, minimum) : string.Format(WarningMany, minimum, missing);
    }

    //"Sem trabalhadores!" vira "Exige 8 trabalhadores!" quando o estoque inteiro não chega ao mínimo do chamado aberto
    public static string DeniedText(string fallback, Call call, int available)
    {
        int minimum = Minimum(call);
        return minimum > 1 && available < minimum ? string.Format(NotEnoughWorkers, minimum) : fallback;
    }
}
