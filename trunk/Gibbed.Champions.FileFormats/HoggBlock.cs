using System.Runtime.InteropServices;

namespace Gibbed.Champions.FileFormats
{
    [StructLayout(LayoutKind.Sequential)]
    internal class HoggBlock
    {
        public long Offset;
        public int Size;
        public uint Unknown2;
        public long Unknown3;
        public short Unknown4;
        public short Unknown5;
        public int EntryId;

        public override string ToString()
        {
            if (this.Size == -1)
            {
                return "*unused*";
            }

            return this.EntryId.ToString() + ", @" + this.Offset.ToString("X16") + " " + this.Size.ToString();
        }
    }
}
