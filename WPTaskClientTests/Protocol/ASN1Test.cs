using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using WPTaskClient.Protocol.ASN1;

namespace WPTaskClientTests.Proto
{
    [TestClass]
    public class ASN1Test
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

        [TestMethod]
        public void TestParseSequence()
        {
            // given
            var i1 = new Integer();
            var i2 = new Integer();
            var s = new Sequence { i1, i2 };
            var bytes = new byte[] {
                0b00_1_10000,0b0_0001001,
                0b00_0_00010, 0b0_000001, 42,
                0b00_0_00010, 0b0_000100, 0xde, 0xad, 0xc0, 0xde,
            };
            var reader = new BinaryReader(new MemoryStream(bytes));
            // when
            s.Parse(reader);
            // then
            CollectionAssert.AreEqual(new byte[] { 42 }, i1.RawData);
            CollectionAssert.AreEqual(new byte[] { 0xde, 0xad, 0xc0, 0xde }, i2.RawData);
        }

        // TODO: Test invalid input
    }
}
