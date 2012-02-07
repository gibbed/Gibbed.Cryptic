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

namespace Gibbed.StarTrekOnline.GenerateSerializer
{
    public class EnumLoader
    {
        public IEnumerable<string> EnumNames { get { return this.Paths.Keys; } }

        private Dictionary<string, string> Paths
            = new Dictionary<string, string>();
        private Dictionary<string, ParserEnumFile> LoadedEnums
            = new Dictionary<string, ParserEnumFile>();

        public EnumLoader(string path)
        {
            foreach (var inputPath in Directory.GetFiles(path, "*.enum.xml", SearchOption.AllDirectories))
            {
                var name = ParserEnumFile.GetNameFromFile(inputPath);
                this.Paths.Add(name, inputPath);
            }
        }

        public ParserEnumFile LoadEnum(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (this.Paths.ContainsKey(name) == false)
            {
                throw new ArgumentException("no such enum", "name");
            }

            if (this.LoadedEnums.ContainsKey(name) == true)
            {
                return this.LoadedEnums[name];
            }

            var e = ParserEnumFile.LoadFile(this.Paths[name]);
            this.LoadedEnums.Add(name, e);
            return e;
        }
    }
}
