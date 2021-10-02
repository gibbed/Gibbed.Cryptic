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
using System.Runtime.InteropServices;
using System.Text;
using Gibbed.Cryptic.ExportSchemas.Natives.x64;
using Gibbed.Cryptic.ExportSchemas.Runtime;
using x32 = Gibbed.Cryptic.ExportSchemas.Natives.x32;

namespace Gibbed.Cryptic.ExportSchemas.Exporters
{
    internal abstract class BaseExporter
    {
        protected readonly RuntimeProcess Runtime;

        protected BaseExporter(RuntimeProcess runtime)
        {
            this.Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        protected delegate bool LocateDelegate(RuntimeProcess runtime, out IntPtr result);

        protected bool Locate(out IntPtr result, params LocateDelegate[] locators)
        {
            var runtime = this.Runtime;
            foreach (var locator in locators.Reverse())
            {
                if (locator(runtime, out result) == true)
                {
                    return true;
                }
            }
            result = default;
            return false;
        }

        protected string ReadTokenString(IntPtr pointer)
        {
            if (pointer == default)
            {
                return null;
            }
            var runtime = this.Runtime;
            if (runtime.Is32Bit == false)
            {
                var header = runtime.ReadStructure<TokenStashTableHeader>(pointer);
                if (header.Magic == TokenStashTableHeader.Signature)
                {
                    pointer = header.ValuePointer;
                }
            }
            else
            {
                var header = runtime.ReadStructure<x32.TokenStashTableHeader>(pointer);
                if (header.Magic == x32.TokenStashTableHeader.Signature)
                {
                    pointer = header.Upgrade().ValuePointer;
                }
            }
            return runtime.ReadStringZ(pointer, Encoding.ASCII);
        }

        protected ulong ToStaticAddress(IntPtr pointer)
        {
            var runtime = this.Runtime;
            var mainModule = runtime.Process.MainModule;
            var minimum = mainModule.BaseAddress.ToInt64();
            var maximum = (mainModule.BaseAddress + mainModule.ModuleMemorySize).ToInt64();
            var value = pointer.ToInt64();
            if (value < minimum || value >= maximum)
            {
                return 0;
            }
            value -= minimum;
            value += runtime.Is32Bit == false
                ? 0x140000000
                : 0x400000;
            return (ulong)value;
        }

        protected static List<KeyValuePair<string, int>> GetOffsets<T>()
        {
            var offsets = new List<KeyValuePair<string, int>>();
            foreach (var field in typeof(T)
                .GetFields(
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance)
                .Select(f => f.Name))
            {
                var offset = Marshal.OffsetOf(typeof(T), field).ToInt32();
                offsets.Add(new KeyValuePair<string, int>(field, offset));
            }
            return offsets;
        }
    }
}
