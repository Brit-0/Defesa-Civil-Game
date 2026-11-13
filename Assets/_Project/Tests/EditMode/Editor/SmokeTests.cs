using NUnit.Framework;

namespace DefesaCivil.CityGen.Tests
{
    public class SmokeTests
    {
        [Test]
        public void LayoutHasVersion()
        {
            Assert.AreEqual(1, new CityLayout().version);
        }
    }
}
