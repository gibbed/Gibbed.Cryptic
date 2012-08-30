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
using Gibbed.IO;

namespace Gibbed.Cryptic.FileFormats
{
    public class TextureFile
    {
        public void Serialize(Stream output)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input)
        {
            var basePosition = input.Position;
            var endian = Endian.Little;

            var headerSize = input.ReadValueU32(endian);
            var fileSize = input.ReadValueU32(endian);

            if (headerSize > 0x1928 ||
                headerSize > fileSize ||
                basePosition + fileSize > input.Length)
            {
                throw new FormatException();
            }

            if (headerSize > 36)
            {
                throw new FormatException("no support for cached mipmaps yet");
            }

            var width = input.ReadValueU32(endian);
            var height = input.ReadValueU32(endian);
            var flags = input.ReadValueU32(endian);
            var unknown14 = input.ReadValueU32(endian);
            var unknown18 = input.ReadValueU32(endian);
            var format = input.ReadValueEnum<Texture.RenderFormat>(endian);
            bool alpha = input.ReadValueB8();
            var verpad = input.ReadBytes(3);

            if (width > 2048 ||
                height > 2048 ||
                format != Texture.RenderFormat.Unknown)
            {
                throw new FormatException();
            }

            throw new NotImplementedException();
        }
    }
}
