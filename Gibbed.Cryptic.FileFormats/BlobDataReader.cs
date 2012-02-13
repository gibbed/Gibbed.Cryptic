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

#define ALLINMEMORY

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Gibbed.IO;

namespace Gibbed.Cryptic.FileFormats
{
    public class BlobDataReader : Serialization.IFileReader
    {
        private Stream Input;

        public bool IsClient { get { return false; } }
        public bool IsServer { get { return false; } }

        public BlobDataReader(Stream input)
        {
            this.Input = input;
        }

        private class ResourceLoader<TType> : Serialization.IStructure
            where TType : Serialization.IStructure, new()
        {
            public List<TType> List = new List<TType>();

            public void Deserialize(Serialization.IFileReader reader, object state)
            {
                this.List = reader.ReadListStructure<TType>(state);
            }

            public void Serialize(Serialization.IFileWriter writer, object state)
            {
                throw new NotSupportedException();
            }

            public void Deserialize(Serialization.IPacketReader reader, object state)
            {
                throw new NotSupportedException();
            }

            public void Serialize(Serialization.IPacketWriter writer, object state)
            {
                throw new NotSupportedException();
            }
        }

        public static List<TType> LoadResource<TType>(Stream input)
            where TType: Serialization.IStructure, new()
        {
            var reader = new BlobDataReader(input);
            var loader = reader.ReadValueStructure<ResourceLoader<TType>>(false, null);
            return loader.List;
        }

        private TType[] ReadNativeArray<TType>(
            int count,
            object state,
            Func<BlobDataReader, object, TType> readValue)
        {
            var array = new TType[count];
            for (uint i = 0; i < count; i++)
            {
                array[i] = readValue(this, state);
            }
            return array;
        }

        private List<TType> ReadNativeList<TType>(
            object state,
            Func<BlobDataReader, object, TType> readValue)
        {
            var count = this.Input.ReadValueU32();
            if (count > 800000)
            {
                throw new FormatException();
            }

            var list = new List<TType>();
            for (uint i = 0; i < count; i++)
            {
                list.Add(readValue(this, state));
            }
            return list;
        }

        public byte ReadValueByte(object state)
        {
            return this.Input.ReadValueU8();
        }

        public byte[] ReadArrayByte(int count, object state)
        {
            var array = new byte[count];
            this.Input.Read(array, 0, count);
            return array;
        }

        public List<byte> ReadListByte(object state)
        {
            throw new NotImplementedException();
        }

        public short ReadValueInt16(object state)
        {
            return (short)this.Input.ReadValueS32();
        }

        public short[] ReadArrayInt16(int count, object state)
        {
            return this.ReadNativeArray<short>(
                count, state, (a, b) => a.ReadValueInt16(b));
        }

        public List<short> ReadListInt16(object state)
        {
            return this.ReadNativeList<short>(
                state, (a, b) => a.ReadValueInt16(b));
        }

        public int ReadValueInt32(object state)
        {
            return this.Input.ReadValueS32();
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
            throw new NotImplementedException();
        }

        public long[] ReadArrayInt64(int count, object state)
        {
            return this.ReadNativeArray<long>(
                count, state, (a, b) => a.ReadValueInt64(b));
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
            return this.Input.ReadStringPascalUncapped();
        }

        public string[] ReadArrayString(int count, object state)
        {
            return this.ReadNativeArray<string>(
                count, state, (a, b) => a.ReadValueString(b));
        }

        public List<string> ReadListString(object state)
        {
            return this.ReadNativeList<string>(
                state, (a, b) => a.ReadValueString(b));
        }

        public string ReadValueCurrentFile(object state)
        {
            return this.Input.ReadStringPascalUncapped();
        }

        public string[] ReadArrayCurrentFile(int count, object state)
        {
            return this.ReadNativeArray<string>(
                count, state, (a, b) => a.ReadValueCurrentFile(b));
        }

