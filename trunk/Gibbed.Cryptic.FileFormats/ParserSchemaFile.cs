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
using System.Globalization;
using System.IO;
using System.Xml.XPath;

namespace Gibbed.Cryptic.FileFormats
{
    public class ParserSchemaFile
    {
        public const int DefaultMaxArraySize = 1200000;

        public string Name;
        public ParserSchema.Table Table;

        private ParserSchemaFile()
        {
        }

        private static int GetIntElement(XPathNavigator nav, string xpath, int defaultValue)
        {
            var node = nav.SelectSingleNode(xpath);
            if (node == null)
            {
                return defaultValue;
            }
            return node.ValueAsInt;
        }

        private static string GetStringElement(XPathNavigator nav, string xpath, string defaultValue)
        {
            var node = nav.SelectSingleNode(xpath);
            if (node == null)
            {
                return defaultValue;
            }
            return node.Value;
        }

        private static ParserSchema.Table LoadTable(XPathNavigator nav)
        {
            var table = new ParserSchema.Table();

            var columnIterator = nav.Select("column");
            while (columnIterator.MoveNext() == true)
            {
                var columnNode = columnIterator.Current;
                if (columnNode == null)
                {
                    throw new InvalidOperationException();
                }

                // ReSharper disable UseObjectOrCollectionInitializer
                var column = new ParserSchema.Column();
                // ReSharper restore UseObjectOrCollectionInitializer
                column.Name = columnNode.GetAttribute("name", "");

                var type = columnNode.GetAttribute("type", "");
                var token = Parser.GlobalTokens.MatchToken(type, out column.Token, out column.Flags);

                var flagIterator = columnNode.Select("flags/flag");
                while (flagIterator.MoveNext() == true)
                {
                    if (flagIterator.Current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    var flag = flagIterator.Current.Value;
                    if (Enum.IsDefined(typeof(Parser.ColumnFlags), flag) == false)
                    {
                        throw new FormatException("invalid schema flag");
                    }

                    column.Flags |= (Parser.ColumnFlags)Enum.Parse(typeof(Parser.ColumnFlags), flag);
                }

                column.Offset = (uint)GetIntElement(columnNode, "offset", 0);

                column.RedundantName = GetStringElement(columnNode, "redundant_name", null);

                column.MinBits = (byte)GetIntElement(columnNode, "min_bits", 0);
                column.FloatRounding = GetStringElement(columnNode, "float_rounding", null);

                column.NumberOfElements = GetIntElement(columnNode, "num_elements", 0);
                column.Default = GetIntElement(columnNode, "default", 0);
                column.StringLength = GetIntElement(columnNode, "string_length", 0);
                column.DefaultString = GetStringElement(columnNode, "default_string", null);
                column.CommandString = GetStringElement(columnNode, "command_string", null);
                column.Size = GetIntElement(columnNode, "size", 0);
                column.BitOffset = GetIntElement(columnNode, "bit_offset", 0);

                var subtable = columnNode.SelectSingleNode("subtable");
                if (subtable != null)
                {
                    column.SubtableExternalName = subtable.GetAttribute("external", "");
                    column.SubtableIsExternal = true;

                    var subtableTable = subtable.SelectSingleNode("table");
                    if (subtableTable != null)
                    {
                        column.SubtableIsExternal = false;
                        column.Subtable = LoadTable(subtableTable);
                    }
                }

                var staticDefineList = columnNode.SelectSingleNode("static_define_list");
                if (staticDefineList != null)
                {
                    column.StaticDefineListExternalName = staticDefineList.GetAttribute("external", "");
                    column.StaticDefineListIsExternal = true;

                    var staticDefineListElements = staticDefineList.SelectSingleNode("elements");
                    if (staticDefineListElements != null)
                    {
                        column.StaticDefineListIsExternal = false;
                        column.StaticDefineList = ParserEnumFile.LoadEnumeration(staticDefineListElements);
                    }
                }

                var formatName = GetStringElement(columnNode, "format", null);
                if (string.IsNullOrEmpty(formatName) == false)
                {
                    ParserSchema.ColumnFormat format;
                    if (Enum.TryParse(formatName, true, out format) == false)
                    {
                        throw new FormatException("invalid column format");
                    }
                    column.Format = format;
                }

                var formatStringIterator = columnNode.Select("format_strings/format_string");
                column.FormatStrings.Clear();
                while (formatStringIterator.MoveNext() == true)
                {
                    var formatStringNode = formatStringIterator.Current;
                    if (formatStringNode == null)
                    {
                        throw new InvalidOperationException();
                    }

                    var formatStringKey = formatStringNode.GetAttribute("name", "");
                    if (string.IsNullOrEmpty(formatStringKey) == true)
                    {
                        throw new FormatException("invalid schema format string key");
                    }

                    /*Console.WriteLine("Adding {0}={1} to {2}",
                        formatStringKey, formatStringNode.Value, column.Name);*/
                    column.FormatStrings.Add(formatStringKey, formatStringNode.Value);
                }

                table.Columns.Add(column);
            }

            return table;
        }

        public static ParserSchemaFile LoadFile(string path)
        {
            using (var input = File.OpenRead(path))
            {
                var doc = new XPathDocument(input);
                var nav = doc.CreateNavigator();

                var schema = new ParserSchemaFile();

                var root = nav.SelectSingleNode("/parser");
                if (root == null)
                {
                    throw new InvalidOperationException();
                }

                schema.Name = root.GetAttribute("name", "");

                var table = root.SelectSingleNode("table");
                schema.Table = LoadTable(table);

                return schema;
            }
        }

        public static string GetNameFromFile(string path)
        {
            using (var input = File.OpenRead(path))
            {
                var doc = new XPathDocument(input);
                var nav = doc.CreateNavigator();

                var root = nav.SelectSingleNode("/parser");
                if (root == null)
                {
                    throw new InvalidOperationException();
                }

                var name = root.GetAttribute("name", "");
                if (string.IsNullOrEmpty(name) == true)
                {
                    throw new InvalidOperationException();
                }

                return name;
            }
        }

        public static uint GetHashFromFile(string path)
        {
            using (var input = File.OpenRead(path))
            {
                var doc = new XPathDocument(input);
                var nav = doc.CreateNavigator();

                var root = nav.SelectSingleNode("/parser");
                if (root == null)
                {
                    throw new InvalidOperationException();
                }

                var hashText = root.GetAttribute("hash", "");
                if (string.IsNullOrEmpty(hashText) == true)
                {
                    throw new InvalidOperationException();
                }

                var hash = hashText.StartsWith("0x") == true
                               ? uint.Parse(hashText, NumberStyles.AllowHexSpecifier)
                               : uint.Parse(hashText);

                return hash;
            }
        }
    }
}
