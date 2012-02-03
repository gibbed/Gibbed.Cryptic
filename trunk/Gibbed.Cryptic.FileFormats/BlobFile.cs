﻿/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
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
using System.Text;
using System.Xml;
using Gibbed.IO;

namespace Gibbed.Cryptic.FileFormats
{
    public class BlobFile
    {
        public uint ParserHash;
        public List<Blob.FileEntry> Files
            = new List<Blob.FileEntry>();
        public List<Blob.DependencyEntry> Dependencies
           = new List<Blob.DependencyEntry>();

        public void Serialize(Stream output)
        {
            output.WriteString("CrypticS", Encoding.ASCII);
            output.WriteValueU32(this.ParserHash);
            output.WriteStringPascal("ParseJ", 4096);
            
            output.WriteStringPascal("Files1", 20);
            using (var data = new MemoryStream())
            {
                data.WriteValueS32(this.Files.Count);
                foreach (var entry in this.Files)
                {
                    data.WriteStringPascal(entry.Name, 260);
                    data.WriteValueU32(entry.Timestamp);
                }

                data.Position = 0;
                output.WriteValueU32((uint)data.Length);
                output.WriteFromStream(data, (uint)data.Length);
            }

            output.WriteStringPascal("Depen1", 20);
            using (var data = new MemoryStream())
            {
                data.WriteValueS32(this.Dependencies.Count);
                foreach (var entry in this.Dependencies)
                {
                    data.WriteValueU32(entry.Unknown0);
                    data.WriteStringPascal(entry.Name, 260);
                    data.WriteValueU32(entry.Unknown1);
                }

                data.Position = 0;
                output.WriteValueU32((uint)data.Length);
                output.WriteFromStream(data, (uint)data.Length);
            }
        }

        public void Deserialize(Stream input)
        {
            if (input.ReadString(8, Encoding.ASCII) != "CrypticS")
            {
                throw new FormatException();
            }

            this.ParserHash = input.ReadValueU32();

            if (input.ReadStringPascal(4096) != "ParseJ")
            {
                throw new FormatException();
            }

            if (input.ReadStringPascal(20) != "Files1")
            {
                throw new FormatException();
            }

            var fileInfoSize = input.ReadValueU32();
            this.Files.Clear();
            using (var data = input.ReadToMemoryStream(fileInfoSize))
            {
                var count = data.ReadValueU32();
                for (uint i = 0; i < count; i++)
                {
                    var entry = new Blob.FileEntry();
                    entry.Name = data.ReadStringPascal(260);
                    entry.Timestamp = data.ReadValueU32();
                    this.Files.Add(entry);
                }

                if (data.Position != data.Length)
                {
                    throw new FormatException();
                }
            }

            if (input.ReadStringPascal(20) != "Depen1")
            {
                throw new FormatException();
            }

            var dependencyInfoSize = input.ReadValueU32();
            this.Dependencies.Clear();
            using (var data = input.ReadToMemoryStream(dependencyInfoSize))
            {
                var count = data.ReadValueU32();
                for (uint i = 0; i < count; i++)
                {
                    var entry = new Blob.DependencyEntry();
                    entry.Unknown0 = data.ReadValueU32();
                    entry.Name = data.ReadStringPascal(260);
                    entry.Unknown1 = data.ReadValueU32();
                    this.Dependencies.Add(entry);
                }

                if (data.Position != data.Length)
                {
                    throw new FormatException();
                }
            }
        }

        public static void DeserializeColumn(ParserSchema.Column column, Stream input, XmlWriter output)
        {
            if ((column.Flags & Parser.ColumnFlags.ALIAS) != 0 ||
                (column.Flags & Parser.ColumnFlags.UNKNOWN_32) != 0 ||
                (column.Flags & Parser.ColumnFlags.NO_WRITE) != 0)
            {
                return;
            }

            var token = Parser.GlobalTokens.GetToken(column.Token);

            if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
            {
                // value
                token.Deserialize(input, column, output);
            }
            else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
            {
                // fixed array
                if (column.Token == 16) // MATPYR
                {
                    token.Deserialize(input, column, output);
                }
                else
                {
                    for (int i = 0; i < column.NumberOfElements; i++)
                    {
                        output.WriteStartElement("item");
                        token.Deserialize(input, column, output);
                        output.WriteEndElement();
                    }
                }
            }
            else
            {
                if (column.Token == 19)
                {
                    token.Deserialize(input, column, output);
                }
                else
                {
                    var count = input.ReadValueU32();
                    if (count > 800000)
                    {
                        throw new FormatException();
                    }

                    for (int i = 0; i < count; i++)
                    {
                        output.WriteStartElement("item");
                        token.Deserialize(input, column, output);
                        output.WriteEndElement();
                    }
                }
            }
        }

        public static void DeserializeTable(ParserSchema.Table table, Stream input, XmlWriter output)
        {
            //output.Flush();

            var dataSize = input.ReadValueU32();
            using (var data = input.ReadToMemoryStream(dataSize))
            {
                /*
                Counter++;
                using (var temp = File.Create(string.Format("{0}.struct", Counter)))
                {
                    temp.WriteFromStream(data, data.Length);
                    data.Position = 0;
                }
                */

                output.WriteStartElement("table");

                foreach (var column in table.Columns)
                {
                    if ((column.Flags & Parser.ColumnFlags.ALIAS) != 0 ||
                        (column.Flags & Parser.ColumnFlags.UNKNOWN_32) != 0 ||
                        (column.Flags & Parser.ColumnFlags.NO_WRITE) != 0)
                    {
                        continue;
                    }

                    //output.WriteComment(string.Format(" offset = {0} ", data.Position));
                    output.WriteStartElement("column");
                    output.WriteAttributeString("name", column.Name);
                    DeserializeColumn(column, data, output);
                    output.WriteEndElement();
                    //output.Flush();
                }

                output.WriteEndElement();

                if (data.Position != data.Length)
                {
                    throw new InvalidOperationException();
                }
            }

            //output.Flush();
        }
    }
}
