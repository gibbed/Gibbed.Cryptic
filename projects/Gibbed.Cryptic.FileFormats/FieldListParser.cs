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

namespace Gibbed.Cryptic.FileFormats
{
    public static class FieldListParser<TType>
    {
        public static FieldList<TType> ToList(int[] bits, int count)
        {
            if (bits.Length != count)
            {
                throw new ArgumentException("unexpected array size", "bits");
            }

            var list = new FieldList<TType>();

            for (int i = 0; i < count; i++)
            {
                if (bits[i] == 0)
                {
                    continue;
                }

                var flags = (uint)bits[i];
                int j = 0;
                while (flags != 0)
                {
                    if ((flags & 1) != 0)
                    {
                        list.Add((TType)Enum.ToObject(typeof(TType), (i * 32) + j));
                    }

                    flags >>= 1;
                    j++;
                }
            }

            return list;
        }

        public static int[] FromList(FieldList<TType> list, int count)
        {
            var bits = new int[count];

            foreach (var item in list)
            {
                var bit = (int)((object)item); // :argh:
                bits[bit >> 5] |= 1 << (bit % 32);
            }

            return bits;
        }
    }
}
