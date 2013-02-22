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
using System.IO;
using System.Text;
using Gibbed.IO;

namespace Gibbed.Cryptic.FileFormats
{
    public static class StreamHelpers
    {
        public static string ReadStringPascal(this Stream stream, int maxLength)
        {
            var length = stream.ReadValueU16();
            if (length > maxLength)
            {
                throw new FormatException("string length exceeds maximum length");
            }
            var text = stream.ReadString(length, Encoding.UTF8);
            var padding = (-(length - 2)) & 3;
            stream.Seek(padding, SeekOrigin.Current);
            return text;
        }

        public static string ReadStringPascalUncapped(this Stream stream)
        {
            var length = stream.ReadValueU16();
            var text = stream.ReadString(length, Encoding.UTF8);
            var padding = (-(length - 2)) & 3;
            stream.Seek(padding, SeekOrigin.Current);
            return text;
        }

        private static readonly byte[] _Padding = new byte[4];

        public static void WriteStringPascal(this Stream stream, string value, int maxLength)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > 0xFFFF)
            {
                throw new InvalidOperationException();
            }
            var length = (ushort)bytes.Length;

            if (length > maxLength)
            {
                throw new ArgumentException("value is too long", "value");
            }

            stream.WriteValueU16(length);
            stream.WriteBytes(bytes);
            stream.Write(_Padding, 0, (-(length - 2)) & 3);
        }

        public static void WriteStringPascalUncapped(this Stream stream, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > 0xFFFF)
            {
                throw new InvalidOperationException();
            }
            var length = (ushort)bytes.Length;

            stream.WriteValueU16(length);
            stream.WriteBytes(bytes);
            stream.Write(_Padding, 0, (-(length - 2)) & 3);
        }
    }
}
