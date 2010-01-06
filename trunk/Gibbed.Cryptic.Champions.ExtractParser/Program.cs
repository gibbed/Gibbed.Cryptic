using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Gibbed.Helpers;
using Gibbed.PortableExecutable;
using System.Xml;

namespace ExtractParser
{
    internal class Program
    {
        private static void GetTypeOffsets(Executable exe, Stream input, List<uint> types)
        {
            List<ParserField> fields = new List<ParserField>();

            while (true)
            {
                ParserField field = input.ReadStructure<ParserField>();

                if (field.Flags == 0 || field.NameOffset == 0) // || exe.ReadString(input, field.NameOffset).Length == 0)
                {
                    break;
                }

                if (field.Serialized == false)
                {
                    continue;
                }

                fields.Add(field);
            }

            foreach (ParserField field in fields)
            {
                if (field.DataOffset != 0 &&
                    field.Type == ParserToken.Structure &&
                    types.Contains(field.DataOffset) == false)
                {
                    types.Add(field.DataOffset);
                    input.Seek(exe.GetFileOffset(field.DataOffset), SeekOrigin.Begin);
                    GetTypeOffsets(exe, input, types);
                }
            }
        }

        private static void ParseType(Executable exe, Stream input)
        {
            List<ParserField> fields = new List<ParserField>();
            Dictionary<ParserField, long> offsets = new Dictionary<ParserField, long>();

            while (true)
            {
                long offset = input.Position;
                ParserField field = input.ReadStructure<ParserField>();

                //if (field.Flags == 0 || field.NameOffset == 0) // || exe.ReadString(input, field.NameOffset).Length == 0)
                if (field.Flags == 0 && (field.NameOffset == 0 || exe.ReadString(input, field.NameOffset).Length == 0))
                {
                    break;
                }

                fields.Add(field);
                offsets.Add(field, offset);
            }

            if (fields.Count == 0)
            {
                throw new Exception();
            }
            
            string name = exe.ReadString(input, fields[0].NameOffset);

            if (fields[0].Type != ParserToken.Ignore)
            {
                throw new Exception();
            }

            string mode;

            // fields.RemoveAt(0);

            if (fields.Count >= 3 &&
                fields[1].Type == ParserToken.Start &&
                exe.ReadString(input, fields[1].NameOffset) == "{" &&
                fields[fields.Count - 1].Type == ParserToken.End &&
                exe.ReadString(input, fields[fields.Count - 1].NameOffset) == "}")
            {
                mode = "Block";
                // fields.RemoveAt(fields.Count - 1);
                // fields.RemoveAt(0);
                
            }
            else if (fields.Count >= 2 &&
                fields[fields.Count - 1].Type == ParserToken.End &&
                exe.ReadString(input, fields[fields.Count - 1].NameOffset) == "\n")
            {
                mode = "Line";
                // fields.RemoveAt(fields.Count - 1);
            }
            else
            {
                throw new Exception();
            }

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            XmlWriter writer = XmlWriter.Create(File.Open("parsers\\parser_" + name + ".xml", FileMode.Create, FileAccess.Write, FileShare.ReadWrite), settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("structure");
            writer.WriteAttributeString("name", name);
            writer.WriteAttributeString("mode", mode);

            int index = 0;
            foreach (ParserField field in fields)
            {
                int fieldIndex = index;
                index++;

                long offset = offsets[field];

                string fieldName = exe.ReadString(input, field.NameOffset);
                byte fieldMode = (byte)((field.Flags >> 18) & 3);

                if (field.Serialized == false)
                {
                    continue;
                }

                Console.Write("{0:X16} {1} {2} {3}  ", field.Flags, fieldMode, field.Type.ToString().PadRight(14), fieldName.PadRight(24));
                Console.WriteLine("{0:X4} {1:X4} [{2:X8}] {3:X8} [{4:X8}] {5:X8} {6:X8} {7:X8}",
                    field.field_4,
                    field.field_6,
                    field.StructureOffset,
                    field.ExplicitCount,
                    field.DataOffset,
                    field.field_1C,
                    field.field_20,
                    field.field_24);

                if (field.Type == ParserToken.Ignore || field.Type == ParserToken.Start || field.Type == ParserToken.End)
                {
                    continue;
                }

                string line = "";

                writer.WriteStartElement("member");
                writer.WriteAttributeString("name", fieldName);
                writer.WriteAttributeString("index", fieldIndex.ToString());
                writer.WriteAttributeString("type", field.Type.ToString());
                
                if (fieldMode == 0)
                {
                    writer.WriteAttributeString("mode", "Value");
                }
                else if (fieldMode == 1)
                {
                    writer.WriteAttributeString("mode", "Dynamic");
                }
                else if (fieldMode == 2)
                {
                    writer.WriteAttributeString("mode", "Static");
                    writer.WriteAttributeString("count", field.ExplicitCount.ToString());
                }

                if (field.Type == ParserToken.Structure)
                {
                    if (field.DataOffset == 0)
                    {
                        writer.WriteAttributeString("reference", "*** LATE BINDER : " + offset.ToString("X8") + " ***");
                    }
                    else
                    {
                        writer.WriteAttributeString("reference", exe.ReadStringX(input, field.DataOffset));
                    }
                }
                else if (field.Type == ParserToken.Reference)
                {
                    writer.WriteAttributeString("reference", exe.ReadString(input, field.DataOffset));
                }
                else if (field.Type == ParserToken.Polymorph)
                {
                    writer.WriteAttributeString("names", field.DataOffset.ToString("X8"));
                    writer.WriteComment("*** POLYMORPH? : " + field.DataOffset.ToString("X8") + "***");
                }
                else
                {
                    if (field.DataOffset != 0 && field.Type != ParserToken.String)
                    {
                        input.Seek(exe.GetFileOffset(field.DataOffset), SeekOrigin.Begin);

                        writer.WriteAttributeString("names", field.DataOffset.ToString("X8"));

                        XmlWriter subwriter = XmlWriter.Create(File.Open("parsers\\names_" + field.DataOffset.ToString("X8") + ".xml", FileMode.Create, FileAccess.Write, FileShare.ReadWrite), settings);
                        subwriter.WriteStartDocument();
                        subwriter.WriteStartElement("values");
                        subwriter.WriteAttributeString("name", field.DataOffset.ToString("X8"));

                        while (true)
                        {
                            UInt32 unk1 = input.ReadValueU32();

                            if (unk1 == 0)
                            {
                                break;
                            }

                            object unk2;

                            if (unk1 == 1 || unk1 == 2 || unk1 == 3)
                            {
                                unk2 = input.ReadValueU32();
                                if ((uint)unk2 != 0)
                                {
                                    subwriter.WriteComment("unk1 == " + unk1.ToString() + " = " + ((uint)unk2).ToString("X8"));
                                }
                                continue;
                            }
                            else if (unk1 == 5)
                            {
                                throw new Exception();
                            }

                            if (field.Type == ParserToken.S32 || field.Type == ParserToken.Flags)
                            {
                                unk2 = input.ReadValueS32();
                            }
                            else
                            {
                                throw new Exception();
                            }

                            subwriter.WriteStartElement("value");
                            subwriter.WriteAttributeString("name", exe.ReadString(input, unk1));
                            subwriter.WriteValue(unk2.ToString());
                            subwriter.WriteEndElement();
                        }

                        subwriter.WriteEndElement();
                        subwriter.WriteEndDocument();
                        subwriter.Flush();
                        subwriter.Close();
                    }
                }
                
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();
        }

        private static void AddBaseType(UInt32 virtualOffset, Executable exe, Stream input, List<uint> types)
        {
            types.Add(virtualOffset);
            input.Seek(exe.GetFileOffset(virtualOffset), SeekOrigin.Begin);
            GetTypeOffsets(exe, input, types);
        }

        public static void Main(string[] args)
        {
            //Stream input = File.OpenRead("..\\..\\..\\private\\FC.9.20090826.14\\GameClient.exe");
            Stream input = File.OpenRead("..\\..\\..\\private\\FC.9.20090905.3\\GameClient.exe");
            Executable exe = new Executable();
            exe.Read(input);

            List<uint> types = new List<uint>();

            //AddBaseType(0x01600F88, exe, input, types);
            //AddBaseType(0x0160C408, exe, input, types);
            //AddBaseType(0x016BE3D0, exe, input, types);
            //AddBaseType(0x01604798, exe, input, types);
            //AddBaseType(0x015F5990, exe, input, types);
            //AddBaseType(0x015F5A58, exe, input, types);
            //AddBaseType(0x01600E98, exe, input, types);
            //AddBaseType(0x01600DA8, exe, input, types);
            /*
            AddBaseType(0x1614BA0, exe, input, types);
            AddBaseType(0x1614C90, exe, input, types);
            AddBaseType(0x1614E08, exe, input, types);
            AddBaseType(0x1614F48, exe, input, types);
            AddBaseType(0x1615098, exe, input, types);
            AddBaseType(0x1615188, exe, input, types);
            AddBaseType(0x16152C8, exe, input, types);
            AddBaseType(0x16153E0, exe, input, types);
            AddBaseType(0x1615520, exe, input, types);
            AddBaseType(0x1615610, exe, input, types);
            AddBaseType(0x1615738, exe, input, types);
            AddBaseType(0x1615940, exe, input, types);
            AddBaseType(0x1615AF8, exe, input, types);
            AddBaseType(0x1615E18, exe, input, types);
            AddBaseType(0x1615F08, exe, input, types);
            AddBaseType(0x1616010, exe, input, types);
            AddBaseType(0x1616100, exe, input, types);
            AddBaseType(0x16161F0, exe, input, types);
            AddBaseType(0x1616308, exe, input, types);
            AddBaseType(0x1616470, exe, input, types);
            AddBaseType(0x1616580, exe, input, types);
            AddBaseType(0x16166E8, exe, input, types);
            AddBaseType(0x1616828, exe, input, types);
            AddBaseType(0x1616918, exe, input, types);
            AddBaseType(0x1616F38, exe, input, types);
            AddBaseType(0x1616AD0, exe, input, types);
            AddBaseType(0x1616BC0, exe, input, types);
            AddBaseType(0x1616D30, exe, input, types);
            AddBaseType(0x1616E48, exe, input, types);
            AddBaseType(0x1617078, exe, input, types);
            AddBaseType(0x1617168, exe, input, types);
            AddBaseType(0x16172A8, exe, input, types);
            AddBaseType(0x1617460, exe, input, types);
            AddBaseType(0x1617550, exe, input, types);
            AddBaseType(0x1617668, exe, input, types);
            AddBaseType(0x1617848, exe, input, types);
            */
            AddBaseType(0x15FA518, exe, input, types);
            foreach (uint type in types)
            {
                input.Seek(exe.GetFileOffset(type), SeekOrigin.Begin);
                ParseType(exe, input);
            }
        }
    }
}
