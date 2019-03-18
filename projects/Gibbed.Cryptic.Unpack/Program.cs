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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gibbed.Cryptic.FileFormats;
using Gibbed.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NDesk.Options;
using Hog = Gibbed.Cryptic.FileFormats.Hog;
using Journal = Gibbed.Cryptic.FileFormats.Journal;

namespace Gibbed.Cryptic.Unpack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool? extractUnknowns = null;
            bool overwriteFiles = false;
            bool verbose = true;
            string pattern = null;

            var options = new OptionSet()
            {
                { "o|overwrite", "overwrite existing files", v => overwriteFiles = v != null },
                {
                    "nu|no-unknowns", "don't extract unknown files",
                    v => extractUnknowns = v != null ? false : extractUnknowns
                },
                {
                    "ou|only-unknowns", "only extract unknown files",
                    v => extractUnknowns = v != null ? true : extractUnknowns
                },
                { "v|verbose", "be verbose", v => verbose = v != null },
                { "f|filter=", "match files using pattern", v => pattern = v },
                { "h|help", "show this message and exit", v => showHelp = v != null },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count < 1 ||
                extras.Count > 2 ||
                showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_file.hogg [output_dir]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, null) + "_unpack";

            Regex regex = null;
            if (string.IsNullOrEmpty(pattern) == false)
            {
                regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            using (var input = File.OpenRead(inputPath))
            {
                var hog = new HogFile();
                hog.Deserialize(input);

                var dataList = new Dictionary<int, byte[]>();
                var dataListEntry = hog.Files
                                       .SingleOrDefault(e => e.AttributeId == 0 && e.Size != -1);
                if (dataListEntry != null)
                {
                    using (var data = ReadFileData(hog, dataListEntry, input))
                    {
                        if (data.ReadValueU32(hog.Endian) != 0)
                        {
                            throw new FormatException();
                        }

                        var count = data.ReadValueS32(hog.Endian);
                        if (count < 0)
                        {
                            throw new FormatException();
                        }

                        for (int i = 0; i < count; i++)
                        {
                            var length = data.ReadValueU32(hog.Endian);
                            dataList.Add(i, data.ReadBytes((int)length));
                        }
                    }
                }

                using (var data = new MemoryStream(hog.DataListJournal))
                {
                    var journal = new JournalFile()
                    {
                        Endian = hog.Endian,
                    };
                    journal.Deserialize(data);

                    foreach (var entry in journal.Entries)
                    {
                        if (entry.Action == Journal.Action.Add)
                        {
                            dataList[entry.TargetId] = (byte[])entry.Data.Clone();
                        }
                        else if (entry.Action == Journal.Action.Remove)
                        {
                            if (dataList.Remove(entry.TargetId) == false)
                            {
                                throw new InvalidOperationException();
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }

                var consumedAttributes = new List<int>();
                var files = hog.Files
                               .Where(e => e.Size != -1 && e.AttributeId != 0)
                               .ToArray();

                long current = 0;
                long total = files.Count();

                var names = new Dictionary<int, string>();

                foreach (var entry in files)
                {
                    current++;

                    if (entry.Unknown5 != -2 || entry.AttributeId == -1)
                    {
                        throw new InvalidOperationException();
                    }

                    if (entry.AttributeId < 0 || entry.AttributeId >= hog.Attributes.Count)
                    {
                        throw new InvalidOperationException();
                    }

                    if ((hog.Attributes[entry.AttributeId].Flags & 1) == 1) // entry is unused
                    {
                        throw new InvalidOperationException();
                    }

                    if (consumedAttributes.Contains(entry.AttributeId) == true)
                    {
                        throw new InvalidOperationException();
                    }

                    consumedAttributes.Add(entry.AttributeId);

                    var attribute = hog.Attributes[entry.AttributeId];

                    string name;

                    if (dataList.ContainsKey(attribute.NameId) == false)
                    {
                        name = Path.Combine("__UNKNOWN", attribute.NameId.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        if (names.ContainsKey(attribute.NameId) == true)
                        {
                            name = names[attribute.NameId];
                        }
                        else
                        {
                            using (var temp = new MemoryStream(dataList[attribute.NameId]))
                            {
                                name = temp.ReadStringZ(Encoding.UTF8);
                                names.Add(attribute.NameId, name);
                            }
                        }

                        name = name.Replace('/', Path.DirectorySeparatorChar);
                        if (name.StartsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)) == true)
                        {
                            name = name.Substring(1);
                        }
                    }

                    if (regex != null && regex.IsMatch(name) == false)
                    {
                        continue;
                    }

                    var entryPath = Path.Combine(outputPath, name);

                    if (overwriteFiles == false &&
                        File.Exists(entryPath) == true)
                    {
                        continue;
                    }

                    var entryDirectory = Path.GetDirectoryName(entryPath);
                    if (entryDirectory != null)
                    {
                        Directory.CreateDirectory(entryDirectory);
                    }

                    if (verbose == true)
                    {
                        Console.WriteLine("[{0}/{1}] {2}", current, total, name);
                    }

                    using (var output = File.Create(entryPath))
                    {
                        ReadFileData(hog, entry, input, output);
                    }
                }
            }
        }

        private static MemoryStream ReadFileData(HogFile hog,
                                                 Hog.FileEntry entry,
                                                 Stream input)
        {
            var data = new MemoryStream();
            ReadFileData(hog, entry, input, data);
            data.Position = 0;
            return data;
        }

        private static void ReadFileData(
            HogFile hog,
            Hog.FileEntry entry,
            Stream input,
            Stream output)
        {
            if (hog.Files.Contains(entry) == false)
            {
                throw new ArgumentException("bad entry", "entry");
            }

            if (entry.Size == -1)
            {
                throw new ArgumentException("cannot read file with size of -1", "entry");
            }

            if (entry.Unknown5 != -2 || entry.AttributeId == -1)
            {
                throw new ArgumentException("strange entry");
            }

            if (entry.AttributeId < 0 || entry.AttributeId >= hog.Attributes.Count)
            {
                throw new ArgumentException("entry pointing to invalid metadata", "entry");
            }

            if ((hog.Attributes[entry.AttributeId].Flags & 1) == 1) // entry is unused
            {
                throw new ArgumentException("entry referencing unused attribute", "entry");
            }

            input.Seek(entry.Offset, SeekOrigin.Begin);
            var attribute = hog.Attributes[entry.AttributeId];

            if (attribute.UncompressedSize != 0)
            {
                using (var temp = input.ReadToMemoryStream(entry.Size))
                {
                    var zlib = new InflaterInputStream(temp);
                    output.WriteFromStream(zlib, attribute.UncompressedSize);
                }
            }
            else
            {
                output.WriteFromStream(input, entry.Size);
            }
        }
    }
}
