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
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.XPath;
using Gibbed.Cryptic.FileFormats;
using NDesk.Options;

namespace Gibbed.Cryptic.ConvertBin
{
    public abstract class Converter<TType>
        where TType: ICrypticStructure, new()
    {
        private static string GetExecutablePath()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        protected abstract string GetPath(TType entry);

        public void Main(uint validHash, string[] args)
        {
            var mode = Mode.Unknown;
            var showHelp = false;

            var options = new OptionSet()
            {
                {
                    "b|xml2bin",
                    "convert xml to bin",
                    v => mode = v != null ? Mode.ToBIN : mode
                },
                {
                    "x|bin2xml",
                    "convert bin to xml",
                    v => mode = v != null ? Mode.ToXML : mode
                },
                {
                    "h|help",
                    "show this message and exit", 
                    v => showHelp = v != null
                },
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

            if (extras.Count < 1 || extras.Count > 2 || showHelp == true || mode == Mode.Unknown)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ -x input_bin [output_dir]", GetExecutableName());
                Console.WriteLine("       {0} [OPTIONS]+ -b input_dir [output_bin]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (mode == Mode.ToXML)
            {
                var inputPath = extras[0];
                var outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, null);

                using (var input = File.OpenRead(inputPath))
                {
                    Console.WriteLine("Loading bin...");
                    var blob = new BlobFile();
                    blob.Deserialize(input);
                    if (blob.ParserHash != validHash)
                    {
                        Console.WriteLine("Invalid parser hash, new version of {0}?", typeof(TType).Name);
                        return;
                    }

                    Directory.CreateDirectory(outputPath);
                    using (var index = File.Create(Path.Combine(outputPath, "@blob.xml")))
                    {
                        Console.WriteLine("Saving XML...");

                        var xml = XmlWriter.Create(index, new XmlWriterSettings() { Indent = true, });

                        xml.WriteStartDocument();
                        xml.WriteStartElement("blob");
                        xml.WriteAttributeString("hash", blob.ParserHash.ToString());

                        xml.WriteStartElement("files");
                        foreach (var entry in blob.Files)
                        {
                            xml.WriteStartElement("file");
                            xml.WriteAttributeString("name", entry.Name);
                            xml.WriteAttributeString("timestamp", entry.Timestamp.ToString());
                            xml.WriteEndElement();
                        }
                        xml.WriteEndElement();

                        xml.WriteStartElement("dependencies");
                        foreach (var entry in blob.Dependencies)
                        {
                            xml.WriteStartElement("dependency");
                            xml.WriteAttributeString("unknown0", entry.Unknown0.ToString());
                            xml.WriteAttributeString("name", entry.Name);
                            xml.WriteAttributeString("unknown1", entry.Unknown1.ToString());
                            xml.WriteEndElement();
                        }
                        xml.WriteEndElement();

                        xml.WriteStartElement("entries");
                        {
                            var serializer = new DataContractSerializer(typeof(TType));

                            Console.WriteLine("Loading entries...");
                            var entries = BlobReader.LoadResource<TType>(input);

                            Console.WriteLine("Saving XML entries...");
                            foreach (var entry in entries)
                            {
                                string entryName = this.GetPath(entry);

                                if (entryName == null)
                                {
                                    throw new InvalidOperationException();
                                }

                                var entryPath = Path.Combine(outputPath, entryName);

                                if (File.Exists(entryPath) == true)
                                {
                                    throw new InvalidOperationException();
                                }

                                xml.WriteElementString("entry", entryName);

                                Directory.CreateDirectory(Path.GetDirectoryName(entryPath));

                                using (var output = File.Create(entryPath))
                                {
                                    var settings = new XmlWriterSettings()
                                    {
                                        Indent = true,
                                        NewLineChars = "\r\n",
                                        NewLineHandling = NewLineHandling.Entitize,
                                    };

                                    var writer = XmlDictionaryWriter.Create(output, settings);
                                    serializer.WriteObject(writer, entry);
                                    writer.Flush();
                                }
                            }
                        }
                        xml.WriteEndElement();
                        xml.WriteEndDocument();

                        xml.Flush();
                    }
                }
            }
            else if (mode == Mode.ToBIN)
            {
                var inputPath = extras[0];
                var outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, ".bin");

                var entries = new List<TType>();
                var serializer = new DataContractSerializer(typeof(TType));

                var blob = new BlobFile();

                using (var index = File.OpenRead(Path.Combine(inputPath, "@blob.xml")))
                {
                    var doc = new XPathDocument(index);
                    var nav = doc.CreateNavigator();

                    Console.WriteLine("Loading XML...");

                    blob.ParserHash = uint.Parse(nav.SelectSingleNode("/blob").GetAttribute("hash", ""));

                    var fileNodes = nav.Select("/blob/files/file");
                    while (fileNodes.MoveNext() == true)
                    {
                        blob.Files.Add(new Cryptic.FileFormats.Blob.FileEntry()
                        {
                            Name = fileNodes.Current.GetAttribute("name", ""),
                            Timestamp = uint.Parse(fileNodes.Current.GetAttribute("timestamp", "")),
                        });
                    }

                    var dependencyNodes = nav.Select("/blob/dependencies/dependency");
                    while (dependencyNodes.MoveNext() == true)
                    {
                        blob.Dependencies.Add(new Cryptic.FileFormats.Blob.DependencyEntry()
                        {
                            Unknown0 = uint.Parse(dependencyNodes.Current.GetAttribute("unknown0", "")),
                            Name = dependencyNodes.Current.GetAttribute("name", ""),
                            Unknown1 = uint.Parse(dependencyNodes.Current.GetAttribute("unknown1", "")),
                        });
                    }

                    Console.WriteLine("Loading XML entries...");

                    var entryNodes = nav.Select("/blob/entries/entry");
                    while (entryNodes.MoveNext() == true)
                    {
                        var entryName = entryNodes.Current.Value;
                        var entryPath = Path.Combine(inputPath, entryName);

                        using (var input = File.OpenRead(entryPath))
                        {
                            var reader = XmlDictionaryReader.Create(input);
                            var entry = (TType)serializer.ReadObject(reader);
                            entries.Add(entry);
                        }
                    }
                }

                using (var output = File.Create(outputPath))
                {
                    Console.WriteLine("Saving bin...");
                    blob.Serialize(output);

                    Console.WriteLine("Saving entries...");
                    BlobWriter.SaveResource(entries, output);
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
