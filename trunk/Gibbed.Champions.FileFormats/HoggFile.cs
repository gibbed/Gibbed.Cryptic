using System;
using System.Collections.Generic;
using System.IO;
using Gibbed.Helpers;

namespace Gibbed.Champions.FileFormats
{
    public class HoggFile
    {
        public Dictionary<string, HoggEntry> Entries = new Dictionary<string, HoggEntry>();

        public void Deserialize(Stream input)
        {
            HoggHeader header = input.ReadStructure<HoggHeader>();
            
            if (header.Magic != 0xDEADF00D)
            {
                throw new FormatException("not a hogg file");
            }

            if (header.Version != 10)
            {
                throw new FormatException("bad hogg version");
            }

            input.Seek(header.Unknown2, SeekOrigin.Current);

            Dictionary<int, string> strings = this.DeserializeStringTable(input.ReadToMemoryStream(header.StringTableSize));
            
            Stream blockTable = input.ReadToMemoryStream(header.BlockTableSize);
            Stream metadataTable = input.ReadToMemoryStream(header.MetadataTableSize);

            List<HoggBlock> blocks = new List<HoggBlock>();
            while (blockTable.Position < blockTable.Length)
            {
                blocks.Add(blockTable.ReadStructure<HoggBlock>());
            }

            List<HoggMetadata> metadatas = new List<HoggMetadata>();
            while (metadataTable.Position < metadataTable.Length)
            {
                metadatas.Add(metadataTable.ReadStructure<HoggMetadata>());
            }

            this.Entries.Clear();

            List<int> usedMetadatas = new List<int>();
            foreach (HoggBlock block in blocks)
            {
                if (block.Size == -1)
                {
                    continue;
                }
                else if (block.Unknown4 != -2 || block.EntryId == -1)
                {
                    throw new Exception("strange block");
                }

                if (block.EntryId < 0 || block.EntryId >= metadatas.Count)
                {
                    throw new FormatException("hogg block pointing to invalid entry");
                }
                else if ((metadatas[block.EntryId].Flags & 1) == 1) // entry is unused
                {
                    throw new FormatException("hogg block referencing unused entry");
                }
                else if (usedMetadatas.Contains(block.EntryId) == true)
                {
                    throw new FormatException("hogg block referencing entry already consumed");
                }

                usedMetadatas.Add(block.EntryId);

                HoggMetadata metadata = metadatas[block.EntryId];

                if (metadata.Unknown04 != -1)
                {
                    throw new Exception("unknown04 is not -1");
                }

                if (strings.ContainsKey(metadata.NameIndex) == false)
                {
                    continue;
                }

                HoggEntry entry = new HoggEntry();
                entry.Name = strings[metadata.NameIndex];
                entry.Offset = block.Offset;
                entry.CompressedSize = block.Size;
                entry.UncompressedSize = metadata.UncompressedSize;
                this.Entries.Add(entry.Name, entry);
            }
        }

        private Dictionary<int, string> DeserializeStringTable(Stream input)
        {
            Dictionary<int, string> rez = new Dictionary<int, string>();

            int unk1 = input.ReadValueS32();
            int size1 = input.ReadValueS32();
            int size2 = input.ReadValueS32();

            if (unk1 != 0 || size1 != size2)
            {
                throw new Exception();
            }

            while (input.Position < 12 + size1)
            {
                byte type = input.ReadValueU8();
                if (type == 1)
                {
                    int id = input.ReadValueS32();
                    string text = input.ReadStringASCII(input.ReadValueU32(), true);

                    rez.Add(id, text);
                }
                else if (type == 2)
                {
                    int id = input.ReadValueS32();
                    rez.Remove(id);
                }
                else
                {
                    throw new Exception();
                }
            }

            return rez;
        }
    }
}
