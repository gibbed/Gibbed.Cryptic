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
    public interface ICrypticStream
    {
        void SerializeValueByte(ref byte value);
        void SerializeValueInt16(ref short value);
        void SerializeValueInt32(ref int value);
        void SerializeValueFloat(ref float value);
        void SerializeValueString(ref string value);
        void SerializeValueReference(ref string value);
        void SerializeValueCurrentFile(ref string value);
        void SerializeValueStructure<TType>(ref TType value, bool optional)
            where TType : ICrypticStructure, new();
        void SerializeValuePolymorph(ref object value, bool optional, Type[] validTypes);
        void SerializeValueBit(ref uint value);

        void SerializeArrayInt16(ref short[] array, int count);
        void SerializeArrayInt32(ref int[] array, int count);
        void SerializeArrayFloat(ref float[] array, int count);

        void SerializeListInt32(ref List<int> list);
        void SerializeListString(ref List<string> list);
        void SerializeListStructure<TType>(ref List<TType> list)
            where TType : ICrypticStructure, new();
        void SerializeListMultiValue(ref List<MultiValueInstruction> list);
    }
}
