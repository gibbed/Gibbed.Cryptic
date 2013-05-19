/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.IO;
using NDesk.Options;

namespace Gibbed.Cryptic.GenerateSerializer
{
    internal class Program
    {
        private static string GetExecutablePath()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static void Main(string[] args)
        {
            var showHelp = false;
            string version = null;
            string inputPath = Path.Combine(GetExecutablePath(), "parsers", "Star Trek Online");
            string targetGameName = "";

            var options = new OptionSet()
            {
                {
                    "g|game=",
                    "set target game",
                    v => targetGameName = v
                    },
                {
                    "p|path=",
                    "set input path",
                    v => inputPath = v
                },
                {
                    "v|version=",
                    "set version",
                    v => version = v
                },
                {
                    "h|help",
                    "show this message and exit", 
                    v => showHelp = v != null
                },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            var targetGame = TargetGame.Unknown;
            if (string.IsNullOrWhiteSpace(targetGameName) == false)
            {
                if (Enum.TryParse(targetGameName, out targetGame) == false)
                {
                    Console.WriteLine("You must specify a valid target game!");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("You must specify a target game!");
                Console.WriteLine();
            }

            if (extras.Count < 0 || extras.Count > 1 || showHelp == true ||
                targetGame == TargetGame.Unknown)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ -g [game] [output_dll]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                Console.WriteLine("Game tarets:");
                foreach (var name in Enum.GetNames(typeof(TargetGame)))
                {
                    Console.WriteLine("  {0}", name);
                }
                return;
            }

            Console.WriteLine("Target = {0}", targetGame);
            Console.WriteLine("Base = {0}", inputPath);

            var parserLoader = new ParserLoader(inputPath);
            var enumLoader = new EnumLoader(inputPath);

            Func<string> defaultName = () => "Gibbed." + targetGame.ToString() + ".Serialization.dll";
            var outputPath = extras.Count > 0 ? extras[0] : defaultName();
            new Generator(targetGame, parserLoader, enumLoader).ExportAssembly(outputPath, version);
        }
    }
}
