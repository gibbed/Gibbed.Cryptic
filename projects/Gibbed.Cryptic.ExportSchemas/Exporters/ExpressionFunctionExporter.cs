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
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.Cryptic.ExportSchemas.Natives.x64;
using Gibbed.Cryptic.ExportSchemas.Runtime;
using x32 = Gibbed.Cryptic.ExportSchemas.Natives.x32;

namespace Gibbed.Cryptic.ExportSchemas.Exporters
{
    internal class ExpressionFunctionExporter : BaseExporter
    {
        public ExpressionFunctionExporter(RuntimeProcess runtime) : base(runtime)
        {
        }

        public void Export(string outputPath)
        {
            var runtime = this.Runtime;

            var locators = runtime.Is32Bit == false
                ? new LocateDelegate[] // 64
                {
                    Locators.x64.ExpressionFunctionTableLocator1.Locate,
                }
                : new LocateDelegate[] // 32
                {
                    Locators.x32.ExpressionFunctionTableLocator1.Locate,
                };
            if (this.Locate(out var stashPointerPointer, locators) == false)
            {
                Console.WriteLine("Warning: failed to locate expression function table.");
                return;
            }

            var stashPointer = runtime.ReadPointer(stashPointerPointer);
            var stashEntries = runtime.ReadStashTable(stashPointer).ToList();
            if (stashEntries.Count == 0)
            {
                return;
            }

            using (var output = File.Create(Path.Combine(outputPath, "expression functions.txt")))
            {
                var writer = new StreamWriter(output);
                foreach (var stashEntry in stashEntries.OrderBy(kv => kv.Name))
                {
                    Export(stashEntry.Name, stashEntry.Value, writer);
                }
                writer.Flush();
            }
        }

        private void Export(string itemName, IntPtr pointer, TextWriter writer)
        {
            var runtime = this.Runtime;

            var sb = new StringBuilder();

            var func = runtime.Is32Bit == false
                ? runtime.ReadStructure<ExpressionFunction>(pointer)
                : runtime.ReadStructure<x32.ExpressionFunction>(pointer).Upgrade();

            var handlerStaticAddress = this.ToStaticAddress(func.Handler);

            sb.AppendFormat("[{0:X}] ", handlerStaticAddress);

            var funcName = runtime.ReadStringZ(func.NamePointer, Encoding.ASCII);
            var codeName = runtime.ReadStringZ(func.ExprCodeNamePointer, Encoding.ASCII);

            var tags = new string[12];
            for (int i = 0; i < 12; i++)
            {
                tags[i] = runtime.ReadStringZ(func.TagPointers[i], Encoding.ASCII);
            }

            var validTags = tags
                .Where(t => string.IsNullOrWhiteSpace(t) == false)
                .ToArray();
            if (validTags.Length > 0)
            {
                sb.AppendFormat("{0} : ", string.Join(", ", validTags));
            }

            sb.AppendFormat("{0} {1}(", this.GetArgumentType(func.ReturnType), funcName);

            for (int i = 0; i < func.ArgumentCount; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.AppendFormat(
                    "{0} {1}",
                    this.GetArgumentType(func.Arguments[i]),
                    runtime.ReadStringZ(func.Arguments[i].NamePointer, Encoding.ASCII));
            }

            sb.Append(")");

            writer.WriteLine(sb.ToString());
        }

        private string GetArgumentType(ExpressionArgument arg)
        {
            return arg.Type switch
            {
                0x0000 => "void",
                0x0002 => "int",
                0x0004 => "flt",
                0x0005 => "intarray",
                0x0006 => "floatarray",
                0x0007 => "Vec3",
                0x0008 => "Vec4",
                0x0009 => "Mat4",
                0x000A => "Quat",
                0x000B => "str",
                0x000C => "multivalarray",
                0x0080 => "entityarray",
                0x0081 => this.Runtime.ReadStringZ(arg.TypeNamePointer, Encoding.ASCII),
                0x0082 => "MultiVal",
                0x2809 => "loc",
                _ => $"*INV:{arg.Type:X8}*",
            };
        }
    }
}
