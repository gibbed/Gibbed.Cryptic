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
using System.Reflection;
using Gibbed.Cryptic.FileFormats;
using Parser = Gibbed.Cryptic.FileFormats.Parser;
using ParserSchema = Gibbed.Cryptic.FileFormats.ParserSchema;

namespace Gibbed.StarTrekOnline.GenerateSerializer
{
    internal static class Helpers
    {
        public static string GetColumnName(ParserSchema.Column column)
        {
            if (string.IsNullOrEmpty(column.Name) == true)
            {
                return "_";
            }

            return column.Name;
        }

        public static bool IsGoodColumn(ParserSchema.Column column)
        {
            if ((column.Flags & Parser.ColumnFlags.ALIAS) != 0 ||
                (column.Flags & Parser.ColumnFlags.UNKNOWN_32) != 0 ||
                (column.Flags & Parser.ColumnFlags.NO_WRITE) != 0 ||
                (column.Flags & Parser.ColumnFlags.SERVER_ONLY) != 0)
            {
                return false;
            }
            else if (column.Token == 0 || // ignore
                column.Token == 1 || // start
                column.Token == 2 || // end
                column.Token == 25) // command
            {
                return false;
            }

            return true;
        }

        public static MethodInfo GetFileSerializerMethod(ParserSchema.Column column)
        {
            var token = Parser.GlobalTokens.GetToken(column.Token);

            string name = null;

            if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
            {
                name = "SerializeValue" + token.GetType().Name;
            }
            else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
            {
                name = "SerializeArray" + token.GetType().Name;
            }
            else
            {
                name = "SerializeList" + token.GetType().Name;
            }

            if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                column.StaticDefineList != null)
            {
                name += "Enum";
            }

            var methodInfo = typeof(ICrypticFileStream).GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Instance);
            if (methodInfo == null)
            {
                throw new NotSupportedException(name + " is missing");
            }

            return methodInfo;
        }

        public static MethodInfo GetPacketSerializerMethod(ParserSchema.Column column)
        {
            var token = Parser.GlobalTokens.GetToken(column.Token);

            string name = null;

            if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
            {
                name = "ReadValue" + token.GetType().Name;
            }
            else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
            {
                name = "ReadArray" + token.GetType().Name;
            }
            else
            {
                name = "ReadList" + token.GetType().Name;
            }

            if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                column.StaticDefineList != null)
            {
                name += "Enum";
            }

            var methodInfo = typeof(ICrypticPacketReader).GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Instance);
            if (methodInfo == null)
            {
                throw new NotSupportedException(name + " is missing");
            }

            return methodInfo;
        }
    }
}
