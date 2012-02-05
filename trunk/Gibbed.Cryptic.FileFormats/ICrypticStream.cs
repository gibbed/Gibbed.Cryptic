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
        void SerializeArrayByte(ref byte[] array, int count);
        void SerializeListByte(ref List<byte> list);

        void SerializeValueInt16(ref short value);
        void SerializeArrayInt16(ref short[] array, int count);
        void SerializeListInt16(ref List<short> list);

        void SerializeValueInt32(ref int value);
        void SerializeArrayInt32(ref int[] array, int count);
        void SerializeListInt32(ref List<int> list);

        void SerializeValueInt64(ref long value);
        void SerializeArrayInt64(ref long[] array, int count);
        void SerializeListInt64(ref List<long> list);

        void SerializeValueFloat(ref float value);
        void SerializeArrayFloat(ref float[] array, int count);
        void SerializeListFloat(ref List<float> list);

        void SerializeValueString(ref string value);
        void SerializeArrayString(ref string[] array, int count);
        void SerializeListString(ref List<string> list);

        void SerializeValueCurrentFile(ref string value);
        void SerializeArrayCurrentFile(ref string[] array, int count);
        void SerializeListCurrentFile(ref List<string> list);

        void SerializeValueTimestamp(ref int value);
        void SerializeArrayTimestamp(ref int[] array, int count);
        void SerializeListTimestamp(ref List<int> list);

        void SerializeValueLineNumber(ref int value);
        void SerializeArrayLineNumber(ref int[] array, int count);
        void SerializeListLineNumber(ref List<int> list);

        void SerializeValueBoolean(ref bool value);
        void SerializeArrayBoolean(ref bool[] array, int count);
        void SerializeListBoolean(ref List<bool> list);

        void SerializeValueBooleanFlag(ref bool value);
        void SerializeArrayBooleanFlag(ref bool[] array, int count);
        void SerializeListBooleanFlag(ref List<bool> list);

        void SerializeValueQUATPYR(ref QUATPYR value);
        void SerializeArrayQUATPYR(ref QUATPYR[] array, int count);
        void SerializeListQUATPYR(ref List<QUATPYR> list);

        void SerializeValueMATPYR(ref MATPYR value);
        void SerializeArrayMATPYR(ref MATPYR[] array, int count);
        void SerializeListMATPYR(ref List<MATPYR> list);

        void SerializeValueFilename(ref string value);
        void SerializeArrayFilename(ref string[] array, int count);
        void SerializeListFilename(ref List<string> list);

        void SerializeValueReference(ref string value);
        void SerializeArrayReference(ref string[] array, int count);
        void SerializeListReference(ref List<string> list);

        void SerializeValueFunctionCall(ref FunctionCall value);
        void SerializeArrayFunctionCall(ref FunctionCall[] array, int count);
        void SerializeListFunctionCall(ref List<FunctionCall> list);

        void SerializeValueStructure<TType>(ref TType value, bool optional) where TType : ICrypticStructure, new();
        void SerializeArrayStructure<TType>(ref TType[] array, int count) where TType : ICrypticStructure, new();
        void SerializeListStructure<TType>(ref List<TType> list) where TType : ICrypticStructure, new();

        void SerializeValuePolymorph(ref object value, bool optional, Type[] validTypes);
        void SerializeArrayPolymorph(ref object[] array, int count, Type[] validTypes);
        void SerializeListPolymorph(ref List<object> list, Type[] validTypes);

        void SerializeValueStashTable(ref StashTable value);
        void SerializeArrayStashTable(ref StashTable[] array, int count);
        void SerializeListStashTable(ref List<StashTable> list);

        void SerializeValueBit(ref uint value);
        void SerializeArrayBit(ref uint[] array, int count);
        void SerializeListBit(ref List<uint> list);

        void SerializeValueMultiValue(ref MultiValue value);
        void SerializeArrayMultiValue(ref MultiValue[] array, int count);
        void SerializeListMultiValue(ref List<MultiValue> list);
    }
}
