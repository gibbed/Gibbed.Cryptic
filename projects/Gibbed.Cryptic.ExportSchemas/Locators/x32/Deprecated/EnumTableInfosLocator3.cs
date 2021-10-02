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
using Gibbed.Cryptic.ExportSchemas.Runtime;

namespace Gibbed.Cryptic.ExportSchemas.Locators.x32.Deprecated
{
    internal static class EnumTableInfosLocator3
    {
        public static bool Locate(RuntimeProcess runtime, out IntPtr result)
        {
            var codePattern = new ByteSearch.Pattern()
            {
                new byte[] { 0x51 },
                0xA1,
                ByteSearch.AnyBytes(4),
                new byte[] { 0xC7, 0x04, 0x24, 0x00, 0x00, 0x00, 0x00 },
                new byte[] { 0x85, 0xC0 },
                new byte[] { 0x74, 0x17 },
                new byte[] { 0x8D, 0x0C, 0x24 },
                new byte[] { 0x51 },
                new byte[] { 0xFF, 0x74, 0x24, 0x0C },
                new byte[] { 0x50 },
                0xE8,
                ByteSearch.AnyBytes(4),
                new byte[] { 0x8B, 0x44, 0x24, 0x0C },
                new byte[] { 0x83, 0xC4, 0x0C },
                new byte[] { 0x59 },
                new byte[] { 0xC3 },
            };
            if (runtime.Search(codePattern, out var codeOffset) == false)
            {
                result = default;
                return false;
            }

            var pointer = runtime.ReadValueU32(codeOffset + 2);
            if (pointer == 0)
            {
                throw new InvalidOperationException();
            }

            result = new IntPtr(pointer);
            return true;
        }
    }
}
