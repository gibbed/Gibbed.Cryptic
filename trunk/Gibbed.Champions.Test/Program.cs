using System;
using System.IO;
using Gibbed.Champions.FileFormats;
using Microsoft.Win32;

namespace Gibbed.Champions.Test
{
    internal class Program
    {
        private static string GetChampionsPath()
        {
            return (string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Champions Online", "InstallLocation", "");
        }

        public static void Main(string[] args)
        {
            string root = GetChampionsPath();

            //Stream input = File.Open(Path.Combine(root, "Live\\cache\\rdrShaderCache.hogg"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            //Stream input = File.Open(Path.Combine(root, "Live\\piggs\\exes.hogg"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Stream input = File.Open(Path.Combine(root, "Live\\piggs\\character.hogg"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            HoggFile hogg = new HoggFile();
            hogg.Deserialize(input);
            input.Close();
        }
    }
}
