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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Parser = Gibbed.Cryptic.FileFormats.Parser;

namespace Gibbed.Cryptic.ExportParserTables
{
    internal class Program
    {
        private static uint FindParserTableInfo(ProcessMemory memory)
        {
            var nameOffset = memory.Search(new ByteSearch("00 66 66 5F 50 61 72 73 65 54 61 62 6C 65 49 6E 66 6F 73 00"));
            if (nameOffset == uint.MaxValue)
            {
                throw new InvalidOperationException();
            }
            nameOffset = nameOffset + 1;

            var bytes = BitConverter.GetBytes(nameOffset);
            var sb = new StringBuilder();
            sb.Append("75 1E ");
            sb.Append("68 xx xx xx xx ");
            sb.AppendFormat("68 {0:X2} {1:X2} {2:X2} {3:X2} ",
                bytes[0], bytes[1], bytes[2], bytes[3]);
            sb.Append("6A 00 ");
            sb.Append("68 00 04 00 00 ");
            sb.Append("E8 xx xx xx xx ");
            sb.Append("83 C4 10 ");
            sb.Append("A3 xx xx xx xx ");
            sb.Append("8B 55 0C ");

            var codeOffset = memory.Search(new ByteSearch(sb.ToString()));
            if (codeOffset == uint.MaxValue)
            {
                throw new InvalidOperationException();
            }

            var pointer = memory.ReadU32(codeOffset + 28);
            if (pointer == 0)
            {
                throw new InvalidOperationException();
            }

            return memory.ReadU32(pointer);
        }

        private static string GetTokenName(NativeColumn column)
        {
            var flags = Parser.ColumnFlags.None;
            flags |= column.Flags & Parser.ColumnFlags.FIXED_ARRAY; // 0x80000
            flags |= column.Flags & Parser.ColumnFlags.EARRAY; // 0x40000
            flags |= column.Flags & Parser.ColumnFlags.INDIRECT; // 0x100000

            var token = Parser.GlobalTokens.GetToken(column.Token);

            if ((flags & Parser.ColumnFlags.INDIRECT) == 0)
            {
                switch (flags)
                {
                    case Parser.ColumnFlags.FIXED_ARRAY: return token.NameDirectFixedArray;
                    case Parser.ColumnFlags.EARRAY: return token.NameDirectArray;
                    default: return token.NameDirectValue;
                }
            }
            else
            {
                switch (flags & ~Parser.ColumnFlags.INDIRECT)
                {
                    case Parser.ColumnFlags.FIXED_ARRAY: return token.NameIndirectFixedArray;
                    case Parser.ColumnFlags.EARRAY: return token.NameIndirectArray;
                    default: return token.NameIndirectValue;
                }
            }
        }

