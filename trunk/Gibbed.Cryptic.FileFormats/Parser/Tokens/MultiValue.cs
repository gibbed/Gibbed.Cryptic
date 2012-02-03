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
using System.Text;
using System.IO;
using System.Xml;
using Gibbed.IO;

namespace Gibbed.Cryptic.FileFormats.Parser.Tokens
{
    internal class MultiValue : BasicValueToken
    {
        public override StorageCompatability Storage
        {
            get
            {
                return
                    StorageCompatability.DirectValue |
                    StorageCompatability.DirectFixedArray |
                    StorageCompatability.DirectArray |
                    StorageCompatability.IndirectArray;
            }
        }

        public override string NameDirectValue { get { return "MULTIVAL"; } }
        public override string NameDirectFixedArray { get { return "MULTIARRAY"; } }
        public override string NameIndirectArray { get { return "MULTIEARRAY"; } }

        private static Dictionary<string, Opcode> NamesToOpcodes;
        static MultiValue()
        {
            NamesToOpcodes = new Dictionary<string, Opcode>();
            foreach (var name in Enum.GetNames(typeof(Opcode)))
            {
                NamesToOpcodes.Add(name, (Opcode)Enum.Parse(typeof(Opcode), name));
            }
        }

        public override void Deserialize(Stream input, ParserSchema.Column column, XmlWriter output)
        {
            var name = input.ReadString(4, true, Encoding.ASCII);
            if (NamesToOpcodes.ContainsKey(name) == false)
            {
                throw new FormatException("invalid opcode in multival");
            }
            var op = NamesToOpcodes[name];

            output.WriteStartElement("opcode");
            output.WriteAttributeString("type", name);

            object arg;
            
            switch (op)
            {
                case Opcode.NON:
                case Opcode.O_P:
                case Opcode.C_P:
                case Opcode.STM:
                case Opcode.LES:
                case Opcode.ADD:
                case Opcode.SUB:
                case Opcode.MUL:
                case Opcode.EQU:
                case Opcode.NOT:
                case Opcode.RET:
                case Opcode.AND:
                case Opcode.NEG:
                case Opcode.ORR:
                case Opcode.BAN:
                case Opcode.RZ_:
                case Opcode.NLE:
                case Opcode.GRE:
                case Opcode.DIV:
                case Opcode.BOR:
                case Opcode.NGR:
                case Opcode.BNT:
                {
                    arg = null;
                    break;
                }

                case Opcode.S_V:
                {
                    arg = input.ReadValueU32();
                    break;
                }

                case Opcode.INT:
                case Opcode.JZ_:
                case Opcode.J__:
                {
                    arg = input.ReadValueS64();
                    break;
                }

                case Opcode.FLT:
                {
                    arg = input.ReadValueF64();
                    break;
                }

                case Opcode.STR:
                case Opcode.FUN:
                case Opcode.OBJ:
                case Opcode.IDS:
                case Opcode.RP_:
                {
                    arg = input.ReadStringPascalUncapped();
                    break;
                }

                default: throw new NotSupportedException("unhandled opcode in multival");
            }

            if (arg != null)
            {
                output.WriteValue(arg.ToString());
            }

            output.WriteEndElement();
            //output.Flush();
        }
    }
}
