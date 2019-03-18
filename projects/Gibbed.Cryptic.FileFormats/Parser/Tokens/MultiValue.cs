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

        private static readonly Dictionary<string, MultiValueOpcode> _NamesToOpcodes;
        static MultiValue()
        {
            _NamesToOpcodes = new Dictionary<string, MultiValueOpcode>();
            foreach (var name in Enum.GetNames(typeof(MultiValueOpcode)))
            {
                _NamesToOpcodes.Add(name, (MultiValueOpcode)Enum.Parse(typeof(MultiValueOpcode), name));
            }
        }

        public override void Deserialize(Stream input, ParserSchema.Column column, XmlWriter output)
        {
            var name = input.ReadString(4, true, Encoding.ASCII);
            if (_NamesToOpcodes.ContainsKey(name) == false)
            {
                throw new FormatException("invalid opcode in multival");
            }
            var op = _NamesToOpcodes[name];

            output.WriteStartElement("opcode");
            output.WriteAttributeString("type", name);

            object arg;
            
            switch (op)
            {
                case MultiValueOpcode.NON:
                case MultiValueOpcode.O_P:
                case MultiValueOpcode.C_P:
                case MultiValueOpcode.STM:
                case MultiValueOpcode.LES:
                case MultiValueOpcode.ADD:
                case MultiValueOpcode.SUB:
                case MultiValueOpcode.MUL:
                case MultiValueOpcode.EQU:
                case MultiValueOpcode.NOT:
                case MultiValueOpcode.RET:
                case MultiValueOpcode.AND:
                case MultiValueOpcode.NEG:
                case MultiValueOpcode.ORR:
                case MultiValueOpcode.BAN:
                case MultiValueOpcode.RZ_:
                case MultiValueOpcode.NLE:
                case MultiValueOpcode.GRE:
                case MultiValueOpcode.DIV:
                case MultiValueOpcode.BOR:
                case MultiValueOpcode.NGR:
                case MultiValueOpcode.BNT:
                {
                    arg = null;
                    break;
                }

                case MultiValueOpcode.S_V:
                {
                    arg = input.ReadValueU32();
                    break;
                }

                case MultiValueOpcode.INT:
                case MultiValueOpcode.JZ_:
                case MultiValueOpcode.J__:
                {
                    arg = input.ReadValueS64();
                    break;
                }

                case MultiValueOpcode.FLT:
                {
                    arg = input.ReadValueF64();
                    break;
                }

                case MultiValueOpcode.STR:
                case MultiValueOpcode.FUN:
                case MultiValueOpcode.OBJ:
                case MultiValueOpcode.IDS:
                case MultiValueOpcode.RP_:
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
