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
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Gibbed.Cryptic.ExportParserTables
{
    /// <summary>
    /// Allows for searching of patterns in an array of bytes.
    /// </summary>
    public class ByteSearch
    {
        private struct BytePatternEntry
        {
            public byte Value;
            public byte Mask;
        }

        /// <summary>
        /// The size of the pattern.
        /// </summary>
        public int Size
        {
            get
            {
                return this.Values.Count;
            }
        }

        private List<BytePatternEntry> Values = new List<BytePatternEntry>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pattern"></param>
        public ByteSearch(string pattern)
        {
            Regex regex;

            regex = new Regex("^(([0-9,a-f]{2})|([x]{2})|(\\s))+$", RegexOptions.IgnoreCase);

            if (regex.Match(pattern).Success == false)
            {
                throw new ArgumentException("invalid pattern", "pattern");
            }

            regex = new Regex("(([0-9,a-f]{2})|([x]{2}))(?:\\s*)", RegexOptions.IgnoreCase);

            foreach (Match match in regex.Matches(pattern))
            {
                Debug.Assert(match.Captures.Count == 1);

                BytePatternEntry entry;
                string capture = match.Captures[0].Value.Trim().ToLower();

                if (capture == "xx")
                {
                    entry.Value = 0xFF;
                    entry.Mask = 0x00;
                }
                else
                {
                    entry.Value = byte.Parse(capture, NumberStyles.HexNumber);
                    entry.Mask = 0xFF;
                }

                this.Values.Add(entry);
            }
        }

        /// <summary>
        /// Match an array of bytes against the pattern.
        /// </summary>
        /// <param name="data"></param>
        /// <returns>True if it matches the pattern</returns>
        public UInt32 Match(byte[] data)
        {
            return this.Match(data, data.Length);
        }

        /// <summary>
        /// Match an array of bytes against the pattern.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size">Size of the data</param>
        /// <returns>True if it matches the pattern</returns>
        public UInt32 Match(byte[] data, int size)
        {
            var remaining = size - (size % this.Size);

            for (int i = 0; i + this.Size <= remaining; i++)
            {
                bool matched = true;
                for (int j = 0; matched == true && j < this.Size; j++)
                {
                    matched =
                        (this.Values[j].Value & this.Values[j].Mask)
                        ==
                        (data[i + j] & this.Values[j].Mask);
                }

                if (matched)
                {
                    return (uint)i;
                }
            }

            return UInt32.MaxValue;
        }
    }
}
