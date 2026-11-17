using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DefesaCivil.CityGen.Tests
{
    public class ShadowTests
    {
        // Sombra para baixo e para a direita, contínua na linha da fachada; o telhado só anda o que a fachada manda
        [Test] public void FacadeIsHeightRoofIsDepth()
        {
            Vector2 o = ShadowModel.Offset;
            Assert.Greater(o.x, 0f, "sol à esquerda: sombra para a direita");
            Assert.Less(o.y, 0f, "sol em cima: sombra para baixo");
            Assert.AreEqual(ShadowModel.Length, o.magnitude, 1e-4f);

            const float facade = 40f;
            Vector2 atFacade = ShadowModel.Project(facade, facade);
            Assert.Less(Vector2.Distance(atFacade, ShadowModel.Project(facade + 1e-3f, facade)), 1e-2f, "contínua na fachada");
            Vector2 roof = ShadowModel.Project(facade + 30f, facade);
            Assert.AreEqual(atFacade.x, roof.x, 1e-4f, "telhado não anda mais para o lado");
            Assert.AreEqual(atFacade.y + 30f, roof.y, 1e-4f, "telhado é fundo: fica onde está");
            Assert.AreEqual(o.y * 70f, ShadowModel.Project(70f, float.MaxValue).y, 1e-4f, "árvore e poste: altura até a ponta");
            Assert.AreEqual(ShadowModel.Foot, ShadowModel.Strength(0f, 70f), 1e-4f);
            Assert.AreEqual(ShadowModel.Tip, ShadowModel.Strength(70f, 70f), 1e-4f);
        }

        // A sombra da base e do armazém novos (StructureShadow, no Play) segue a assada: base visível no pé do desenho e
        // fachada da mesma altura (malha justa do sprite x medida do catálogo), em até 2 px de 32
        [TestCase(StructureType.Guard)]
        [TestCase(StructureType.Warehouse)]
        public void ExpansionShadowMatchesBakedRule(string type)
        {
            var cat = SpriteCatalog.Load();
            var s = CityGenPaths.LoadLayout().structures.First(x => x.type == type);
            var cs = cat.Get(s.sprite);
            var go = new GameObject("sombra de teste", typeof(RectTransform));
            try
            {
                ((RectTransform)go.transform).sizeDelta = new Vector2(cs.widthU, cs.heightU);
                go.AddComponent<Image>().sprite = SpriteCatalog.LoadSprite(s.sprite);
                var shadow = go.AddComponent<StructureShadow>();
                var so = new SerializedObject(shadow);
                so.FindProperty("baseOffset").floatValue = cs.padBottomU;
                so.ApplyModifiedPropertiesWithoutUndo();

                var vh = new VertexHelper();
                shadow.ModifyMesh(vh);
                Assert.AreEqual(6, vh.currentVertCount, "três linhas: retângulo, fachada, topo");

                Rect r = ((RectTransform)go.transform).rect;
                float baseY = r.yMin + cs.padBottomU, facade = cs.visHU * ShadowModel.FacadeFraction;
                var v = new UIVertex();
                vh.PopulateUIVertex(ref v, 2);
                Vector2 want = new Vector2(r.xMin, baseY) + ShadowModel.Project(facade, facade);
                Assert.Less(Vector2.Distance(want, v.position), 2f / 32f, $"linha da fachada: {v.position} x {want}");
                vh.PopulateUIVertex(ref v, 0);
                Assert.AreEqual(ShadowModel.Foot, v.color.a / 255f, 0.01f, "força do pé");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // O chão assado não pinta sombra nem canteiro do que o terreno da expansão esconde, e só disso
        [Test] public void LotsHideOnlyDecorationsOnTheirPatch()
        {
            var cat = SpriteCatalog.Load();
            var l = CityGenPaths.LoadLayout();
            var hidden = CityGround.HiddenByLots(l, cat);
            Assert.Greater(hidden.Count, 0, "os terrenos ficam em praça e mata: alguma árvore sai");
            var lots = ExpansionData.Load().lots;
            foreach (var d in l.decorations.Where(d => hidden.Contains(d.id)))
                Assert.IsTrue(lots.Any(lot => Mathf.Abs(d.bottomCenter.x - lot.entry.x) < 3f && d.bottomCenter.y > lot.entry.y && d.bottomCenter.y < lot.entry.y + 4f),
                    $"decoração {d.id} longe dos terrenos");
        }
    }
}
