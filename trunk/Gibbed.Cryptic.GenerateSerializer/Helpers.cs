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
using System.Linq;
using System.Reflection;
using Gibbed.Cryptic.FileFormats;
using Parser = Gibbed.Cryptic.FileFormats.Parser;
using ParserSchema = Gibbed.Cryptic.FileFormats.ParserSchema;
using Serialization = Gibbed.Cryptic.FileFormats.Serialization;

namespace Gibbed.Cryptic.GenerateSerializer
{
    internal static class Helpers
    {
        public static string GetColumnName(
            ParserSchema.Table table,
            ParserSchema.Column column)
        {
            if (string.IsNullOrEmpty(column.Name) == true)
            {
                if (table.Columns.Count(c => string.IsNullOrEmpty(c.Name)) > 1)
                {
                    return "__unnamed_" + column.Offset.ToString("X");
                }

                return "_";
            }

            if (table.Columns.Count(c => c != column && c.Name.ToLowerInvariant() == column.Name.ToLowerInvariant()) > 1)
            {
                throw new InvalidOperationException();
            }

            return column.Name;
        }

        public static bool IsGoodColumn(ParserSchema.Column column)
        {
            if (column.Flags.HasAnyOptions(Parser.ColumnFlags.REDUNDANTNAME |
                                           Parser.ColumnFlags.UNOWNED |
                                           Parser.ColumnFlags.NO_WRITE) == true)
            {
                return false;
            }

            if (column.Token == Parser.TokenType.Ignore ||
                column.Token == Parser.TokenType.Start ||
                column.Token == Parser.TokenType.End ||
                column.Token == Parser.TokenType.Command)
            {
                return false;
            }

            if (string.IsNullOrEmpty(column.Name) == true)
            {
                return false;
            }

            return true;
        }

        public static MethodInfo GetReadMethod(ParserSchema.Column column)
        {
            var token = Parser.GlobalTokens.GetToken(column.Token);

            string name = "Read";

            if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY) == false)
            {
                name += "Value";
            }
            else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
            {
                name += "Array";
            }
            else
            {
                name += "List";
            }

            name += token.GetType().Name;

            if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                column.StaticDefineList != null)
            {
                name += "Enum";
            }

            var methodInfo = typeof(Serialization.IBaseReader).GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Instance);
            if (methodInfo == null)
            {
                throw new NotSupportedException(name + " is missing");
            }

            return methodInfo;
        }

        public static MethodInfo GetWriteMethod(ParserSchema.Column column)
        {
            var token = Parser.GlobalTokens.GetToken(column.Token);

            string name = "Write";

            if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY) == false)
            {
                name += "Value";
            }
            else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
            {
                name += "Array";
            }
            else
            {
                name += "List";
            }

            name += token.GetType().Name;

            if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                column.StaticDefineList != null)
            {
                name += "Enum";
            }

            var methodInfo = typeof(Serialization.IBaseWriter).GetMethod(
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
