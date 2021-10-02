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
using System.Runtime.InteropServices;

namespace Gibbed.Cryptic.ExportSchemas.Natives.x32
{
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal class ParseColumn
    {
        public uint NamePointer;
        public ulong Type;
        public uint Offset;
        public ParseColumnParameterValue Parameter0;
        public ParseColumnParameterValue Parameter1;
        public uint Format;
        public uint FormatStringPointer;

        public x64.ParseColumn Upgrade()
        {
            return new x64.ParseColumn()
            {
                NamePointer = new IntPtr(this.NamePointer),
                Type = this.Type,
                Offset = this.Offset,
                Parameter0 = this.Parameter0.Upgrade(),
                Parameter1 = this.Parameter1.Upgrade(),
                Format = this.Format,
                FormatStringPointer = new IntPtr(this.FormatStringPointer),
            };
        }
    }
}
