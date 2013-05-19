/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
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

namespace Gibbed.Cryptic.ExportSchemas
{
    internal static class Adler32
    {
        public static uint Hash(byte[] buffer, int offset, int count, uint hash)
        {
            uint upper = (ushort)((hash & 0xFFFF0000) >> 16);
            uint lower = (ushort)((hash & 0x0000FFFF) >> 0);

            do
            {
                int run = Math.Min(5552, count);
                int end = offset + run;

                for (; offset < end; offset++)
                {
                    lower += buffer[offset];
                    upper += lower;
                }

                lower %= 0xFFF1;
                upper %= 0xFFF1;

                count -= run;
            }
            while (count > 0);

            return ((upper << 16) & 0xFFFF0000) | ((lower & 0x0000FFFF) << 0);
        }

        public static uint Hash(string value, uint hash)
        {
            var bytes = Encoding.ASCII.GetBytes(value.ToUpperInvariant());
            return Hash(bytes, 0, bytes.Length, hash);
        }

        public static uint Hash(int value, uint hash)
        {
            var bytes = BitConverter.GetBytes(value);
            return Hash(bytes, 0, bytes.Length, hash);
        }

        public static uint Hash(uint value, uint hash)
        {
            var bytes = BitConverter.GetBytes(value);
            return Hash(bytes, 0, bytes.Length, hash);
        }

        public static uint Hash(ulong value, uint hash)
        {
            var bytes = BitConverter.GetBytes(value);
            return Hash(bytes, 0, bytes.Length, hash);
        }
    }
}
