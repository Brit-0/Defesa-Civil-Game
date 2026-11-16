using NUnit.Framework;

namespace DefesaCivil.CityGen.Tests
{
    public class ScaleAuditTests
    {
        [Test] public void DecorationFitsNextToHouses()
        {
            ScaleAudit.Check(SpriteCatalog.Load(), out var failures);
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }
    }
}
