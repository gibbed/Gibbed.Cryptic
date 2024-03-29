﻿/* Copyright (c) 2021 Rick (rick 'at' gibbed 'dot' us)
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

using System.Globalization;
using System.Collections.Generic;

namespace Gibbed.Cryptic.FileFormats.ParseSchema
{
    public class Column
    {
        public string Name;
        public Parse.TokenType Token;

        public uint Offset;

        public Parse.ColumnFlags Flags;

        public string RedundantName;

        public string FloatRounding;
        public byte MinBits;

        public int NumberOfElements;
        public int Default;
        public int StringLength;
        public string DefaultString;
        public string CommandString;
        public int Size;
        public int BitOffset;

        public string DictionaryName;

        public Table Subtable;
        public bool SubtableIsExternal;
        public string SubtableExternalName;

        public Enumeration StaticDefineList;
        public string StaticDefineListExternalName;
        public bool StaticDefineListIsExternal;

        public ColumnFormat Format = ColumnFormat.None;
        public readonly Dictionary<string, string> FormatStrings = new Dictionary<string, string>();

        public string GetFormatStringAsString(string key, string defaultValue)
        {
            if (this.FormatStrings.ContainsKey(key) == false)
            {
                return defaultValue;
            }
            return this.FormatStrings[key];
        }

        public int GetFormatStringAsInt(string key, int defaultValue)
        {
            var text = this.GetFormatStringAsString(key, null);
            if (text == null)
            {
                return defaultValue;
            }
            return int.Parse(text, CultureInfo.InvariantCulture);
        }

        public bool GetFormatStringAsBool(string key, bool defaultValue)
        {
            return this.GetFormatStringAsInt(key, defaultValue == true ? 1 : 0) != 0;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
