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

namespace Gibbed.Cryptic.FileFormats.Serialization
{
    public interface IBaseWriter
    {
        bool IsClient { get; }
        bool IsServer { get; }

        void WriteValueByte(byte value, object state);
        void WriteArrayByte(byte[] array, int count, object state);
        void WriteListByte(List<byte> list, object state);

        void WriteValueInt16(short value, object state);
        void WriteArrayInt16(short[] array, int count, object state);
        void WriteListInt16(List<short> list, object state);

        void WriteValueInt32(int value, object state);
        void WriteArrayInt32(int[] array, int count, object state);
        void WriteListInt32(List<int> list, object state);

        void WriteValueInt64(long value, object state);
        void WriteArrayInt64(long[] array, int count, object state);
        void WriteListInt64(List<long> list, object state);

        void WriteValueFloat(float value, object state);
        void WriteArrayFloat(float[] array, int count, object state);
        void WriteListFloat(List<float> list, object state);

        void WriteValueString(string value, object state);
        void WriteArrayString(string[] array, int count, object state);
        void WriteListString(List<string> list, object state);

        void WriteValueCurrentFile(string value, object state);
        void WriteArrayCurrentFile(string[] array, int count, object state);
        void WriteListCurrentFile(List<string> list, object state);

        void WriteValueTimestamp(int value, object state);
        void WriteArrayTimestamp(int[] array, int count, object state);
        void WriteListTimestamp(List<int> list, object state);

        void WriteValueLineNumber(int value, object state);
        void WriteArrayLineNumber(int[] array, int count, object state);
        void WriteListLineNumber(List<int> list, object state);

        void WriteValueBoolean(bool value, object state);
        void WriteArrayBoolean(bool[] array, int count, object state);
        void WriteListBoolean(List<bool> list, object state);

        void WriteValueBooleanFlag(bool value, object state);
        void WriteArrayBooleanFlag(bool[] array, int count, object state);
        void WriteListBooleanFlag(List<bool> list, object state);

        void WriteValueQUATPYR(float value, object state);
        void WriteArrayQUATPYR(float[] array, int count, object state);
        void WriteListQUATPYR(List<float> list, object state);

        void WriteValueMATPYR(MATPYR value, object state);
        void WriteArrayMATPYR(MATPYR[] array, int count, object state);
        void WriteListMATPYR(List<MATPYR> list, object state);

        void WriteValueFilename(string value, object state);
        void WriteArrayFilename(string[] array, int count, object state);
        void WriteListFilename(List<string> list, object state);

        void WriteValueReference(string value, object state);
        void WriteArrayReference(string[] array, int count, object state);
        void WriteListReference(List<string> list, object state);

        void WriteValueFunctionCall(FunctionCall value, object state);
        void WriteArrayFunctionCall(FunctionCall[] array, int count, object state);
        void WriteListFunctionCall(List<FunctionCall> list, object state);

        void WriteValueStructure<TType>(TType value, bool optional, object state) where TType : IStructure, new();
        void WriteArrayStructure<TType>(TType[] array, int count, object state) where TType : IStructure, new();
        void WriteListStructure<TType>(List<TType> list, object state) where TType : IStructure, new();

        void WriteValuePolymorph(object value, Type[] validTypes, bool optional, object state);
        void WriteArrayPolymorph(object[] array, Type[] validTypes, int count, object state);
        void WriteListPolymorph(List<object> list, Type[] validTypes, object state);

        void WriteValueStashTable(StashTable value, object state);
        void WriteArrayStashTable(StashTable[] array, int count, object state);
        void WriteListStashTable(List<StashTable> list, object state);

        void WriteValueBit(uint value, int bitOffset, object state);
        void WriteArrayBit(uint[] array, int bitOffset, int count, object state);
        void WriteListBit(List<uint> list, int bitOffset, object state);

        void WriteValueMultiValue(MultiValue value, object state);
        void WriteArrayMultiValue(MultiValue[] array, int count, object state);
        void WriteListMultiValue(List<MultiValue> list, object state);

        void WriteValueByteEnum<TType>(TType value, object state);
        void WriteArrayByteEnum<TType>(TType[] array, int count, object state);
        void WriteListByteEnum<TType>(List<TType> list, object state);

        void WriteValueInt16Enum<TType>(TType value, object state);
        void WriteArrayInt16Enum<TType>(TType[] array, int count, object state);
        void WriteListInt16Enum<TType>(List<TType> list, object state);

        void WriteValueInt32Enum<TType>(TType value, object state);
        void WriteArrayInt32Enum<TType>(TType[] array, int count, object state);
        void WriteListInt32Enum<TType>(List<TType> list, object state);

        void WriteValueBitEnum<TType>(TType value, int bitOffset, object state);
        void WriteArrayBitEnum<TType>(TType[] array, int bitOffset, int count, object state);
        void WriteListBitEnum<TType>(List<TType> list, int bitOffset, object state);
    }
}
