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

namespace Gibbed.Cryptic.FileFormats
{
    public class MultiValue
    {
        private static Dictionary<uint, StaticVariableType> ValuesToStaticVariables;

        public static bool TryParseStaticVariable(uint value, out StaticVariableType sv)
        {
            if (ValuesToStaticVariables.ContainsKey(value) == false)
            {
                sv = StaticVariableType.Activation;
                return false;
            }

            sv = ValuesToStaticVariables[value];
            return true;
        }

        private static Dictionary<string, MultiValueOpcode> NamesToOpcodes;
        private static Dictionary<uint, MultiValueOpcode> ValuesToOpcodes;

        public static bool TryParseOpcode(string name, out MultiValueOpcode op)
        {
            if (NamesToOpcodes.ContainsKey(name) == false)
            {
                op = MultiValueOpcode.INV;
                return false;
            }

            op = NamesToOpcodes[name];
            return true;
        }

        public static bool TryParseOpcode(uint value, out MultiValueOpcode op)
        {
            if (ValuesToOpcodes.ContainsKey(value) == false)
            {
                op = MultiValueOpcode.INV;
                return false;
            }

            op = ValuesToOpcodes[value];
            return true;
        }

        static MultiValue()
        {
            NamesToOpcodes = new Dictionary<string, MultiValueOpcode>();
            ValuesToOpcodes = new Dictionary<uint, MultiValueOpcode>();
            foreach (MultiValueOpcode value in Enum.GetValues(typeof(MultiValueOpcode)))
            {
                var name = Enum.GetName(typeof(MultiValueOpcode), value);
                if (name.Length != 3 ||
                    name.ToUpperInvariant() != name)
                {
                    continue;
                }

                NamesToOpcodes.Add(name, value);
                ValuesToOpcodes.Add((uint)value, value);
            }

            ValuesToStaticVariables = new Dictionary<uint, StaticVariableType>();
            foreach (StaticVariableType value in Enum.GetValues(typeof(StaticVariableType)))
            {
                ValuesToStaticVariables.Add((uint)value, value);
            }
        }

        public MultiValueOpcode Op;
        public string OpName { get { return Enum.GetName(typeof(MultiValueOpcode), this.Op); } }

        public object Arg;
    }
}
