using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DefesaCivil.CityGen.Tests
{
    public class SceneValidatorTests
    {
        static SpriteCatalog cat;
        TestCityScene ts;
        CityLayout layout;

        [OneTimeSetUp] public void Load() => cat = SpriteCatalog.Load();
        [SetUp] public void SetUp()
        {
            ts = TestCityScene.Create();
            layout = CityGenerator.Generate(5150, ts.center, cat, "t");
            new CityBuilder(layout, ts.refs, cat, undo: false).Run();
        }
        [TearDown] public void TearDown() => ts.Close();

        ValidationReport V(SceneBaseline b = null) => SceneValidator.Validate(ts.refs, layout, cat, b);
        Transform AnyHouse => ts.refs.houses.Cast<Transform>().First(t => t.GetComponent<House>().GetType() == typeof(House));
        Transform AnyTree => ts.refs.trees.GetChild(0);

        [Test] public void BuiltSceneHasNoErrorsOrWarnings()
        {
            var r = V();
            Assert.AreEqual(0, r.errors, string.Join("\n", r.violations.Take(10)));
            Assert.AreEqual(0, r.warnings, string.Join("\n", r.violations.Take(10)));
        }

        [Test] public void R05_WrongSprite()
        {
            var img = AnyHouse.Find("Visual").GetComponent<Image>();
            var path = UnityEditor.AssetDatabase.GetAssetPath(img.sprite);
            string wrong = img.sprite.name.EndsWith("_N") ? "_E" : "_N"; // direção que não é a da fileira
            img.sprite = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(x => x.name.EndsWith(wrong));
            Assert.IsTrue(V().Has("R05"));
        }

        [Test] public void R11_R31_SizeAndCollider()
        {
            var rt = (RectTransform)AnyHouse;
            rt.GetComponent<BoxCollider2D>().size = Vector2.one;
            Assert.IsTrue(V().Has("R31"));
            rt.sizeDelta *= 1.5f;
            Assert.IsTrue(V().Has("R11"));
        }

        [Test] public void R14_NullEntry()
        {
            AnyHouse.GetComponent<House>().entry = null;
            Assert.IsTrue(V().Has("R14"));
        }

        [Test] public void R20_TreeInStreet()
        {
            var t = AnyTree;
            // para fora de qualquer quarteirão: com -50 a árvore na escala real (16/11) caía inteira no quarteirão de baixo
            t.position += new Vector3(0, -1000f, 0);
            Assert.IsTrue(V().Has("R20"));
        }

        [Test] public void R26_ForeignObject()
        {
            new GameObject("Intruso", typeof(RectTransform)).transform.SetParent(ts.refs.houses, false);
            Assert.IsTrue(V().Has("R26"));
        }

        [Test] public void R26_BgCanvasIsAllowed()
        {
            Assert.IsFalse(V().violations.Any(v => v.ruleId == "R26" && v.@object == "Canvas"));
        }

        [Test] public void R29_MissingObject()
        {
            Object.DestroyImmediate(AnyTree.gameObject);
            Assert.IsTrue(V().Has("R29"));
        }

        [Test] public void R30_WrongOrder()
        {
            var first = ts.refs.houses.GetChild(0);
            first.GetComponent<Canvas>().sortingOrder += 500;
            Assert.IsTrue(V().Has("R30"));
        }

        [Test] public void R34_WrongFraming()
        {
            ts.refs.polygonCamera.orthographicSize = 3f;
            Assert.IsTrue(V().Has("R34"));
        }

        [Test] public void R27_LimiterMoved()
        {
            ts.refs.limiter.transform.position += Vector3.right;
            Assert.IsTrue(V().Has("R27"));
        }

        [Test] public void R28_Baseline()
        {
            var b = SceneBaseline.Capture(ts.refs);
            Assert.IsFalse(V(b).Has("R28"));
            ts.refs.polygonCamera.orthographicSize += 1f; // permitido
            Assert.IsFalse(V(b).Has("R28"));
            ts.refs.polygonCamera.fieldOfView = 30f; // não permitido
            Assert.IsTrue(V(b).Has("R28"));
        }

        [Test] public void R26_ForeignWaypoint()
        {
            Assert.IsFalse(V().Has("R26"));
            new GameObject("Waypoint").AddComponent<Waypoint>().transform.SetParent(ts.refs.waypoints, false);
            Assert.IsTrue(V().Has("R26"));
        }
    }
}
