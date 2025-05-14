using System.IO;
using System.Text;

namespace IndexerLib.Helpers
{
    class MyBinaryReader : BinaryReader
    {
        public MyBinaryReader(Stream input)
            : base(input) { }
        public MyBinaryReader(Stream input, Encoding encoding)
            : base(input, encoding) { }
        public MyBinaryReader(Stream input, Encoding encoding, bool leaveOpen)
            : base(input, encoding, leaveOpen) { }

        public new int Read7BitEncodedInt()
        {
           return base.Read7BitEncodedInt();
        }

    }
}
