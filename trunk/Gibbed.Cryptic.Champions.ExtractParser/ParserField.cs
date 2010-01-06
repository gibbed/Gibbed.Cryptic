using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace ExtractParser
{
    [StructLayout(LayoutKind.Sequential)]
    public class ParserField
    {
        public UInt32 NameOffset;
        public UInt16 field_4;
        public UInt16 field_6;
        public UInt64 Flags;
        public UInt32 StructureOffset;
        public UInt32 ExplicitCount;
        public UInt32 DataOffset; // Parameter?
        public UInt32 field_1C;
        public UInt32 field_20;
        public UInt32 field_24;

        public ParserToken Type
        {
            get
            {
                return (ParserToken)(this.Flags & 0xFF);
            }

            set
            {
                this.Flags = (this.Flags & ~(UInt32)0xFF) | (byte)(value);
            }
        }

        public bool Serialized
        {
            get
            {
                if ((this.Flags & 0x400000) != 0 || (this.Flags & 0x100000000) != 0 || (this.Flags & 0x8000000) != 0)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
