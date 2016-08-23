using NUnit.Framework;

namespace Plainion.CI.Tests
{
    [TestFixture]
    class DummyTests
    {
        [Test]
        public void WillSucceed()
        {
            Assert.That( true,Is.True );
        }

        [Test]
        public void WillFail()
        {
            Assert.Fail();
        }
    }
}
