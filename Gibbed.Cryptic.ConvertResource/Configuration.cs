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
using System.Linq;
using Newtonsoft.Json;

namespace Gibbed.Cryptic.ConvertResource
{
    [JsonObject(MemberSerialization.OptIn)]
    internal class Configuration
    {
        public Schema GetSchema(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            name = name.ToLowerInvariant();
            return this.Schemas
                .FirstOrDefault(s => s.Key.ToLowerInvariant() == name)
                .Value;
        }

        [JsonProperty(PropertyName = "schemas", Required = Required.Always)]
        public Dictionary<string, Schema> Schemas
            = new Dictionary<string, Schema>();

        [JsonProperty(PropertyName = "schema_aliases", Required = Required.AllowNull)]
        public Dictionary<string, string> SchemaAliases
            = new Dictionary<string, string>();

        [JsonObject(MemberSerialization.OptIn)]
        public class Schema
        {
            [JsonProperty(PropertyName = "mode", Required = Required.Always)]
            public string Mode;

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

            [JsonProperty(PropertyName = "key", Required = Required.Default)]
            public string Key;

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
