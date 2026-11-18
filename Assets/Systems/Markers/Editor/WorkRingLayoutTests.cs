using NUnit.Framework;
using UnityEngine;

// Testes de editor da v5F (#525): a trilha escura do anel de progresso fica sempre logo atrás do anel, também na 2ª obra
// da mesma casa (antes ela passava para a frente e escurecia o anel e a janela dourada do ponto certo).
public class WorkRingLayoutTests
{
    private GameObject parent;

    [TearDown]
    public void TearDown()
    {
        if (parent != null) Object.DestroyImmediate(parent);
    }

    private Transform Child(string name)
    {
        var child = new GameObject(name, typeof(RectTransform)).transform;
        child.SetParent(parent.transform, false);
        return child;
    }

    [Test]
    public void PlaceBehind_NewTrackGoesRightBeforeTheRing()
    {
        parent = new GameObject("Progress parent", typeof(RectTransform));
        Child("Visual");
        Transform ring = Child("Progress");
        Child("Icon");
        Transform track = Child("ProgressTrack");

        WorkRingLayout.PlaceBehind(track, ring);

        Assert.AreEqual(ring.GetSiblingIndex() - 1, track.GetSiblingIndex());
    }

    [Test]
    public void PlaceBehind_SecondWorkKeepsTheTrackBehind()
    {
        parent = new GameObject("Progress parent", typeof(RectTransform));
        Child("Visual");
        Transform ring = Child("Progress");
        Child("Icon");
        Transform track = Child("ProgressTrack");

        // 1ª, 2ª e 3ª obra na mesma casa: a trilha já existe e está antes do anel
        for (int work = 0; work < 3; work++)
        {
            WorkRingLayout.PlaceBehind(track, ring);
            Assert.AreEqual(ring.GetSiblingIndex() - 1, track.GetSiblingIndex(), $"obra {work + 1}");
        }

        // trilha bem antes do anel (outro irmão entrou no meio): volta a encostar atrás dele
        track.SetSiblingIndex(0);
        WorkRingLayout.PlaceBehind(track, ring);
        Assert.AreEqual(ring.GetSiblingIndex() - 1, track.GetSiblingIndex());
    }
}
