/* Copyright (c) 2021 Rick (rick 'at' gibbed 'dot' us)
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
using System.Xml;

namespace Gibbed.Cryptic.ExportSchemas.Exporters
{
    internal static class FormatStringExporter
    {
        public static void Export(XmlWriter xml, string formatString)
        {
            if (formatString == null)
            {
                throw new ArgumentNullException(nameof(formatString));
            }

            Dictionary<string, string> formatStrings;
            try
            {
                formatStrings = Parse(formatString);
            }
            catch (FormatException)
            {
                xml.WriteComment($" failed to parse format string: {formatString} ");
                return;
            }

            foreach (var kv in formatStrings)
            {
                xml.WriteStartElement("format_string");
                xml.WriteAttributeString("name", kv.Key);
                xml.WriteValue(kv.Value);
                xml.WriteEndElement();
            }
        }

        private static Dictionary<string, string> Parse(string formatString)
        {
            if (formatString == null)
            {
                throw new ArgumentNullException(nameof(formatString));
            }

            var tokens = Tokenize(formatString, 256);
            var formatStrings = new Dictionary<string, string>();
            for (int i = 0; i < tokens.Length;)
            {
                if (i + 2 >= tokens.Length)
                {
                    throw new FormatException();
                }

                if (tokens[i + 1] != "=")
                {
                    throw new FormatException();
                }

                var key = tokens[i + 0];
                var value = tokens[i + 2];

                if (value.StartsWith("\"") == true)
                {
                    value = value.Substring(1, value.Length - 2);
                    formatStrings.Add(key, value);
                }
                else if (value.StartsWith("<&") == true)
                {
                    // todo: figure it out
                    throw new NotSupportedException();
                }
                else
                {
                    int dummy;
                    if (int.TryParse(value, out dummy) == false)
                    {
                        throw new FormatException();
                    }
                    /* normally I would store the value as an int somehow, but
                     * it's easier to just parse the value on demand later
                     */
                    formatStrings.Add(key, value);
                }

                i += 3;
                if (i < tokens.Length)
                {
                    if (tokens[i] != ",")
                    {
                        throw new FormatException();
                    }
                    i++;
                }
            }
            return formatStrings;
        }

        private static string[] Tokenize(string text, int maxTokens)
        {
            var tokens = new List<string>();
            for (int i = 0; i < text.Length;)
            {
                if (IsTokenWhitespace(text[i]) == true)
                {
                    i++;
                }
                else if (text[i] == '"')
                {
                    if (tokens.Count >= maxTokens)
                    {
                        throw new InvalidOperationException();
                    }

                    var tokenStart = i;
                    i++;

                    for (; i < text.Length;)
                    {
                        if (text[i] == '"')
                        {
                            break;
                        }

                        if (text[i] == '\\')
                        {
                            if (i + 1 >= text.Length)
                            {
                                throw new FormatException();
                            }

                            i += 2;
                        }
                        else
                        {
                            i++;
                        }
                    }

                    if (i >= text.Length ||
                        text[i] != '"')
                    {
                        throw new FormatException();
                    }
                    i++;

                    tokens.Add(text.Substring(tokenStart, i - tokenStart));

                    if (i < text.Length &&
                        IsTokenWhitespace(text[i]) == false)
                    {
                        throw new FormatException();
                    }
                    i++;
                }
                else if (i + 1 >= text.Length ||
                         text[i + 0] != '<' ||
                         text[i + 1] != '&')
                {
                    if (tokens.Count >= maxTokens)
                    {
                        throw new InvalidOperationException();
                    }

                    var tokenStart = i;
                    i++;

                    for (; i < text.Length;)
                    {
                        if (IsTokenWhitespace(text[i]) == true)
                        {
                            break;
                        }

                        i++;
                    }

                    tokens.Add(text.Substring(tokenStart, i - tokenStart));

                    i++;
                }
                else
                {
                    if (tokens.Count >= maxTokens)
                    {
                        throw new InvalidOperationException();
                    }

                    var end = text.IndexOf("&>", i + 2, StringComparison.Ordinal);
                    if (end < 0)
                    {
                        throw new FormatException();
                    }

                    tokens.Add(text.Substring(i, end + 2 - i));

                    i = end + 2;
                    if (i < text.Length &&
                        IsTokenWhitespace(text[i]) == false)
                    {
                        throw new FormatException();
                    }
                }
            }
            return tokens.ToArray();
        }

        private static bool IsTokenWhitespace(char c)
        {
            return c == ' ' ||
                   c == '\t' ||
                   c == '\n' ||
                   c == '\r';
        }
    }
}
