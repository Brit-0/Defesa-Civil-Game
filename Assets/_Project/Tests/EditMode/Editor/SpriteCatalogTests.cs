using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace DefesaCivil.CityGen.Tests
{
    public class SpriteCatalogTests
    {
        static SpriteCatalog cat;
        [OneTimeSetUp] public void Load() => cat = SpriteCatalog.Load();

        [Test] public void PoolCounts()
        {
            Assert.AreEqual(21, cat.Suburban.Count, "Suburban Casa_*");
            Assert.AreEqual(33, cat.Commercial.Count, "Commercial Predio/Arranha_Ceu/Fundo");
            Assert.AreEqual(20, cat.Industrial.Count, "Industrial Fabrica_*");
            Assert.AreEqual(133, cat.Decoration.Count, "tudo que não é estrutura nem da pasta Carros, menos as exclusões de Nature");
            Assert.IsFalse(cat.Decoration.Any(s => s.category == "Carros"), "nenhum sprite de carro");
        }

        [Test] public void AllSpritesEndWithS()
        {
            foreach (var s in cat.Suburban.Concat(cat.Commercial).Concat(cat.Industrial).Concat(cat.Decoration))
                StringAssert.EndsWith("_S", s.name);
        }

        [Test] public void ExclusionsRespected()
        {
            Assert.IsFalse(cat.Commercial.Any(s => s.texture == "Guarda_Sol"));
            foreach (var bad in new[] { "Barraca", "Barraca_Aberta", "Saco_Dormir", "Canoa", "Remo", "Fogueira_Pedras", "Lenha", "Placa_Madeira", "Ponte_Pedra", "Horta_1", "Milho_3", "Trigo_2", "Abobora", "Cenoura", "Melancia", "Nabo", "Vitoria_Regia_Flor" })
                Assert.IsFalse(cat.Decoration.Any(s => s.texture == bad), bad);
            Assert.IsTrue(cat.Decoration.Any(s => s.texture == "Estatua_Anel"), "monumentos permitidos");
        }

        [Test] public void NativeSizeIsPixelsOver32()
        {
            var casaD = cat.Suburban.First(s => s.texture == "Casa_D");
            Assert.AreEqual(112f / 32f, casaD.widthU, 1e-5f);
            Assert.AreEqual(128f / 32f, casaD.heightU, 1e-5f);
        }

        [Test] public void VisibleRectIsMeasured()
        {
            var casaD = cat.ByTexture("Casa_D");
            Assert.AreEqual(17f / 32f, casaD.padBottomU, 1e-5f);
            Assert.AreEqual(11f / 32f, casaD.padLeftU, 1e-5f);
            Assert.AreEqual(90f / 32f, casaD.visWU, 1e-5f);
            Assert.AreEqual(68f / 32f, casaD.visHU, 1e-5f);
            var rc = casaD.RectCenterFor(new Vector2(10f, 5f));
            Assert.AreEqual(new Vector2(10f, 5f), casaD.BaseCenterFor(rc - new Vector2(casaD.widthU, casaD.heightU) / 2f));
        }

        [Test] public void NorthVariants()
        {
            foreach (var s in cat.Suburban.Concat(cat.Commercial).Concat(cat.Industrial))
            {
                var n = cat.North(s);
                Assert.IsNotNull(n, s.name);
                StringAssert.EndsWith("_N", n.name);
                Assert.AreSame(n, cat.Get(n.ToRef()));
                Assert.IsTrue(cat.InHousePool(n.ToRef(), out _));
            }
        }

        [Test] public void LookupAndLoad()
        {
            var r = cat.Industrial[0].ToRef();
            Assert.IsTrue(cat.InGuardWarehousePool(r));
            Assert.IsTrue(cat.InHousePool(r, out var d)); Assert.AreEqual(District.Industrial, d);
            Assert.IsFalse(cat.InDecorationPool(r));
            Assert.IsNotNull(SpriteCatalog.LoadSprite(r));
            Assert.IsNull(cat.Get(new SpriteRef { guid = "x", name = "y" }));
        }
    }
}
