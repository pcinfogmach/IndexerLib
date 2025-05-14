using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndexerLib.Helpers
{
    public static class CompactInt
    {
        public static void WriteCompactInt(this BinaryWriter writer, int value)
        {
            if (value <= byte.MaxValue)
            {
                writer.Write((byte)0); // type marker
                writer.Write((byte)value);
            }
            else if (value <= ushort.MaxValue)
            {
                writer.Write((byte)1);
                writer.Write((ushort)value);
            }
            else
            {
                writer.Write((byte)2);
                writer.Write(value);
            }
        }

        public static int ReadCompactInt(this BinaryReader reader)
        {
            byte marker = reader.ReadByte();

            switch (marker)
            {
                case 0:
                    return reader.ReadByte();
                case 1:
                    return reader.ReadUInt16();
                case 2:
                    return reader.ReadInt32();
                default:
                    throw new InvalidDataException("Invalid compact int marker.");
            }
        }
    }
}
