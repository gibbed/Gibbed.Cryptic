using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gibbed.Helpers;

namespace Gibbed.Cryptic.Champions.Deparse
{
    public static partial class StreamHelpers
    {
        public static bool ReadValueBit(this Stream stream)
        {
            byte first = stream.ReadValueU8();

            if (first == 0)
            {
                return false;
            }
            else if (first == 1)
            {
                return true;
            }

            throw new FormatException("boolean bit > 1");
        }

        public static Int32 ReadValuePackedS32(this Stream stream)
        {
            byte first = stream.ReadValueU8();
            UInt32 value = 0;

            byte size = (byte)(first & 3);
            value |= (byte)((first >> 2) & 0x3F);

            if (size == 1)
            {
                value |= (UInt32)stream.ReadValueU8() << 6;
            }
            else if (size == 2)
            {
                value |= (UInt32)stream.ReadValueU16() << 6;
            }
            else if (size == 3)
            {
                value |= (UInt32)stream.ReadValueU32() << 6;
            }

            return (Int32)value;
        }
    }
}
