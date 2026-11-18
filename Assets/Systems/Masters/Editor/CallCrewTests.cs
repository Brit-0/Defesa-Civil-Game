using NUnit.Framework;
using UnityEngine;

// Testes de editor do mínimo de trabalhadores no chamado (v6G, CallCrew): o número sai do Call.workersNeeded (e, na
// enchente e no meteoro, do BlockDisasterData), o texto do pino, do painel e da aba usa esse número, e o aviso de envio
// curto conta quantos faltam. Sem cena.
public class CallCrewTests
{
    private static Call NewCall(int needed) => new Call { workersNeeded = needed, type = CallType.Problem };

    [Test]
    public void Minimum_ComesFromTheCall_AndNeverDropsBelowOne()
    {
        Assert.AreEqual(1, CallCrew.Minimum(null));
        Assert.AreEqual(1, CallCrew.Minimum(NewCall(0)));
        Assert.AreEqual(1, CallCrew.Minimum(NewCall(1)));
        Assert.AreEqual(2, CallCrew.Minimum(NewCall(2)));
        Assert.AreEqual(8, CallCrew.Minimum(NewCall(8)));

        Assert.IsFalse(CallCrew.Shows(null));
        Assert.IsFalse(CallCrew.Shows(NewCall(1)), "conserto comum não ganha o aviso");
        Assert.IsTrue(CallCrew.Shows(NewCall(2)));
        Assert.IsTrue(CallCrew.Shows(NewCall(5)), "qualquer chamado com mínimo ganha o aviso, não só enchente e meteoro");
    }

    [Test]
    public void Missing_MatchesTheDispatchRuleOfTheGuardManager()
    {
        // GuardManager nega o envio com size < workersNeeded: Enough tem que dizer o mesmo
        for (int needed = 1; needed <= 8; needed++)
        {
            for (int team = 0; team <= 10; team++)
            {
                Assert.AreEqual(team >= needed, CallCrew.Enough(team, needed), $"equipe {team} de {needed}");
                Assert.AreEqual(Mathf.Max(0, needed - team), CallCrew.Missing(team, needed));
            }
        }
        Assert.AreEqual(2, CallCrew.Missing(-3, 2), "equipe negativa conta como vazia");
    }

    [Test]
    public void Texts_UseTheNumberGiven()
    {
        Assert.AreEqual("Exige 1 trabalhador", CallCrew.Requirement(1));
        Assert.AreEqual("Exige 2 trabalhadores", CallCrew.Requirement(2));
        Assert.AreEqual("Exige 8 trabalhadores", CallCrew.Requirement(8));
        Assert.AreEqual("Exige 13 trabalhadores", CallCrew.Requirement(13));
        Assert.AreEqual("Exige 1 trabalhador", CallCrew.Requirement(0));

        Assert.AreEqual("0/2", CallCrew.Counter(0, 2));
        Assert.AreEqual("1/2", CallCrew.Counter(1, 2));
        Assert.AreEqual("9/8", CallCrew.Counter(9, 8), "gente a mais aparece como é");
        Assert.AreEqual("0/1", CallCrew.Counter(-1, 0));

        Assert.AreEqual("2", CallCrew.PinLabel(2));
        Assert.AreEqual("8", CallCrew.PinLabel(8));
    }

    [Test]
    public void Shortfall_AndWarning_CountWhoIsMissing()
    {
        Assert.AreEqual("Faltam 2 trabalhadores", CallCrew.Shortfall(0, 2));
        Assert.AreEqual("Falta 1 trabalhador", CallCrew.Shortfall(1, 2));
        Assert.AreEqual("Faltam 3 trabalhadores", CallCrew.Shortfall(5, 8));
        Assert.AreEqual("", CallCrew.Shortfall(2, 2));
        Assert.AreEqual("", CallCrew.Shortfall(9, 8));

        Assert.AreEqual("Exige 2: falta 1!", CallCrew.Warning(1, 2));
        Assert.AreEqual("Exige 8: faltam 3!", CallCrew.Warning(5, 8));
        Assert.AreEqual("Exige 8: faltam 8!", CallCrew.Warning(0, 8));
        Assert.AreEqual("", CallCrew.Warning(8, 8), "equipe completa não avisa");
    }

    [Test]
    public void DeniedText_SaysTheMinimumOnlyWhenTheStockCannotReachIt()
    {
        const string fallback = "Sem trabalhadores!";
        Assert.AreEqual("Exige 8 trabalhadores!", CallCrew.DeniedText(fallback, NewCall(8), 3));
        Assert.AreEqual("Exige 2 trabalhadores!", CallCrew.DeniedText(fallback, NewCall(2), 1));
        Assert.AreEqual(fallback, CallCrew.DeniedText(fallback, NewCall(8), 8), "estoque chega ao mínimo: falta é de outro tipo");
        Assert.AreEqual(fallback, CallCrew.DeniedText(fallback, NewCall(1), 0), "chamado comum fica como era");
        Assert.AreEqual(fallback, CallCrew.DeniedText(fallback, null, 0));
    }

    [Test]
    public void FloodAndMeteor_ShowTheNumbersOfTheBlockDisasterData()
    {
        BlockDisasterData data = Resources.Load<BlockDisasterData>(BlockDisasterData.ResourcePath);
        Assert.IsNotNull(data, "BlockDisasterData em Resources");

        // O mesmo Call que o BlockDisasterManager monta para o bueiro e para o canteiro
        foreach (int fromData in new[] { data.floodWorkers, data.meteorWorkers })
        {
            Call call = NewCall(Mathf.Max(1, fromData));
            int minimum = CallCrew.Minimum(call);
            Assert.AreEqual(Mathf.Max(1, fromData), minimum);
            Assert.AreEqual(minimum > 1, CallCrew.Shows(call));
            StringAssert.Contains(minimum.ToString(), CallCrew.Requirement(minimum));
            Assert.AreEqual(minimum.ToString(), CallCrew.PinLabel(minimum));
            StringAssert.EndsWith("/" + minimum, CallCrew.Counter(0, minimum));
        }
    }
}
