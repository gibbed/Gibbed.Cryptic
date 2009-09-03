using System;
using System.IO;
using Gibbed.Champions.FileFormats;
using Gibbed.Helpers;
using Ionic.Zlib;
using NConsoler;

namespace Gibbed.Champions.Bacon
{
    internal partial class Program
    {
        [Action(Description = "Unpack a cement (*.rcf) file")]
        public static void Unpack(
            [Required(Description = "input cement file")]
            string inputPath,
            [Required(Description = "output directory")]
            string outputPath,
            [Optional(false, "o", Description = "overwrite existing files")]
            bool overwrite)
        {
            Stream input = File.Open(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Directory.CreateDirectory(outputPath);

            PiggFile pigg = new PiggFile();
            pigg.Deserialize(input);

            Console.WriteLine("{0} files in hogg file.", pigg.Entries.Count);

            long counter = 0;
            long skipped = 0;
            long totalCount = pigg.Entries.Count;
            foreach (PiggFile.Entry entry in pigg.Entries.Values)
            {
                counter++;

                string partPath = entry.Name;

                // fix name
                if (Path.DirectorySeparatorChar != '/')
                {
                    partPath = partPath.Replace('/', Path.DirectorySeparatorChar);
                }

                Directory.CreateDirectory(Path.Combine(outputPath, Path.GetDirectoryName(partPath)));
                string entryPath = Path.Combine(outputPath, partPath);

                if (overwrite == false && File.Exists(entryPath) == true)
                {
                    Console.WriteLine("{1:D5}/{2:D5} !! {0}", partPath, counter, totalCount);
                    skipped++;
                    continue;
                }
                else
                {
                    Console.WriteLine("{1:D5}/{2:D5} => {0}", partPath, counter, totalCount);
                }

                input.Seek(entry.Offset, SeekOrigin.Begin);

                Stream output = File.Open(entryPath, FileMode.Create, FileAccess.Write, FileShare.Read);

                if (entry.UncompressedSize != 0)
                {
                    // ZlibStream likes to choke if there's more data after the entire zlib block for some reason
                    // tempfix
                    MemoryStream temporary = input.ReadToMemoryStream(entry.CompressedSize);

                    ZlibStream zlib = new ZlibStream(temporary, CompressionMode.Decompress, true);
                    int left = entry.UncompressedSize;
                    byte[] block = new byte[4096];
                    while (left > 0)
                    {
                        int read = zlib.Read(block, 0, Math.Min(block.Length, left));
                        if (read == 0)
                        {
                            break;
                        }
                        else if (read < 0)
                        {
                            throw new Exception("zlib error");
                        }

                        output.Write(block, 0, read);
                        left -= read;
                    }
                    
                    zlib.Close();
                }
                else
                {
                    long left = entry.CompressedSize;
                    byte[] data = new byte[4096];
                    while (left > 0)
                    {
                        int block = (int)(Math.Min(left, 4096));
                        input.Read(data, 0, block);
                        output.Write(data, 0, block);
                        left -= block;
                    }
                }

                output.Close();
            }

            input.Close();

            if (skipped > 0)
            {
                Console.WriteLine("{0} files not overwritten.", skipped);
            }
        }
    }
}