        private static Dictionary<Parser.ColumnFlags, string> BasicFlags = new Dictionary<Parser.ColumnFlags,string>()
        {
            {Parser.ColumnFlags.FIXED_ARRAY, "FIXED_ARRAY"},
            {Parser.ColumnFlags.INDIRECT, "INDIRECT"},
            {Parser.ColumnFlags.POOL_STRING, "POOL_STRING"},
            {Parser.ColumnFlags.ESTRING, "ESTRING"},
            {Parser.ColumnFlags.OBJECTTYPE, "OBJECTTYPE"},
            {Parser.ColumnFlags.STRUCTPARAM, "STRUCTPARAM"},
            {Parser.ColumnFlags.ALWAYS_ALLOC, "ALWAYS_ALLOC"},
            {Parser.ColumnFlags.NON_NULL_REF, "NON_NULL_REF"},
            {Parser.ColumnFlags.REQUIRED, "REQUIRED"},
            {Parser.ColumnFlags.NO_WRITE, "NO_WRITE"},
            {Parser.ColumnFlags.NO_NETSEND, "NO_NETSEND"},
            {Parser.ColumnFlags.FLATEMBED, "FLATEMBED"},
            {Parser.ColumnFlags.NO_TEXT_SAVE, "NO_TEXT_SAVE"},
            {Parser.ColumnFlags.GLOBAL_NAME, "GLOBAL_NAME"},
            {Parser.ColumnFlags.USEDFIELD, "USEDFIELD"},
            {Parser.ColumnFlags.USEROPTIONBIT_1, "USEROPTIONBIT_1"},
            {Parser.ColumnFlags.USEROPTIONBIT_2, "USEROPTIONBIT_2"},
            {Parser.ColumnFlags.USEROPTIONBIT_3, "USEROPTIONBIT_3"},
            {Parser.ColumnFlags.POOL_STRING_DB, "POOL_STRING_DB"},
            {Parser.ColumnFlags.DEFAULT_FIELD, "DEFAULT_FIELD"},
            {Parser.ColumnFlags.DEMO_IGNORE, "DEMO_IGNORE"},
            {Parser.ColumnFlags.PUPPET_NO_COPY, "PUPPET_NO_COPY"},
            {Parser.ColumnFlags.SUBSCRIBE, "SUBSCRIBE"},
            {Parser.ColumnFlags.SERVER_ONLY, "SERVER_ONLY"},
            {Parser.ColumnFlags.CLIENT_ONLY, "CLIENT_ONLY"},
            {Parser.ColumnFlags.SELF_ONLY, "SELF_ONLY"},
            {Parser.ColumnFlags.SELF_AND_TEAM_ONLY, "SELF_AND_TEAM_ONLY"},
            {Parser.ColumnFlags.LOGIN_SUBSCRIBE, "LOGIN_SUBSCRIBE"},
            {Parser.ColumnFlags.KEY, "KEY"},
            {Parser.ColumnFlags.PERSIST, "PERSIST"},
            {Parser.ColumnFlags.NO_TRANSACT, "NO_TRANSACT"},
            {Parser.ColumnFlags.SOMETIMES_TRANSACT, "SOMETIMES_TRANSACT"},
            {Parser.ColumnFlags.VITAL_REF, "VITAL_REF"},
            {Parser.ColumnFlags.NON_NULL_REF__ERROR_ONLY, "NON_NULL_REF__ERROR_ONLY"},
            {Parser.ColumnFlags.DIRTY_BIT, "DIRTY_BIT"},
            {Parser.ColumnFlags.NO_INHERIT, "NO_INHERIT"},
            {Parser.ColumnFlags.IGNORE_STRUCT, "IGNORE_STRUCT"},
            {Parser.ColumnFlags.SPECIAL_DEFAULT, "SPECIAL_DEFAULT"},
            {Parser.ColumnFlags.PARSETABLE_INFO, "PARSETABLE_INFO"},
            {Parser.ColumnFlags.INHERITANCE_STRUCT, "INHERITANCE_STRUCT"},
            {Parser.ColumnFlags.STRUCT_NORECURSE, "STRUCT_NORECURSE"},
            {Parser.ColumnFlags.CASE_SENSITIVE, "CASE_SENSITIVE"},
            {Parser.ColumnFlags.EDIT_ONLY, "EDIT_ONLY"},
            {Parser.ColumnFlags.NO_INDEX, "NO_INDEX"},
            {Parser.ColumnFlags.NO_LOG, "NO_LOG"},
        };

        private static string[] FloatRounding =
        {
            null,
            "HUNDREDTHS",
            "TENTHS",
            "ONES",
            "FIVES",
            "TENS",
        };

        public static Parser.ColumnFlags BasicFlagsMask = BasicFlags
            .Aggregate<KeyValuePair<Parser.ColumnFlags, string>, Parser.ColumnFlags>(
                Parser.ColumnFlags.None,
                (a, b) => a | b.Key);

