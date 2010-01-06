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
        private static string DeparseFile(Parser parser, Stream input, Dictionary<string, Parser> parsers, List<KeyValuePair<string, object>> datums)
        {
            string filename = null;

            foreach (Parser.Member member in parser.Members)
            {
                int count;

                if (member.Mode == Parser.Member.DataMode.Dynamic || member.Type == Parser.Member.DataType.Structure)
                {
                    count = input.ReadValueS32();
                }
                else if (member.Mode == Parser.Member.DataMode.Value)
                {
                    count = 1;
                }
                else if (member.Mode == Parser.Member.DataMode.Static)
                {
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
                        int length = input.ReadValueS32();
                        if (length == 0)
                        {
                            continue;
                        }
                        
                        MemoryStream memory = input.ReadToMemoryStream(length);
                        Structure structure = new Structure();
                        structure.Type = subparser.Name;
                        structure.Mode = subparser.Mode;
                        DeparseFile(subparser, memory, parsers, structure.Datums);
                        datums.Add(new KeyValuePair<string, object>(member.Name, structure));
                    }
                }
                else if (member.Type == Parser.Member.DataType.Multival)
                {
                    for (int i = 0; i < count; i++)
                    {
                        string opn = input.ReadStringASCII(4, true);
                        MultivalOp op = (MultivalOp)Enum.Parse(typeof(MultivalOp), opn);

                        if (
                            op == MultivalOp.O_P ||
                            op == MultivalOp.C_P ||
                            op == MultivalOp.SUB ||
                            op == MultivalOp.MUL ||
                            op == MultivalOp.ADD ||
                            op == MultivalOp.LES ||
                            op == MultivalOp.RET)
                        {
                        }
                        else if (
                            op == MultivalOp.FUN ||
                            op == MultivalOp.STR
                            )
                        {
                            string value = input.ReadStringPascal16();
                        }
                        else if (
                            op == MultivalOp.INT ||
                            op == MultivalOp.JZ_
                            )
                        {
                            long value = input.ReadValueS64();
                        }
                        else if (
                            op == MultivalOp.FLT
                            )
                        {
                            double value = input.ReadValueF64();
                        }
                        else
                        {
                            throw new Exception("unhandled op " + op.ToString());
                        }
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
                            value = input.ReadStringPascal16();

                            if (member.Type == Parser.Member.DataType.CurrentFile)
                            {
                                filename = value;
                            }

                            if (value.Contains("\r") || value.Contains("\n") || value.Contains("\t"))
                            {
                                value = "\"" + value.Replace("\"", "\\\"") + "\"";
                            }
                        }
                        else if (member.Type == Parser.Member.DataType.U8)
                        {
                            value = input.ReadValueU8().ToString();
                        }
                        else if (member.Type == Parser.Member.DataType.Bit)
                        {
                            value = input.ReadValueS32().ToString();
                        }
                        else if (member.Type == Parser.Member.DataType.S32)
                        {
                            value = input.ReadValueS32().ToString();

                            if (member.PossibleNames.ContainsKey(value))
                            {
                                value = member.PossibleNames[value]; // += " # --> " + member.PossibleNames[value];
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

            return filename;
        }
    }
}
