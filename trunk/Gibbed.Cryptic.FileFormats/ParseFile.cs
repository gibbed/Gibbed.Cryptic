using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gibbed.Helpers;

namespace Gibbed.Cryptic.FileFormats
{
    public class ParseFile
    {
        public UInt32 Type;
        public List<Parse.File> Files = new List<Parse.File>();
        public MemoryStream Data = null;

        public void Deserialize(Stream input)
        {
            if (input.ReadStringASCII(8) != "CrypticS")
            {
                throw new FormatException("not a bin file");
            }

            this.Type = input.ReadValueU32();

            if (input.ReadStringPascal16() != "ParseJ")
            {
                throw new FormatException("ParseJ");
            }

            this.DeserializeFiles(input);
            this.DeserializeDependencies(input);
            this.DeserializeData(input);
        }

        private void DeserializeFiles(Stream input)
        {
            if (input.ReadStringPascal16() != "Files1")
            {
                throw new FormatException("Files1");
            }

            Stream memory = input.ReadToMemoryStream(input.ReadValueU32());

            this.Files.Clear();
            int count = memory.ReadValueS32();
            for (int i = 0; i < count; i++)
            {
                Parse.File file = new Parse.File();
                file.Name = memory.ReadStringPascal16();
                file.Unknown = memory.ReadValueU32();
                this.Files.Add(file);
            }
        }

        private void DeserializeDependencies(Stream input)
        {
            if (input.ReadStringPascal16() != "Depen1")
            {
                throw new FormatException("Depen1");
            }

            Stream memory = input.ReadToMemoryStream(input.ReadValueU32());

            int count = memory.ReadValueS32();
            if (count != 0)
            {
                throw new Exception();
            }
        }

        private void DeserializeData(Stream input)
        {
            this.Data = input.ReadToMemoryStream(input.ReadValueU32());
        }
    }
}
