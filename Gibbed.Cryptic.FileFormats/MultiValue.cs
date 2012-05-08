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
        private static readonly Dictionary<string, StaticVariableType> _NamesToStaticVariables;
        private static readonly Dictionary<uint, StaticVariableType> _ValuesToStaticVariables;

        public static bool TryParseStaticVariable(string name, out StaticVariableType sv)
        {
            if (_NamesToStaticVariables.ContainsKey(name) == false)
            {
                sv = StaticVariableType.Activation;
                return false;
            }

            sv = _NamesToStaticVariables[name];
            return true;
        }

        public static bool TryParseStaticVariable(uint value, out StaticVariableType sv)
        {
            if (_ValuesToStaticVariables.ContainsKey(value) == false)
            {
                sv = StaticVariableType.Activation;
                return false;
            }

            sv = _ValuesToStaticVariables[value];
            return true;
        }

        private static readonly Dictionary<string, MultiValueOpcode> _NamesToOpcodes;
        private static readonly Dictionary<uint, MultiValueOpcode> _ValuesToOpcodes;

        public static bool TryParseOpcode(string name, out MultiValueOpcode op)
        {
            if (_NamesToOpcodes.ContainsKey(name) == false)
            {
                op = MultiValueOpcode.INV;
                return false;
            }

            op = _NamesToOpcodes[name];
            return true;
        }

        public static bool TryParseOpcode(uint value, out MultiValueOpcode op)
        {
            if (_ValuesToOpcodes.ContainsKey(value) == false)
            {
                op = MultiValueOpcode.INV;
                return false;
            }

            op = _ValuesToOpcodes[value];
            return true;
        }

        static MultiValue()
        {
            _NamesToOpcodes = new Dictionary<string, MultiValueOpcode>();
            _ValuesToOpcodes = new Dictionary<uint, MultiValueOpcode>();
            foreach (MultiValueOpcode value in Enum.GetValues(typeof(MultiValueOpcode)))
            {
                var name = Enum.GetName(typeof(MultiValueOpcode), value);
                if (name.Length != 3 ||
                    name.ToUpperInvariant() != name)
                {
                    continue;
                }

                _NamesToOpcodes.Add(name, value);
                _ValuesToOpcodes.Add((uint)value, value);
            }

            _NamesToStaticVariables = new Dictionary<string, StaticVariableType>();
            _ValuesToStaticVariables = new Dictionary<uint, StaticVariableType>();
            foreach (StaticVariableType value in Enum.GetValues(typeof(StaticVariableType)))
            {
                var name = Enum.GetName(typeof(StaticVariableType), value);
                _NamesToStaticVariables.Add(name, value);
                _ValuesToStaticVariables.Add((uint)value, value);
            }
        }

        public MultiValueOpcode Op;
        public string OpName { get { return Enum.GetName(typeof(MultiValueOpcode), this.Op); } }

        public object Arg;
    }
}
