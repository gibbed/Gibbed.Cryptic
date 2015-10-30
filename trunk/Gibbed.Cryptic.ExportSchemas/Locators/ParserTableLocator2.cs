/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
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

namespace Gibbed.Cryptic.ExportSchemas.Locators
{
    internal class ParserTableLocator2
    {
        public static bool Locate(ProcessMemory memory, out uint result)
        {
            var namePattern = Helpers.ToPattern("ff_ParseTableInfos");
            uint nameOffset;
            if (memory.Search(namePattern, out nameOffset) == false)
            {
                result = 0;
                return false;
            }
            nameOffset = nameOffset + 1;

            var codePattern = new ByteSearch.Pattern()
            {
                new byte[] { 0x83, 0x3D }, // cmp pointer1, 0
                ByteSearch.AnyBytes(4),
                new byte[] { 0x00 },
                new byte[] { 0x75, 0x21 }, // jnz
                0x68, // push 0x0000????
                ByteSearch.AnyBytes(2),
                new byte[] { 0x00, 0x00 },
                0x68, // push "ff_ParseTableInfos"
                BitConverter.GetBytes(nameOffset),
                new byte[] { 0x6A, 0x00 }, // push 0
                new byte[] { 0x68, 0x00, 0x04, 0x00, 0x00 }, // push 0x400
                0xE8, // call 0x????????
                ByteSearch.AnyBytes(4),
                new byte[] { 0x8B, 0x55, 0x1C, }, // mov edx, [ebp+1Ch]
                new byte[] { 0x83, 0xC4, 0x10 }, // add esp, 10h
                0xA3, // mov pointer2, eax
                ByteSearch.AnyBytes(4),
            };

            uint codeOffset;
            if (memory.Search(codePattern, out codeOffset) == false)
            {
                result = 0;
                return false;
            }

            var pointer1 = memory.ReadU32(codeOffset + 2);
            if (pointer1 == 0)
            {
                throw new InvalidOperationException();
            }

            var pointer2 = memory.ReadU32(codeOffset + 38);
            if (pointer2 == 0)
            {
                throw new InvalidOperationException();
            }

            if (pointer1 != pointer2)
            {
                throw new InvalidOperationException();
            }

            result = memory.ReadU32(pointer1);
            return true;
        }
    }
}
