/* Copyright (c) 2013 Rick (rick 'at' gibbed 'dot' us)
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Gibbed.Cryptic.FileFormats;
using NDesk.Options;
using Blob = Gibbed.Cryptic.FileFormats.Blob;

namespace Gibbed.Cryptic.ConvertResource
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

            var configBasePath = Path.Combine(GetExecutablePath(), "serializers", "resources");
            var config = Configuration.Load(configBasePath);

            string schemaName = null;
            var mode = Mode.Unknown;
            var showHelp = false;

            var options = new OptionSet()
            {
                { "b|xml2bin", "convert xml to bin", v => mode = v != null ? Mode.Import : mode },
                { "x|bin2xml", "convert bin to xml", v => mode = v != null ? Mode.Export : mode },
                { "s|schema=", "override schema name", v => schemaName = v },
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

            if (extras.Count < 1 || extras.Count > 2 ||
                showHelp == true || mode == Mode.Unknown)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ -x input_bin [output_dir]", GetExecutableName());
                Console.WriteLine("       {0} [OPTIONS]+ -b input_dir [output_bin]", GetExecutableName());
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
                                     : Path.ChangeExtension(inputPath, null);

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
                        Console.WriteLine("Don't know how to handle '{0}'  with a hash of '{1}'.",
                                          schemaName,
                                          blob.ParserHash);
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

                    var assemblyPath = Path.Combine(GetExecutablePath(),
                                                    "serializers",
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

                    var resource = new Resource
                    {
                        Schema = schemaName,
                        ParserHash = blob.ParserHash,
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

                    var loadResource = typeof(BlobDataReader)
                        .GetMethod("LoadResource", BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(type);

                    Console.WriteLine("Loading entries...");

                    Func<int, string> getFileNameFromIndex =
                        i =>
                        {
                            if (i < 0 || i >= resource.Files.Count)
                            {
                                throw new KeyNotFoundException("file index " +
                                                               i.ToString(CultureInfo.InvariantCulture) +
                                                               " is out of range");
                            }

                            return resource.Files[i].Name;
                        };

                    var list = (IList)loadResource.Invoke(
                        null,
                        new object[] { input, schema.IsClient, schema.IsServer, getFileNameFromIndex });

                    var entries = list.Cast<object>();
                    var listType = typeof(List<>).MakeGenericType(type);

                    Console.WriteLine("Saving entries to XML...");
                    switch (schema.Mode.ToLowerInvariant())
                    {
                        case "single":
                        {
                            var serializer = new DataContractSerializer(listType);

                            const string entryName = "entries.xml";
                            resource.Entries.Add(entryName);

                            var entryPath = Path.Combine(outputPath, entryName);
                            if (File.Exists(entryPath) == true)
                            {
                                throw new InvalidOperationException();
                            }

                            var entryParentPath = Path.GetDirectoryName(entryPath);
                            if (string.IsNullOrEmpty(entryParentPath) == false)
                            {
                                Directory.CreateDirectory(entryParentPath);
                            }

                            using (var output = File.Create(entryPath))
                            {
                                var localList = (IList)Activator.CreateInstance(listType);
                                foreach (var entry in entries)
                                {
                                    localList.Add(entry);
                                }

                                var writer = XmlWriter.Create(output, xmlSettings);
                                serializer.WriteStartObject(writer, listType);
                                writer.WriteAttributeString("xmlns", "c", "", "http://datacontract.gib.me/cryptic");
                                //writer.WriteAttributeString("xmlns", "i", "", "http://www.w3.org/2001/XMLSchema-instance");
                                writer.WriteAttributeString("xmlns",
                                                            "a",
                                                            "",
                                                            "http://schemas.microsoft.com/2003/10/Serialization/Arrays");
                                //writer.WriteAttributeString("xmlns", "s", "", "http://datacontract.gib.me/startrekonline");
                                serializer.WriteObjectContent(writer, localList);
                                serializer.WriteEndObject(writer);
                                writer.Flush();
                            }

                            break;
                        }

                        case "file":
                        {
                            var serializer = new DataContractSerializer(listType);

                            var fileNameFieldName = "FileName";
                            if (string.IsNullOrEmpty(target.FileNameKey) == false)
                            {
                                fileNameFieldName = target.FileNameKey;
                            }

                            var fileNameField = type.GetField(fileNameFieldName,
                                                              BindingFlags.Public | BindingFlags.Instance);
                            if (fileNameField == null)
                            {
                                Console.WriteLine("Class '{0}' does not expose '{1}'!",
                                                  target.Class,
                                                  fileNameFieldName);
                                return;
                            }

                            var uniqueFileNames = entries
                                .Select(i => (string)fileNameField.GetValue(i))
                                .Distinct();
                            foreach (var fileName in uniqueFileNames)
                            {
                                var entryName = fileName;
                                entryName = entryName.Replace('/', '\\');
                                entryName = Path.ChangeExtension(entryName, ".xml");
                                resource.Entries.Add(entryName);

                                var entryPath = Path.Combine(outputPath, entryName);
                                if (File.Exists(entryPath) == true)
                                {
                                    throw new InvalidOperationException();
                                }

                                var entryParentPath = Path.GetDirectoryName(entryPath);
                                if (string.IsNullOrEmpty(entryParentPath) == false)
                                {
                                    Directory.CreateDirectory(entryParentPath);
                                }

                                using (var output = File.Create(entryPath))
                                {
                                    var localEntries = (IList)Activator.CreateInstance(listType);
                                    string name = fileName;
                                    foreach (var entry in entries
                                        .Where(e => (string)(fileNameField.GetValue(e)) == name))
                                    {
                                        localEntries.Add(entry);
                                    }

                                    var writer = XmlWriter.Create(output, xmlSettings);
                                    serializer.WriteStartObject(writer, listType);
                                    writer.WriteAttributeString("xmlns", "c", "", "http://datacontract.gib.me/cryptic");
                                    //writer.WriteAttributeString("xmlns", "i", "", "http://www.w3.org/2001/XMLSchema-instance");
                                    writer.WriteAttributeString("xmlns",
                                                                "a",
                                                                "",
                                                                "http://schemas.microsoft.com/2003/10/Serialization/Arrays");
                                    //writer.WriteAttributeString("xmlns", "s", "", "http://datacontract.gib.me/startrekonline");
                                    serializer.WriteObjectContent(writer, localEntries);
                                    serializer.WriteEndObject(writer);
                                    writer.Flush();
                                }
                            }

                            break;
                        }

                        case "name":
                        {
                            var serializer = new DataContractSerializer(type);

                            if (string.IsNullOrEmpty(target.Key) == true)
                            {
                                Console.WriteLine("No key set for '{0}'!",
                                                  schemaName);
                                return;
                            }

                            var keyField = type.GetField(target.Key,
                                                         BindingFlags.Public | BindingFlags.Instance);
                            if (keyField == null)
                            {
                                Console.WriteLine("Class '{0}' does not expose '{1}'!",
                                                  target.Class,
                                                  target.Key);
                                return;
                            }

                            foreach (var entry in entries)
                            {
                                var entryName = Path.ChangeExtension((string)keyField.GetValue(entry),
                                                                     ".xml");

                                resource.Entries.Add(entryName);

                                var entryPath = Path.Combine(outputPath, entryName);
                                if (File.Exists(entryPath) == true)
                                {
                                    throw new InvalidOperationException();
                                }

                                var entryParentPath = Path.GetDirectoryName(entryPath);
                                if (string.IsNullOrEmpty(entryParentPath) == false)
                                {
                                    Directory.CreateDirectory(entryParentPath);
                                }

                                using (var output = File.Create(entryPath))
                                {
                                    var writer = XmlWriter.Create(output, xmlSettings);
                                    serializer.WriteStartObject(writer, entry);
                                    writer.WriteAttributeString("xmlns", "c", "", "http://datacontract.gib.me/cryptic");
                                    //writer.WriteAttributeString("xmlns", "i", "", "http://www.w3.org/2001/XMLSchema-instance");
                                    writer.WriteAttributeString("xmlns",
                                                                "a",
                                                                "",
                                                                "http://schemas.microsoft.com/2003/10/Serialization/Arrays");
                                    //writer.WriteAttributeString("xmlns", "s", "", "http://datacontract.gib.me/startrekonline");
                                    serializer.WriteObjectContent(writer, entry);
                                    serializer.WriteEndObject(writer);
                                    writer.Flush();
                                }
                            }

                            break;
                        }

                        case "entry":
                        {
                            var serializer = new DataContractSerializer(type);

                            var fileNameFieldName = "FileName";
                            if (string.IsNullOrEmpty(target.FileNameKey) == false)
                            {
                                fileNameFieldName = target.FileNameKey;
                            }

                            var fileNameField = type.GetField(fileNameFieldName,
                                                              BindingFlags.Public | BindingFlags.Instance);
                            if (fileNameField == null)
                            {
                                Console.WriteLine("Class '{0}' does not expose '{1}'!",
                                                  target.Class,
                                                  fileNameFieldName);
                                return;
                            }

                            if (string.IsNullOrEmpty(target.Key) == true)
                            {
                                Console.WriteLine("No key set for '{0}'!",
                                                  schemaName);
                                return;
                            }

                            var keyField = type.GetField(target.Key,
                                                         BindingFlags.Public | BindingFlags.Instance);
                            if (keyField == null)
                            {
                                Console.WriteLine("Class '{0}' does not expose '{1}'!",
                                                  target.Class,
                                                  target.Key);
                                return;
                            }

                            foreach (var entry in entries)
                            {
                                var entryName = (string)fileNameField.GetValue(entry);
                                entryName = entryName.Replace('/', '\\');
                                entryName = Path.ChangeExtension(
                                    Path.Combine(entryName, (string)keyField.GetValue(entry)),
                                    ".xml");

                                resource.Entries.Add(entryName);

                                var entryPath = Path.Combine(outputPath, entryName);
                                if (File.Exists(entryPath) == true)
                                {
                                    throw new InvalidOperationException();
                                }

                                var entryParentPath = Path.GetDirectoryName(entryPath);
                                if (string.IsNullOrEmpty(entryParentPath) == false)
                                {
                                    Directory.CreateDirectory(entryParentPath);
                                }

                                using (var output = File.Create(entryPath))
                                {
                                    var writer = XmlWriter.Create(output, xmlSettings);
                                    serializer.WriteStartObject(writer, entry);
                                    writer.WriteAttributeString("xmlns", "c", "", "http://datacontract.gib.me/cryptic");
                                    //writer.WriteAttributeString("xmlns", "i", "", "http://www.w3.org/2001/XMLSchema-instance");
                                    writer.WriteAttributeString("xmlns",
                                                                "a",
                                                                "",
                                                                "http://schemas.microsoft.com/2003/10/Serialization/Arrays");
                                    //writer.WriteAttributeString("xmlns", "s", "", "http://datacontract.gib.me/startrekonline");
                                    serializer.WriteObjectContent(writer, entry);
                                    serializer.WriteEndObject(writer);
                                    writer.Flush();
                                }
                            }

                            break;
                        }

                        default:
                        {
                            throw new NotSupportedException();
                        }
                    }

                    Console.WriteLine("Saving index...");
                    using (var output = File.Create(Path.Combine(outputPath, "@resource.xml")))
                    {
                        var writer = XmlWriter.Create(output, xmlSettings);
                        var serializer = new XmlSerializer(typeof(Resource));
                        serializer.Serialize(writer, resource);
                        writer.Flush();
                    }
                }
            }
            else if (mode == Mode.Import)
            {
                var inputPath = extras[0];
                var outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, ".bin");

                Console.WriteLine("Loading index...");
                Resource resource;
                using (var input = File.OpenRead(Path.Combine(inputPath, "@resource.xml")))
                {
                    var reader = XmlReader.Create(input);
                    var serializer = new XmlSerializer(typeof(Resource));
                    resource = (Resource)serializer.Deserialize(reader);
                }

                var schema = config.GetSchema(resource.Schema);
                if (schema == null)
                {
                    Console.WriteLine("Don't know how to handle '{0}'!", resource.Schema);
                    return;
                }

                var target = schema.GetTarget(resource.ParserHash);
                if (target == null)
                {
                    Console.WriteLine("Don't know how to handle '{0}' with a hash of {1}.",
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

                var assemblyPath = Path.Combine(GetExecutablePath(),
                                                "serializers",
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

                var blob = new BlobFile
                {
                    ParserHash = resource.ParserHash,
                };

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

                var listType = typeof(List<>).MakeGenericType(type);
                var entries = (IList)Activator.CreateInstance(listType);

                Console.WriteLine("Loading entries from XML...");
                switch (schema.Mode.ToLowerInvariant())
                {
                    case "single":
                    case "file":
                    {
                        var serializer = new DataContractSerializer(listType);

                        foreach (var entryName in resource.Entries)
                        {
                            var entryPath = Path.IsPathRooted(entryName) == true
                                                ? entryName
                                                : Path.Combine(inputPath, entryName);

                            using (var input = File.OpenRead(entryPath))
                            {
                                var reader = XmlReader.Create(input);
                                var localEntries = (IList)serializer.ReadObject(reader);
                                foreach (var entry in localEntries)
                                {
                                    entries.Add(entry);
                                }
                            }
                        }

                        break;
                    }

                    case "name":
                    case "entry":
                    {
                        var serializer = new DataContractSerializer(type);

                        foreach (var entryName in resource.Entries)
                        {
                            var entryPath = Path.IsPathRooted(entryName) == true
                                                ? entryName
                                                : Path.Combine(inputPath, entryName);

                            using (var input = File.OpenRead(entryPath))
                            {
                                var reader = XmlReader.Create(input);
                                var entry = serializer.ReadObject(reader);
                                entries.Add(entry);
                            }
                        }

                        break;
                    }

                    default:
                    {
                        throw new NotSupportedException();
                    }
                }

                if (string.IsNullOrEmpty(target.Key) == false)
                {
                    var keyField = type.GetField(target.Key,
                                                 BindingFlags.Public | BindingFlags.Instance);
                    if (keyField == null)
                    {
                        Console.WriteLine("Class '{0}' does not expose '{1}'!",
                                          target.Class,
                                          target.Key);
                        return;
                    }

                    Console.WriteLine("Sorting entries...");
                    var sortedEntries = entries
                        .Cast<object>()
                        .OrderBy(keyField.GetValue)
                        .ToList();
                    entries.Clear();
                    foreach (var entry in sortedEntries)
                    {
                        entries.Add(entry);
                    }

                    Func<string, int> getIndexFromFileName = s => blob.Files.FindIndex(fe => fe.Name == s);

                    Console.WriteLine("Saving entries...");
                    var saveResource = typeof(BlobDataWriter)
                        .GetMethod("SaveResource", BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(type);
                    using (var output = File.Create(outputPath))
                    {
                        blob.Serialize(output);

                        saveResource.Invoke(
                            null,
                            new object[] { entries, output, schema.IsClient, schema.IsServer, getIndexFromFileName });
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
