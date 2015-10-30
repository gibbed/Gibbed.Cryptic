/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
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

namespace Gibbed.Cryptic.ExportSchemas
{
    public class ByteSearch
    {
        private readonly List<PatternEntry> _Values;

        public ByteSearch(params Pattern[] patterns)
        {
            if (patterns == null)
            {
                throw new ArgumentNullException("patterns");
            }

            this._Values = new List<PatternEntry>();
            foreach (var pattern in patterns)
            {
                foreach (var entry in pattern)
                {
                    this._Values.Add(entry);
                }
            }
        }

        public int Size
        {
            get { return this._Values.Count; }
        }

        public static bool Match(byte[] bytes, Pattern pattern, out uint result)
        {
            return Match(bytes, 0, bytes.Length, pattern, out result);
        }

        public static bool Match(byte[] buffer, int offset, int count, Pattern pattern, out uint result)
        {
            var remaining = count - (count % pattern.Count);

            for (int i = 0; i + pattern.Count <= remaining; i++)
            {
                bool matched = true;
                for (int j = 0; matched == true && j < pattern.Count; j++)
                {
                    var value = buffer[i + j] & pattern[j].Mask;
                    matched = pattern[j].MaskedValue == value;
                }

                if (matched == true)
                {
                    result = (uint)i;
                    return true;
                }
            }

            result = 0;
            return false;
        }

        public struct PatternEntry
        {
            public readonly byte Value;
            public readonly byte Mask;
            public readonly byte MaskedValue;

            public PatternEntry(byte value)
                : this(value, 0xFF)
            {
            }

            public PatternEntry(byte value, byte mask)
            {
                this.Value = value;
                this.Mask = mask;
                this.MaskedValue = (byte)(value & mask);
            }

            public override string ToString()
            {
                if (this.Mask == 0xFF)
                {
                    return this.Value.ToString("X2");
                }
                
                if (this.Mask == 0)
                {
                    return "??";
                }

                return string.Format("(&{0:X2}={1:X2})",
                                     this.Mask,
                                     this.Value);
            }
        }

        public class Pattern : IEnumerable<PatternEntry>
        {
            private readonly List<PatternEntry> _Entries;

            public Pattern()
            {
                this._Entries = new List<PatternEntry>();
            }

            public PatternEntry this[int i]
            {
                get { return this._Entries[i]; }
            }

            public int Count
            {
                get { return this._Entries.Count; }
            }

            public void Add(byte value)
            {
                this._Entries.Add(new PatternEntry(value, 0xFF));
            }

            public void Add(byte[] values)
            {
                foreach (var value in values)
                {
                    this.Add(value);
                }
            }

            public void Add(byte[,] values)
            {
                var width = values.GetLength(0);
                var height = values.GetLength(1);
                if (height != 2)
                {
                    throw new ArgumentOutOfRangeException("values", "values must be an array of byte[,2]");
                }

                for (int i = 0; i < width; i++)
                {
                    this._Entries.Add(new PatternEntry(values[i, 0], values[i, 1]));
                }
            }

            IEnumerator<PatternEntry> IEnumerable<PatternEntry>.GetEnumerator()
            {
                return this._Entries.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this._Entries.GetEnumerator();
            }
        }

        public static byte[,] AnyBytes(int count)
        {
            return new byte[count, 2];
        }
    }
}
