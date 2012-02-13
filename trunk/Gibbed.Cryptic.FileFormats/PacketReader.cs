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
    public class PacketReader : Serialization.IPacketReader
    {
        private Stream Input;

        public bool IsClient { get { return false; } }
        public bool IsServer { get { return false; } }

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

        public TType ReadNativeStructure<TType>(object state)
            where TType : Serialization.IStructure, new()
        {
            var instance = new TType();
            instance.Deserialize(this, state);
            return instance;
        }

        private TType[] ReadNativeArray<TType>(
            int count, object state, Func<PacketReader, object, TType> readValue)
        {
            var array = new TType[count];
            for (uint i = 0; i < count; i++)
            {
                array[i] = readValue(this, state);
            }
            return array;
        }

        private List<TType> ReadNativeList<TType>(
            object state, Func<PacketReader, object, TType> readValue)
        {
            var unknownFlag = (bool)state;

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

        public byte ReadValueByte(object state)
        {
            var unknownFlag = (bool)state;

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

        public byte[] ReadArrayByte(int count, object state)
        {
            return this.ReadNativeArray<byte>(
                count, state, (a, b) => a.ReadValueByte(b));
        }

        public List<byte> ReadListByte(object state)
        {
            throw new NotImplementedException();
        }

        public short ReadValueInt16(object state)
        {
            var unknownFlag = (bool)state;

            if (unknownFlag == true)
            {
                return (short)this.ReadNativeInt32Packed();
            }
            else
            {
                if (this.ReadNativeBoolean() == true)
                {
                    if (this.ReadNativeBoolean() == true)
                    {
                        return (short)this.ReadNativeInt32Packed();
                    }
                    else
                    {
                        return (short)(-this.ReadNativeInt32Packed());
                    }
                }
                else
                {
                    return 0;
                }
            }
        }

        public short[] ReadArrayInt16(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<short> ReadListInt16(object state)
        {
            throw new NotImplementedException();
        }

        public int ReadValueInt32(object state)
        {
            var unknownFlag = (bool)state;

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

        public int[] ReadArrayInt32(int count, object state)
        {
            return this.ReadNativeArray<int>(
                count, state, (a, b) => a.ReadValueInt32(b));
        }

        public List<int> ReadListInt32(object state)
        {
            return this.ReadNativeList<int>(
                state, (a, b) => a.ReadValueInt32(b));
        }

        public long ReadValueInt64(object state)
        {
            var unknownFlag = (bool)state;

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

        public long[] ReadArrayInt64(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<long> ReadListInt64(object state)
        {
            throw new NotImplementedException();
        }

        public float ReadValueFloat(object state)
        {
            return this.Input.ReadValueF32();
        }

        public float[] ReadArrayFloat(int count, object state)
        {
            return this.ReadNativeArray<float>(
                count, state, (a, b) => a.ReadValueFloat(b));
        }

        public List<float> ReadListFloat(object state)
        {
            return this.ReadNativeList<float>(
                state, (a, b) => a.ReadValueFloat(b));
        }

        public string ReadValueString(object state)
        {
            return this.Input.ReadStringZ(Encoding.UTF8);
        }

        public string[] ReadArrayString(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<string> ReadListString(object state)
        {
            return this.ReadNativeList<string>(
                state, (a, b) => a.ReadValueString(b));
        }

        public string ReadValueCurrentFile(object state)
        {
            return this.ReadValueString(state);
        }

        public string[] ReadArrayCurrentFile(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<string> ReadListCurrentFile(object state)
        {
            throw new NotImplementedException();
        }

        public int ReadValueTimestamp(object state)
        {
            throw new NotImplementedException();
        }

        public int[] ReadArrayTimestamp(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<int> ReadListTimestamp(object state)
        {
            throw new NotImplementedException();
        }

        public int ReadValueLineNumber(object state)
        {
            throw new NotImplementedException();
        }

        public int[] ReadArrayLineNumber(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<int> ReadListLineNumber(object state)
        {
            throw new NotImplementedException();
        }

        public bool ReadValueBoolean(object state)
        {
            var value = this.ReadValueByte(state);
            if (value != 0 && value != 1)
            {
                throw new FormatException();
            }
            return value > 0;
        }

        public bool[] ReadArrayBoolean(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<bool> ReadListBoolean(object state)
        {
            throw new NotImplementedException();
        }

        public bool ReadValueBooleanFlag(object state)
        {
            throw new NotImplementedException();
        }

        public bool[] ReadArrayBooleanFlag(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<bool> ReadListBooleanFlag(object state)
        {
            throw new NotImplementedException();
        }

        public QUATPYR ReadValueQUATPYR(object state)
        {
            throw new NotImplementedException();
        }

        public QUATPYR[] ReadArrayQUATPYR(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<QUATPYR> ReadListQUATPYR(object state)
        {
            throw new NotImplementedException();
        }

        public MATPYR ReadValueMATPYR(object state)
        {
            throw new NotImplementedException();
        }

        public MATPYR[] ReadArrayMATPYR(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<MATPYR> ReadListMATPYR(object state)
        {
            throw new NotImplementedException();
        }

        public string ReadValueFilename(object state)
        {
            throw new NotImplementedException();
        }

        public string[] ReadArrayFilename(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<string> ReadListFilename(object state)
        {
            throw new NotImplementedException();
        }

        public string ReadValueReference(object state)
        {
            return this.Input.ReadStringZ(Encoding.UTF8);
        }

        public string[] ReadArrayReference(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<string> ReadListReference(object state)
        {
            throw new NotImplementedException();
        }

        public FunctionCall ReadValueFunctionCall(object state)
        {
            throw new NotImplementedException();
        }

        public FunctionCall[] ReadArrayFunctionCall(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<FunctionCall> ReadListFunctionCall(object state)
        {
            throw new NotImplementedException();
        }

        public TType ReadValueStructure<TType>(bool optional, object state)
            where TType : Serialization.IStructure, new()
        {
            if (this.ReadNativeBoolean() == true)
            {
                var instance = new TType();
                instance.Deserialize(this, state);
                return instance;
            }
            else
            {
                if (optional == false)
                {
                    //throw new InvalidOperationException();
                }

                return default(TType);
            }
        }

        public TType[] ReadArrayStructure<TType>(int count, object state)
            where TType : Serialization.IStructure, new()
        {
            throw new NotImplementedException();
        }

        public List<TType> ReadListStructure<TType>(object state)
            where TType : Serialization.IStructure, new()
        {
            return this.ReadNativeList<TType>(
                state, (a, b) => a.ReadValueStructure<TType>(false, b));
        }

        public object ReadValuePolymorph(Type[] validTypes, bool optional, object state)
        {
            if (this.ReadNativeBoolean() == true)
            {
                var unk = this.ReadNativeBoolean();
                var index = this.ReadNativeInt32Packed();
                var type = validTypes[index];

                var instance = (Serialization.IStructure)Activator.CreateInstance(type);
                instance.Deserialize(this, state);
                return instance;
            }
            else
            {
                if (optional == false)
                {
                    //throw new InvalidOperationException();
                }

                return null;
            }
        }

        public object[] ReadArrayPolymorph(Type[] validTypes, int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<object> ReadListPolymorph(Type[] validTypes, object state)
        {
            return this.ReadNativeList<object>(
                state, (a, b) => a.ReadValuePolymorph(validTypes, false, b));
        }

        public StashTable ReadValueStashTable(object state)
        {
            throw new NotImplementedException();
        }

        public StashTable[] ReadArrayStashTable(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<StashTable> ReadListStashTable(object state)
        {
            throw new NotImplementedException();
        }

        public uint ReadValueBit(int bitOffset, object state)
        {
            var bits = (bitOffset & 0xFFFF0000) >> 16;
            if (bits > 32)
            {
                throw new InvalidOperationException();
            }

            uint value = 0;
            for (int i = 0; i < bits; i += 8)
            {
                value |= (uint)this.Input.ReadValueU8() << i;
            }
            return value;
        }

        public uint[] ReadArrayBit(int bitOffset, int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<uint> ReadListBit(int bitOffset, object state)
        {
            throw new NotImplementedException();
        }

        public MultiValue ReadValueMultiValue(object state)
        {
            MultiValueOpcode op;
            var _ = this.Input.ReadValueU32();
            if (MultiValue.TryParseOpcode(_, out op) == false)
            {
                throw new FormatException();
            }

            object arg;
            switch (op & MultiValueOpcode.TypeMask)
            {
                case MultiValueOpcode.NON:
                {
                    arg = null;
                    break;
                }

                case MultiValueOpcode.StaticVariable:
                {
                    StaticVariableType sv;
                    if (MultiValue.TryParseStaticVariable(this.ReadNativeUInt32Packed(), out sv) == false)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "multival {0} had an unexpected static variable index",
                                op));
                    }
                    arg = sv;
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
                {
                    arg = this.Input.ReadStringZ(Encoding.UTF8);
                    break;
                }

                default:
                {
                    throw new NotSupportedException("unhandled opcode in multival");
                }
            }

            return new MultiValue()
            {
                Op = op,
                Arg = arg,
            };
        }

        public MultiValue[] ReadArrayMultiValue(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<MultiValue> ReadListMultiValue(object state)
        {
            return this.ReadNativeList<MultiValue>(
                state, (a, b) => a.ReadValueMultiValue(b));
        }

        public TType ReadValueByteEnum<TType>(object state)
        {
            return (TType)Enum.ToObject(typeof(TType), this.ReadValueByte(state));
        }

        public TType[] ReadArrayByteEnum<TType>(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<TType> ReadListByteEnum<TType>(object state)
        {
            throw new NotImplementedException();
        }

        public TType ReadValueInt16Enum<TType>(object state)
        {
            throw new NotImplementedException();
        }

        public TType[] ReadArrayInt16Enum<TType>(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<TType> ReadListInt16Enum<TType>(object state)
        {
            throw new NotImplementedException();
        }

        public TType ReadValueInt32Enum<TType>(object state)
        {
            return (TType)Enum.ToObject(typeof(TType), this.ReadValueInt32(state));
        }

        public TType[] ReadArrayInt32Enum<TType>(int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<TType> ReadListInt32Enum<TType>(object state)
        {
            return this.ReadNativeList<TType>(
                state, (a, b) => a.ReadValueInt32Enum<TType>(b));
        }

        public TType ReadValueBitEnum<TType>(int bitOffset, object state)
        {
            return (TType)Enum.ToObject(typeof(TType), this.ReadValueBit(bitOffset, state));
        }

        public TType[] ReadArrayBitEnum<TType>(int bitOffset, int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<TType> ReadListBitEnum<TType>(int bitOffset, object state)
        {
            throw new NotImplementedException();
        }
    }
}
