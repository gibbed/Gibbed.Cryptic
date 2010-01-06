using System;
using System.IO;
using Gibbed.Helpers;
using Gibbed.Cryptic.FileFormats;
using Microsoft.Win32;

namespace Gibbed.Cryptic.Test
{
    internal class Program
    {
        private static string GetChampionsPath()
        {
            return (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Champions Online", "InstallLocation", "");
        }

        public static void Main(string[] args)
        {
            /*
            string root = GetChampionsPath();

            //Stream input = File.Open(Path.Combine(root, "Champions Online\\Live\\cache\\rdrShaderCache.hogg"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            //Stream input = File.Open(Path.Combine(root, "Champions Online\\Live\\piggs\\exes.hogg"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            //Stream input = File.Open(Path.Combine(root, "Champions Online\\Live\\piggs\\character.hogg"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Stream input = File.Open(Path.Combine(root, "Champions Online\\Live\\piggs\\texture.hogg"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            PiggFile pigg = new PiggFile();
            pigg.Deserialize(input);
            input.Close();
            */

            foreach (string wtexPath in Directory.GetFiles(Environment.CurrentDirectory, "*.wtex", SearchOption.AllDirectories))
            {
                string ddsPath = Path.ChangeExtension(wtexPath, ".dds");

                if (File.Exists(ddsPath) == true)
                {
                    continue;
                }

                Stream input = File.OpenRead(wtexPath);
                Stream output = File.Open(ddsPath, FileMode.CreateNew, FileAccess.Write);

                int headerSize = input.ReadValueS32();
                input.Seek(headerSize, SeekOrigin.Begin);

                long left = input.Length - headerSize;
                byte[] data = new byte[4096];
                while (left > 0)
                {
                    int block = (int)(Math.Min(left, 4096));
                    input.Read(data, 0, block);
                    output.Write(data, 0, block);
                    left -= block;
                }

                input.Close();
                output.Close();

                File.Delete(wtexPath);
            }
        }
    }
}
