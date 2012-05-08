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
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Gibbed.Cryptic.FileFormats;
using NDesk.Options;
using Newtonsoft.Json;
using Blob = Gibbed.Cryptic.FileFormats.Blob;

namespace Gibbed.Cryptic.ConvertObject
{
    internal class Program
    {
        private static string GetExecutablePath()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static void Main(string[] args)
        {
            var xmlSettings = new XmlWriterSettings()
            {
                Indent = true,
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
            };

            var configPath = Path.Combine(
                GetExecutablePath(), "parsers", "objects.json");

            Configuration config;
            if (File.Exists(configPath) == false)
            {
                config = new Configuration();
            }
            else
            {
                string configText;
                using (var input = File.OpenRead(configPath))
                {
                    var reader = new StreamReader(input);
                    configText = reader.ReadToEnd();
                }
                config = JsonConvert.DeserializeObject<Configuration>(configText);
            }

            string schemaName = null;
            var mode = Mode.Unknown;
            var showHelp = false;

            var options = new OptionSet()
            {
                {
                    "b|xml2bin",
                    "convert xml to bin",
                    v => mode = v != null ? Mode.Import : mode
                    },
                {
                    "x|bin2xml",
                    "convert bin to xml",
                    v => mode = v != null ? Mode.Export : mode
                    },
                {
                    "s|schema=",
                    "override schema name",
                    v => schemaName = v
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

            // try to figure out what they want to do
            if (mode == Mode.Unknown &&
                extras.Count >= 1)
            {
                var testPath = extras[0];

                if (Directory.Exists(testPath) == true)
                {
                    mode = Mode.Import;
                }
                else if (File.Exists(testPath) == true)
                {
                    mode = Mode.Export;
                }
            }

            if (extras.Count < 1 || extras.Count > 2 || showHelp == true || mode == Mode.Unknown)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ -x input_bin [output_xml]", GetExecutableName());
                Console.WriteLine("       {0} [OPTIONS]+ -b input_xml [output_bin]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (mode == Mode.Export)
            {
                var inputPath = extras[0];
                var outputPath = extras.Count > 1
                                     ? extras[1]
                                     : Path.ChangeExtension(inputPath, ".xml");

                using (var input = File.OpenRead(inputPath))
                {
                    Console.WriteLine("Loading bin...");
                    var blob = new BlobFile();
                    blob.Deserialize(input);

                    if (schemaName == null)
                    {
                        schemaName = Path.GetFileNameWithoutExtension(inputPath);
                    }

                    var schema = config.GetSchema(schemaName);
                    if (schema == null)
                    {
                        Console.WriteLine("Don't know how to handle '{0}'!", schemaName);
                        return;
                    }

                    var target = schema.GetTarget(blob.ParserHash);
                    if (target == null)
                    {
                        Console.WriteLine("Don't know how to handle '{0}' with a hash of '{1}'.",
                                          schemaName,
                                          blob.ParserHash);
                        return;
                    }

                    var version = target.FirstVersion();
                    if (version == null)
                    {
                        Console.WriteLine("No support for '{0}' with a hash of '{1}'.",
                                          schemaName,
                                          blob.ParserHash);
                        return;
                    }

                    var assemblyPath = Path.Combine(
                        GetExecutablePath(),
                        "parsers",
                        "assemblies",
                        version + ".dll");
                    if (File.Exists(assemblyPath) == false)
                    {
                        Console.WriteLine("Assembly '{0}' appears to be missing!",
                                          Path.GetFileName(assemblyPath));
                        return;
                    }

                    var assembly = Assembly.LoadFrom(assemblyPath);
                    var type = assembly.GetType(target.Class);
                    if (type == null)
                    {
                        Console.WriteLine("Assembly '{0}' does not expose '{1}'!",
                                          Path.GetFileName(assemblyPath),
                                          target.Class);
                        return;
                    }

                    var resource = new Resource();
                    resource.Schema = schemaName;
                    resource.ParserHash = blob.ParserHash;

                    foreach (var file in blob.Files)
                    {
                        resource.Files.Add(new Resource.FileEntry()
                        {
                            Name = file.Name,
                            Timestamp = file.Timestamp,
                        });
                    }

                    foreach (var dependency in blob.Dependencies)
                    {
                        resource.Dependencies.Add(new Resource.DependencyEntry()
                        {
                            Type = dependency.Type,
                            Name = dependency.Name,
                            Hash = dependency.Hash,
                        });
                    }

                    var loadObject = typeof(BlobDataReader)
                        .GetMethod("LoadObject", BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(type);
                    var data = loadObject.Invoke(null,
                                                 new object[]
                                                 {
                                                     input, schema.IsClient, schema.IsServer
                                                 });

                    Console.WriteLine("Saving object to XML...");
                    using (var output = File.Create(outputPath))
                    {
                        var writer = XmlWriter.Create(output, xmlSettings);
                        writer.WriteStartDocument();
                        writer.WriteStartElement("object");

                        writer.WriteStartElement("data");
                        var objectWriter = XmlDictionaryWriter.Create(writer, xmlSettings);
                        var objectSerializer = new DataContractSerializer(type);
                        objectSerializer.WriteStartObject(objectWriter, type);
                        objectWriter.WriteAttributeString("xmlns", "c", null, "http://datacontract.gib.me/cryptic");
                        //objectWriter.WriteAttributeString("xmlns", "i", null, "http://www.w3.org/2001/XMLSchema-instance");
                        objectWriter.WriteAttributeString("xmlns",
                                                          "a",
                                                          null,
                                                          "http://schemas.microsoft.com/2003/10/Serialization/Arrays");
                        //objectWriter.WriteAttributeString("xmlns", "s", null, "http://datacontract.gib.me/startrekonline");
                        objectSerializer.WriteObjectContent(objectWriter, data);
                        objectSerializer.WriteEndObject(objectWriter);
                        objectWriter.Flush();
                        writer.WriteEndElement();

                        var resourceWriter = XmlDictionaryWriter.Create(writer, xmlSettings);
                        var resourceSerializer = new XmlSerializer(typeof(Resource));
                        resourceSerializer.Serialize(resourceWriter, resource);
                        resourceWriter.Flush();

                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                        writer.Flush();
                    }
                }
            }
            else if (mode == Mode.Import)
            {
                var inputPath = extras[0];
                var outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, ".bin");

                Console.WriteLine("Loading XML...");

                Type type;
                Resource resource;
                object data;

                var blob = new BlobFile();

                using (var input = File.OpenRead(inputPath))
                {
                    var doc = new XPathDocument(input);
                    var nav = doc.CreateNavigator();

                    var resourceNode = nav.SelectSingleNode("/object/resource");
                    var resourceSerializer = new XmlSerializer(typeof(Resource));
                    resource = (Resource)resourceSerializer.Deserialize(resourceNode.ReadSubtree());

                    var schema = config.GetSchema(resource.Schema);
                    if (schema == null)
                    {
                        Console.WriteLine("Don't know how to handle '{0}'!", resource.Schema);
                        return;
                    }

                    var target = schema.GetTarget(resource.ParserHash);
                    if (target == null)
                    {
                        Console.WriteLine("Don't know how to handle '{0}' with a hash of {1:X8}.",
                                          resource.Schema,
                                          resource.ParserHash);
                        return;
                    }

                    var version = target.FirstVersion();
                    if (version == null)
                    {
                        Console.WriteLine("No support for '{0}' with a hash of {1:X8}.",
                                          resource.Schema,
                                          resource.ParserHash);
                        return;
                    }

                    var assemblyPath = Path.Combine(
                        GetExecutablePath(),
                        "parsers",
                        "assemblies",
                        version + ".dll");
                    if (File.Exists(assemblyPath) == false)
                    {
                        Console.WriteLine("Assembly '{0}' appears to be missing!",
                                          Path.GetFileName(assemblyPath));
                        return;
                    }

                    var assembly = Assembly.LoadFrom(assemblyPath);
                    type = assembly.GetType(target.Class);
                    if (type == null)
                    {
                        Console.WriteLine("Assembly '{0}' does not expose '{1}'!",
                                          Path.GetFileName(assemblyPath),
                                          target.Class);
                        return;
                    }

                    blob.ParserHash = resource.ParserHash;

                    foreach (var file in resource.Files)
                    {
                        blob.Files.Add(new Blob.FileEntry()
                        {
                            Name = file.Name,
                            Timestamp = file.Timestamp,
                        });
                    }

                    foreach (var dependency in resource.Dependencies)
                    {
                        blob.Dependencies.Add(new Blob.DependencyEntry()
                        {
                            Type = dependency.Type,
                            Name = dependency.Name,
                            Hash = dependency.Hash,
                        });
                    }

                    var objectNode = nav.SelectSingleNode("/object/data");
                    objectNode.MoveToFirstChild();

                    var subtree = objectNode.ReadSubtree();

                    var objectSerializer = new DataContractSerializer(type);
                    data = objectSerializer.ReadObject(subtree);

                    Console.WriteLine("Saving object...");
                    var saveResource = typeof(BlobDataWriter)
                        .GetMethod("SaveObject", BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(type);
                    using (var output = File.Create(outputPath))
                    {
                        blob.Serialize(output);
                        saveResource.Invoke(null,
                                            new object[]
                                            {
                                                data, output, schema.IsClient, schema.IsServer,
                                            });
                    }
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
