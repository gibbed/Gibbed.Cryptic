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
using Gibbed.Cryptic.FileFormats;

namespace Gibbed.Cryptic.GenerateSerializer
{
    public class EnumLoader
    {
        private readonly Dictionary<string, string> _Paths;
        private readonly Dictionary<string, ParserEnumFile> _LoadedEnums;

        public IEnumerable<string> EnumNames
        {
            get { return this._Paths.Keys; }
        }

        public EnumLoader(string path)
        {
            this._Paths = new Dictionary<string, string>();
            this._LoadedEnums = new Dictionary<string, ParserEnumFile>();

            foreach (var inputPath in Directory.GetFiles(path, "*.enum.xml", SearchOption.AllDirectories))
            {
                var name = ParserEnumFile.GetNameFromFile(inputPath);
                this._Paths.Add(name, inputPath);
            }
        }

        public ParserEnumFile LoadEnum(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (this._Paths.ContainsKey(name) == false)
            {
                throw new ArgumentException("no such enum", "name");
            }

            if (this._LoadedEnums.ContainsKey(name) == true)
            {
                return this._LoadedEnums[name];
            }

            var e = ParserEnumFile.LoadFile(this._Paths[name]);
            this._LoadedEnums.Add(name, e);
            return e;
        }
    }
}
