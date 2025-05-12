using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace IndexerLib.Tokens
{
    public static class TokenSerializer
    {
        // Serializes multiple tokens consecutively
        public static byte[] SerializeMany(List<Token> tokens)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                foreach (var token in tokens)
                    DoSerialization(writer, token);

                writer.Flush();
                return ms.ToArray();
            }
        }


        public static byte[] Serialize(Token token)
        {
            using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
                {
                    DoSerialization(writer, token);

                    writer.Flush();
                return ms.ToArray();
            }
        }

        static void DoSerialization(BinaryWriter writer, Token token)
        {
            writer.Write(token.FilePath ?? string.Empty);
            writer.Write(token.Postings.Count);

            foreach (var p in token.Postings)
            {
                writer.Write(p.Position);
                writer.Write(p.StartIndex);
                writer.Write(p.Length);
            }
        }


        // Deserializes the consecutive tokens one by one
        public static IEnumerable<Token> Deserialize(byte[] data)
        {
            if (data == null)
                yield return null;
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms, Encoding.UTF8))
            {
                while (true)
                {
                    Token token;
                    try
                    {
                        token = new Token { FilePath = reader.ReadString() };
                        int count = reader.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            token.Postings.Add(new Postings
                            {
                                Position = reader.ReadInt32(),
                                StartIndex = reader.ReadInt32(),
                                Length = reader.ReadInt32()
                            });
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        yield break;
                    }

                    yield return token;
                }
            }
        }
    }
}
