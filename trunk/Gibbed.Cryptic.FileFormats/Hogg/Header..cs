using System.Runtime.InteropServices;

namespace Gibbed.Cryptic.FileFormats.Hogg
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Header
    {
        public uint Magic;
        public short Version;
        public short Unknown2;
        public int BlockTableSize;
        public int MetadataTableSize;
        public int Unknown5;
        public int DataDeltaTableSize;
    }
}
