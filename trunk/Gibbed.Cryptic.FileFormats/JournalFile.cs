/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
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
using System.IO;
using Gibbed.IO;

namespace Gibbed.Cryptic.FileFormats
{
    public class JournalFile
    {
        public Endian Endian = Endian.Little;

        public List<Journal.Entry> Entries
            = new List<Journal.Entry>();

        public void Serialize(Stream output)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input)
        {
            var endian = this.Endian;

            var unknown = input.ReadValueU32(endian);
            var size1 = input.ReadValueU32(endian);
            var size2 = input.ReadValueU32(endian);

            if (unknown != 0 ||
                size1 != size2)
            {
                throw new FormatException();
            }

            this.Entries.Clear();
            using (var data = input.ReadToMemoryStream(size1))
            {
                while (data.Position < data.Length)
                {
                    var entry = new Journal.Entry();
                    entry.Action = data.ReadValueEnum<Journal.Action>();
                    entry.TargetId = data.ReadValueS32(endian);

                    if (entry.Action == Journal.Action.Add)
                    {
                        var length = data.ReadValueU32(endian);
                        entry.Data = data.ReadBytes(length);
                    }
                    else if (entry.Action == Journal.Action.Remove)
                    {
                    }
                    else
                    {
                        throw new FormatException();
                    }

                    this.Entries.Add(entry);
                }
            }
        }
    }
}
