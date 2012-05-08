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
    public class ParserEnumFile
    {
        public string Name;
        public ParserSchema.Enumeration Enum;

        private ParserEnumFile()
        {
        }

        public static ParserSchema.Enumeration LoadEnumeration(XPathNavigator nav)
        {
            var elements = new ParserSchema.Enumeration();

            var type = nav.GetAttribute("type", "").ToLowerInvariant();
            
            switch (type)
            {
                case "int": elements.Type = ParserSchema.EnumerationType.Int; break;
                case "string": elements.Type = ParserSchema.EnumerationType.String; break;
                default: throw new InvalidOperationException();
            }

            int duplicate = 0;
            var elementIterator = nav.Select("element");
            while (elementIterator.MoveNext() == true)
            {
                var element = elementIterator.Current;
                if (element == null)
                {
                    throw new InvalidOperationException();
                }

                var name = element.GetAttribute("name", "");
                var value = element.Value;

                if (elements.Elements.ContainsKey(name) &&
                    elements.Elements[name] == value)
                {
                    continue;
                }

                if (elements.Elements.ContainsKey(name) == true)
                {
                    name = name + "__" + duplicate.ToString(CultureInfo.InvariantCulture);
                }

                elements.Elements.Add(name, value);
            }

            return elements;
        }

        public static ParserEnumFile LoadFile(string path)
        {
            using (var input = File.OpenRead(path))
            {
                var doc = new XPathDocument(input);
                var nav = doc.CreateNavigator();

                var schema = new ParserEnumFile();

                var root = nav.SelectSingleNode("/enum");
                if (root == null)
                {
                    throw new InvalidOperationException();
                }

                schema.Name = root.GetAttribute("name", "");

                var table = root.SelectSingleNode("elements");
                schema.Enum = LoadEnumeration(table);

                return schema;
            }
        }

        public static string GetNameFromFile(string path)
        {
            using (var input = File.OpenRead(path))
            {
                var doc = new XPathDocument(input);
                var nav = doc.CreateNavigator();

                var root = nav.SelectSingleNode("/enum");
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
    }
}
