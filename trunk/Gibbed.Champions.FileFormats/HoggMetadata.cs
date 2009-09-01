using System.Runtime.InteropServices;

namespace Gibbed.Champions.FileFormats
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct HoggMetadata
    {
        public int NameIndex;
        public int Unknown04;
        public int UncompressedSize;
        public uint Flags;

        public override string ToString()
        {
            if ((this.Flags & 1) == 1)
            {
                return "*unused*";
            }

            return this.NameIndex.ToString();
        }
    }
}
