using System.Runtime.InteropServices;

namespace Gibbed.Cryptic.FileFormats.Hogg
{
    [StructLayout(LayoutKind.Sequential)]
    internal class Block
    {
        public long Offset;
        public int Size;
        public uint Unknown2;
        public long Unknown3;
        public short Unknown4;
        public short Unknown5;
        public int MetadataId;

        public override string ToString()
        {
            if (this.Size == -1)
            {
                return "*unused*";
            }

            return this.MetadataId.ToString() + ", @" + this.Offset.ToString("X16") + " " + this.Size.ToString();
        }
    }
}
