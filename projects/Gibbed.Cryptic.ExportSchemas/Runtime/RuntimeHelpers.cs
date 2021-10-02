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

namespace Gibbed.Cryptic.ExportSchemas.Runtime
{
    internal static class RuntimeHelpers
    {
        public static bool Search(this RuntimeProcess runtime, ByteSearch.Pattern pattern, out IntPtr result)
        {
            const int blockSize = 0x00A00000;
            var data = new byte[blockSize];
            var address = runtime.Process.MainModule.BaseAddress;
            var totalSize = runtime.Process.MainModule.ModuleMemorySize;
            for (int i = 0; i < totalSize; i += blockSize - pattern.Count)
            {
                int size = Math.Min(blockSize, totalSize - i);
                runtime.Read(address + i, data, 0, size);

                int offset;
                if (ByteSearch.Match(data, 0, size, pattern, out offset) == true)
                {
                    result = address + i + offset;
                    return true;
                }
            }
            result = default;
            return false;
        }
    }
}