        private static void ExportTable(
            ProcessMemory memory,
            uint address,
            XmlWriter xml,
            List<KeyValuePair<string, uint>> parsers)
        {
            xml.WriteComment(string.Format(" {0:X8} ",
                0x00400000u + (address - (uint)memory.MainModuleAddress.ToInt32())));

            var currentAddress = address;

            var flags = new Dictionary<Parser.ColumnFlags, string>();
            foreach (Parser.ColumnFlags value in Enum.GetValues(typeof(Parser.ColumnFlags)))
            {
                flags.Add(value, Enum.GetName(typeof(Parser.ColumnFlags), value));
            }

            var columns = new List<KeyValuePair<NativeColumn, string>>();
            while (true)
            {
                var column = memory.ReadStructure<NativeColumn>(currentAddress);
                currentAddress += 40;
                
                var name = memory.ReadStringZ(column.NamePointer);
                
                if (column.Type == 0)
                {
                    if (string.IsNullOrEmpty(name) == true)
                    {
                        break;
                    }
                }

                columns.Add(new KeyValuePair<NativeColumn, string>(column, name));
            }

            foreach (var columnKV in columns)
            {
                var column = columnKV.Key;
                var name = columnKV.Value;

                xml.WriteStartElement("column");

                if (string.IsNullOrEmpty(name) == false)
                {
                    xml.WriteAttributeString("name", name);
                }

                var token = Parser.GlobalTokens.GetToken(column.Token);

                xml.WriteAttributeString("type", GetTokenName(column));

                if (column.Token != 0 &&
                    column.Token != 1 &&
                    column.Token != 2)
                {
                    xml.WriteElementString("offset", column.Offset.ToString());
                }

                var values = new List<string>();

                if ((column.Flags & Parser.ColumnFlags.PARSETABLE_INFO) != 0)
                {
                    xml.WriteStartElement("flags");
                    xml.WriteElementString("flag", "PARSETABLE_INFO");
                    xml.WriteEndElement();

                    var formatString = memory.ReadStringZ(column.Unknown20);
                    if (string.IsNullOrEmpty(formatString) == false)
                    {
                        xml.WriteElementString("format_string", formatString);
                    }
                }
                else
                {
                    var basicFlags = column.Flags;
                    basicFlags &= ~Parser.ColumnFlags.FIXED_ARRAY;
                    basicFlags &= ~Parser.ColumnFlags.EARRAY;
                    basicFlags &= ~Parser.ColumnFlags.INDIRECT;

                    if ((basicFlags & BasicFlagsMask) != 0)
                    {
                        xml.WriteStartElement("flags");

                        foreach (var kv in BasicFlags)
                        {
                            if ((basicFlags & kv.Key) != 0)
                            {
                                xml.WriteElementString("flag", kv.Value);
                            }
                        }

                        xml.WriteEndElement();
                    }

                    if ((column.Flags & Parser.ColumnFlags.ALIAS) != 0)
                    {
                        xml.WriteStartElement("redundant");
                        xml.WriteEndElement();

                        var aliased = columns
                            .Where(c => c.Key != column)
                            .Where(c => (c.Key.Flags & Parser.ColumnFlags.ALIAS) == 0)
                            .Where(c => c.Key.Offset == column.Offset)
                            .Where(c => c.Key.Token == column.Token)
                            .Where(c => c.Key.Token != 23 || c.Key.Parameter0 == column.Parameter0)
                            .FirstOrDefault();

                        if (aliased.Key != null)
                        {
                            xml.WriteElementString("redundant_name", aliased.Value);
                        }
                    }
                    //else
                    {
                        if (column.MinBits != 0)
                        {
                            if (column.Token == 7)
                            {
                                xml.WriteElementString("float_rounding", FloatRounding[column.MinBits]);
                            }
                            else
                            {
                                xml.WriteElementString("min_bits", column.MinBits.ToString());
                            }
                        }

                        switch (token.GetParameter(column.Flags, 0))
                        {
                            case Parser.ColumnParameter.NumberOfElements:
                            {
                                xml.WriteElementString("num_elements", ((int)column.Parameter0).ToString());
                                break;
                            }

                            case Parser.ColumnParameter.Default:
                            {
                                if (column.Parameter0 != 0)
                                {
                                    xml.WriteElementString("default", ((int)column.Parameter0).ToString());
                                }
                                break;
                            }

                            case Parser.ColumnParameter.StringLength:
                            {
                                xml.WriteElementString("string_length", ((int)column.Parameter0).ToString());
                                break;
                            }

                            case Parser.ColumnParameter.DefaultString:
                            {
                                if (column.Parameter0 != 0)
                                {
                                    xml.WriteElementString("default_string", memory.ReadStringZ(column.Parameter0) ?? "");
                                }
                                break;
                            }

                            case Parser.ColumnParameter.CommandString:
                            {
                                if (column.Parameter0 != 0)
                                {
                                    xml.WriteElementString("command_string", memory.ReadStringZ(column.Parameter0) ?? "");
                                }
                                break;
                            }

                            case Parser.ColumnParameter.Size:
                            {
                                xml.WriteElementString("size", ((int)column.Parameter0).ToString());
                                break;
                            }

                            case Parser.ColumnParameter.BitOffset:
                            {
                                xml.WriteElementString("bit_offset", ((int)column.Parameter0).ToString());
                                break;
                            }
                        }

                        switch (token.GetParameter(column.Flags, 1))
                        {
                            case Parser.ColumnParameter.StaticDefineList:
                            {
                                if (column.Parameter1 != 0)
                                {
                                    xml.WriteStartElement("static_define_list");
                                    xml.WriteComment(string.Format(" {0:X8} ", column.Parameter1));
                                    xml.WriteEndElement();
                                }
                                break;
                            }

                            case Parser.ColumnParameter.DictionaryName:
                            {
                                if (column.Parameter1 != 0)
                                {
                                    xml.WriteElementString("dictionary_name", memory.ReadStringZ(column.Parameter1) ?? "");
                                }
                                break;
                            }

                            case Parser.ColumnParameter.Subtable:
                            {
                                if (column.Parameter1 == 0)
                                {
                                    xml.WriteElementString("subtable", "NULL?");
                                }
                                else
                                {
                                    xml.WriteStartElement("subtable");

                                    var parser = parsers.SingleOrDefault(p => p.Value == column.Parameter1);
                                    if (parser.Key == null)
                                    {
                                        //xml.WriteElementString("subtable", "NOT FOUND? " + column.Parameter1.ToString("X*"));

                                        xml.WriteStartElement("table");
                                        ExportTable(memory, column.Parameter1, xml, parsers);
                                        xml.WriteEndElement();
                                    }
                                    else
                                    {
                                        xml.WriteAttributeString("external", parser.Key);
                                    }

                                    xml.WriteEndElement();
                                }
                                break;
                            }
                        }
                    }
                }

                xml.WriteEndElement();
            }
        }

