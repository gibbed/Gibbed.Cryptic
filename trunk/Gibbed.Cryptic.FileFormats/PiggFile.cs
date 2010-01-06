using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gibbed.Helpers;

namespace Gibbed.Champions.FileFormats
{
    public class PiggFile
    {
        public class Entry
        {
            public string Name;
            public long Offset;
            public int CompressedSize;
            public int UncompressedSize;
        }

        public Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();

        public void Deserialize(Stream input)
        {
            HoggFile hogg = new HoggFile();
            hogg.Deserialize(input);

            Dictionary<int, byte[]> datas = new Dictionary<int, byte[]>();
            
            // Load the file data list
            Hogg.Block dataListBlock = hogg.Blocks.SingleOrDefault(candidate => candidate.MetadataId == 0 && candidate.Size != -1);
            if (dataListBlock != null)
            {
                MemoryStream dataList = hogg.ReadBlockToMemoryStream(dataListBlock, input);

                int unk1 = dataList.ReadValueS32();
                int count = dataList.ReadValueS32();

                if (unk1 != 0)
                {
                    throw new FormatException("unk1 in data list is not 0");
                }

                for (int i = 0; i < count; i++)
                {
                    int length = dataList.ReadValueS32();
                    byte[] data = new byte[length];
                    dataList.Read(data, 0, data.Length);
                    datas.Add(i, data);
                }
            }

            foreach (Hogg.DataDelta delta in hogg.DataDeltas)
            {
                if (delta.Action == 1)
                {
                    //datas.Add(delta.Id, delta.Data);
                    datas[delta.Id] = delta.Data;
                }
                else if (delta.Action == 2)
                {
                    if (datas.Remove(delta.Id) == false)
                    {
                        throw new KeyNotFoundException("data delta wanted to remove not found");
                    }
                }
                else
                {
                    throw new InvalidOperationException("invalid action");
                }
            }

            this.Entries.Clear();

            List<int> usedMetadatas = new List<int>();
            foreach (Hogg.Block block in hogg.Blocks)
            {
                if (block.Size == -1)
                {
                    continue;
                }
                else if (block.Unknown4 != -2 || block.MetadataId == -1)
                {
                    throw new Exception("strange block");
                }
                else if (block.MetadataId < 0 || block.MetadataId >= hogg.Metadatas.Count)
                {
                    throw new FormatException("block pointing to invalid metadata");
                }
                else if ((hogg.Metadatas[block.MetadataId].Flags & 1) == 1) // entry is unused
                {
                    throw new FormatException("block referencing unused metadata");
                }
                else if (usedMetadatas.Contains(block.MetadataId) == true)
                {
                    throw new FormatException("block referencing metadata already consumed");
                }

                usedMetadatas.Add(block.MetadataId);

                // don't add the data list
                if (block.MetadataId == 0)
                {
                    continue;
                }

                Hogg.Metadata metadata = hogg.Metadatas[block.MetadataId];

                if (datas.ContainsKey(metadata.NameIndex) == true)
                {
                    Entry entry = new Entry();
                    entry.Name = datas[metadata.NameIndex].ToStringASCIIZ(0);
                    entry.Offset = block.Offset;
                    entry.CompressedSize = block.Size;
                    entry.UncompressedSize = metadata.UncompressedSize;
                    this.Entries.Add(entry.Name, entry);
                }
                else
                {
                    Entry entry = new Entry();
                    entry.Name = Path.Combine("__UNKNOWN", metadata.NameIndex.ToString());
                    entry.Offset = block.Offset;
                    entry.CompressedSize = block.Size;
                    entry.UncompressedSize = metadata.UncompressedSize;
                    this.Entries.Add(entry.Name, entry);
                }
            }
        }
    }
}
