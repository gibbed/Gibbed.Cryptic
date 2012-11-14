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
        private readonly Stream _Input;

        private readonly bool _IsClient;

        public bool IsClient
        {
            get { return this._IsClient; }
        }

        private readonly bool _IsServer;

        public bool IsServer
        {
            get { return this._IsServer; }
        }

        private readonly Func<uint, string> _GetFileNameFromIndex;

        public BlobDataReader(Stream input, bool isClient, bool isServer, Func<uint, string> getFileNameFromIndex)
        {
            this._Input = input;
            this._IsClient = isClient;
            this._IsServer = isServer;
            this._GetFileNameFromIndex = getFileNameFromIndex;
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

        public static TType LoadObject<TType>(Stream input, bool isClient, bool isServer, Func<uint, string> getFileNameFromIndex)
            where TType : Serialization.IStructure, new()
        {
            var reader = new BlobDataReader(input, isClient, isServer, getFileNameFromIndex);
            return reader.ReadValueStructure<TType>(false, null);
        }

        public static List<TType> LoadResource<TType>(Stream input, bool isClient, bool isServer, Func<uint, string> getFileNameFromIndex)
            where TType : Serialization.IStructure, new()
        {
            var reader = new BlobDataReader(input, isClient, isServer, getFileNameFromIndex);
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
            var count = this._Input.ReadValueU32();
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
            return this._Input.ReadValueU8();
        }

        public byte[] ReadArrayByte(int count, object state)
        {
            var array = new byte[count];
            this._Input.Read(array, 0, count);
            return array;
        }

        public List<byte> ReadListByte(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueByte(b));
        }

        public short ReadValueInt16(object state)
        {
            return (short)this._Input.ReadValueS32();
        }

        public short[] ReadArrayInt16(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueInt16(b));
        }

        public List<short> ReadListInt16(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueInt16(b));
        }

        public int ReadValueInt32(object state)
        {
            return this._Input.ReadValueS32();
        }

        public int[] ReadArrayInt32(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueInt32(b));
        }

        public List<int> ReadListInt32(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueInt32(b));
        }

        public long ReadValueInt64(object state)
        {
            throw new NotImplementedException();
        }

        public long[] ReadArrayInt64(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueInt64(b));
        }

        public List<long> ReadListInt64(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueInt64(b));
        }

        public float ReadValueFloat(object state)
        {
            return this._Input.ReadValueF32();
        }

        public float[] ReadArrayFloat(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueFloat(b));
        }

        public List<float> ReadListFloat(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueFloat(b));
        }

        public string ReadValueString(object state)
        {
            return this._Input.ReadStringPascalUncapped();
        }

        public string[] ReadArrayString(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueString(b));
        }

        public List<string> ReadListString(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueString(b));
        }

        public string ReadValueCurrentFile(object state)
        {
            var index = this._Input.ReadValueU32();
            return this._GetFileNameFromIndex(index);
        }

        public string[] ReadArrayCurrentFile(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueCurrentFile(b));
        }

        public List<string> ReadListCurrentFile(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueCurrentFile(b));
        }

        public int ReadValueTimestamp(object state)
        {
            return this._Input.ReadValueS32();
        }

        public int[] ReadArrayTimestamp(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueTimestamp(b));
        }

        public List<int> ReadListTimestamp(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueTimestamp(b));
        }

        public int ReadValueLineNumber(object state)
        {
            return this._Input.ReadValueS32();
        }

        public int[] ReadArrayLineNumber(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueLineNumber(b));
        }

        public List<int> ReadListLineNumber(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueLineNumber(b));
        }

        public bool ReadValueBoolean(object state)
        {
            throw new NotImplementedException();
        }

        public bool[] ReadArrayBoolean(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueBoolean(b));
        }

        public List<bool> ReadListBoolean(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueBoolean(b));
        }

        public bool ReadValueBooleanFlag(object state)
        {
            return this._Input.ReadValueB8();
        }

        public bool[] ReadArrayBooleanFlag(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueBooleanFlag(b));
        }

        public List<bool> ReadListBooleanFlag(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueBooleanFlag(b));
        }

        public float ReadValueQUATPYR(object state)
        {
            return this._Input.ReadValueF32();
        }

        public float[] ReadArrayQUATPYR(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueQUATPYR(b));
        }

        public List<float> ReadListQUATPYR(object state)
        {
            return this.ReadNativeList(
               state, (a, b) => a.ReadValueQUATPYR(b));
        }

        public MATPYR ReadValueMATPYR(object state)
        {
            throw new NotImplementedException();
        }

        public MATPYR[] ReadArrayMATPYR(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueMATPYR(b));
        }

        public List<MATPYR> ReadListMATPYR(object state)
        {
            return this.ReadNativeList(
               state, (a, b) => a.ReadValueMATPYR(b));
        }

        public string ReadValueFilename(object state)
        {
            throw new NotImplementedException();
        }

        public string[] ReadArrayFilename(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueFilename(b));
        }

        public List<string> ReadListFilename(object state)
        {
            return this.ReadNativeList(
               state, (a, b) => a.ReadValueFilename(b));
        }

        public string ReadValueReference(object state)
        {
            return this._Input.ReadStringPascalUncapped();
        }

        public string[] ReadArrayReference(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueReference(b));
        }

        public List<string> ReadListReference(object state)
        {
            return this.ReadNativeList(
               state, (a, b) => a.ReadValueReference(b));
        }

        public FunctionCall ReadValueFunctionCall(object state)
        {
            throw new NotImplementedException();
        }

        public FunctionCall[] ReadArrayFunctionCall(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueFunctionCall(b));
        }

        public List<FunctionCall> ReadListFunctionCall(object state)
        {
            return this.ReadNativeList(
               state, (a, b) => a.ReadValueFunctionCall(b));
        }

        private Serialization.IStructure ReadValueStructure(
            Type type, bool optional, object state)
        {
            if (optional == true)
            {
                var hasValue = this._Input.ReadValueU32();
                if (hasValue == 0)
                {
                    return null;
                }

                if (hasValue != 1)
                {
                    throw new FormatException();
                }
            }

            var dataSize = this._Input.ReadValueU32();

#if ALLINMEMORY
            var start = this._Input.Position;
            var end = this._Input.Position + dataSize;

            var instance = (Serialization.IStructure)Activator.CreateInstance(type);
            instance.Deserialize(this, state);

            if (this._Input.Position != end)
            {
                throw new FormatException();
            }

            return instance;
#else
            using (var data = this.Input.ReadToMemoryStream(dataSize))
            {
                var instance = (ICrypticStructure)Activator.CreateInstance(type);
                instance.Serialize(new BlobReader(data, isCLient, isServer));

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
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueStructure<TType>(false, b));
        }

        public List<TType> ReadListStructure<TType>(object state)
            where TType : Serialization.IStructure, new()
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueStructure<TType>(false, b));
        }

        public object ReadValuePolymorph(Type[] validTypes, bool optional, object state)
        {
            if (optional == true)
            {
                var hasValue = this._Input.ReadValueU32();
                if (hasValue == 0)
                {
                    return null;
                }

                if (hasValue != 1)
                {
                    throw new FormatException();
                }
            }

            var index = this._Input.ReadValueS32();
            var type = validTypes[index];
            return ReadValueStructure(type, false, state);
        }

        public object[] ReadArrayPolymorph(Type[] validTypes, int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValuePolymorph(validTypes, false, b));
        }

        public List<object> ReadListPolymorph(Type[] validTypes, object state)
        {
            return this.ReadNativeList(
               state, (a, b) => a.ReadValuePolymorph(validTypes, false, b));
        }

        public StashTable ReadValueStashTable(object state)
        {
            throw new NotImplementedException();
        }

        public StashTable[] ReadArrayStashTable(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueStashTable(b));
        }

        public List<StashTable> ReadListStashTable(object state)
        {
            return this.ReadNativeList(
               state, (a, b) => a.ReadValueStashTable(b));
        }

        public uint ReadValueBit(int bitOffset, object state)
        {
            return this._Input.ReadValueU32();
        }

        public uint[] ReadArrayBit(int bitOffset, int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueBit(bitOffset, b));
        }

        public List<uint> ReadListBit(int bitOffset, object state)
        {
            return this.ReadNativeList(
               state, (a, b) => a.ReadValueBit(bitOffset, b));
        }

        public MultiValue ReadValueMultiValue(object state)
        {
            object arg;

            var name = this._Input.ReadString(4, true, Encoding.ASCII);
            MultiValueOpcode op;
            if (MultiValue.TryParseOpcode(name, out op) == false)
            {
                throw new FormatException();
            }

            // ReSharper disable BitwiseOperatorOnEnumWihtoutFlags
            switch (op & MultiValueOpcode.TypeMask) // ReSharper restore BitwiseOperatorOnEnumWihtoutFlags
            {
                case MultiValueOpcode.NON:
                {
                    arg = null;
                    break;
                }

                case MultiValueOpcode.StaticVariable:
                {
                    StaticVariableType sv;
                    if (MultiValue.TryParseStaticVariable(this._Input.ReadValueU32(), out sv) == false)
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
                    arg = this._Input.ReadValueS64();
                    break;
                }

                case MultiValueOpcode.FLT:
                {
                    arg = this._Input.ReadValueF64();
                    break;
                }

                case MultiValueOpcode.STR:
                {
                    arg = this._Input.ReadStringPascalUncapped();
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
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueMultiValue(b));
        }

        public List<MultiValue> ReadListMultiValue(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueMultiValue(state));
        }

        public TType ReadValueByteEnum<TType>(object state)
        {
            return (TType)Enum.ToObject(typeof(TType), this._Input.ReadValueU8());
        }

        public TType[] ReadArrayByteEnum<TType>(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueByteEnum<TType>(b));
        }

        public List<TType> ReadListByteEnum<TType>(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueByteEnum<TType>(state));
        }

        public TType ReadValueInt16Enum<TType>(object state)
        {
            return (TType)Enum.ToObject(typeof(TType), (short)this._Input.ReadValueS32());
        }

        public TType[] ReadArrayInt16Enum<TType>(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueInt16Enum<TType>(b));
        }

        public List<TType> ReadListInt16Enum<TType>(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueInt16Enum<TType>(state));
        }

        public TType ReadValueInt32Enum<TType>(object state)
        {
            return (TType)Enum.ToObject(typeof(TType), this._Input.ReadValueS32());
        }

        public TType[] ReadArrayInt32Enum<TType>(int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueInt32Enum<TType>(b));
        }

        public List<TType> ReadListInt32Enum<TType>(object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueInt32Enum<TType>(b));
        }

        public TType ReadValueBitEnum<TType>(int bitOffset, object state)
        {
            return (TType)Enum.ToObject(typeof(TType), this._Input.ReadValueU32());
        }

        public TType[] ReadArrayBitEnum<TType>(int bitOffset, int count, object state)
        {
            return this.ReadNativeArray(
                count, state, (a, b) => a.ReadValueBitEnum<TType>(bitOffset, b));
        }

        public List<TType> ReadListBitEnum<TType>(int bitOffset, object state)
        {
            return this.ReadNativeList(
                state, (a, b) => a.ReadValueBitEnum<TType>(bitOffset, b));
        }
    }
}
