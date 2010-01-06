using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Xml;
using Gibbed.Cryptic.FileFormats;

namespace Gibbed.Cryptic.Champions.Deparse
{
    internal partial class Program
    {
        private static void WriteTextStructure(TextWriter writer, Structure structure, int level)
        {
            string padding = "".PadLeft(level, '\t');

            if (structure.Mode == StructureMode.Block)
            {
                writer.WriteLine("{0}{{", padding);

                foreach (KeyValuePair<string, object> datum in structure.Datums)
                {
                    if (datum.Value is Structure)
                    {
                        Structure substructure = (Structure)datum.Value;

                        if (substructure.Mode == StructureMode.Block)
                        {
                            writer.WriteLine("{0}\t{1}", padding, datum.Key);
                            WriteTextStructure(writer, substructure, level + 1);
                        }
                        else if (substructure.Mode == StructureMode.Line)
                        {
                            List<string> values = new List<string>();

                            foreach (KeyValuePair<string, object> subdatum in substructure.Datums)
                            {
                                if (subdatum.Value is string)
                                {
                                    values.Add((string)subdatum.Value);
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }

                            writer.WriteLine("{0}\t{1} {2}", padding, datum.Key, String.Join(" ", values.ToArray()));
                        }
                    }
                    else if (datum.Value is string)
                    {
                        writer.WriteLine("{0}\t{1} {2}", padding, datum.Key, datum.Value);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }

                writer.WriteLine("{0}}}", padding);
            }
        }

        private static void WriteXmlStructure(XmlWriter writer, Structure structure, int level)
        {
            writer.WriteStartElement("structure");
            writer.WriteAttributeString("type", structure.Type);

            foreach (KeyValuePair<string, object> datum in structure.Datums)
            {
                if (datum.Value is Structure)
                {
                    Structure substructure = (Structure)datum.Value;
                    writer.WriteStartElement("value");
                    writer.WriteAttributeString("name", datum.Key);
                    WriteXmlStructure(writer, substructure, level + 1);
                    writer.WriteEndElement();
                }
                else if (datum.Value is string)
                {
                    writer.WriteStartElement("value");
                    writer.WriteAttributeString("name", datum.Key);
                    writer.WriteValue(datum.Value);
                    writer.WriteEndElement();
                }
                else
                {
                    throw new Exception();
                }
            }

            writer.WriteEndElement();
        }

        public static void Main(string[] args)
        {
            Dictionary<string, Parser> parsers = new Dictionary<string, Parser>();
            LoadParsers(parsers);
            
            foreach (string packetPath in Directory.GetFiles("T:\\Games\\MMO\\Cryptic Studios\\Champions Online\\Live\\packets", "*.bin"))
            {
                Stream input = File.OpenRead(packetPath);
                Parser parser = parsers["PowerDef"];
                Structure root = new Structure();
                root.Type = "PowerDef";
                root.Mode = StructureMode.Block;
                DeparsePacket(parser, input, parsers, root.Datums);
                input.Close();

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;

                string xmlPath;

                object nameDatum = root.Datums.SingleOrDefault(candidate => candidate.Key == "Name").Value;
                if (nameDatum != null)
                {
                    xmlPath = Path.Combine(Path.GetDirectoryName(packetPath), (string)nameDatum + ".xml");
                }
                else
                {
                    xmlPath = Path.ChangeExtension(packetPath, "xml");
                }
                   
                XmlWriter writer = XmlWriter.Create(xmlPath, settings);
                writer.WriteStartDocument();
                WriteXmlStructure(writer, root, 0);
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
            }

            /*
            Stream input = File.OpenRead("extracted\\bin\\InventoryBags.bin");
            //Stream input = File.OpenRead("extracted\\bin\\PowerTables.bin");
            //Stream input = File.OpenRead("extracted\\bin\\PowerStats.bin");
            //Stream input = File.OpenRead("extracted\\bin\\AttribSets.bin");
            //Stream input = File.OpenRead("extracted\\bin\\Attributes.bin");
            //Stream input = File.OpenRead("extracted\\bin\\DamageNames.bin");

            ParseFile parse = new ParseFile();
            parse.Deserialize(input);
            input.Close();

            Parser parser = parsers.SingleOrDefault(candidate => candidate.Value.Id == parse.Type).Value;
            if (parser == null)
            {
                Console.WriteLine("Don't know how to deparse this file ({0}).", parse.Type);
                return;
            }

            int counter = 0;
            while (parse.Data.Position < parse.Data.Length)
            {
                Structure root = new Structure();
                root.Mode = StructureMode.Block;
                string filename = DeparseFile(parser, parse.Data, parsers, root.Datums);

                if (filename == null)
                {
                    filename = String.Format("deparsed\\{0:D5}.txt", counter);
                }
                else
                {
                    filename = Path.Combine("deparsed", filename.Replace("/", "\\"));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(filename));

                TextWriter writer = new StreamWriter(File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
                WriteTextStructure(writer, root, 0);
                writer.Close();

                counter++;
            }
            */
        }
    }
}
