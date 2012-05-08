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
using System.Collections.Generic;
using Enumerable = System.Linq.Enumerable;

namespace Gibbed.Cryptic.FileFormats
{
    public static class EnumParser<T>
        where T: struct
    {
        public static string ToStringValue(T value)
        {
            return Enum.Format(typeof(T), value, "g");
        }

        public static T FromStringValue(string value)
        {
            T parsed;
            if (Enum.TryParse(value, out parsed) == false)
            {
                throw new FormatException();
            }
            return parsed;
        }

        public static List<T> FromStringList(List<string> list)
        {
            var items = new List<T>();
            foreach (var item in list)
            {
                T value;
                if (Enum.TryParse(item, out value) == false)
                {
                    throw new FormatException();
                }
                items.Add(value);
            }
            return items;
        }

        public static List<string> ToStringList(List<T> list)
        {
            var items = new List<string>();
            if (list != null)
            {
                items.AddRange(Enumerable.Select(list, item => Enum.Format(typeof(T), item, "g")));
            }
            return items;
        }
    }
}
