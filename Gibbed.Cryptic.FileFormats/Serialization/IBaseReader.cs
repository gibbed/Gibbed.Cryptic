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
    public interface IBaseReader
    {
        bool IsClient { get; }
        bool IsServer { get; }

        byte ReadValueByte(object state);
        byte[] ReadArrayByte(int count, object state);
        List<byte> ReadListByte(object state);

        short ReadValueInt16(object state);
        short[] ReadArrayInt16(int count, object state);
        List<short> ReadListInt16(object state);

        int ReadValueInt32(object state);
        int[] ReadArrayInt32(int count, object state);
        List<int> ReadListInt32(object state);

        long ReadValueInt64(object state);
        long[] ReadArrayInt64(int count, object state);
        List<long> ReadListInt64(object state);

        float ReadValueFloat(object state);
        float[] ReadArrayFloat(int count, object state);
        List<float> ReadListFloat(object state);

        string ReadValueString(object state);
        string[] ReadArrayString(int count, object state);
        List<string> ReadListString(object state);

        string ReadValueCurrentFile(object state);
        string[] ReadArrayCurrentFile(int count, object state);
        List<string> ReadListCurrentFile(object state);

        int ReadValueTimestamp(object state);
        int[] ReadArrayTimestamp(int count, object state);
        List<int> ReadListTimestamp(object state);

        int ReadValueLineNumber(object state);
        int[] ReadArrayLineNumber(int count, object state);
        List<int> ReadListLineNumber(object state);

        bool ReadValueBoolean(object state);
        bool[] ReadArrayBoolean(int count, object state);
        List<bool> ReadListBoolean(object state);

        bool ReadValueBooleanFlag(object state);
        bool[] ReadArrayBooleanFlag(int count, object state);
        List<bool> ReadListBooleanFlag(object state);

        QUATPYR ReadValueQUATPYR(object state);
        QUATPYR[] ReadArrayQUATPYR(int count, object state);
        List<QUATPYR> ReadListQUATPYR(object state);

        MATPYR ReadValueMATPYR(object state);
        MATPYR[] ReadArrayMATPYR(int count, object state);
        List<MATPYR> ReadListMATPYR(object state);

        string ReadValueFilename(object state);
        string[] ReadArrayFilename(int count, object state);
        List<string> ReadListFilename(object state);

        string ReadValueReference(object state);
        string[] ReadArrayReference(int count, object state);
        List<string> ReadListReference(object state);

        FunctionCall ReadValueFunctionCall(object state);
        FunctionCall[] ReadArrayFunctionCall(int count, object state);
        List<FunctionCall> ReadListFunctionCall(object state);

        TType ReadValueStructure<TType>(bool optional, object state) where TType : IStructure, new();
        TType[] ReadArrayStructure<TType>(int count, object state) where TType : IStructure, new();
        List<TType> ReadListStructure<TType>(object state) where TType : IStructure, new();

        object ReadValuePolymorph(Type[] validTypes, bool optional, object state);
        object[] ReadArrayPolymorph(Type[] validTypes, int count, object state);
        List<object> ReadListPolymorph(Type[] validTypes, object state);

        StashTable ReadValueStashTable(object state);
        StashTable[] ReadArrayStashTable(int count, object state);
        List<StashTable> ReadListStashTable(object state);

        uint ReadValueBit(int bitOffset, object state);
        uint[] ReadArrayBit(int bitOffset, int count, object state);
        List<uint> ReadListBit(int bitOffset, object state);

        MultiValue ReadValueMultiValue(object state);
        MultiValue[] ReadArrayMultiValue(int count, object state);
        List<MultiValue> ReadListMultiValue(object state);

        TType ReadValueByteEnum<TType>(object state);
        TType[] ReadArrayByteEnum<TType>(int count, object state);
        List<TType> ReadListByteEnum<TType>(object state);

        TType ReadValueInt16Enum<TType>(object state);
        TType[] ReadArrayInt16Enum<TType>(int count, object state);
        List<TType> ReadListInt16Enum<TType>(object state);

        TType ReadValueInt32Enum<TType>(object state);
        TType[] ReadArrayInt32Enum<TType>(int count, object state);
        List<TType> ReadListInt32Enum<TType>(object state);

        TType ReadValueBitEnum<TType>(int bitOffset, object state);
        TType[] ReadArrayBitEnum<TType>(int bitOffset, int count, object state);
        List<TType> ReadListBitEnum<TType>(int bitOffset, object state);
    }
}
