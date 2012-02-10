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
    public interface ICrypticPacketReader
    {
        bool ReadNativeBoolean();
        int ReadNativeInt32Packed();
        TType ReadNativeStructure<TType>()
            where TType : ICrypticStructure, new();

        byte ReadValueByte(bool unknownFlag);
        byte[] ReadArrayByte(int count, bool unknownFlag);
        List<byte> ReadListByte(bool unknownFlag);

        short ReadValueInt16(bool unknownFlag);
        short[] ReadArrayInt16(int count, bool unknownFlag);
        List<short> ReadListInt16(bool unknownFlag);

        int ReadValueInt32(bool unknownFlag);
        int[] ReadArrayInt32(int count, bool unknownFlag);
        List<int> ReadListInt32(bool unknownFlag);

        long ReadValueInt64(bool unknownFlag);
        long[] ReadArrayInt64(int count, bool unknownFlag);
        List<long> ReadListInt64(bool unknownFlag);

        float ReadValueFloat(bool unknownFlag);
        float[] ReadArrayFloat(int count, bool unknownFlag);
        List<float> ReadListFloat(bool unknownFlag);

        string ReadValueString(bool unknownFlag);
        string[] ReadArrayString(int count, bool unknownFlag);
        List<string> ReadListString(bool unknownFlag);

        string ReadValueCurrentFile(bool unknownFlag);
        string[] ReadArrayCurrentFile(int count, bool unknownFlag);
        List<string> ReadListCurrentFile(bool unknownFlag);

        int ReadValueTimestamp(bool unknownFlag);
        int[] ReadArrayTimestamp(int count, bool unknownFlag);
        List<int> ReadListTimestamp(bool unknownFlag);

        int ReadValueLineNumber(bool unknownFlag);
        int[] ReadArrayLineNumber(int count, bool unknownFlag);
        List<int> ReadListLineNumber(bool unknownFlag);

        bool ReadValueBoolean(bool unknownFlag);
        bool[] ReadArrayBoolean(int count, bool unknownFlag);
        List<bool> ReadListBoolean(bool unknownFlag);

        bool ReadValueBooleanFlag(bool unknownFlag);
        bool[] ReadArrayBooleanFlag(int count, bool unknownFlag);
        List<bool> ReadListBooleanFlag(bool unknownFlag);

        QUATPYR ReadValueQUATPYR(bool unknownFlag);
        QUATPYR[] ReadArrayQUATPYR(int count, bool unknownFlag);
        List<QUATPYR> ReadListQUATPYR(bool unknownFlag);

        MATPYR ReadValueMATPYR(bool unknownFlag);
        MATPYR[] ReadArrayMATPYR(int count, bool unknownFlag);
        List<MATPYR> ReadListMATPYR(bool unknownFlag);

        string ReadValueFilename(bool unknownFlag);
        string[] ReadArrayFilename(int count, bool unknownFlag);
        List<string> ReadListFilename(bool unknownFlag);

        string ReadValueReference(bool unknownFlag);
        string[] ReadArrayReference(int count, bool unknownFlag);
        List<string> ReadListReference(bool unknownFlag);

        FunctionCall ReadValueFunctionCall(bool unknownFlag);
        FunctionCall[] ReadArrayFunctionCall(int count, bool unknownFlag);
        List<FunctionCall> ReadListFunctionCall(bool unknownFlag);

        TType ReadValueStructure<TType>(bool optional, bool unknownFlag) where TType : ICrypticStructure, new();
        TType[] ReadArrayStructure<TType>(int count, bool unknownFlag) where TType : ICrypticStructure, new();
        List<TType> ReadListStructure<TType>(bool unknownFlag) where TType : ICrypticStructure, new();

        object ReadValuePolymorph(Type[] validTypes, bool optional, bool unknownFlag);
        object[] ReadArrayPolymorph(Type[] validTypes, int count, bool unknownFlag);
        List<object> ReadListPolymorph(Type[] validTypes, bool unknownFlag);

        StashTable ReadValueStashTable(bool unknownFlag);
        StashTable[] ReadArrayStashTable(int count, bool unknownFlag);
        List<StashTable> ReadListStashTable(bool unknownFlag);

        uint ReadValueBit(int bitOffset, bool unknownFlag);
        uint[] ReadArrayBit(int bitOffset, int count, bool unknownFlag);
        List<uint> ReadListBit(int bitOffset, bool unknownFlag);

        MultiValue ReadValueMultiValue(bool unknownFlag);
        MultiValue[] ReadArrayMultiValue(int count, bool unknownFlag);
        List<MultiValue> ReadListMultiValue(bool unknownFlag);

        TType ReadValueByteEnum<TType>(bool unknownFlag);
        TType[] ReadArrayByteEnum<TType>(int count, bool unknownFlag);
        List<TType> ReadListByteEnum<TType>(bool unknownFlag);

        TType ReadValueInt16Enum<TType>(bool unknownFlag);
        TType[] ReadArrayInt16Enum<TType>(int count, bool unknownFlag);
        List<TType> ReadListInt16Enum<TType>(bool unknownFlag);

        TType ReadValueInt32Enum<TType>(bool unknownFlag);
        TType[] ReadArrayInt32Enum<TType>(int count, bool unknownFlag);
        List<TType> ReadListInt32Enum<TType>(bool unknownFlag);

        TType ReadValueBitEnum<TType>(bool unknownFlag);
        TType[] ReadArrayBitEnum<TType>(int count, bool unknownFlag);
        List<TType> ReadListBitEnum<TType>(bool unknownFlag);
    }
}
