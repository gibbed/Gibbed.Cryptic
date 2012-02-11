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
using System.IO;
using System.Text;
using Gibbed.IO;

namespace Gibbed.Cryptic.FileFormats
{
    public class PacketReader : ICrypticPacketReader
    {
        private Stream Input;

        public PacketReader(Stream input)
        {
            this.Input = input;
        }

        public bool ReadNativeBoolean()
        {
            var value = this.Input.ReadValueU8();
            if (value != 0 && value != 1)
            {
                throw new FormatException();
            }
            return value > 0;
        }

        public int ReadNativeInt32Packed()
        {
            return (int)this.ReadNativeUInt32Packed();
        }

        public uint ReadNativeUInt32Packed()
        {
            var first = this.Input.ReadValueU8();
            uint value = 0;

            var size = (byte)(first & 3);
            value |= (byte)((first >> 2) & 0x3F);

            if (size == 1)
            {
                value |= (uint)this.Input.ReadValueU8() << 6;
            }
            else if (size == 2)
            {
                value |= (uint)this.Input.ReadValueU16() << 6;
            }
            else if (size == 3)
            {
                value |= (uint)this.Input.ReadValueU32() << 6;
            }

            return value;
        }

        public TType ReadNativeStructure<TType>()
            where TType : ICrypticStructure, new()
        {
            var instance = new TType();
            instance.Serialize(this, false);
            return instance;
        }

        private TType[] ReadNativeArray<TType>(
            int count, Func<ICrypticPacketReader, bool, TType> readValue, bool unknownFlag)
        {
            var array = new TType[count];
            for (uint i = 0; i < count; i++)
            {
                array[i] = readValue(this, unknownFlag);
            }
            return array;
        }

        private List<TType> ReadNativeList<TType>(
            Func<ICrypticPacketReader, bool, TType> readValue, bool unknownFlag)
        {
            var indexed = this.ReadNativeBoolean();
            if (indexed == true)
            {
                throw new NotImplementedException();
            }
            else
            {
                var count = this.Input.ReadValueS32();
                if (count < 0 || count > 8192)
                {
                    throw new FormatException();
                }

                var newUnknownFlag = this.ReadNativeBoolean();

                var list = new List<TType>();
                for (uint i = 0; i < count; i++)
                {
                    list.Add(readValue(this, unknownFlag || newUnknownFlag));
                }
                return list;
            }
        }

        public byte ReadValueByte(bool unknownFlag)
        {
            if (unknownFlag == true)
            {
                return (byte)this.ReadNativeInt32Packed();
            }
            else
            {
                if (this.ReadNativeBoolean() == true)
                {
                    if (this.ReadNativeBoolean() == true)
                    {
                        return (byte)this.ReadNativeInt32Packed();
                    }
                    else
                    {
                        return (byte)(-this.ReadNativeInt32Packed());
                    }
                }
                else
                {
                    return 0;
                }
            }
        }

        public byte[] ReadArrayByte(int count, bool unknownFlag)
        {
            return this.ReadNativeArray<byte>(count,
                (a, b) => a.ReadValueByte(b), unknownFlag);
        }

