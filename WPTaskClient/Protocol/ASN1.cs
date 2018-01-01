using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Subset of ASN.1 (x.690) parsing as required for DER Representation of RSA Private Keys (integer and sequence)
// See https://www.itu.int/ITU-T/studygroups/com17/languages/X.690-0207.pdf
namespace WPTaskClient.Protocol.ASN1
{
    // first group of octets: Identifier
    public enum IdentifierType
    {
        Mask = 0x1F,
        Integer = 0x02,
        Sequence = 0x10,
        MultiByteType = 0x1F, // see following bytes for type number (unsupported)
    }
    enum IdentifierConstructed
    {
        Mask = 0x01 << 5,
        Primitive = 0x00 << 5, // i.e. the contents are the actual data
        Constructed = 0x01 << 5, // i.e. the contents are themselves to be parsed
    }
    public enum IdentifierClass
    {
        Mask = 0x03 << 6,
        Universal = 0x00 << 6, // only supported value
        Application = 0x01 << 6,
        ContextSpecific = 0x02 << 6,
        Private = 0x03 << 6,
    }
    // second group of octets: length
    public enum LengthForm
    {
        Mask = 0x01 << 7,
        Short = 0x00 << 7, // rest of byte contains length
        Long = 0x01 << 7, // rest of byte contains number of length bytes (or 0 for indefinite, which is illegal in DER)
    }

    public class Header
    {
        private readonly byte Identifier;
        public readonly int Length;

        public Header(BinaryReader reader)
        {
            Identifier = reader.ReadByte();
            if (Type == IdentifierType.MultiByteType)
            {
                throw new NotImplementedException("multi-byte ASN.1 DER types not implemented");
            }
            var lengthByte = reader.ReadByte();
            var form = (LengthForm)lengthByte & LengthForm.Mask;
            var lengthData = lengthByte & ((uint)LengthForm.Mask - 1);
            if (form == LengthForm.Short)
            {
                Length = (int)lengthData;
            }
            else
            {
                if (lengthData == 0) // indefinite form - illegal
                {
                    throw new NotImplementedException("indefinite length values not implemented");
                }
                if (lengthData > 4)
                {
                    throw new NotImplementedException("length > 2^32 not implemented");
                }
                uint length = 0;
                for (uint i = 0; i < lengthData; i++)
                {
                    length = (length << 8) | reader.ReadByte();
                }
                if (length > int.MaxValue)
                {
                    throw new NotImplementedException("length > 2^31 not implemented");
                }
                Length = (int)length;
            }
        }

        public IdentifierType Type => (IdentifierType)Identifier & IdentifierType.Mask;
        public bool Constructed => ((IdentifierConstructed)Identifier & IdentifierConstructed.Mask) == IdentifierConstructed.Constructed;
        public IdentifierClass Class => (IdentifierClass)Identifier & IdentifierClass.Mask;
    }

    public class Element
    {
        private Header header;
        public byte[] rawData;

        public Header Header => header;
        public byte[] RawData => rawData;

        public virtual void Parse(BinaryReader reader)
        {
            header = new Header(reader);
            rawData = reader.ReadBytes(header.Length);
            if (rawData.Length < header.Length)
            {
                throw new FormatException(string.Format("expected element of length {0} but only has {1} bytes available", header.Length, rawData.Length));
            }
        }
    }

    public class Integer : Element
    {
        public override void Parse(BinaryReader reader)
        {
            base.Parse(reader);
            if (Header.Type != IdentifierType.Integer)
            {
                throw new FormatException(string.Format("expected integer, got type {0}", Header.Type));
            }
        }
    }

    public class Sequence : Element, IEnumerable
    {
        private ICollection<Element> elements = new LinkedList<Element>();

        public void Add(Element element)
        {
            elements.Add(element);
        }

        public IEnumerator GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        public override void Parse(BinaryReader reader)
        {
            base.Parse(reader);
            if (Header.Type != IdentifierType.Sequence)
            {
                throw new FormatException(string.Format("expected sequence, got type {0}", Header.Type));
            }
            using (var subReader = new BinaryReader(new MemoryStream(RawData)))
            {
                foreach (var element in elements)
                {
                    element.Parse(subReader);
                }
            }
        }
    }
}
