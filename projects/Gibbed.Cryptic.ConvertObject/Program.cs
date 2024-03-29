﻿/* Copyright (c) 2021 Rick (rick 'at' gibbed 'dot' us)
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
using Blob = Gibbed.Cryptic.FileFormats.Blob;

namespace Gibbed.Cryptic.ConvertObject
{
    internal class Program
    {
        private static string GetExecutablePath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private static string GetExecutableName()
        {
            return Path.GetFileName(Assembly.GetExecutingAssembly().Location);
        }

        public static void Main(string[] args)
        {
            var xmlSettings = new XmlWriterSettings()
            {
                Indent = true,
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
            };

            var configBasePath = Path.Combine(GetExecutablePath(), "serializers", "objects");
            var config = Configuration.Load(configBasePath);

            string parseName = null;
            var mode = Mode.Unknown;
            var showHelp = false;

            var options = new OptionSet()
            {
                { "b|xml2bin", "convert xml to bin", v => mode = v != null ? Mode.Import : mode },
                { "x|bin2xml", "convert bin to xml", v => mode = v != null ? Mode.Export : mode },
                { "p|parse=", "override parse name", v => parseName = v },
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

            // try to figure out what they want to do
            if (mode == Mode.Unknown && extras.Count >= 1)
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

                    if (parseName == null)
                    {
                        parseName = Path.GetFileNameWithoutExtension(inputPath);
                    }

                    var schema = config.GetSchema(parseName);
                    if (schema == null)
                    {
                        Console.WriteLine(
                            "Don't know how to handle '{0}'  with a hash of '{1}'.",
                            parseName,
                            blob.ParseHash);
                        return;
                    }

                    var target = schema.GetTarget(blob.ParseHash);
                    if (target == null)
                    {
                        Console.WriteLine(
                            "Don't know how to handle '{0}' with a hash of '{1}'.",
                            parseName,
                            blob.ParseHash);
                        return;
                    }

                    var version = target.FirstVersion();
                    if (version == null)
                    {
                        Console.WriteLine(
                            "No support for '{0}' with a hash of '{1}'.",
                            parseName,
                            blob.ParseHash);
                        return;
                    }

                    var assemblyPath = Path.Combine(
                        GetExecutablePath(),
                        "serializers",
                        "assemblies",
                        version + ".dll");
                    if (File.Exists(assemblyPath) == false)
                    {
                        Console.WriteLine(
                            "Assembly '{0}' appears to be missing!",
                            Path.GetFileName(assemblyPath));
                        return;
                    }

                    var assembly = Assembly.LoadFrom(assemblyPath);
                    var type = assembly.GetType(target.Class);
                    if (type == null)
                    {
                        Console.WriteLine(
                            "Assembly '{0}' does not expose '{1}'!",
                            Path.GetFileName(assemblyPath),
                            target.Class);
                        return;
                    }

                    var resource = new Resource()
                    {
                        Parse = parseName,
                        ParseHash = blob.ParseHash,
                    };

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

                    Func<int, string> getFileNameFromIndex =
                        i =>
                        {
                            if (i < 0 || i >= resource.Files.Count)
                            {
                                throw new KeyNotFoundException($"file index {i} is out of range");
                            }

                            return resource.Files[i].Name;
                        };

                    var loadObject = typeof(BlobDataReader)
                        .GetMethod("LoadObject", BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(type);
                    var data = loadObject.Invoke(
                        null,
                        new object[] { input, schema.IsClient, schema.IsServer, getFileNameFromIndex });

                    Console.WriteLine("Saving object to XML...");
                    using (var output = File.Create(outputPath))
                    {
                        var writer = XmlWriter.Create(output, xmlSettings);
                        writer.WriteStartDocument();
                        writer.WriteStartElement("object");

                        writer.WriteStartElement("data");
                        var objectWriter = XmlWriter.Create(writer, xmlSettings);
                        var objectSerializer = new DataContractSerializer(type);
                        objectSerializer.WriteStartObject(objectWriter, type);
                        objectWriter.WriteAttributeString("xmlns", "c", "", "http://datacontract.gib.me/cryptic");
                        //objectWriter.WriteAttributeString("xmlns", "i", "", "http://www.w3.org/2001/XMLSchema-instance");
                        objectWriter.WriteAttributeString(
                            "xmlns",
                            "a",
                            "",
                            "http://schemas.microsoft.com/2003/10/Serialization/Arrays");
                        //objectWriter.WriteAttributeString("xmlns", "s", "", "http://datacontract.gib.me/startrekonline");
                        objectSerializer.WriteObjectContent(objectWriter, data);
                        objectSerializer.WriteEndObject(objectWriter);
                        objectWriter.Flush();
                        writer.WriteEndElement();

                        var resourceWriter = XmlWriter.Create(writer, xmlSettings);
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

                var blob = new BlobFile();

                using (var input = File.OpenRead(inputPath))
                {
                    var doc = new XPathDocument(input);
                    var nav = doc.CreateNavigator();

                    var resourceNode = nav.SelectSingleNode("/object/resource");
                    if (resourceNode == null)
                    {
                        throw new InvalidOperationException();
                    }
                    var resourceSerializer = new XmlSerializer(typeof(Resource));
                    var resource = (Resource)resourceSerializer.Deserialize(resourceNode.ReadSubtree());

                    var parse = config.GetSchema(resource.Parse);
                    if (parse == null)
                    {
                        Console.WriteLine("Don't know how to handle '{0}'!", resource.Parse);
                        return;
                    }

                    var target = parse.GetTarget(resource.ParseHash);
                    if (target == null)
                    {
                        Console.WriteLine(
                            "Don't know how to handle '{0}' with a hash of {1}.",
                            resource.Parse,
                            resource.ParseHash);
                        return;
                    }

                    var version = target.FirstVersion();
                    if (version == null)
                    {
                        Console.WriteLine(
                            "No support for '{0}' with a hash of {1:X8}.",
                            resource.Parse,
                            resource.ParseHash);
                        return;
                    }

                    var assemblyPath = Path.Combine(
                        GetExecutablePath(),
                        "serializers",
                        "assemblies",
                        version + ".dll");
                    if (File.Exists(assemblyPath) == false)
                    {
                        Console.WriteLine(
                            "Assembly '{0}' appears to be missing!",
                            Path.GetFileName(assemblyPath));
                        return;
                    }

                    var assembly = Assembly.LoadFrom(assemblyPath);
                    var type = assembly.GetType(target.Class);
                    if (type == null)
                    {
                        Console.WriteLine(
                            "Assembly '{0}' does not expose '{1}'!",
                            Path.GetFileName(assemblyPath),
                            target.Class);
                        return;
                    }

                    blob.ParseHash = resource.ParseHash;

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
                    if (objectNode == null)
                    {
                        throw new InvalidOperationException();
                    }

                    objectNode.MoveToFirstChild();

                    var subtree = objectNode.ReadSubtree();

                    var objectSerializer = new DataContractSerializer(type);
                    var data = objectSerializer.ReadObject(subtree);

                    Func<string, int> getIndexFromFileName = s => blob.Files.FindIndex(fe => fe.Name == s);

                    Console.WriteLine("Saving object...");
                    var saveResource = typeof(BlobDataWriter)
                        .GetMethod("SaveObject", BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(type);
                    using (var output = File.Create(outputPath))
                    {
                        blob.Serialize(output);
                        saveResource.Invoke(
                            null,
                            new[] { data, output, parse.IsClient, parse.IsServer, getIndexFromFileName });
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
