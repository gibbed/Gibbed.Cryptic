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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gibbed.Cryptic.FileFormats;
using ParseSchema = Gibbed.Cryptic.FileFormats.ParseSchema;

namespace Gibbed.Cryptic.GenerateSerializer
{
    public class ParseLoader
    {
        private readonly Dictionary<string, string> _Paths;
        private readonly Dictionary<string, ParseSchemaFile> _LoadedParses;

        public IEnumerable<string> ParseNames
        {
            get { return this._Paths.Keys; }
        }

        public ParseLoader(string path)
        {
            this._Paths = new Dictionary<string, string>();
            this._LoadedParses = new Dictionary<string, ParseSchemaFile>();

            foreach (var inputPath in Directory.GetFiles(path, "*.parse.xml", SearchOption.AllDirectories))
            {
                var name = ParseSchemaFile.GetNameFromFile(inputPath);
                if (name == "ReferenceEArrayTemplate")
                {
                    continue;
                }

                this._Paths.Add(name, inputPath);
            }
        }

        private void ResolveParse(ParseSchema.Table table)
        {
            foreach (var column in table
                .Columns
                .Where(c => c.Subtable != null))
            {
                ResolveParse(column.Subtable);
            }

            foreach (var column in table
                .Columns
                .Where(c => string.IsNullOrEmpty(c.SubtableExternalName) == false))
            {
                var external = this.LoadParse(column.SubtableExternalName);
                column.Subtable = external.Table ?? throw new InvalidOperationException();
            }
        }

        public ParseSchemaFile LoadParse(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (this._Paths.ContainsKey(name) == false)
            {
                throw new ArgumentException("no such parse", nameof(name));
            }

            if (this._LoadedParses.ContainsKey(name) == true)
            {
                return this._LoadedParses[name];
            }

            var parse = ParseSchemaFile.LoadFile(this._Paths[name]);
            this._LoadedParses.Add(name, parse);
            ResolveParse(parse.Table);
            return parse;
        }
    }
}
