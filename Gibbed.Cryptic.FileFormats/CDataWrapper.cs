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
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Gibbed.Cryptic.FileFormats
{
    [XmlSchemaProvider("GenerateSchema")]
    public sealed class CDataWrapper : IXmlSerializable
    {
        public static implicit operator string(CDataWrapper value)
        {
            if (value == null)
            {
                return null;
            }

            return value.Value;
        }

        public static implicit operator CDataWrapper(string value)
        {
            if (value == null)
            {
                return null;
            }

            return new CDataWrapper
            {
                Value = value,
            };
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public static XmlQualifiedName GenerateSchema(XmlSchemaSet xs)
        {
            return XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String).QualifiedName;
        }

        public void WriteXml(XmlWriter writer)
        {
            if (string.IsNullOrEmpty(this.Value) == false)
            {
                writer.WriteCData(Value);
            }
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement == true)
            {
                this.Value = "";
            }
            else
            {
                reader.Read();

                switch (reader.NodeType)
                {
                    case XmlNodeType.EndElement:
                    {
                        this.Value = "";
                        break;
                    }

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    {
                        this.Value = reader.ReadContentAsString();
                        break;
                    }

                    default:
                    {
                        throw new InvalidOperationException("expected text / cdata");
                    }
                }
            }
        }

        public string Value { get; set; }

        public override string ToString()
        {
            return Value;
        }
    }
}
