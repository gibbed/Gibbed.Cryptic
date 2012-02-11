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
    public class BlobWriter : ICrypticFileStream
    {
        private Stream Output;

        public BlobWriter(Stream output)
        {
            this.Output = output;
        }

        private class ResourceLoader<TType> : ICrypticStructure
            where TType : ICrypticStructure, new()
        {
            public List<TType> List = new List<TType>();

            public void Serialize(ICrypticFileStream stream)
            {
                stream.SerializeListStructure<TType>(ref this.List);
            }

            public void Serialize(ICrypticPacketReader reader, bool unknownFlag)
            {
                throw new NotSupportedException();
            }
        }

        public static void SaveResource<TType>(List<TType> list, Stream output)
            where TType : ICrypticStructure, new()
        {
            var loader = new ResourceLoader<TType>();
            loader.List.AddRange(list);

#if ALLINMEMORY
            using (var data = new MemoryStream())
            {
                var reader = new BlobWriter(data);
                reader.SerializeValueStructure(ref loader, false);

                data.Position = 0;
                output.WriteFromStream(data, data.Length);
            }
#else
            var reader = new BlobWriter(output);
            reader.SerializeValueStructure(ref loader, false);
#endif
        }

        delegate void WriteNativeValue<TType>(ICrypticFileStream stream, ref TType value);

        private void WriteNativeArray<TType>(
            ref TType[] array, int count, WriteNativeValue<TType> writeValue)
        {
            if (array.Length != count)
            {
                throw new ArgumentException("count mismatch in array", "array");
            }

            for (uint i = 0; i < count; i++)
            {
                writeValue(this, ref array[i]);
            }
        }

        private void WriteNativeList<TType>(
            ref List<TType> list, WriteNativeValue<TType> writeValue)
        {
            if (list.Count > 800000)
            {
                throw new ArgumentException("too many items in list", "list");
            }

            this.Output.WriteValueS32(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                writeValue(this, ref item);
            }
        }

        public void SerializeValueByte(ref byte value)
        {
            this.Output.WriteValueU8(value);
        }

        public void SerializeArrayByte(ref byte[] array, int count)
        {
            if (array.Length != count)
            {
                throw new ArgumentException("array count mismatch", "array");
            }

            this.Output.Write(array, 0, count);
        }

        public void SerializeListByte(ref List<byte> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueInt16(ref short value)
        {
            this.Output.WriteValueS32(value);
        }

        public void SerializeArrayInt16(ref short[] array, int count)
        {
            this.WriteNativeArray<short>(
                ref array, count,
                (ICrypticFileStream a, ref short b) =>
                    a.SerializeValueInt16(ref b));
        }

        public void SerializeListInt16(ref List<short> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueInt32(ref int value)
        {
            this.Output.WriteValueS32(value);
        }

        public void SerializeArrayInt32(ref int[] array, int count)
        {
            this.WriteNativeArray<int>(
                ref array, count,
                (ICrypticFileStream a, ref int b) =>
                    a.SerializeValueInt32(ref b));
        }

        public void SerializeListInt32(ref List<int> list)
        {
            this.WriteNativeList<int>(
                ref list,
                (ICrypticFileStream a, ref int b) =>
                    a.SerializeValueInt32(ref b));
        }

        public void SerializeValueInt64(ref long value)
        {
            throw new NotImplementedException();
        }

        public void SerializeArrayInt64(ref long[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListInt64(ref List<long> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueFloat(ref float value)
        {
            this.Output.WriteValueF32(value);
        }

        public void SerializeArrayFloat(ref float[] array, int count)
        {
            this.WriteNativeArray<float>(
                ref array, count,
                (ICrypticFileStream a, ref float b) => a.SerializeValueFloat(ref b));
        }

        public void SerializeListFloat(ref List<float> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueString(ref string value)
        {
            this.Output.WriteStringPascalUncapped(value);
        }

        public void SerializeArrayString(ref string[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListString(ref List<string> list)
        {
            this.WriteNativeList<string>(
                ref list,
                (ICrypticFileStream a, ref string b) =>
                    a.SerializeValueString(ref b));
        }

        public void SerializeValueCurrentFile(ref string value)
        {
            this.Output.WriteStringPascalUncapped(value);
        }

        public void SerializeArrayCurrentFile(ref string[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListCurrentFile(ref List<string> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueTimestamp(ref int value)
        {
            throw new NotImplementedException();
        }

        public void SerializeArrayTimestamp(ref int[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListTimestamp(ref List<int> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueLineNumber(ref int value)
        {
            throw new NotImplementedException();
        }

        public void SerializeArrayLineNumber(ref int[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListLineNumber(ref List<int> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueBoolean(ref bool value)
        {
            throw new NotImplementedException();
        }

        public void SerializeArrayBoolean(ref bool[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListBoolean(ref List<bool> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueBooleanFlag(ref bool value)
        {
            throw new NotImplementedException();
        }

        public void SerializeArrayBooleanFlag(ref bool[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListBooleanFlag(ref List<bool> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueQUATPYR(ref QUATPYR value)
        {
            throw new NotImplementedException();
        }

        public void SerializeArrayQUATPYR(ref QUATPYR[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListQUATPYR(ref List<QUATPYR> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueMATPYR(ref MATPYR value)
        {
            throw new NotImplementedException();
        }

        public void SerializeArrayMATPYR(ref MATPYR[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListMATPYR(ref List<MATPYR> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueFilename(ref string value)
        {
            throw new NotImplementedException();
        }

        public void SerializeArrayFilename(ref string[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListFilename(ref List<string> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueReference(ref string value)
        {
            this.Output.WriteStringPascalUncapped(value);
        }

        public void SerializeArrayReference(ref string[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListReference(ref List<string> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueFunctionCall(ref FunctionCall value)
        {
            throw new NotImplementedException();
        }

        public void SerializeArrayFunctionCall(ref FunctionCall[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListFunctionCall(ref List<FunctionCall> list)
        {
            throw new NotImplementedException();
        }

        private void SerializeStructure(ICrypticStructure value, bool optional)
        {
            if (optional == true)
            {
                this.Output.WriteValueS32(value == null ? 0 : 1);
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
            var size = this.Output.Position;
            this.Output.Seek(4, SeekOrigin.Current);

            var start = this.Output.Position;
            value.Serialize(this);

            var end = this.Output.Position;

            this.Output.Seek(size, SeekOrigin.Begin);
            this.Output.WriteValueU32((uint)(end - start));
            this.Output.Seek(end, SeekOrigin.Begin);
#endif
        }

        public void SerializeValueStructure<TType>(ref TType value, bool optional)
            where TType : ICrypticStructure, new()
        {
            this.SerializeStructure(value, optional);
        }

        public void SerializeArrayStructure<TType>(ref TType[] array, int count)
            where TType : ICrypticStructure, new()
        {
            throw new NotImplementedException();
        }

        public void SerializeListStructure<TType>(ref List<TType> list)
            where TType : ICrypticStructure, new()
        {
            this.WriteNativeList<TType>(
                ref list,
                (ICrypticFileStream a, ref TType b) =>
                    a.SerializeValueStructure<TType>(ref b, false));

        }

        public void SerializeValuePolymorph(ref object value, bool optional, Type[] validTypes)
        {
            if (optional == true)
            {
                this.Output.WriteValueS32(value == null ? 0 : 1);
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

            var index = Array.IndexOf<Type>(validTypes, value.GetType());
            if (index < 0)
            {
                throw new InvalidOperationException();
            }

            this.Output.WriteValueS32(index);
            this.SerializeStructure((ICrypticStructure)value, false);
        }

        public void SerializeArrayPolymorph(ref object[] array, int count, Type[] validTypes)
        {
            throw new NotImplementedException();
        }

        public void SerializeListPolymorph(ref List<object> list, Type[] validTypes)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueStashTable(ref StashTable value)
        {
            throw new NotImplementedException();
        }

        public void SerializeArrayStashTable(ref StashTable[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListStashTable(ref List<StashTable> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueBit(ref uint value)
        {
            this.Output.WriteValueU32(value);
        }

        public void SerializeArrayBit(ref uint[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListBit(ref List<uint> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueMultiValue(ref MultiValue value)
        {
            this.Output.WriteString(value.OpName, 4, Encoding.ASCII);

            switch (value.Op & MultiValueOpcode.TypeMask)
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
                    if (value.Arg.GetType() == typeof(uint))
                    {
                        this.Output.WriteValueU32((uint)value.Arg);
                    }
                    else if (value.Arg.GetType() == typeof(StaticVariableType))
                    {
                        this.Output.WriteValueU32((uint)((StaticVariableType)value.Arg));
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "multival {0} had an unexpected data type (got {1}, should be uint or StaticVariable)",
                                value.Op, value.Arg.GetType().Name));
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
                                value.Op, value.Arg.GetType().Name));
                    }

                    this.Output.WriteValueS64((long)value.Arg);
                    break;
                }

                case MultiValueOpcode.FLT:
                {
                    if (value.Arg.GetType() != typeof(double))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "multival {0} had an unexpected data type (got {1}, should be double)",
                                value.Op, value.Arg.GetType().Name));
                    }

                    this.Output.WriteValueF64((double)value.Arg);
                    break;
                }

                case MultiValueOpcode.STR:
                {
                    if (value.Arg.GetType() != typeof(string))
                    {
                        throw new InvalidOperationException(
                            string.Format(
                                "multival {0} had an unexpected data type (got {1}, should be string)",
                                value.Op, value.Arg.GetType()));
                    }

                    this.Output.WriteStringPascalUncapped((string)value.Arg);
                    break;
                }

                default:
                    throw new InvalidOperationException(
                        string.Format(
                            "multival {0} had an unsupported argument data type",
                            value.Op));
            }
        }

        public void SerializeArrayMultiValue(ref MultiValue[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListMultiValue(ref List<MultiValue> list)
        {
            this.WriteNativeList<MultiValue>(
                ref list,
                (ICrypticFileStream a, ref MultiValue b) =>
                    a.SerializeValueMultiValue(ref b));
        }

        public void SerializeValueByteEnum<TType>(ref TType value)
        {
            this.Output.WriteValueU8((byte)Convert.ChangeType(value, typeof(byte)));
        }

        public void SerializeArrayByteEnum<TType>(ref TType[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListByteEnum<TType>(ref List<TType> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueInt16Enum<TType>(ref TType value)
        {
            this.Output.WriteValueS32((int)((short)Convert.ChangeType(value, typeof(short))));
        }

        public void SerializeArrayInt16Enum<TType>(ref TType[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListInt16Enum<TType>(ref List<TType> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueInt32Enum<TType>(ref TType value)
        {
            this.Output.WriteValueS32((int)Convert.ChangeType(value, typeof(int)));
        }

        public void SerializeArrayInt32Enum<TType>(ref TType[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListInt32Enum<TType>(ref List<TType> list)
        {
            this.WriteNativeList<TType>(
                ref list,
                (ICrypticFileStream a, ref TType b) =>
                    a.SerializeValueInt32Enum<TType>(ref b));
        }

        public void SerializeValueBitEnum<TType>(ref TType value)
        {
            this.Output.WriteValueU32((uint)Convert.ChangeType(value, typeof(uint)));
        }

        public void SerializeArrayBitEnum<TType>(ref TType[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListBitEnum<TType>(ref List<TType> list)
        {
            throw new NotImplementedException();
        }
    }
}
