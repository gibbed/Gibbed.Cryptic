using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParserSchema = Gibbed.Cryptic.FileFormats.ParserSchema;

namespace Gibbed.StarTrekOnline.GenerateSerializer
{
    internal class QueuedType
    {
        public string Name;
        public ParserSchema.Table Table;
        public QueuedType Parent;

        public QueuedType()
            : this(null, null, null)
        {
        }

        public QueuedType(string name, ParserSchema.Table table)
            : this(name, table, null)
        {
        }

        public QueuedType(string name, ParserSchema.Table table, QueuedType parent)
        {
            if (name != null && string.IsNullOrEmpty(name) == true)
            {
                throw new ArgumentException();
            }

            this.Name = name;
            this.Table = table;
            this.Parent = parent;
        }

        public string Key
        {
            get
            {
                if (this.Parent == null)
                {
                    return this.Name;
                }

                return this.Parent.Key + "." + this.Name;
            }
        }
    }
}
