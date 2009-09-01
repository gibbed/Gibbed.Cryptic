using System.Runtime.InteropServices;

namespace Gibbed.Champions.FileFormats
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct HoggHeader
    {
        public uint Magic;
        public short Version;
        public short Unknown2;
        public int BlockTableSize;
        public int MetadataTableSize;
        public int Unknown5;
        public int StringTableSize;
    }
}
