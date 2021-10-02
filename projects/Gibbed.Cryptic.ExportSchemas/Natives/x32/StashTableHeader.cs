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
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct StashTableHeader
    {
        public uint Count1;
        public uint Count2;
        public int AllocatedCount;
        public uint Unknown0C;
        public uint Unknown10;
        public uint EntriesPointer;

        public x64.StashTableHeader Upgrade()
        {
            return new x64.StashTableHeader()
            {
                Count1 = this.Count1,
                Count2 = this.Count2,
                AllocatedCount = this.AllocatedCount,
                Unknown0C = this.Unknown0C,
                Unknown10 = new IntPtr(this.Unknown10),
                EntriesPointer = new IntPtr(this.EntriesPointer),
            };
        }
    }
}
