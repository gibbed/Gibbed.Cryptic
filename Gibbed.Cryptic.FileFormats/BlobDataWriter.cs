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
    public class BlobDataWriter : Serialization.IFileWriter
    {
        private readonly Stream _Output;

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

        private readonly Func<string, uint> _GetIndexFromFileName;

        public BlobDataWriter(Stream output, bool isClient, bool isServer, Func<string, uint> getIndexFromFileName)
        {
            this._Output = output;
            this._IsClient = isClient;
            this._IsServer = isServer;
            this._GetIndexFromFileName = getIndexFromFileName;
        }

        private class ResourceLoader<TType> : Serialization.IStructure
            where TType : Serialization.IStructure, new()
        {
            public readonly List<TType> List = new List<TType>();

            public void Deserialize(Serialization.IFileReader reader, object state)
            {
                throw new NotSupportedException();
            }

            public void Serialize(Serialization.IFileWriter writer, object state)
            {
                writer.WriteListStructure(this.List, state);
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

        public static void SaveObject<TType>(TType instance, Stream output, bool isClient, bool isServer, Func<string, uint> getIndexFromFileName)
            where TType : Serialization.IStructure, new()
        {
            using (var data = new MemoryStream())
            {
                var reader = new BlobDataWriter(data, isClient, isServer, getIndexFromFileName);
                reader.WriteValueStructure(instance, false, null);

                data.Position = 0;
                output.WriteFromStream(data, data.Length);
            }
        }

        public static void SaveResource<TType>(List<TType> list, Stream output, bool isClient, bool isServer, Func<string, uint> getIndexFromFileName)
            where TType : Serialization.IStructure, new()
        {
            var loader = new ResourceLoader<TType>();
            loader.List.AddRange(list);

#if ALLINMEMORY
            using (var data = new MemoryStream())
            {
                var reader = new BlobDataWriter(data, isClient, isServer, getIndexFromFileName);
                reader.WriteValueStructure(loader, false, null);

                data.Position = 0;
                output.WriteFromStream(data, data.Length);
            }
#else
            var reader = new BlobWriter(output, isClient, isServer);
            reader.SerializeValueStructure(loader, false);
#endif
        }

        private void WriteNativeArray<TType>(
            TType[] array, int count, object state, Action<BlobDataWriter, TType, object> writeValue)
        {
            if (array.Length != count)
            {
                throw new ArgumentException("count mismatch in array", "array");
            }

            for (uint i = 0; i < count; i++)
            {
                writeValue(this, array[i], state);
            }
        }

        private void WriteNativeList<TType>(
            List<TType> list, object state, Action<BlobDataWriter, TType, object> writeValue)
        {
            if (list.Count > 800000)
            {
                throw new ArgumentException("too many items in list", "list");
            }

            this._Output.WriteValueS32(list.Count);
            foreach (var item in list)
            {
                writeValue(this, item, state);
            }
        }

        public void WriteValueByte(byte value, object state)
        {
            this._Output.WriteValueU8(value);
        }

        public void WriteArrayByte(byte[] array, int count, object state)
        {
            if (array.Length != count)
            {
                throw new ArgumentException("array count mismatch", "array");
            }

            this._Output.Write(array, 0, count);
        }

        public void WriteListByte(List<byte> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueByte(b, c));
        }

        public void WriteValueInt16(short value, object state)
        {
            this._Output.WriteValueS32(value);
        }

        public void WriteArrayInt16(short[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueInt16(b, c));
        }

        public void WriteListInt16(List<short> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueInt16(b, c));
        }

        public void WriteValueInt32(int value, object state)
        {
            this._Output.WriteValueS32(value);
        }

        public void WriteArrayInt32(int[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueInt32(b, c));
        }

        public void WriteListInt32(List<int> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueInt32(b, c));
        }

        public void WriteValueInt64(long value, object state)
        {
            throw new NotImplementedException();
        }

        public void WriteArrayInt64(long[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueInt64(b, c));
        }

        public void WriteListInt64(List<long> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueInt64(b, c));
        }

        public void WriteValueFloat(float value, object state)
        {
            this._Output.WriteValueF32(value);
        }

        public void WriteArrayFloat(float[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueFloat(b, c));
        }

        public void WriteListFloat(List<float> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueFloat(b, c));
        }

        public void WriteValueString(string value, object state)
        {
            this._Output.WriteStringPascalUncapped(value);
        }

        public void WriteArrayString(string[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueString(b, c));
        }

        public void WriteListString(List<string> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueString(b, c));
        }

        public void WriteValueCurrentFile(string value, object state)
        {
            var index = this._GetIndexFromFileName(value);
            this._Output.WriteValueU32(index);
        }

        public void WriteArrayCurrentFile(string[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueCurrentFile(b, c));
        }

        public void WriteListCurrentFile(List<string> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueCurrentFile(b, c));
        }

        public void WriteValueTimestamp(int value, object state)
        {
            this._Output.WriteValueS32(value);
        }

        public void WriteArrayTimestamp(int[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueTimestamp(b, c));
        }

        public void WriteListTimestamp(List<int> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueTimestamp(b, c));
        }

        public void WriteValueLineNumber(int value, object state)
        {
            this._Output.WriteValueS32(value);
        }

        public void WriteArrayLineNumber(int[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueLineNumber(b, c));
        }

        public void WriteListLineNumber(List<int> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueLineNumber(b, c));
        }

        public void WriteValueBoolean(bool value, object state)
        {
            throw new NotImplementedException();
        }

        public void WriteArrayBoolean(bool[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueBoolean(b, c));
        }

        public void WriteListBoolean(List<bool> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueBoolean(b, c));
        }

        public void WriteValueBooleanFlag(bool value, object state)
        {
            this._Output.WriteValueB8(value);
        }

        public void WriteArrayBooleanFlag(bool[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueBooleanFlag(b, c));
        }

        public void WriteListBooleanFlag(List<bool> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueBooleanFlag(b, c));
        }

        public void WriteValueQUATPYR(float value, object state)
        {
            this._Output.WriteValueF32(value);
        }

        public void WriteArrayQUATPYR(float[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueQUATPYR(b, c));
        }

        public void WriteListQUATPYR(List<float> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueQUATPYR(b, c));
        }

        public void WriteValueMATPYR(MATPYR value, object state)
        {
            throw new NotImplementedException();
        }

        public void WriteArrayMATPYR(MATPYR[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueMATPYR(b, c));
        }

        public void WriteListMATPYR(List<MATPYR> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueMATPYR(b, c));
        }

        public void WriteValueFilename(string value, object state)
        {
            throw new NotImplementedException();
        }

        public void WriteArrayFilename(string[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueFilename(b, c));
        }

        public void WriteListFilename(List<string> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueFilename(b, c));
        }

        public void WriteValueReference(string value, object state)
        {
            this._Output.WriteStringPascalUncapped(value);
        }

        public void WriteArrayReference(string[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueReference(b, c));
        }

        public void WriteListReference(List<string> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueReference(b, c));
        }

        public void WriteValueFunctionCall(FunctionCall value, object state)
        {
            throw new NotImplementedException();
        }

        public void WriteArrayFunctionCall(FunctionCall[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueFunctionCall(b, c));
        }

        public void WriteListFunctionCall(List<FunctionCall> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueFunctionCall(b, c));
        }

        private void WriteStructure(
            Serialization.IStructure value, bool optional, object state)
        {
            if (optional == true)
            {
                this._Output.WriteValueS32(value == null ? 0 : 1);
                if (value == null)
                {
                    return;
                }
            }
            else
            {
                if (value == null)
                {
                    throw new InvalidOperationException();
                }
            }

#if !ALLINMEMORY
            using (var data = new MemoryStream())
            {
                value.Serialize(new BlobWriter(data));
                data.Position = 0;
                this.Output.WriteValueU32((uint)data.Length);
                this.Output.WriteFromStream(data, data.Length);
            }
#else
            var size = this._Output.Position;
            this._Output.Seek(4, SeekOrigin.Current);

            var start = this._Output.Position;
            value.Serialize(this, state);

            var end = this._Output.Position;

            this._Output.Seek(size, SeekOrigin.Begin);
            this._Output.WriteValueU32((uint)(end - start));
            this._Output.Seek(end, SeekOrigin.Begin);
#endif
        }

        public void WriteValueStructure<TType>(TType value, bool optional, object state)
            where TType : Serialization.IStructure, new()
        {
            this.WriteStructure(value, optional, state);
        }

        public void WriteArrayStructure<TType>(TType[] array, int count, object state)
            where TType : Serialization.IStructure, new()
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueStructure(b, false, c));
        }

        public void WriteListStructure<TType>(List<TType> list, object state)
            where TType : Serialization.IStructure, new()
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueStructure(b, false, c));
        }

        public void WriteValuePolymorph(object value, Type[] validTypes, bool optional, object state)
        {
            if (optional == true)
            {
                this._Output.WriteValueS32(value == null ? 0 : 1);
                if (value == null)
                {
                    return;
                }
            }
            else
            {
                if (value == null)
                {
                    throw new InvalidOperationException();
                }
            }

            var index = Array.IndexOf(validTypes, value.GetType());
            if (index < 0)
            {
                throw new InvalidOperationException();
            }

            this._Output.WriteValueS32(index);
            this.WriteStructure((Serialization.IStructure)value, false, state);
        }

        public void WriteArrayPolymorph(object[] array, Type[] validTypes, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValuePolymorph(b, validTypes, false, c));
        }

        public void WriteListPolymorph(List<object> list, Type[] validTypes, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValuePolymorph(b, validTypes, false, c));
        }

        public void WriteValueStashTable(StashTable value, object state)
        {
            throw new NotImplementedException();
        }

        public void WriteArrayStashTable(StashTable[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueStashTable(b, c));
        }

        public void WriteListStashTable(List<StashTable> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueStashTable(b, c));
        }

        public void WriteValueBit(uint value, int bitOffset, object state)
        {
            this._Output.WriteValueU32(value);
        }

        public void WriteArrayBit(uint[] array, int bitOffset, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueBit(b, bitOffset, c));
        }

        public void WriteListBit(List<uint> list, int bitOffset, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueBit(b, bitOffset, c));
        }

        public void WriteValueMultiValue(MultiValue value, object state)
        {
            this._Output.WriteString(value.OpName, 4, Encoding.ASCII);

            // ReSharper disable BitwiseOperatorOnEnumWihtoutFlags
            switch (value.Op & MultiValueOpcode.TypeMask) // ReSharper restore BitwiseOperatorOnEnumWihtoutFlags
            {
                case MultiValueOpcode.NON:
                {
                    if (value.Arg != null)
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "multival {0} had a non-null argument",
                                value.Op));
                    }

                    break;
                }

                case MultiValueOpcode.StaticVariable:
                {
                    if (value.Arg is uint)
                    {
                        this._Output.WriteValueU32((uint)value.Arg);
                    }
                    else if (value.Arg.GetType() == typeof(StaticVariableType))
                    {
                        this._Output.WriteValueU32((uint)((StaticVariableType)value.Arg));
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "multival {0} had an unexpected data type (got {1}, should be uint or StaticVariable)",
                                value.Op,
                                value.Arg.GetType().Name));
                    }

                    break;
                }

                case MultiValueOpcode.INT:
                {
                    if (value.Arg.GetType() != typeof(long))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "multival {0} had an unexpected data type (got {1}, should be long)",
                                value.Op,
                                value.Arg.GetType().Name));
                    }

                    this._Output.WriteValueS64((long)value.Arg);
                    break;
                }

                case MultiValueOpcode.FLT:
                {
                    if (value.Arg.GetType() != typeof(double))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "multival {0} had an unexpected data type (got {1}, should be double)",
                                value.Op,
                                value.Arg.GetType().Name));
                    }

                    this._Output.WriteValueF64((double)value.Arg);
                    break;
                }

                case MultiValueOpcode.STR:
                {
                    if (value.Arg.GetType() != typeof(string))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "multival {0} had an unexpected data type (got {1}, should be string)",
                                value.Op,
                                value.Arg.GetType()));
                    }

                    this._Output.WriteStringPascalUncapped((string)value.Arg);
                    break;
                }

                default:
                    throw new InvalidOperationException(
                        string.Format(
                            "multival {0} had an unsupported argument data type",
                            value.Op));
            }
        }

        public void WriteArrayMultiValue(MultiValue[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueMultiValue(b, c));
        }

        public void WriteListMultiValue(List<MultiValue> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueMultiValue(b, c));
        }

        public void WriteValueByteEnum<TType>(TType value, object state)
        {
            this._Output.WriteValueU8((byte)Convert.ChangeType(value, typeof(byte)));
        }

        public void WriteArrayByteEnum<TType>(TType[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueByteEnum(b, c));
        }

        public void WriteListByteEnum<TType>(List<TType> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueByteEnum(b, c));
        }

        public void WriteValueInt16Enum<TType>(TType value, object state)
        {
            this._Output.WriteValueS32((short)Convert.ChangeType(value, typeof(short)));
        }

        public void WriteArrayInt16Enum<TType>(TType[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueInt16Enum(b, c));
        }

        public void WriteListInt16Enum<TType>(List<TType> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueInt16Enum(b, c));
        }

        public void WriteValueInt32Enum<TType>(TType value, object state)
        {
            this._Output.WriteValueS32((int)Convert.ChangeType(value, typeof(int)));
        }

        public void WriteArrayInt32Enum<TType>(TType[] array, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueInt32Enum(b, c));
        }

        public void WriteListInt32Enum<TType>(List<TType> list, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueInt32Enum(b, c));
        }

        public void WriteValueBitEnum<TType>(TType value, int bitOffset, object state)
        {
            this._Output.WriteValueU32((uint)Convert.ChangeType(value, typeof(uint)));
        }

        public void WriteArrayBitEnum<TType>(TType[] array, int bitOffset, int count, object state)
        {
            this.WriteNativeArray(
                array, count, state, (a, b, c) => a.WriteValueBitEnum(b, bitOffset, c));
        }

        public void WriteListBitEnum<TType>(List<TType> list, int bitOffset, object state)
        {
            this.WriteNativeList(
                list, state, (a, b, c) => a.WriteValueBitEnum(b, bitOffset, c));
        }
    }
}
