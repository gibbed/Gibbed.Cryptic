using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gibbed.Helpers;

namespace Gibbed.Cryptic.Champions.MakeCostume
{
    internal class Program
    {
        #region Checksum
        private static UInt16[] ChecksumMagic = new UInt16[]
            {
			    0xBCD1, 0xBB65, 0x42C2, 0xDFFE, 0x9666, 0x431B, 0x8504, 0xEB46,
			    0x6379, 0xD460, 0xCF14, 0x53CF, 0xDB51, 0xDB08, 0x12C8, 0xF602,
			    0xE766, 0x2394, 0x250D, 0xDCBB, 0xA678, 0x02AF, 0xA5C6, 0x7EA6,
			    0xB645, 0xCB4D, 0xC44B, 0xE5DC, 0x9FE6, 0x5B5C, 0x35F5, 0x701A,
			    0x220F, 0x6C38, 0x1A56, 0x4CA3, 0xFFC6, 0xB152, 0x8D61, 0x7A58,
			    0x9025, 0x8B3D, 0xBF0F, 0x95A3, 0xE5F4, 0xC127, 0x3BED, 0x320B,
			    0xB7F3, 0x6054, 0x333C, 0xD383, 0x8154, 0x5242, 0x4E0D, 0x0A94,
			    0x7028, 0x8689, 0x3A22, 0x0980, 0x1847, 0xB0F1, 0x9B5C, 0x4176,
			    0xB858, 0xD542, 0x1F6C, 0x2497, 0x6A5A, 0x9FA9, 0x8C5A, 0x7743,
			    0xA8A9, 0x9A02, 0x4918, 0x438C, 0xC388, 0x9E2B, 0x4CAD, 0x01B6,
			    0xAB19, 0xF777, 0x365F, 0x1EB2, 0x091E, 0x7BF8, 0x7A8E, 0x5227,
			    0xEAB1, 0x2074, 0x4523, 0xE781, 0x01A3, 0x163D, 0x3B2E, 0x287D,
			    0x5E7F, 0xA063, 0xB134, 0x8FAE, 0x5E8E, 0xB7B7, 0x4548, 0x1F5A,
			    0xFA56, 0x7A24, 0x900F, 0x42DC, 0xCC69, 0x02A0, 0x0B22, 0xDB31,
			    0x71FE, 0x0C7D, 0x1732, 0x1159, 0xCB09, 0xE1D2, 0x1351, 0x52E9,
			    0xF536, 0x5A4F, 0xC316, 0x6BF9, 0x8994, 0xB774, 0x5F3E, 0xF6D6,
			    0x3A61, 0xF82C, 0xCC22, 0x9D06, 0x299C, 0x09E5, 0x1EEC, 0x514F,
			    0x8D53, 0xA650, 0x5C6E, 0xC577, 0x7958, 0x71AC, 0x8916, 0x9B4F,
			    0x2C09, 0x5211, 0xF6D8, 0xCAAA, 0xF7EF, 0x287F, 0x7A94, 0xAB49,
			    0xFA2C, 0x7222, 0xE457, 0xD71A, 0x00C3, 0x1A76, 0xE98C, 0xC037,
			    0x8208, 0x5C2D, 0xDFDA, 0xE5F5, 0x0B45, 0x15CE, 0x8A7E, 0xFCAD,
			    0xAA2D, 0x4B5C, 0xD42E, 0xB251, 0x907E, 0x9A47, 0xC9A6, 0xD93F,
			    0x085E, 0x35CE, 0xA153, 0x7E7B, 0x9F0B, 0x25AA, 0x5D9F, 0xC04D,
			    0x8A0E, 0x2875, 0x4A1C, 0x295F, 0x1393, 0xF760, 0x9178, 0x0F5B,
			    0xFA7D, 0x83B4, 0x2082, 0x721D, 0x6462, 0x0368, 0x67E2, 0x8624,
			    0x194D, 0x22F6, 0x78FB, 0x6791, 0xB238, 0xB332, 0x7276, 0xF272,
			    0x47EC, 0x4504, 0xA961, 0x9FC8, 0x3FDC, 0xB413, 0x007A, 0x0806,
			    0x7458, 0x95C6, 0xCCAA, 0x18D6, 0xE2AE, 0x1B06, 0xF3F6, 0x5050,
			    0xC8E8, 0xF4AC, 0xC04C, 0xF41C, 0x992F, 0xAE44, 0x5F1B, 0x1113,
			    0x1738, 0xD9A8, 0x19EA, 0x2D33, 0x9698, 0x2FE9, 0x323F, 0xCDE2,
			    0x6D71, 0xE37D, 0xB697, 0x2C4F, 0x4373, 0x9102, 0x075D, 0x8E25,
			    0x1672, 0xEC28, 0x6ACB, 0x86CC, 0x186E, 0x9414, 0xD674, 0xD1A5,
            };

        private static UInt32 Checksum(byte[] data)
        {
            UInt16 sum1 = 0;
            UInt16 sum2 = 0;
            
            for (int i = 0; i < data.Length; i++)
            {
                sum1 += ChecksumMagic[data[i]];
                sum2 += sum1;
            }

            return (UInt32)(sum2 << 16) | (UInt32)sum1;
        }
        #endregion

        private static void WriteTag(Stream output, byte id, string text)
        {
            output.WriteValueU16(0x1C02, false);
            output.WriteValueU8(id);
            output.WriteValueU16((ushort)text.Length, false);
            output.WriteStringASCII(text);
        }

        private static void WriteTag(Stream output, byte id, byte[] data)
        {
            output.WriteValueU16(0x1C02, false);
            output.WriteValueU8(id);
            output.WriteValueU16((ushort)data.Length, false);
            output.Write(data, 0, data.Length);
        }

        public static void Main(string[] args)
        {
            Stream input;
            
            input = File.OpenRead(args[0]);
            byte[] costume = new byte[input.Length];
            input.Read(costume, 0, costume.Length);
            input.Close();

            input = File.OpenRead("grenade.jpg");

            Stream output = File.OpenWrite(args[1]);

            UInt32 checksum = Checksum(costume);

            MemoryStream data = new MemoryStream();
            data.WriteValueU16(0x1C02, false);
            data.WriteValueU16(0x0000);
            data.WriteValueU8(0x02);
            data.WriteValueU8(0x00);
            data.WriteValueU8(0x02);

            WriteTag(data, 25, "FightClub");
            WriteTag(data, 25, "FC");
            WriteTag(data, 25, "Gender:Female");
            WriteTag(data, 120, "MAKE ME A CHAMP!");
            WriteTag(data, 120, String.Format("7799{0:D10}", (Int32)checksum) + "\0");
            WriteTag(data, 202, costume);

            MemoryStream itpc = new MemoryStream();
            itpc.WriteStringASCIIZ("Photoshop 3.0");
            itpc.WriteStringASCII("8BIM");
            itpc.WriteValueU8(4);
            itpc.WriteValueU8(4);
            itpc.WriteValueU32(0);
            itpc.WriteValueU16((ushort)data.Length, false);
            
            data.Seek(0, SeekOrigin.Begin);
            itpc.Seek(0, SeekOrigin.Begin);

            output.WriteFromStream(input.ReadToMemoryStream(20), 20);
            output.WriteValueU16(0xFFED, false);
            output.WriteValueU16((ushort)(2 + itpc.Length + data.Length), false);
            output.WriteFromStream(itpc, itpc.Length);
            output.WriteFromStream(data, data.Length);
            output.WriteFromStream(input.ReadToMemoryStream(38555), 38555);

            output.Close();
            input.Close();
        }
    }
}