        public static void Main(string[] args)
        {
            var process = Process.GetProcessesByName("GameClient")
                .Where(p =>
                    p.MainWindowTitle == "Champions Online" ||
                    p.MainWindowTitle == "Star Trek Online")
                .FirstOrDefault();
            if (process == null)
            {
                return;
            }

            var outputPath = Path.Combine("parsers", process.MainWindowTitle);
            Directory.CreateDirectory(outputPath);

            using (var memory = new ProcessMemory())
            {
                if (memory.Open(process) == false)
                {
                    return;
                }

                var stashPointer = FindParserTableInfo(memory);
                var stashCount = memory.ReadS32(stashPointer + 0x08);
                var stashEntryPointer = memory.ReadU32(stashPointer + 0x14);
                var stashEntries = memory.ReadBytes(stashEntryPointer, stashCount * 8);

                var parsers = new List<KeyValuePair<string, uint>>();

                for (int i = 0; i < stashCount; i++)
                {
                    var namePointer = BitConverter.ToUInt32(stashEntries, (i * 8) + 0);
                    var dataPointer = BitConverter.ToUInt32(stashEntries, (i * 8) + 4);

                    if (namePointer == 0 &&
                        dataPointer == 0)
                    {
                        continue;
                    }

                    var name = memory.ReadStringZ(namePointer);

                    parsers.Add(new KeyValuePair<string,uint>(name, dataPointer));
                }

                foreach (var kv in parsers)
                {
                    var name = kv.Key;
                    var pointer = kv.Value;

                    Console.WriteLine(name);

                    var settings = new XmlWriterSettings()
                    {
                        Indent = true,
                    };

                    using (var xml = XmlWriter.Create(Path.Combine(outputPath, name + ".schema.xml"), settings))
                    {
                        xml.WriteStartDocument();
                        xml.WriteStartElement("parser");
                        xml.WriteAttributeString("name", name);

                        xml.WriteStartElement("table");
                        ExportTable(memory, pointer, xml, parsers);
                        xml.WriteEndElement();

                        xml.WriteEndElement();
                        xml.WriteEndDocument();
                    }
                }
            }
        }
    }
}
