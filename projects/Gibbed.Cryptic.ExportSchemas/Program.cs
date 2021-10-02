/* Copyright (c) 2021 Rick (rick 'at' gibbed 'dot' us)
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Gibbed.Cryptic.ExportSchemas.Exporters;
using Gibbed.Cryptic.ExportSchemas.Runtime;

namespace Gibbed.Cryptic.ExportSchemas
{
    internal static class Program
    {
        private static string GetExecutablePath()
        {
            using var process = Process.GetCurrentProcess();
            var path = Path.GetFullPath(process.MainModule.FileName);
            return Path.GetFullPath(path);
        }

        public static void Main(string[] args)
        {
            var process = FindSuitableProcess(out var projectName);
            if (process == null)
            {
                return;
            }

            using (var runtime = new RuntimeProcess())
            {
                if (runtime.OpenProcess(process) == false)
                {
                    return;
                }

                Export(runtime, projectName);
            }
        }

        private static void Export(RuntimeProcess runtime, string projectName)
        {
            var expressionFunctionExporter = new ExpressionFunctionExporter(runtime);
            var enumExporter = new EnumExporter(runtime);
            var parseExporter = new ParseExporter(runtime);

            var executablePath = GetExecutablePath();
            var binPath = Path.GetDirectoryName(executablePath);

            var outputPath = Path.Combine(binPath, "..", "configs", "schemas", projectName);
            Directory.CreateDirectory(outputPath);

            expressionFunctionExporter.Export(outputPath);
            var enums = enumExporter.Export(outputPath);
            parseExporter.Export(outputPath, enums);
        }

        private static Process FindSuitableProcess(out string projectName)
        {
            var gameNames = new[]
            {
                "Champions Online",
                "Star Trek Online",
                "Neverwinter",
            };

            Process process;

            projectName = null;

            process = Process.GetProcessesByName("GameClient")
                             .FirstOrDefault(p => Array.IndexOf(gameNames, p.MainWindowTitle) >= 0);
            if (process != null)
            {
                var path = process.MainModule.FileName;
                path = Path.GetDirectoryName(path);

                var candidate = Path.GetFileName(path);
                if (candidate == "x86" || candidate == "x64")
                {
                    path = Path.GetDirectoryName(path);
                    path = Path.GetFileName(path);
                }
                else
                {
                    path = candidate;
                }

                if (path == null)
                {
                    throw new InvalidOperationException();
                }

                projectName = Path.Combine(process.MainWindowTitle, path);
                return process;
            }

            foreach (var gameName in gameNames)
            {
                process = Process.GetProcessesByName(gameName)
                                 .FirstOrDefault();
                if (process != null)
                {
                    projectName = Path.Combine(gameName, "Launcher");
                    return process;
                }
            }

            return null;
        }
    }
}
