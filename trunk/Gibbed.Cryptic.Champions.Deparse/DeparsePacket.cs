using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Gibbed.Helpers;

namespace Gibbed.Cryptic.Champions.Deparse
{
    internal partial class Program
    {
        private static void DeparsePacket(Parser parser, Stream input, Dictionary<string, Parser> parsers, List<KeyValuePair<string, object>> datums)
        {
            byte unk0 = input.ReadValueU8();

            if (unk0 != 0)
            {
                throw new Exception();
            }

            while (true)
            {
                byte hasValue = input.ReadValueU8();
                
                if (hasValue == 0)
                {
                    break;
                }
                else if (hasValue != 1)
                {
                    throw new Exception();
                }

                int index = input.ReadValuePackedS32();

                Parser.Member member = parser.Members.SingleOrDefault(candidate => candidate.Index == index);
                if (member == null)
                {
                    throw new Exception();
                }

                int count;

                if (member.Mode == Parser.Member.DataMode.Dynamic)
                {
                    byte unk2 = input.ReadValueU8();

                    if (unk2 != 0)
                    {
                        throw new Exception();
                    }

                    count = input.ReadValueS32();
                    byte unk3 = input.ReadValueU8();

                    if (count != 0)
                    {
                    }
                }
                else if (member.Mode == Parser.Member.DataMode.Value)
                {
                    count = 1;
                }
                else if (member.Mode == Parser.Member.DataMode.Static)
                {
                    throw new NotImplementedException();
                    count = member.ExplicitCount;
                }
                else
                {
                    throw new Exception();
                }

                if (member.Type == Parser.Member.DataType.Structure)
                {
                    Parser subparser = parsers[member.Reference];

                    for (int i = 0; i < count; i++)
                    {
                        byte unk1 = input.ReadValueU8();

                        if (unk1 == 0)
                        {
                        }
                        else if (unk1 == 1)
                        {
                            Structure structure = new Structure();
                            structure.Type = subparser.Name;
                            structure.Mode = subparser.Mode;
                            DeparsePacket(subparser, input, parsers, structure.Datums);

                            if (structure.Datums.Count > 0)
                            {
                                datums.Add(new KeyValuePair<string, object>(member.Name, structure));
                            }
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                }
                else if (member.Type == Parser.Member.DataType.Polymorph)
                {
                    for (int i = 0; i < count; i++)
                    {
                        bool hasPolymorph = input.ReadValueBit();
                        if (hasPolymorph == false)
                        {
                            continue;
                        }

                        bool unk1 = input.ReadValueBit();

                        if (unk1 == false)
                        {
                            throw new Exception();
                        }

                        string value = input.ReadValuePackedS32().ToString();

                        if (member.PossibleNames.ContainsKey(value))
                        {
                            value = member.PossibleNames[value];
                        }
                        else
                        {
                            throw new InvalidOperationException("missing polymorph value " + value);
                        }

                        Parser subparser = parsers[value];

                        Structure structure = new Structure();
                        structure.Type = subparser.Name;
                        structure.Mode = subparser.Mode;
                        DeparsePacket(subparser, input, parsers, structure.Datums);

                        if (structure.Datums.Count > 0)
                        {
                            datums.Add(new KeyValuePair<string, object>(member.Name, structure));
                        }
                    }
                }
                else if (member.Type == Parser.Member.DataType.Multival)
                {
                    List<string> values = new List<string>();

                    for (int i = 0; i < count; i++)
                    {
                        int instruction = input.ReadValueS32();

                        if (instruction == -1 || instruction == 0xFFFF)
                        {
                            throw new InvalidOperationException("invalid instruction");
                        }

                        MultivalOp op = (MultivalOp)Enum.ToObject(typeof(MultivalOp), instruction & ~0xFF);
                        MultivalOp type = (MultivalOp)Enum.ToObject(typeof(MultivalOp), instruction & 0xFF);

                        if (type == MultivalOp.NON && op != MultivalOp.NON)
                        {
                            values.Add(op.ToString());
                        }
                        else if (type == MultivalOp.WTF)
                        {
                            int wtf = input.ReadValuePackedS32();
                            values.Add(op.ToString() + " " + wtf.ToString());
                        }
                        else if (type == MultivalOp.INT)
                        {
                            int lo = input.ReadValuePackedS32();
                            int hi = input.ReadValuePackedS32();

                            long value = ((long)hi << 32) | (long)lo;
                            values.Add(op.ToString() + " " + value.ToString());
                        }
                        else if (type == MultivalOp.FLT)
                        {
                            int lo = input.ReadValuePackedS32();
                            int hi = input.ReadValuePackedS32();

                            // (hi << 32) | lo
                            double value = BitConverter.Int64BitsToDouble(((long)hi << 32) | (long)lo);
                            values.Add(op.ToString() + " " + value.ToString());
                        }
                        else if (type == MultivalOp.STR)
                        {
                            string value = input.ReadStringASCIIZ();
                            values.Add(op.ToString() + " " + value.ToString());
                        }
                        else
                        {
                            throw new Exception("unhandled type " + type.ToString());
                        }
                    }

                    if (values.Count > 0)
                    {
                        datums.Add(new KeyValuePair<string, object>(member.Name, String.Join(", ", values.ToArray())));
                    }
                }
                else
                {
                    List<string> values = new List<string>();
                    for (int i = 0; i < count; i++)
                    {
                        string value = null;

                        if (member.Type == Parser.Member.DataType.String ||
                            member.Type == Parser.Member.DataType.CurrentFile ||
                            member.Type == Parser.Member.DataType.Reference)
                        {
                            value = input.ReadStringASCIIZ();

                            if (value.Contains("\r") || value.Contains("\n") || value.Contains("\t"))
                            {
                                value = "\"" + value.Replace("\"", "\\\"") + "\"";
                            }
                        }
                        else if (member.Type == Parser.Member.DataType.U8)
                        {
                            value = input.ReadValuePackedS32().ToString();
                        }
                        else if (member.Type == Parser.Member.DataType.Bit)
                        {
                            value = input.ReadValueU8().ToString();
                        }
                        else if (member.Type == Parser.Member.DataType.S32)
                        {
                            value = input.ReadValuePackedS32().ToString();

                            if (member.PossibleNames.ContainsKey(value))
                            {
                                value = member.PossibleNames[value]; // += " # --> " + member.PossibleNames[value];
                            }
                        }
                        else if (member.Type == Parser.Member.DataType.Flags)
                        {
                            int flags = input.ReadValuePackedS32();

                            if (flags == 0)
                            {
                                value = flags.ToString();
                            }
                            else
                            {
                                List<string> names = new List<string>();

                                for (int bit = 0; bit < 32; bit++)
                                {
                                    int flag = 1 << bit;
                                    if ((flags & flag) == flag)
                                    {
                                        if (member.PossibleNames.ContainsKey(flag.ToString()))
                                        {
                                            names.Add(member.PossibleNames[flag.ToString()]);
                                        }
                                    }
                                }

                                value = String.Join(" | ", names.ToArray());
                            }
                        }
                        else if (member.Type == Parser.Member.DataType.F32)
                        {
                            value = input.ReadValueF32().ToString();
                        }
                        else
                        {
                            throw new Exception(member.Type.ToString() + " unhandled");
                        }

                        if (value != null && value != "")
                        {
                            values.Add(value);
                        }
                    }

                    if (values.Count > 0)
                    {
                        datums.Add(new KeyValuePair<string, object>(member.Name, String.Join(", ", values.ToArray())));
                    }
                }
            }
        }
    }
}
