/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Text;

namespace Gibbed.Cryptic.ExportSchemas
{
    internal static class Locator
    {
        private static string ToByteString(string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        public static uint FindEnumTable(ProcessMemory memory)
        {
            StringBuilder sb;

            var messageOffset = memory.Search(
                new ByteSearch("00 " + ToByteString("StaticDefine does not have a name.") + " 00"));
            if (messageOffset == uint.MaxValue)
            {
                //throw new InvalidOperationException();
                return 0;
            }
            messageOffset = messageOffset + 1;

            var messageOffsetBytes = BitConverter.GetBytes(messageOffset);
            sb = new StringBuilder();
            sb.Append("57 56 ");
            sb.Append("E8 xx xx xx xx ");
            sb.Append("8B F8 83 C4 04 ");
            sb.Append("C7 45 FC 00 00 00 00 ");
            sb.Append("85 FF 75 0B 5F ");
            sb.AppendFormat("B8 {0:X2} {1:X2} {2:X2} {3:X2} ",
                            messageOffsetBytes[0],
                            messageOffsetBytes[1],
                            messageOffsetBytes[2],
                            messageOffsetBytes[3]);
            sb.Append("5E 8B E5 5D C3 ");

            var codeOffset1 = memory.Search(new ByteSearch(sb.ToString()));
            if (codeOffset1 == uint.MaxValue)
            {
                throw new InvalidOperationException();
            }

            var codeOffset2 = memory.ReadU32(codeOffset1 + 3);
            if (codeOffset2 == 0)
            {
                throw new InvalidOperationException();
            }

            codeOffset2 += codeOffset1 + 2 + 5;

            sb = new StringBuilder();
            sb.Append("55 8B EC 51 ");
            sb.Append("8B 0D xx xx xx xx ");
            sb.Append("33 C0 89 45 FC 85 C9 74 14 8B 55 08 8D 45 FC 50 52 51 ");
            sb.Append("E8 xx xx xx xx ");
            sb.Append("8B 45 FC 83 C4 0C 8B E5 5D C3 ");

            var codeOffset3 = memory.Search(new ByteSearch(sb.ToString()));
            if (codeOffset3 == uint.MaxValue)
            {
                throw new InvalidOperationException();
            }

            var pointer = memory.ReadU32(codeOffset3 + 6);
            if (pointer == 0)
            {
                throw new InvalidOperationException();
            }

            return memory.ReadU32(pointer);
        }

        public static uint FindParserTable(ProcessMemory memory)
        {
            var nameOffset = memory.Search(new ByteSearch("00 " + ToByteString("ff_ParseTableInfos") + " 00"));
            if (nameOffset == uint.MaxValue)
            {
                //throw new InvalidOperationException();
                return 0;
            }
            nameOffset = nameOffset + 1;

            var bytes = BitConverter.GetBytes(nameOffset);
            var sb = new StringBuilder();
            sb.Append("75 21 ");
            sb.Append("68 xx xx xx xx ");
            sb.AppendFormat("68 {0:X2} {1:X2} {2:X2} {3:X2} ",
                            bytes[0],
                            bytes[1],
                            bytes[2],
                            bytes[3]);
            sb.Append("6A 00 68 00 04 00 00 ");
            sb.Append("E8 xx xx xx xx ");
            sb.Append("8B 55 1C ");
            sb.Append("83 C4 10 ");
            sb.Append("A3 xx xx xx xx ");

            var codeOffset = memory.Search(new ByteSearch(sb.ToString()));
            if (codeOffset == uint.MaxValue)
            {
                //throw new InvalidOperationException();
                return 0;
            }

            var pointer = memory.ReadU32(codeOffset + 31);
            if (pointer == 0)
            {
                throw new InvalidOperationException();
            }

            return memory.ReadU32(pointer);
        }

        public static uint FindOldParserTable(ProcessMemory memory)
        {
            var nameOffset = memory.Search(new ByteSearch("00 " + ToByteString("ff_ParseTableInfos") + " 00"));
            if (nameOffset == uint.MaxValue)
            {
                //throw new InvalidOperationException();
                return 0;
            }
            nameOffset = nameOffset + 1;

            var bytes = BitConverter.GetBytes(nameOffset);
            var sb = new StringBuilder();
            sb.Append("75 1E ");
            sb.Append("68 xx xx xx xx ");
            sb.AppendFormat("68 {0:X2} {1:X2} {2:X2} {3:X2} ",
                            bytes[0],
                            bytes[1],
                            bytes[2],
                            bytes[3]);
            sb.Append("6A 00 68 00 04 00 00 ");
            sb.Append("E8 xx xx xx xx ");
            sb.Append("83 C4 10 ");
            sb.Append("A3 xx xx xx xx ");
            sb.Append("8B 55 0C ");

            var codeOffset = memory.Search(new ByteSearch(sb.ToString()));
            if (codeOffset == uint.MaxValue)
            {
                //throw new InvalidOperationException();
                return 0;
            }

            var pointer = memory.ReadU32(codeOffset + 28);
            if (pointer == 0)
            {
                throw new InvalidOperationException();
            }

            return memory.ReadU32(pointer);
        }

        public static uint FindExpressionFunctionTable(ProcessMemory memory)
        {
            StringBuilder sb;

            sb = new StringBuilder();
            sb.Append("55 8B EC 51 ");
            sb.Append("83 3D xx xx xx xx 00 ");
            sb.Append("53 56 57 ");
            sb.Append("75 xx ");
            sb.Append("68 xx 00 00 00 ");
            sb.Append("68 xx xx xx xx ");
            sb.Append("68 80 00 00 00 ");
            sb.Append("E8 xx xx xx xx ");
            sb.Append("68 xx 00 00 00 ");
            sb.Append("68 xx xx xx xx ");
            sb.Append("6A 00 6A 20 ");
            sb.Append("A3 xx xx xx xx ");
            sb.Append("E8 xx xx xx xx ");

            var codeOffset = memory.Search(new ByteSearch(sb.ToString()));
            if (codeOffset == uint.MaxValue)
            {
                //throw new InvalidOperationException();
                return 0;
            }

            var pointer1 = memory.ReadU32(codeOffset + 6);
            if (pointer1 == 0)
            {
                throw new InvalidOperationException();
            }

            var pointer2 = memory.ReadU32(codeOffset + 51);
            if (pointer2 == 0)
            {
                throw new InvalidOperationException();
            }

            if (pointer1 != pointer2)
            {
                throw new InvalidOperationException();
            }

            return memory.ReadU32(pointer1);
        }
    }
}
