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

namespace Gibbed.Cryptic.ExportParserTables
{
    internal static class Locator
    {
        public static uint FindEnumTable(ProcessMemory memory)
        {
            StringBuilder sb;

            var messageOffset = memory.Search(new ByteSearch("00 53 74 61 74 69 63 44 65 66 69 6E 65 20 64 6F 65 73 20 6E 6F 74 20 68 61 76 65 20 61 20 6E 61 6D 65 2E 00"));
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
            var nameOffset = memory.Search(new ByteSearch("00 66 66 5F 50 61 72 73 65 54 61 62 6C 65 49 6E 66 6F 73 00"));
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
                bytes[0], bytes[1], bytes[2], bytes[3]);
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
            sb.Append("55 8B EC 81 EC F0 00 00 00 53 56 57 ");
            sb.Append("8D BD 10 FF FF FF B9 3C 00 00 00 B8 CC CC CC CC F3 AB ");
            sb.Append("C7 45 F8 00 00 00 00 8D 45 F8 50 8B 4D 08 51 ");
            sb.Append("E8 xx xx xx xx ");
            sb.Append("83 C4 04 50 ");
            sb.Append("8B 15 xx xx xx xx ");
            sb.Append("52 ");
            sb.Append("E8 xx xx xx xx ");
            sb.Append("83 C4 0C 83 7D F8 00 ");
            sb.Append("0F 84 xx xx xx xx ");
            sb.Append("C7 45 E0 00 00 00 00 ");
            sb.Append("C7 45 D4 xx xx xx xx ");
            sb.Append("8B 45 F8 81 B8 64 01 00 00 81 00 00 00 ");
            sb.Append("75 xx ");
            sb.Append("8B 45 F8 8B 88 7C 01 00 00 89 4D D4 EB xx ");
            sb.Append("8B 45 F8 81 B8 64 01 00 00 82 00 00 00 ");
            sb.Append("75 xx ");
            sb.Append("C7 45 D4 xx xx xx xx ");
            sb.Append("EB xx ");

            var codeOffset = memory.Search(new ByteSearch(sb.ToString()));
            if (codeOffset == uint.MaxValue)
            {
                //throw new InvalidOperationException();
                return 0;
            }

            var pointer = memory.ReadU32(codeOffset + 56);
            if (pointer == 0)
            {
                throw new InvalidOperationException();
            }

            return memory.ReadU32(pointer);
        }
    }
}
