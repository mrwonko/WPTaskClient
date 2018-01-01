using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using WPTaskClient.Protocol.ASN1;

namespace WPTaskClientTests.Proto
{
    [TestClass]
    class ASN1Test
    {
        [TestMethod]
        public void TestParseHeader()
        {
            // given
            var bytes = new byte[] { 0b00_1_10000, 0b1_0000010, 0x13, 0xd8 };
            var reader = new BinaryReader(new MemoryStream(bytes));
            // when
            var header = new Header(reader);
            // then
            Assert.AreEqual(IdentifierType.Sequence, header.Type);
            Assert.AreEqual(IdentifierClass.Universal, header.Class);
            Assert.AreEqual(true, header.Constructed);
            Assert.AreEqual(0x13d8, header.Length);
        }
    }
}
