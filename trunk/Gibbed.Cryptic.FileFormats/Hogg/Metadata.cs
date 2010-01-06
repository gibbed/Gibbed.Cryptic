using System.Runtime.InteropServices;

namespace Gibbed.Cryptic.FileFormats.Hogg
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Metadata
    {
        public int NameIndex;
        public int HeaderIndex; // appears to contain the header of the texture file
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
