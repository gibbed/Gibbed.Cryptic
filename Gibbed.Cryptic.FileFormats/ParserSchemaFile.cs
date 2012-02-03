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
        public string Name = null;
        public ParserSchema.Table Table = null;

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
            
            var _columns = nav.Select("column");
            while (_columns.MoveNext() == true)
            {
                var _column = _columns.Current;

                var column = new ParserSchema.Column();
                column.Name = _column.GetAttribute("name", "");

                var _type = _column.GetAttribute("type", "");
                var token = Parser.GlobalTokens.MatchToken(_type, out column.Token, out column.Flags);

                var _flags = _column.Select("flags/flag");
                while (_flags.MoveNext() == true)
                {
                    var _flag = _flags.Current.Value;
                    if (Enum.IsDefined(typeof(Parser.ColumnFlags), _flag) == false)
                    {
                        throw new FormatException();
                    }

                    column.Flags |= (Parser.ColumnFlags)Enum.Parse(typeof(Parser.ColumnFlags), _flag);
                }

                column.RedundantName = GetStringElement(_column, "redundant_name", null);

                column.MinBits = (byte)GetIntElement(_column, "min_bits", 0);
                column.FloatRounding = GetStringElement(_column, "float_rounding", null);

                column.NumberOfElements = GetIntElement(_column, "num_elements", 0);
                column.Default = GetIntElement(_column, "default", 0);
                column.StringLength = GetIntElement(_column, "string_length", 0);
                column.DefaultString = GetStringElement(_column, "default_string", null);
                column.CommandString = GetStringElement(_column, "command_string", null);
                column.Size = GetIntElement(_column, "size", 0);
                column.BitOffset = GetIntElement(_column, "bit_offset", 0);

                var _subtable = _column.SelectSingleNode("subtable");
                if (_subtable != null)
                {
                    column.SubtableExternalName = _subtable.GetAttribute("external", "");
                    
                    var _subtable_table = _subtable.SelectSingleNode("table");
                    if (_subtable_table != null)
                    {
                        column.Subtable = LoadTable(_subtable_table);
                    }
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
                var name = root.GetAttribute("name", "");

                if (name == null)
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
                var _hash = root.GetAttribute("hash", "");

                if (_hash == null)
                {
                    throw new InvalidOperationException();
                }

                uint hash;
                if (_hash.StartsWith("0x") == true)
                {
                    hash = uint.Parse(_hash, NumberStyles.AllowHexSpecifier);
                }
                else
                {
                    hash = uint.Parse(_hash);
                }

                return hash;
            }
        }
    }
}
