/* Copyright (c) 2013 Rick (rick 'at' gibbed 'dot' us)
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
using Newtonsoft.Json;

namespace Gibbed.Cryptic.ConvertObject
{
    [JsonObject(MemberSerialization.OptIn)]
    internal class Configuration
    {
        private readonly Dictionary<string, Schema> _Schemas = new Dictionary<string, Schema>();
        private readonly Dictionary<string, string> _Aliases = new Dictionary<string, string>();

        public Dictionary<string, Schema> Schemas
        {
            get { return _Schemas; }
        }

        public Dictionary<string, string> Aliases
        {
            get { return _Aliases; }
        }

        public static Configuration Load(string basePath)
        {
            var config = new Configuration();

            if (Directory.Exists(basePath) == true)
            {
                var paths = Directory.GetFiles(basePath, "*.serializer.json", SearchOption.AllDirectories);
                foreach (var path in paths)
                {
                    string text;
                    using (var input = File.OpenRead(path))
                    {
                        var reader = new StreamReader(input);
                        text = reader.ReadToEnd();
                    }
                    var schema = JsonConvert.DeserializeObject<Schema>(text);

                    config._Schemas.Add(schema.Name.ToLowerInvariant(), schema);
                    if (schema.Aliases != null)
                    {
                        foreach (var alias in schema.Aliases)
                        {
                            config._Aliases.Add(alias.ToLowerInvariant(), schema.Name.ToLowerInvariant());
                        }
                    }
                }
            }

            return config;
        }

        public Schema GetSchema(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            name = name.ToLowerInvariant();

            var alias = this._Aliases.FirstOrDefault(s => s.Key == name).Value;
            if (string.IsNullOrEmpty(alias) == false)
            {
                name = alias;
            }

            return this._Schemas.FirstOrDefault(s => s.Key == name).Value;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Schema
        {
            [JsonProperty(PropertyName = "name", Required = Required.Always)]
            public string Name;

            [JsonProperty(PropertyName = "aliases", Required = Required.Default)]
            public List<string> Aliases = new List<string>();

            [JsonProperty(PropertyName = "is_client", Required = Required.Default)]
            public bool IsClient;

            [JsonProperty(PropertyName = "is_server", Required = Required.Default)]
            public bool IsServer;

            [JsonProperty(PropertyName = "targets", Required = Required.Always)]
            public List<AssemblyTarget> Targets = new List<AssemblyTarget>();

            public AssemblyTarget GetTarget(uint hash)
            {
                return this.Targets.SingleOrDefault(
                    t => t.ParserHash == hash);
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class AssemblyTarget
        {
            [JsonProperty(PropertyName = "parser_hash", Required = Required.Always)]
            public uint ParserHash;

            [JsonProperty(PropertyName = "class", Required = Required.Always)]
            public string Class;

            [JsonProperty(PropertyName = "versions", Required = Required.Always)]
            public List<string> Versions;

            public string FirstVersion()
            {
                return this.Versions.FirstOrDefault();
            }
        }
    }
}
