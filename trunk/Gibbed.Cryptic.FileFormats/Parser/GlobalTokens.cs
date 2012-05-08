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

namespace Gibbed.Cryptic.FileFormats.Parser
{
    public class GlobalTokens
    {
        private static readonly Token[] _Tokens =
        {
            new Tokens.Ignore(), // 0
            new Tokens.Start(), // 1
            new Tokens.End(), // 2

            new Tokens.Byte(), // 3
            new Tokens.Int16(), // 4
            new Tokens.Int32(), // 5
            new Tokens.Int64(), // 6
            new Tokens.Float(), // 7
            new Tokens.String(), // 8
            new Tokens.CurrentFile(), // 9
            new Tokens.Timestamp(), // 10
            new Tokens.LineNumber(), // 11
            new Tokens.Boolean(), // 12
            null, // 13
            new Tokens.BooleanFlag(), // 14
            new Tokens.QUATPYR(), // 15
            new Tokens.MATPYR(), // 16
            new Tokens.Filename(), // 17
            new Tokens.Reference(), // 18
            new Tokens.FunctionCall(), // 19
            new Tokens.Structure(), // 20
            new Tokens.Polymorph(), // 21
            new Tokens.StashTable(), // 22
            new Tokens.Bit(), // 23
            new Tokens.MultiValue(), // 24
            new Tokens.Command(), // 25
        };

        public static byte Count { get { return (byte)_Tokens.Length; } }

        public static Token MatchToken(string name, out byte id, out ColumnFlags flags)
        {
            name = name.ToLowerInvariant();

            for (byte i = 0; i < _Tokens.Length; i++)
            {
                var token = _Tokens[i];
                if (token == null)
                {
                    continue;
                }

                if (token.NameDirectValue != null &&
                    name == token.NameDirectValue.ToLowerInvariant())
                {
                    id = i;
                    flags = ColumnFlags.None;
                    return token;
                }

                if (token.NameDirectFixedArray != null &&
                    name == token.NameDirectFixedArray.ToLowerInvariant())
                {
                    id = i;
                    flags = ColumnFlags.FIXED_ARRAY;
                    return token;
                }

                if (token.NameDirectArray != null &&
                    name == token.NameDirectArray.ToLowerInvariant())
                {
                    id = i;
                    flags = ColumnFlags.EARRAY;
                    return token;
                }

                if (token.NameIndirectValue != null &&
                    name == token.NameIndirectValue.ToLowerInvariant())
                {
                    id = i;
                    flags = ColumnFlags.INDIRECT;
                    return token;
                }

                if (token.NameIndirectFixedArray != null &&
                    name == token.NameIndirectFixedArray.ToLowerInvariant())
                {
                    id = i;
                    flags = ColumnFlags.INDIRECT | ColumnFlags.FIXED_ARRAY;
                    return token;
                }

                if (token.NameIndirectArray != null &&
                    name == token.NameIndirectArray.ToLowerInvariant())
                {
                    id = i;
                    flags = ColumnFlags.INDIRECT | ColumnFlags.EARRAY;
                    return token;
                }
            }

            throw new ArgumentException("token not found", "name");
        }

        public static Token GetToken(byte index)
        {
            if (index < 0 || index >= _Tokens.Length)
            {
                throw new ArgumentException("invalid token index", "index");
            }

            return _Tokens[index];
        }
    }
}
