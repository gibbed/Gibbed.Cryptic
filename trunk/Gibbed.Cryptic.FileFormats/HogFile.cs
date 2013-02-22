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
using System.IO;
using Gibbed.IO;

namespace Gibbed.Cryptic.FileFormats
{
    public class HogFile
    {
        public const uint Signature = 0xDEADF00D;

        public Endian Endian;
        public ushort Version;

        public List<Hog.FileEntry> Files = new List<Hog.FileEntry>();
        public List<Hog.AttributeEntry> Attributes = new List<Hog.AttributeEntry>();

        public byte[] OperationJournal;
        public byte[] DataListJournal;

        public void Serialize(Stream output)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(Stream input)
        {
            var magic = input.ReadValueU32(Endian.Little);
            if (magic != Signature &&
                magic.Swap() != Signature)
            {
                throw new FormatException("bad hog magic");
            }
            var endian = magic == Signature ? Endian.Little : Endian.Big;

            var version = input.ReadValueU16(endian);
            if (version < 10 || version > 11)
            {
                throw new FormatException("unsupported hog version");
            }

            var operationJournalSize = input.ReadValueU16(endian);
            var fileListSize = input.ReadValueU32(endian);
            var attributeListSize = input.ReadValueU32(endian);
            var dataListFileNumber = input.ReadValueU32(endian);
            var dataListJournalSize = input.ReadValueU32(endian);

            if (operationJournalSize > 1024)
            {
                throw new FormatException("bad hog operation journal size");
            }

            this.OperationJournal = input.ReadBytes(operationJournalSize);
            this.DataListJournal = input.ReadBytes(dataListJournalSize);

            this.Files.Clear();
            using (var data = input.ReadToMemoryStream(fileListSize))
            {
                while (data.Position < data.Length)
                {
                    // ReSharper disable UseObjectOrCollectionInitializer
                    var entry = new Hog.FileEntry();
                    // ReSharper restore UseObjectOrCollectionInitializer
                    entry.Offset = data.ReadValueS64(endian);
                    entry.Size = data.ReadValueS32(endian);
                    entry.Timestamp = data.ReadValueU32(endian);
                    entry.Checksum = data.ReadValueU32(endian);
                    entry.Unknown4 = data.ReadValueU32(endian);
                    entry.Unknown5 = data.ReadValueS16(endian);
                    entry.Unknown6 = data.ReadValueS16(endian);
                    entry.AttributeId = data.ReadValueS32(endian);
                    this.Files.Add(entry);
                }
            }

            this.Attributes.Clear();
            using (var data = input.ReadToMemoryStream(attributeListSize))
            {
                while (data.Position < data.Length)
                {
                    // ReSharper disable UseObjectOrCollectionInitializer
                    var entry = new Hog.AttributeEntry();
                    // ReSharper restore UseObjectOrCollectionInitializer
                    entry.NameId = data.ReadValueS32(endian);
                    entry.HeaderDataId = data.ReadValueS32(endian);
                    entry.UncompressedSize = data.ReadValueU32(endian);
                    entry.Flags = data.ReadValueU32(endian);
                    this.Attributes.Add(entry);
                }
            }

            this.Endian = endian;
            this.Version = version;
        }
    }
}
