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
using System.Text;
using Gibbed.Cryptic.ExportSchemas.Natives.x64;
using Gibbed.Cryptic.ExportSchemas.Runtime;
using x32 = Gibbed.Cryptic.ExportSchemas.Natives.x32;

namespace Gibbed.Cryptic.ExportSchemas
{
    internal static class StashTable
    {
        private static StashTableEntry<T>[] ReadStashTable<T>(
            this RuntimeBase runtime, IntPtr pointer, Func<IntPtr, T> callback)
        {
            if (pointer == IntPtr.Zero)
            {
                return new StashTableEntry<T>[0];
            }

            StashTableHeader header;
            if (runtime.Is32Bit == false)
            {
                header = runtime.ReadStructure<StashTableHeader>(pointer);
            }
            else
            {
                header = runtime.ReadStructure<x32.StashTableHeader>(pointer).Upgrade();
            }

            if (header.AllocatedCount == 0)
            {
                return new StashTableEntry<T>[0];
            }

            StashTableEntry[] entries;
            if (runtime.Is32Bit == false)
            {
                entries = runtime.ReadStructureArray<StashTableEntry>(header.EntriesPointer, header.AllocatedCount);
            }
            else
            {
                var entries32 = runtime.ReadStructureArray<x32.StashTableEntry>(header.EntriesPointer, header.AllocatedCount);
                entries = new StashTableEntry[header.AllocatedCount];
                for (int i = 0; i < header.AllocatedCount; i++)
                {
                    entries[i] = entries32[i].Upgrade();
                }
            }

            var items = new StashTableEntry<T>[header.AllocatedCount];
            int o = 0;
            for (int i = 0; i < header.AllocatedCount; i++)
            {
                var entry = entries[i];
                if (entry.NamePointer == default && entry.ValuePointer == default)
                {
                    continue;
                }
                var name = runtime.ReadStringZ(entry.NamePointer, Encoding.ASCII);
                var value = callback(entry.ValuePointer);
                items[o] = new StashTableEntry<T>(name, value);
                o++;
            }
            Array.Resize(ref items, o);
            return items;
        }

        public static StashTableEntry<IntPtr>[] ReadStashTable(this RuntimeBase runtime, IntPtr pointer)
        {
            return runtime.ReadStashTable(pointer, p => p);
        }

        public static StashTableEntry<string>[] ReadStringStashTable(this RuntimeBase runtime, IntPtr pointer)
        {
            return runtime.ReadStashTable(pointer, p => runtime.ReadStringZ(p, Encoding.ASCII));
        }
    }
}
