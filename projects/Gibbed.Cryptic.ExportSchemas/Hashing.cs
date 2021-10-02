/* Copyright (c) 2021 Rick (rick 'at' gibbed 'dot' us)
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
using Gibbed.Cryptic.ExportSchemas.Natives.x64;
using Gibbed.Cryptic.ExportSchemas.Runtime;
using Gibbed.Cryptic.FileFormats;
using Parse = Gibbed.Cryptic.FileFormats.Parse;

namespace Gibbed.Cryptic.ExportSchemas
{
    // TODO(gibbed): This is all broken for 64-bit and hasn't been updated as it's
    // not used anywhere currently.
    internal static class Hashing
    {
        private static uint HashTable(RuntimeBase runtime, IntPtr address, uint hash)
        {
            throw new NotImplementedException();

            var columns = new List<KeyValuePair<ParseColumn, string>>();
            var currentAddress = address;
            while (true)
            {
                var column = runtime.ReadStructure<ParseColumn>(currentAddress);
                currentAddress += 40;

                var name = runtime.ReadStringZ(column.NamePointer, Encoding.ASCII);

                if (column.Type == 0)
                {
                    if (string.IsNullOrEmpty(name) == true)
                    {
                        break;
                    }
                }

                columns.Add(new KeyValuePair<ParseColumn, string>(column, name));
            }

            foreach (var kv in columns)
            {
                var column = kv.Key;

                if (column.Flags.HasAny(Parse.ColumnFlags.REDUNDANTNAME |
                                        Parse.ColumnFlags.UNOWNED) == true)
                {
                    continue;
                }

                var name = kv.Value;

                if (string.IsNullOrEmpty(name) == false)
                {
                    hash = Adler32.Hash(name, hash);
                }

                hash = Adler32.Hash(column.Type, hash);

                var token = Parse.GlobalTokens.GetToken(column.Token);

                switch (token.GetParameter(column.Flags, 0))
                {
                    case Parse.ColumnParameter.NumberOfElements:
                    case Parse.ColumnParameter.Default:
                    case Parse.ColumnParameter.StringLength:
                    case Parse.ColumnParameter.Size:
                    {
                        hash = Adler32.Hash(column.Parameter0.Int32, hash);
                        break;
                    }

                    case Parse.ColumnParameter.BitOffset:
                    {
                        hash = Adler32.Hash(column.Parameter0.Int32 >> 16, hash);
                        break;
                    }

                    case Parse.ColumnParameter.DefaultString:
                    case Parse.ColumnParameter.CommandString:
                    {
                        if (column.Parameter0.Pointer != default)
                        {
                            hash = Adler32.Hash(runtime.ReadStringZ(column.Parameter0.Pointer, Encoding.ASCII), hash);
                        }
                        break;
                    }
                }

                var param1 = token.GetParameter(column.Flags, 1);

                if (column.Parameter1.Pointer != default &&
                    (column.Token == 20 || column.Token == 21) &&
                    address != column.Parameter1.Pointer &&
                    column.Flags.HasAny(Parse.ColumnFlags.STRUCT_NORECURSE) == false)
                {
                    hash = HashTable(runtime, column.Parameter1.Pointer, hash);
                }

                if (column.Parameter1.Pointer != default &&
                    param1 == Parse.ColumnParameter.StaticDefineList)
                {
                    hash = HashStaticDefineList(runtime, column.Parameter1.Pointer, hash);
                }

                if (column.Token == 23)
                {
                    var formatString = runtime.ReadStringZ(column.FormatStringPointer, Encoding.ASCII);
                    if (string.IsNullOrEmpty(formatString) == false)
                    {
                        hash = Adler32.Hash(formatString, hash);
                    }
                }
            }

            return hash;
        }

        private static uint HashKeyValueList(RuntimeBase runtime, IntPtr baseAddress, uint hash)
        {
            var entries = runtime.ReadStringStashTable(baseAddress);
            var sb = new StringBuilder();
            foreach (var entry in entries.OrderBy(e => e.Name.ToLowerInvariant()))
            {
                sb.Append(entry.Name);
                sb.Append(entry.Value);
            }
            return Adler32.Hash(sb.ToString(), hash);
        }

        private static uint HashStaticDefineList(RuntimeBase runtime, IntPtr baseAddress, uint hash)
        {
            var valueType = 4;

            while (true)
            {
                var type = runtime.ReadValueU32(baseAddress);
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
                        var listAddress = runtime.ReadPointer(baseAddress + 4);
                        baseAddress += 8;

                        if (listAddress != IntPtr.Zero)
                        {
                            hash = HashKeyValueList(runtime, listAddress, hash);
                        }

                        break;
                    }

                    case 5:
                    {
                        var parent = runtime.ReadPointer(baseAddress + 4);
                        return HashStaticDefineList(runtime, parent, hash);
                    }

                    default:
                    {
                        // TODO(gibbed): FIXME
                        var name = runtime.ReadStringZ(new IntPtr(type), Encoding.ASCII);
                        hash = Adler32.Hash(name, hash);

                        switch (valueType)
                        {
                            case 1:
                            {
                                var value = runtime.ReadValueU32(baseAddress + 4);
                                hash = Adler32.Hash(value, hash);
                                baseAddress += 8;
                                break;
                            }

                            case 2:
                            {
                                var value = runtime.ReadStringZ(baseAddress + 4, Encoding.ASCII);
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
