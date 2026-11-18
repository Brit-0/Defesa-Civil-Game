using NUnit.Framework;
using UnityEngine;

// Testes de editor da v6A (#526): a seta do pino de chamado e a da placa da expansão apontam para o alvo à vista. Com a
// casa (ou o terreno) logo abaixo da margem de cima, o pino e a placa desciam e a seta virava para cima, para longe dele.
public class MarkerArrowTests
{
    [Test]
    public void PinArrow_PointsDown_AlsoWhenTheTopMarginPushesThePinOntoTheRoof()
    {
        var top = new Vector2(0f, 100f);
        var middle = new Vector2(0f, 60f);

        // pino no lugar, em cima da casa: mira o topo, seta para baixo
        Vector2 above = new Vector2(0f, 140f);
        Assert.Less((MapMarkers.ArrowAim(above, top, middle, false) - above).y, 0f);

        // margem de cima empurrou o pino para baixo do topo (em cima do telhado): mira o meio, ainda para baixo
        Vector2 pushed = new Vector2(0f, 80f);
        Vector2 aim = MapMarkers.ArrowAim(pushed, top, middle, false);
        Assert.AreEqual(middle, aim);
        Assert.Less((aim - pushed).y, 0f);

        // na borda continua mirando o meio da casa
        Assert.AreEqual(middle, MapMarkers.ArrowAim(new Vector2(500f, 0f), top, middle, true));
    }

    [Test]
    public void ExpansionSign_GoesToTheEdgeOnlyWithTheLotOutOfView()
    {
        var safe = Rect.MinMaxRect(-100f, -50f, 100f, 50f);

        // terreno à vista e placa na faixa da margem de cima: a placa entra em safe, sem seta de borda
        Vector2 sign = new Vector2(30f, 70f);
        Vector2 lot = new Vector2(30f, 40f);
        Vector2 inside = ExpansionMath.PlaceSign(sign, lot, safe, out float angle, out bool clamped);
        Assert.IsFalse(clamped, "terreno à vista: a placa não vai para a borda");
        Assert.AreEqual(new Vector2(30f, 50f), inside);
        Assert.GreaterOrEqual(inside.y, lot.y, "a placa fica em cima do terreno e a seta de repouso aponta para baixo");

        // terreno fora da vista, acima: borda de cima, seta para fora (para cima)
        Vector2 edge = ExpansionMath.PlaceSign(new Vector2(30f, 400f), new Vector2(30f, 370f), safe, out angle, out clamped);
        Assert.IsTrue(clamped);
        Assert.AreEqual(50f, edge.y, 1e-3f);
        Assert.Less(Mathf.Abs(Mathf.DeltaAngle(0f, angle)), 10f);

        // placa e terreno à vista: fica onde está
        Vector2 still = ExpansionMath.PlaceSign(new Vector2(10f, 10f), new Vector2(10f, -5f), safe, out _, out clamped);
        Assert.IsFalse(clamped);
        Assert.AreEqual(new Vector2(10f, 10f), still);
    }
}
