using System;
using System.Collections.Generic;
using System.IO;
using Gibbed.Helpers;
using Ionic.Zlib;

namespace Gibbed.Cryptic.FileFormats
{
    internal class HoggFile
    {
        public List<Hogg.DataDelta> DataDeltas = new List<Hogg.DataDelta>();
        public List<Hogg.Block> Blocks = new List<Hogg.Block>();
        public List<Hogg.Metadata> Metadatas = new List<Hogg.Metadata>();

        public MemoryStream ReadBlockToMemoryStream(Hogg.Block block, Stream input)
        {
            MemoryStream output = new MemoryStream();
            this.ReadBlockToStream(block, input, output);
            output.Seek(0, SeekOrigin.Begin);
            return output;
        }

        public void ReadBlockToStream(Hogg.Block block, Stream input, Stream output)
        {
            if (this.Blocks.Contains(block) == false)
            {
                throw new ArgumentException("block is not from this hogg", "block");
            }

            if (block.Size == -1)
            {
                throw new Exception("bad block");
            }
            else if (block.Unknown4 != -2 || block.MetadataId == -1)
            {
                throw new Exception("strange block");
            }
            else if (block.MetadataId < 0 || block.MetadataId >= this.Metadatas.Count)
            {
                throw new FormatException("block pointing to invalid metadata");
            }
            else if ((this.Metadatas[block.MetadataId].Flags & 1) == 1) // entry is unused
            {
                throw new FormatException("block referencing unused metadata");
            }

            Hogg.Metadata metadata = this.Metadatas[block.MetadataId];

            MemoryStream rez = new MemoryStream();
            input.Seek(block.Offset, SeekOrigin.Begin);

            if (metadata.UncompressedSize != 0)
            {
                // ZlibStream likes to choke if there's more data after the entire zlib block for some reason
                // tempfix
                MemoryStream temporary = input.ReadToMemoryStream(block.Size);

                ZlibStream zlib = new ZlibStream(temporary, CompressionMode.Decompress, true);
                int left = metadata.UncompressedSize;
                byte[] data = new byte[4096];
                while (left > 0)
                {
                    int read = zlib.Read(data, 0, Math.Min(data.Length, left));
                    if (read == 0)
                    {
                        break;
                    }
                    else if (read < 0)
                    {
                        throw new Exception("zlib error");
                    }

                    output.Write(data, 0, read);
                    left -= read;
                }

                zlib.Close();
            }
            else
            {
                int left = block.Size;
                byte[] data = new byte[4096];
                while (left > 0)
                {
                    int read = input.Read(data, 0, Math.Min(left, 4096));
                    output.Write(data, 0, read);
                    left -= read;
                }
            }
        }

        public void Deserialize(Stream input)
        {
            Hogg.Header header = input.ReadStructure<Hogg.Header>();
            
            if (header.Magic != 0xDEADF00D)
            {
                throw new FormatException("not a hogg file");
            }

            if (header.Version != 10)
            {
                throw new FormatException("bad hogg version");
            }

            input.Seek(header.Unknown2, SeekOrigin.Current);

            this.DataDeltas.Clear();
            this.DeserializeDataDeltaTable(input.ReadToMemoryStream(header.DataDeltaTableSize));
            
            Stream blockTable = input.ReadToMemoryStream(header.BlockTableSize);
            Stream metadataTable = input.ReadToMemoryStream(header.MetadataTableSize);

            this.Blocks.Clear();
            while (blockTable.Position < blockTable.Length)
            {
                this.Blocks.Add(blockTable.ReadStructure<Hogg.Block>());
            }

            this.Metadatas.Clear();
            while (metadataTable.Position < metadataTable.Length)
            {
                this.Metadatas.Add(metadataTable.ReadStructure<Hogg.Metadata>());
            }
        }

        private void DeserializeDataDeltaTable(Stream input)
        {
            int unk1 = input.ReadValueS32();
            int size1 = input.ReadValueS32();
            int size2 = input.ReadValueS32();

            if (unk1 != 0 || size1 != size2)
            {
                throw new Exception();
            }

            while (input.Position < 12 + size1)
            {
                Hogg.DataDelta delta = new Hogg.DataDelta();
                delta.Action = input.ReadValueU8();
                
                if (delta.Action == 1) // add
                {
                    delta.Id = input.ReadValueS32();
                    int length = input.ReadValueS32();
                    delta.Data = new byte[length];
                    input.Read(delta.Data, 0, delta.Data.Length);
                }
                else if (delta.Action == 2)
                {
                    delta.Id = input.ReadValueS32();
                }
                else
                {
                    throw new FormatException("invalid action");
                }

                this.DataDeltas.Add(delta);
            }
        }
    }
}