        public List<byte> ReadListByte(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public short ReadValueInt16(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public short[] ReadArrayInt16(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<short> ReadListInt16(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public int ReadValueInt32(bool unknownFlag)
        {
            if (unknownFlag == true)
            {
                return this.ReadNativeInt32Packed();
            }
            else
            {
                if (this.ReadNativeBoolean() == true)
                {
                    if (this.ReadNativeBoolean() == true)
                    {
                        return (int)this.ReadNativeInt32Packed();
                    }
                    else
                    {
                        return -((int)this.ReadNativeInt32Packed());
                    }
                }
                else
                {
                    return 0;
                }
            }
        }

        public int[] ReadArrayInt32(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<int> ReadListInt32(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public long ReadValueInt64(bool unknownFlag)
        {
            if (unknownFlag == true)
            {
                var lo = this.ReadNativeUInt32Packed();
                var hi = this.ReadNativeUInt32Packed();

                return (long)(((ulong)hi << 32) | (ulong)lo);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public long[] ReadArrayInt64(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<long> ReadListInt64(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public float ReadValueFloat(bool unknownFlag)
        {
            return this.Input.ReadValueF32();
        }

        public float[] ReadArrayFloat(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<float> ReadListFloat(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public string ReadValueString(bool unknownFlag)
        {
            return this.Input.ReadStringZ(Encoding.UTF8);
        }

        public string[] ReadArrayString(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<string> ReadListString(bool unknownFlag)
        {
            return this.ReadNativeList<string>((a, b) => a.ReadValueString(b), unknownFlag);
        }

        public string ReadValueCurrentFile(bool unknownFlag)
        {
            return this.ReadValueString(unknownFlag);
        }

        public string[] ReadArrayCurrentFile(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<string> ReadListCurrentFile(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public int ReadValueTimestamp(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public int[] ReadArrayTimestamp(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<int> ReadListTimestamp(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public int ReadValueLineNumber(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public int[] ReadArrayLineNumber(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<int> ReadListLineNumber(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public bool ReadValueBoolean(bool unknownFlag)
        {
            var value = this.ReadValueByte(unknownFlag);
            if (value != 0 && value != 1)
            {
                throw new FormatException();
            }
            return value > 0;
        }

        public bool[] ReadArrayBoolean(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<bool> ReadListBoolean(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public bool ReadValueBooleanFlag(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public bool[] ReadArrayBooleanFlag(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<bool> ReadListBooleanFlag(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public QUATPYR ReadValueQUATPYR(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public QUATPYR[] ReadArrayQUATPYR(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<QUATPYR> ReadListQUATPYR(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public MATPYR ReadValueMATPYR(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public MATPYR[] ReadArrayMATPYR(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<MATPYR> ReadListMATPYR(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public string ReadValueFilename(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public string[] ReadArrayFilename(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<string> ReadListFilename(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public string ReadValueReference(bool unknownFlag)
        {
            return this.Input.ReadStringZ(Encoding.UTF8);
        }

        public string[] ReadArrayReference(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<string> ReadListReference(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public FunctionCall ReadValueFunctionCall(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public FunctionCall[] ReadArrayFunctionCall(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<FunctionCall> ReadListFunctionCall(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public TType ReadValueStructure<TType>(bool optional, bool unknownFlag) where TType : ICrypticStructure, new()
        {
            if (this.ReadNativeBoolean() == true)
            {
                var instance = new TType();
                instance.Serialize(this, unknownFlag);
                return instance;
            }
            else
            {
                return default(TType);
            }
        }

        public TType[] ReadArrayStructure<TType>(int count, bool unknownFlag) where TType : ICrypticStructure, new()
        {
            throw new NotImplementedException();
        }

        public List<TType> ReadListStructure<TType>(bool unknownFlag) where TType : ICrypticStructure, new()
        {
            return this.ReadNativeList<TType>(
                (a, b) => a.ReadValueStructure<TType>(false, b), unknownFlag);
        }

        public object ReadValuePolymorph(Type[] validTypes, bool optional, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public object[] ReadArrayPolymorph(Type[] validTypes, int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<object> ReadListPolymorph(Type[] validTypes, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public StashTable ReadValueStashTable(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public StashTable[] ReadArrayStashTable(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<StashTable> ReadListStashTable(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public uint ReadValueBit(int bitOffset, bool unknownFlag)
        {
            var bits = (bitOffset & 0xFFFF0000) >> 16;
            if (bits > 32)
            {
                throw new InvalidOperationException();
            }

            var bytes = bits % 8;
            uint value = 0;
            for (int i = 0, j = 0; i < bytes; i++, j += 8)
            {
                value |= (uint)this.Input.ReadValueU8() << j;
            }
            return value;
        }

        public uint[] ReadArrayBit(int bitOffset, int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<uint> ReadListBit(int bitOffset, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public MultiValue ReadValueMultiValue(bool unknownFlag)
        {
            MultiValueOpcode op;
            var _ = this.Input.ReadValueU32();
            if (MultiValue.TryParseOpcode(_, out op) == false)
            {
                throw new FormatException();
            }

            object arg;
            switch (op)
            {
                case MultiValueOpcode.O_P:
                case MultiValueOpcode.C_P:
                case MultiValueOpcode.MUL:
                case MultiValueOpcode.DIV:
                {
                    arg = null;
                    break;
                }

                case MultiValueOpcode.INT:
                {
                    var lo = this.ReadNativeUInt32Packed();
                    var hi = this.ReadNativeUInt32Packed();
                    arg = (long)(((ulong)hi << 32) | (ulong)lo);
                    break;
                }

                case MultiValueOpcode.FLT:
                {
                    var lo = this.ReadNativeUInt32Packed();
                    var hi = this.ReadNativeUInt32Packed();
                    arg = BitConverter.Int64BitsToDouble(((long)hi << 32) | (long)lo);
                    break;
                }

                case MultiValueOpcode.STR:
                case MultiValueOpcode.FUN:
                {
                    arg = this.Input.ReadStringZ(Encoding.UTF8);
                    break;
                }

                default: throw new NotSupportedException("unhandled opcode in multival");
            }

            return new MultiValue()
            {
                Op = op,
                Arg = arg,
            };
        }

        public MultiValue[] ReadArrayMultiValue(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<MultiValue> ReadListMultiValue(bool unknownFlag)
        {
            return this.ReadNativeList<MultiValue>(
                (a, b) => a.ReadValueMultiValue(unknownFlag), unknownFlag);
        }

        public TType ReadValueByteEnum<TType>(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public TType[] ReadArrayByteEnum<TType>(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<TType> ReadListByteEnum<TType>(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public TType ReadValueInt16Enum<TType>(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public TType[] ReadArrayInt16Enum<TType>(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<TType> ReadListInt16Enum<TType>(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public TType ReadValueInt32Enum<TType>(bool unknownFlag)
        {
            return (TType)Enum.ToObject(typeof(TType), this.ReadValueInt32(unknownFlag));
        }

        public TType[] ReadArrayInt32Enum<TType>(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<TType> ReadListInt32Enum<TType>(bool unknownFlag)
        {
            return this.ReadNativeList<TType>(
                (a, b) => a.ReadValueInt32Enum<TType>(b), unknownFlag);
        }

        public TType ReadValueBitEnum<TType>(bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public TType[] ReadArrayBitEnum<TType>(int count, bool unknownFlag)
        {
            throw new NotImplementedException();
        }

        public List<TType> ReadListBitEnum<TType>(bool unknownFlag)
        {
            throw new NotImplementedException();
        }
    }
}
