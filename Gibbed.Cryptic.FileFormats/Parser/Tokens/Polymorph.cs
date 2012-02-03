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
using System.IO;
using System.Xml;
using Gibbed.IO;

namespace Gibbed.Cryptic.FileFormats.Parser.Tokens
{
    internal class Polymorph : Token
    {
        public override StorageCompatability Storage
        {
            get
            {
                return
                    StorageCompatability.DirectValue |
                    StorageCompatability.IndirectValue |
                    StorageCompatability.IndirectArray;
            }
        }

        public override string NameDirectValue { get { return "EMBEDDEDPOLYMORPH"; } }
        public override string NameIndirectValue { get { return "OPTIONALPOLYMORPH"; } }
        public override string NameIndirectArray { get { return "POLYMORPH"; } }

        public override ColumnParameter GetParameter(ColumnFlags flags, int index)
        {
            switch (index)
            {
                case 0:
                {
                    if ((flags & ColumnFlags.INDIRECT) != 0)
                    {
                        return ColumnParameter.None;
                    }

                    return ColumnParameter.Unknown1;
                }

                case 1: return ColumnParameter.Subtable;
                default: return ColumnParameter.None;
            }
        }

        public override void Deserialize(Stream input, ParserSchema.Column column, XmlWriter output)
        {
            var flags = Parser.ColumnFlags.None;
            flags |= column.Flags & Parser.ColumnFlags.FIXED_ARRAY;
            flags |= column.Flags & Parser.ColumnFlags.EARRAY;
            flags |= column.Flags & Parser.ColumnFlags.INDIRECT;

            if (flags == ColumnFlags.INDIRECT)
            {
                var hasValue = input.ReadValueU32();
                if (hasValue == 0)
                {
                    return;
                }
            }

            var index = input.ReadValueS32();
            var target = column.Subtable.Columns[index];

            output.WriteStartElement("polymorph");
            output.WriteAttributeString("name", target.Name);

            BlobFile.DeserializeColumn(target, input, output);

            output.WriteEndElement();
        }
    }
}