        public List<string> ReadListCurrentFile(object state)
        {
            return this.ReadNativeList<string>(
                state, (a, b) => a.ReadValueCurrentFile(b));
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
            throw new NotImplementedException();
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
            return this.Input.ReadStringPascalUncapped();
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

        private Serialization.IStructure ReadValueStructure(
            Type type, bool optional, object state)
        {
            if (optional == true)
            {
                var hasValue = this.Input.ReadValueU32();
                if (hasValue == 0)
                {
                    return null;
                }
                else if (hasValue != 1)
                {
                    throw new FormatException();
                }
            }

            var dataSize = this.Input.ReadValueU32();

#if ALLINMEMORY
            var end = this.Input.Position + dataSize;

            var instance = (Serialization.IStructure)Activator.CreateInstance(type);
            instance.Deserialize(this, state);

            if (this.Input.Position != end)
            {
                throw new FormatException();
            }

            return instance;
#else
            using (var data = this.Input.ReadToMemoryStream(dataSize))
            {
                var instance = (ICrypticStructure)Activator.CreateInstance(type);
                instance.Serialize(new BlobReader(data));

                if (data.Position != data.Length)
                {
                    throw new FormatException();
                }

                return instance;
            }
#endif
        }

        public TType ReadValueStructure<TType>(bool optional, object state)
            where TType : Serialization.IStructure, new()
        {
            return (TType)ReadValueStructure(typeof(TType), optional, state);
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
                state, (a, b) => a.ReadValueStructure<TType>(false, state));
        }

        public object ReadValuePolymorph(Type[] validTypes, bool optional, object state)
        {
            if (optional == true)
            {
                var hasValue = this.Input.ReadValueU32();
                if (hasValue == 0)
                {
                    return null;
                }
                else if (hasValue != 1)
                {
                    throw new FormatException();
                }
            }

            var index = this.Input.ReadValueS32();
            var type = validTypes[index];
            return ReadValueStructure(type, false, state);
        }

        public object[] ReadArrayPolymorph(Type[] validTypes, int count, object state)
        {
            throw new NotImplementedException();
        }

        public List<object> ReadListPolymorph(Type[] validTypes, object state)
        {
            throw new NotImplementedException();
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
            return this.Input.ReadValueU32();
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
            object arg;

            var name = this.Input.ReadString(4, true, Encoding.ASCII);
            MultiValueOpcode op;
            if (MultiValue.TryParseOpcode(name, out op) == false)
            {
                throw new FormatException();
            }

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
                    if (MultiValue.TryParseStaticVariable(this.Input.ReadValueU32(), out sv) == false)
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
                    arg = this.Input.ReadValueS64();
                    break;
                }

                case MultiValueOpcode.FLT:
                {
                    arg = this.Input.ReadValueF64();
                    break;
                }

                case MultiValueOpcode.STR:
                {
                    arg = this.Input.ReadStringPascalUncapped();
                    break;
                }

                default:
                throw new InvalidOperationException(
                    string.Format(
                        "multival {0} had an unsupported argument data type",
                        op));
            }

            return new MultiValue()
            {
                Op = op,
                Arg = arg,
            };
        }

        public MultiValue[] ReadArrayMultiValue(int count, object state)
        {
            throw new NotSupportedException();
        }

        public List<MultiValue> ReadListMultiValue(object state)
        {
            return this.ReadNativeList<MultiValue>(
                state, (a, b) => a.ReadValueMultiValue(state));
        }

        public TType ReadValueByteEnum<TType>(object state)
        {
            return (TType)Enum.ToObject(typeof(TType), this.Input.ReadValueU8());
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
            return (TType)Enum.ToObject(typeof(TType), (short)this.Input.ReadValueS32());
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
            return (TType)Enum.ToObject(typeof(TType), this.Input.ReadValueS32());
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
            return (TType)Enum.ToObject(typeof(TType), this.Input.ReadValueU32());
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
