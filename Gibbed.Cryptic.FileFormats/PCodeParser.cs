using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gibbed.Cryptic.FileFormats
{
    public static class PCodeParser
    {
        private static string Escape(string input)
        {
            var sb = new StringBuilder();
            foreach (char t in input)
            {
                switch (t)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    default: sb.Append(t); break;
                }
            }
            return sb.ToString();
        }

        private static readonly Dictionary<MultiValueOpcode, string> _SimpleStatements = new Dictionary<MultiValueOpcode, string>()
        {
            {MultiValueOpcode.ADD, "add"},
            {MultiValueOpcode.SUB, "sub"},
            {MultiValueOpcode.NEG, "neg"},
            {MultiValueOpcode.MUL, "mul"},
            {MultiValueOpcode.DIV, "div"},
            {MultiValueOpcode.EXP, "exp"},
            {MultiValueOpcode.BAN, "binary and"},
            {MultiValueOpcode.BOR, "binary or"},
            {MultiValueOpcode.BNT, "binary not"},
            {MultiValueOpcode.BXR, "binary xor"},
            {MultiValueOpcode.O_P, "("},
            {MultiValueOpcode.C_P, ")"},
            {MultiValueOpcode.O_B, "["},
            {MultiValueOpcode.C_B, "]"},
            {MultiValueOpcode.EQU, "equals"},
            {MultiValueOpcode.LES, "less"},
            {MultiValueOpcode.NGR, "notgreater"},
            {MultiValueOpcode.GRE, "greater"},
            {MultiValueOpcode.NLE, "notless"},
            //FUN
            //IDS
            //S_V
            //{MultiValueOpcode.COM, ","},
            {MultiValueOpcode.AND, "and"},
            {MultiValueOpcode.ORR, "or"},
            {MultiValueOpcode.NOT, "not"},
            //IF_
            //ELS
            //ELF
            //EIF
            {MultiValueOpcode.RET, "return"},
            {MultiValueOpcode.RZ_, "retifzero"},
            //J__
            //JZ_
            //{MultiValueOpcode.CON, "continuation"},
            {MultiValueOpcode.STM, ";"},
            //RP_
            //OBJ
            //L_M
            //L_S
        };

        public static CDataWrapper ToStringValue(List<MultiValue> list)
        {
            var sb = new StringBuilder();

            var labels = new string[list.Count];
            foreach (MultiValue t in list)
            {
                if (t.Op == MultiValueOpcode.J__ ||
                    t.Op == MultiValueOpcode.JZ_)
                {
                    var arg = (long)t.Arg;
                    if (arg < 0 || arg > list.Count)
                    {
                        throw new InvalidOperationException();
                    }
                    var argi = (int)arg;
                    if (labels[argi] == null)
                    {
                        labels[argi] = string.Format("label_{0}", argi);
                    }
                }
            }

            sb.AppendLine();
            bool labeled = false;
            int level = 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (labels[i] != null)
                {
                    labeled = true;
                    sb.AppendLine(labels[i] + ":");
                }
                
                var item = list[i];

                if (item.Op == MultiValueOpcode.C_P)
                {
                    level--;
                }

                if (level > 0)
                {
                    sb.Append("".PadLeft(2 * (level + (labeled == true ? 1 : 0))));
                }

                if (item.Op == MultiValueOpcode.O_P)
                {
                    level++;
                }

                if (_SimpleStatements.ContainsKey(item.Op) == true)
                {
                    sb.AppendFormat("{0}", _SimpleStatements[item.Op]);
                }
                else
                {
                    switch (item.Op)
                    {
                        case MultiValueOpcode.INT:
                        {
                            sb.AppendFormat("int {0}",
                                (long)item.Arg);
                            break;
                        }

                        case MultiValueOpcode.FLT:
                        {
                            sb.AppendFormat("float {0}",
                                (double)item.Arg);
                            break;
                        }

                        case MultiValueOpcode.STR:
                        {
                            sb.AppendFormat("str \"{0}\"",
                                Escape((string)item.Arg));
                            break;
                        }

                        case MultiValueOpcode.OBJ:
                        {
                            sb.AppendFormat("objpath \"{0}\"",
                                Escape((string)item.Arg));
                            break;
                        }

                        case MultiValueOpcode.RP_:
                        {
                            sb.AppendFormat("rootpath \"{0}\"",
                                Escape((string)item.Arg));
                            break;
                        }

                        case MultiValueOpcode.IDS:
                        {
                            sb.AppendFormat("ident \"{0}\"",
                                Escape((string)item.Arg));
                            break;
                        }

                        case MultiValueOpcode.FUN:
                        {
                            sb.AppendFormat("call \"{0}\"",
                                Escape((string)item.Arg));
                            break;
                        }

                        case MultiValueOpcode.S_V:
                        {
                            StaticVariableType arg;

                            if (item.Arg is uint)
                            {
                                arg = (StaticVariableType)((uint)item.Arg);
                            }
                            else
                            {
                                arg = (StaticVariableType)item.Arg;
                            }

                            sb.AppendFormat("static {0}",
                                arg);
                            break;
                        }

                        case MultiValueOpcode.J__:
                        {
                            sb.AppendFormat("j {0}",
                                labels[(long)item.Arg]);
                            break;
                        }

                        case MultiValueOpcode.JZ_:
                        {
                            sb.AppendFormat("jz {0}",
                                labels[(long)item.Arg]);
                            break;
                        }

                        default: throw new NotSupportedException();
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static readonly Dictionary<string, MultiValueOpcode> _SimpleOps = new Dictionary<string, MultiValueOpcode>()
        {
            {"OP_ADD", MultiValueOpcode.ADD},
            {"OP_SUB", MultiValueOpcode.SUB},
            {"OP_NEG", MultiValueOpcode.NEG},
            {"OP_MUL", MultiValueOpcode.MUL},
            {"OP_DIV", MultiValueOpcode.DIV},
            {"OP_EXP", MultiValueOpcode.EXP},

            {"OP_BAN", MultiValueOpcode.BAN},
            {"OP_BOR", MultiValueOpcode.BOR},
            {"OP_BNT", MultiValueOpcode.BNT},
            {"OP_BXR", MultiValueOpcode.BXR},
            {"OP_O_P", MultiValueOpcode.O_P},
            {"OP_C_P", MultiValueOpcode.C_P},
            {"OP_O_B", MultiValueOpcode.O_B},
            {"OP_C_B", MultiValueOpcode.C_B},
            {"OP_EQU", MultiValueOpcode.EQU},
            {"OP_LES", MultiValueOpcode.LES},
            {"OP_NGR", MultiValueOpcode.NGR},
            {"OP_GRE", MultiValueOpcode.GRE},
            {"OP_NLE", MultiValueOpcode.NLE},

            {"OP_AND", MultiValueOpcode.AND},
            {"OP_ORR", MultiValueOpcode.ORR},
            {"OP_NOT", MultiValueOpcode.NOT},
            
            {"OP_RET", MultiValueOpcode.RET},
            {"OP_RZ_", MultiValueOpcode.RZ_},

            {"OP_STM", MultiValueOpcode.STM},
        };

        public static List<MultiValue> FromStringValue(CDataWrapper value)
        {
            var parser = new Irony.Parsing.Parser(new Grammars.PCodeGrammar());
            var tree = parser.Parse(value);
            if (tree.Status != Irony.Parsing.ParseTreeStatus.Parsed)
            {
                throw new FormatException();
            }

            var list = new List<MultiValue>();
            var labels = new Dictionary<string, int>();

            foreach (var line in tree.Root.ChildNodes)
            {
                var label = line.ChildNodes.SingleOrDefault(c => c.Term.Name == "LABEL");
                if (label != null)
                {
                    labels.Add(label.ChildNodes[0].Token.ValueString, list.Count);
                }

                var statement = line.ChildNodes.SingleOrDefault(c => c.Term.Name == "STATEMENT");
                if (statement != null)
                {
                    var code = statement.ChildNodes[0];

                    MultiValueOpcode op;
                    object arg = null;

                    if (_SimpleOps.ContainsKey(code.Term.Name) == true)
                    {
                        op = _SimpleOps[code.Term.Name];
                    }
                    else
                    {
                        switch (code.Term.Name)
                        {
                            case "OP_J__":
                            {
                                op = MultiValueOpcode.J__;
                                arg = code.ChildNodes[1].Token.ValueString;
                                break;
                            }

                            case "OP_JZ_":
                            {
                                op = MultiValueOpcode.JZ_;
                                arg = code.ChildNodes[1].Token.ValueString;
                                break;
                            }

                            case "OP_S_V":
                            {
                                op = MultiValueOpcode.S_V;
                                StaticVariableType sv;
                                if (MultiValue.TryParseStaticVariable(code.ChildNodes[1].Token.Text, out sv) == false)
                                {
                                    throw new FormatException();
                                }
                                arg = sv;
                                break;
                            }

                            case "OP_STR":
                            {
                                op = MultiValueOpcode.STR;
                                arg = code.ChildNodes[1].Token.ValueString;
                                break;
                            }

                            case "OP_INT":
                            {
                                op = MultiValueOpcode.INT;
                                arg = code.ChildNodes[1].Token.Value;
                                break;
                            }

                            case "OP_FLT":
                            {
                                op = MultiValueOpcode.FLT;
                                arg = code.ChildNodes[1].Token.Value;
                                break;
                            }

                            case "OP_OBJ":
                            {
                                op = MultiValueOpcode.OBJ;
                                arg = code.ChildNodes[1].Token.ValueString;
                                break;
                            }

                            case "OP_RP_":
                            {
                                op = MultiValueOpcode.RP_;
                                arg = code.ChildNodes[1].Token.ValueString;
                                break;
                            }


                            case "OP_IDS":
                            {
                                op = MultiValueOpcode.IDS;
                                arg = code.ChildNodes[1].Token.ValueString;
                                break;
                            }

                            case "OP_FUN":
                            {
                                op = MultiValueOpcode.FUN;
                                arg = code.ChildNodes[1].Token.ValueString;
                                break;
                            }

                            default: throw new NotImplementedException();
                        }
                    }

                    list.Add(new MultiValue()
                        {
                            Op = op,
                            Arg = arg,
                        });
                }
            }

            foreach (var mv in list.Where(
                item =>
                    item.Op == MultiValueOpcode.J__ ||
                    item.Op == MultiValueOpcode.JZ_))
            {
                var label = (string)mv.Arg;
                if (labels.ContainsKey(label) == false)
                {
                    throw new InvalidOperationException();
                }
                mv.Arg = (long)labels[label];
            }

            return list;
        }
    }
}
