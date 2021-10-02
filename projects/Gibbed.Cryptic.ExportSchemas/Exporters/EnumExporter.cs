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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Gibbed.Cryptic.ExportSchemas.Natives.x64;
using Gibbed.Cryptic.ExportSchemas.Runtime;
using static Gibbed.Cryptic.FileFormats.InvariantShorthand;
using x32 = Gibbed.Cryptic.ExportSchemas.Natives.x32;

namespace Gibbed.Cryptic.ExportSchemas.Exporters
{
    internal class EnumExporter : BaseExporter
    {
        public EnumExporter(RuntimeProcess runtime) : base(runtime)
        {
        }

        public List<KeyValuePair<string, IntPtr>> Export(string outputBasePath)
        {
            var runtime = this.Runtime;

            var enums = new List<KeyValuePair<string, IntPtr>>();

            var locators = runtime.Is32Bit == false
                ? new LocateDelegate[] // 64
                {
                    Locators.x64.EnumTableInfosLocator1.Locate,
                }
                : new LocateDelegate[] // 32
                {
                    Locators.x32.EnumTableInfosLocator1.Locate,
                };
            if (this.Locate(out var stashPointerPointer, locators) == false)
            {
                Console.WriteLine("Warning: failed to locate enum table.");
                return enums;
            }

            var stashPointer = runtime.ReadPointer(stashPointerPointer);
            var stashTable = runtime.ReadStashTable(stashPointer).ToList();
            var pointerMap = new Dictionary<IntPtr, string>();
            foreach (var stashEntry in stashTable)
            {
                // PowerCategories is a duplicate of PowerCategory
                if (stashEntry.Name == "PowerCategories")
                {
                    continue;
                }
                if (pointerMap.TryGetValue(stashEntry.Value, out var otherName) == true)
                {
                    if (stashEntry.Name != otherName)
                    {
                        throw new InvalidOperationException();
                    }
                }
                else
                {
                    pointerMap[stashEntry.Value] = stashEntry.Name;
                }
            }

            if (stashTable.Count == 0)
            {
                return enums;
            }

            Directory.CreateDirectory(Path.Combine(outputBasePath, "enums"));

            foreach (var stashEntry in stashTable.OrderBy(stashEntry => stashEntry.Name))
            {
                var name = stashEntry.Name;
                var pointer = stashEntry.Value;

                // PowerCategories is a duplicate of PowerCategory
                if (name == "PowerCategories")
                {
                    continue;
                }

                var info = Read(pointer);

                enums.Add(new KeyValuePair<string, IntPtr>(name, pointer));

                Console.WriteLine("[enum] {0}", name);

                var settings = new XmlWriterSettings()
                {
                    Indent = true,
                };

                var outputPath = Path.Combine(outputBasePath, "enums", name + ".enum.xml");
                using (var xml = XmlWriter.Create(outputPath, settings))
                {
                    xml.WriteStartDocument();
                    xml.WriteStartElement("enum");
                    xml.WriteAttributeString("name", name);
                    if (info.ParentPointer != default)
                    {
                        var parentName = pointerMap[info.ParentPointer];
                        xml.WriteStartElement("parent");
                        xml.WriteValue(parentName);
                        xml.WriteEndElement();
                    }
                    xml.WriteStartElement("elements");
                    xml.WriteAttributeString("type", info.ValueType switch
                    {
                        EnumValueType.Int => "int",
                        EnumValueType.String => "string",
                        _ => throw new NotSupportedException(),
                    });
                    foreach (var kv in info.Elements.OrderBy(kv => kv.Value, GetComparerByType(info.ValueType)))
                    {
                        xml.WriteStartElement("element");
                        xml.WriteAttributeString("name", kv.Key);
                        xml.WriteValue(kv.Value);
                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement();
                    xml.WriteEndElement();
                    xml.WriteEndDocument();
                }
            }

            return enums;
        }

        private IComparer<string> GetComparerByType(EnumValueType valueType)
        {
            return valueType switch
            {
                EnumValueType.Int => new IntComparer(),
                EnumValueType.String => StringComparer.Ordinal,
                _ => throw new NotSupportedException(),
            };
        }

        private class IntComparer : IComparer<string>
        {
            public int Compare(string left, string right)
            {
                var leftValue = long.Parse(left);
                var rightValue = long.Parse(right);
                return leftValue.CompareTo(rightValue);
            }
        }

        private enum EnumValueType : byte
        {
            Int = 1,
            String = 2,
            Dynamic = 3,
            Invalid = 4,
            Parent = 5,
        }

        private struct EnumInfo
        {
            public EnumValueType ValueType;
            public IntPtr ParentPointer;
            public List<KeyValuePair<string, string>> Elements;
        }

        private EnumInfo Read(IntPtr pointer)
        {
            var runtime = this.Runtime;
            //var address = this.ToStaticAddress(pointer);

            var elementSize = runtime.Is32Bit == false
                ? Marshal.SizeOf(typeof(EnumElement))
                : Marshal.SizeOf(typeof(x32.EnumElement));

            IntPtr parentPointer = default;
            var elements = new List<KeyValuePair<string, string>>();
            var valueType = EnumValueType.Invalid;
            while (true)
            {
                var element = runtime.Is32Bit == false
                    ? runtime.ReadStructure<EnumElement>(pointer)
                    : runtime.ReadStructure<x32.EnumElement>(pointer).Upgrade();
                if (element.Name.Pointer == default)
                {
                    break;
                }
                pointer += elementSize;

                if (element.Name.UInt64 == 1)
                {
                    valueType = EnumValueType.Int;
                    continue;
                }
                else if (element.Name.UInt64 == 2)
                {
                    valueType = EnumValueType.String;
                    continue;
                }
                else if (element.Name.UInt64 == 3)
                {
                    var dynamicInfoPointer = runtime.ReadPointer(element.Value.Pointer);
                    while (dynamicInfoPointer != default)
                    {
                        var dynamicInfo = runtime.Is32Bit == false
                            ? runtime.ReadStructure<EnumDynamicInfo>(dynamicInfoPointer)
                            : runtime.ReadStructure<x32.EnumDynamicInfo>(dynamicInfoPointer).Upgrade();
                        if (dynamicInfo.StashTablePointer != default)
                        {
                            var dynamicElements = runtime.ReadStringStashTable(dynamicInfo.StashTablePointer);
                            foreach (var dynamicElement in dynamicElements)
                            {
                                elements.Add(new KeyValuePair<string, string>(dynamicElement.Name, dynamicElement.Value));
                            }
                        }
                        dynamicInfoPointer = dynamicInfo.NextPointer;
                    }
                    continue;
                }
                else if (element.Name.UInt64 == 5)
                {
                    parentPointer = element.Value.Pointer;
                    break;
                }

                var name = runtime.ReadStringZ(element.Name.Pointer, Encoding.ASCII);
                object value = valueType switch
                {
                    EnumValueType.Int => element.Value.Int32,
                    EnumValueType.String => runtime.ReadStringZ(element.Value.Pointer, Encoding.ASCII),
                    _ => throw new NotSupportedException(),
                };
                elements.Add(new KeyValuePair<string, string>(name, _($"{value}")));
            }
            EnumInfo info;
            info.ValueType = valueType;
            info.ParentPointer = parentPointer;
            info.Elements = elements;
            return info;
        }
    }
}
