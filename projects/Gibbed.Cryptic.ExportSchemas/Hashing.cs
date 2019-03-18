/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gibbed.Cryptic.FileFormats;
using Parser = Gibbed.Cryptic.FileFormats.Parser;

namespace Gibbed.Cryptic.ExportSchemas
{
    internal static class Hashing
    {
        public static uint HashTable(ProcessMemory memory, uint address, uint hash)
        {
            var columns = new List<KeyValuePair<Natives.ParserColumn, string>>();
            var currentAddress = address;
            while (true)
            {
                var column = memory.ReadStructure<Natives.ParserColumn>(currentAddress);
                currentAddress += 40;

                var name = memory.ReadStringZ(column.NamePointer);

                if (column.Type == 0)
                {
                    if (string.IsNullOrEmpty(name) == true)
                    {
                        break;
                    }
                }

                columns.Add(new KeyValuePair<Natives.ParserColumn, string>(column, name));
            }

            foreach (var kv in columns)
            {
                var column = kv.Key;

                if (column.Flags.HasAnyOptions(Parser.ColumnFlags.REDUNDANTNAME |
                                               Parser.ColumnFlags.UNOWNED) == true)
                {
                    continue;
                }

                var name = kv.Value;

                if (string.IsNullOrEmpty(name) == false)
                {
                    hash = Adler32.Hash(name, hash);
                }

                hash = Adler32.Hash(column.Type, hash);

                var token = Parser.GlobalTokens.GetToken(column.Token);

                switch (token.GetParameter(column.Flags, 0))
                {
                    case Parser.ColumnParameter.NumberOfElements:
                    case Parser.ColumnParameter.Default:
                    case Parser.ColumnParameter.StringLength:
                    case Parser.ColumnParameter.Size:
                    {
                        hash = Adler32.Hash(column.Parameter0, hash);
                        break;
                    }

                    case Parser.ColumnParameter.BitOffset:
                    {
                        hash = Adler32.Hash(column.Parameter0 >> 16, hash);
                        break;
                    }

                    case Parser.ColumnParameter.DefaultString:
                    case Parser.ColumnParameter.CommandString:
                    {
                        if (column.Parameter0 != 0)
                        {
                            hash = Adler32.Hash(memory.ReadStringZ(column.Parameter0), hash);
                        }
                        break;
                    }
                }

                var param1 = token.GetParameter(column.Flags, 1);

                if (column.Parameter1 != 0 &&
                    (column.Token == 20 || column.Token == 21) &&
                    address != column.Parameter1 &&
                    column.Flags.HasAnyOptions(Parser.ColumnFlags.STRUCT_NORECURSE) == false)
                {
                    hash = HashTable(memory, column.Parameter1, hash);
                }

                if (column.Parameter1 != 0 &&
                    param1 == Parser.ColumnParameter.StaticDefineList)
                {
                    hash = HashStaticDefineList(memory, column.Parameter1, hash);
                }

                if (column.Token == 23)
                {
                    var formatString = memory.ReadStringZ(column.FormatStringPointer);
                    if (string.IsNullOrEmpty(formatString) == false)
                    {
                        hash = Adler32.Hash(formatString, hash);
                    }
                }
            }

            return hash;
        }

        private static uint HashKeyValueList(ProcessMemory memory, uint baseAddress, uint hash)
        {
            var entries = new List<KeyValuePair<string, string>>();
            var listAddress = memory.ReadU32(baseAddress);

            var count = memory.ReadS32(listAddress + 8);
            if (count > 0)
            {
                var entriesAddress = memory.ReadU32(listAddress + 4);
                var data = memory.ReadAllBytes(entriesAddress, count * 8);

                for (int i = 0; i < count; i++)
                {
                    var name = memory.ReadStringZ(BitConverter.ToUInt32(data, (i * 8) + 0));
                    var value = memory.ReadStringZ(BitConverter.ToUInt32(data, (i * 8) + 4));
                    entries.Add(new KeyValuePair<string, string>(name, value));
                }
            }

            var sb = new StringBuilder();
            foreach (var entry in entries.OrderBy(e => e.Key.ToLowerInvariant()))
            {
                sb.Append(entry.Key);
                sb.Append(entry.Value);
            }

            return Adler32.Hash(sb.ToString(), hash);
        }

        public static uint HashStaticDefineList(ProcessMemory memory, uint baseAddress, uint hash)
        {
            var valueType = 4;

            while (true)
            {
                var type = memory.ReadU32(baseAddress);
                if (type == 0)
                {
                    break;
                }

                switch (type)
                {
                    case 1:
                    {
                        valueType = 1;
                        baseAddress += 8;
                        break;
                    }

                    case 2:
                    {
                        valueType = 2;
                        baseAddress += 8;
                        break;
                    }

                    case 3:
                    {
                        var listAddress = memory.ReadU32(baseAddress + 4);
                        baseAddress += 8;

                        if (listAddress != 0)
                        {
                            hash = HashKeyValueList(memory, listAddress, hash);
                        }

                        break;
                    }

                    case 5:
                    {
                        var parent = memory.ReadU32(baseAddress + 4);
                        return HashStaticDefineList(memory, parent, hash);
                    }

                    default:
                    {
                        var name = memory.ReadStringZ(type);
                        hash = Adler32.Hash(name, hash);

                        switch (valueType)
                        {
                            case 1:
                            {
                                var value = memory.ReadU32(baseAddress + 4);
                                hash = Adler32.Hash(value, hash);
                                baseAddress += 8;
                                break;
                            }

                            case 2:
                            {
                                var value = memory.ReadStringZ(baseAddress + 4);
                                hash = Adler32.Hash(value, hash);
                                baseAddress += 8;
                                break;
                            }

                            default:
                            {
                                throw new NotImplementedException();
                            }
                        }

                        break;
                    }
                }
            }

            return hash;
        }
    }
}
