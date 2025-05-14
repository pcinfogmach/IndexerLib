using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using IndexerLib.Helpers;

namespace IndexerLib.Tokens
{
    public static class TokenSerializer
    {
        public static byte[] Serialize(Token token)
        {
            using (var ms = new MemoryStream())
            using (var writer = new MyBinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                DoSerialization(writer, token);
                return ms.ToArray();
            }
        }

        static void DoSerialization(MyBinaryWriter writer, Token token)
        {
            writer.Write7BitEncodedInt(token.ID);
            writer.Write7BitEncodedInt(token.Postings.Count);

            int prevPos = 0, prevStart = 0;
            foreach (var p in token.Postings.OrderBy(x => x.Position))
            {
                writer.Write7BitEncodedInt(p.Position - prevPos);
                writer.Write7BitEncodedInt(p.StartIndex - prevStart);
                writer.Write7BitEncodedInt(p.Length);

                prevPos = p.Position;
                prevStart = p.StartIndex;
            }
        }


        // Deserializes the consecutive tokens one by one using delta decoding
        public static IEnumerable<Token> Deserialize(byte[] data)
        {
            if (data == null)
                yield break;

            using (var ms = new MemoryStream(data))
            using (var reader = new MyBinaryReader(ms, Encoding.UTF8))
            {
                while (ms.Position < ms.Length)
                {
                    var token = DoDeserialize(reader);
                    if (token == null)
                        yield break;

                    yield return token;
                }
            }
        }

        static Token DoDeserialize(MyBinaryReader reader)
        {
            try
            {
                var token = new Token { ID = reader.Read7BitEncodedInt() };

                int count = reader.Read7BitEncodedInt();
                int prevPos = 0, prevStart = 0;
                for (int i = 0; i < count; i++)
                {
                    prevPos += reader.Read7BitEncodedInt();
                    prevStart += reader.Read7BitEncodedInt();
                    int len = reader.Read7BitEncodedInt();

                    token.Postings.Add(new Postings
                    {
                        Position = prevPos,
                        StartIndex = prevStart,
                        Length = len
                    });
                }
                return token;
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }

    }
}
