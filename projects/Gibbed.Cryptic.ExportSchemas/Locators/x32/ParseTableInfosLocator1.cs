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

namespace Gibbed.Cryptic.ExportSchemas.Locators.x32
{
    internal static class ParseTableInfosLocator1
    {
        public static bool Locate(RuntimeProcess runtime, out IntPtr result)
        {
            var namePattern = ByteSearch.ToPattern("ff_ParseTableInfos");
            if (runtime.Search(namePattern, out var nameOffset) == false)
            {
                result = default;
                return false;
            }
            nameOffset = nameOffset + 1;

            var codePattern = new ByteSearch.Pattern()
            {
                new byte[] { 0x83, 0x3D }, // cmp pointer1, 0
                ByteSearch.AnyBytes(4),
                new byte[] { 0x00 },
                new byte[] { 0x75, 0x1E }, // jnz
                0x68, // push 0x0000????
                ByteSearch.AnyBytes(2),
                new byte[] { 0x00, 0x00 },
                0x68, // push "ff_ParseTableInfos"
                BitConverter.GetBytes(nameOffset.ToInt32()),
                new byte[] { 0x6A, 0x00 }, // push 0
                new byte[] { 0x68, 0x00, 0x04, 0x00, 0x00 }, // push 0x400
                0xE8, // call 0x????????
                ByteSearch.AnyBytes(4),
                new byte[] { 0x83, 0xC4, 0x10 }, // add esp, 10h
                0xA3,
                ByteSearch.AnyBytes(4), // mov pointer2, eax
            };
            if (runtime.Search(codePattern, out var codeOffset) == false)
            {
                result = default;
                return false;
            }

            var pointer1 = runtime.ReadValueU32(codeOffset + 2);
            if (pointer1 == 0)
            {
                throw new InvalidOperationException();
            }

            var pointer2 = runtime.ReadValueU32(codeOffset + 35);
            if (pointer2 == 0)
            {
                throw new InvalidOperationException();
            }

            if (pointer1 != pointer2)
            {
                throw new InvalidOperationException();
            }

            result = new IntPtr(pointer1);
            return true;
        }
    }
}
