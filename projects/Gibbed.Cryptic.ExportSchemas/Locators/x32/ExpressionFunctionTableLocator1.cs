﻿/* Copyright (c) 2021 Rick (rick 'at' gibbed 'dot' us)
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
    internal static class ExpressionFunctionTableLocator1
    {
        public static bool Locate(RuntimeProcess runtime, out IntPtr result)
        {
            var codePattern = new ByteSearch.Pattern()
            {
                new byte[] { 0x55 }, // push ebp
                new byte[] { 0x8B, 0xEC }, // mov ebp, esp
                new byte[] { 0x51 }, // push ecx
                new byte[] { 0x83, 0x3D }, // cmp pointer1, 0
                ByteSearch.AnyBytes(4),
                new byte[] { 0x00 },
                new byte[] { 0x53 }, // push ebx
                new byte[] { 0x56 }, // push esi
                new byte[] { 0x57 }, // push edi
                new byte[] { 0x75 }, // jz
                ByteSearch.AnyBytes(1),
                new byte[] { 0x68 }, // push $
                ByteSearch.AnyBytes(1),
                new byte[] { 0x00, 0x00, 0x00 },
                new byte[] { 0x68 }, // push $
                ByteSearch.AnyBytes(4),
                new byte[] { 0x68 }, // push $
                ByteSearch.AnyBytes(1),
                new byte[] { 0x00, 0x00, 0x00 },
                new byte[] { 0xE8 }, // call $
                ByteSearch.AnyBytes(4),
                new byte[] { 0x68 }, // push $
                ByteSearch.AnyBytes(1),
                new byte[] { 0x00, 0x00, 0x00 },
                new byte[] { 0x68 }, // push $
                ByteSearch.AnyBytes(4),
                new byte[] { 0x6A, 0x00 }, // push 0x00
                new byte[] { 0x6A, 0x20 }, // push 0x20
                new byte[] { 0xA3 }, // mov pointer1, eax
                ByteSearch.AnyBytes(4),
                new byte[] { 0xE8 }, // call $
                //ByteSearch.AnyBytes(4),
            };
            if (runtime.Search(codePattern, out var codeOffset) == false)
            {
                result = default;
                return false;
            }

            var pointer1 = runtime.ReadValueU32(codeOffset + 6);
            if (pointer1 == 0)
            {
                throw new InvalidOperationException();
            }

            var pointer2 = runtime.ReadValueU32(codeOffset + 51);
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
