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
            if (array.Length != count)
            {
                throw new ArgumentException("array count mismatch", "array");
            }

            for (int i = 0; i < count; i++)
            {
                this.Output.WriteValueS32(array[i]);
            }
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
            if (array.Length != count)
            {
                throw new ArgumentException("array count mismatch", "array");
            }

            for (int i = 0; i < count; i++)
            {
                this.Output.WriteValueS32(array[i]);
            }
        }

        public void SerializeListInt32(ref List<int> list)
        {
            if (list.Count > 800000)
            {
                throw new ArgumentException("list has too many items", "list");
            }

            this.Output.WriteValueS32(list.Count);
            foreach (var item in list)
            {
                this.Output.WriteValueS32(item);
            }
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
            if (array.Length != count)
            {
                throw new ArgumentException("array count mismatch", "array");
            }

            for (int i = 0; i < count; i++)
            {
                this.Output.WriteValueF32(array[i]);
            }
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
            if (list.Count > 800000)
            {
                throw new ArgumentException("list has too many items", "list");
            }

            this.Output.WriteValueS32(list.Count);
            foreach (var item in list)
            {
                this.Output.WriteStringPascalUncapped(item);
            }
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
            if (list.Count > 800000)
            {
                throw new ArgumentException("list has too many items", "list");
            }

            this.Output.WriteValueS32(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                this.SerializeValueStructure<TType>(ref item, false);
            }
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
            throw new NotImplementedException();
        }

        public void SerializeArrayMultiValue(ref MultiValue[] array, int count)
        {
            throw new NotImplementedException();
        }

        // todo: create real multival handling
        public void SerializeListMultiValue(ref List<MultiValue> list)
        {
            if (list.Count > 800000)
            {
                throw new ArgumentException("list has too many items", "list");
            }

            this.Output.WriteValueS32(list.Count);
            foreach (var item in list)
            {
                this.Output.WriteString(item.OpName, 4, Encoding.ASCII);

                switch (item.Op)
                {
                    case MultiValueOpcode.NON:
                    case MultiValueOpcode.O_P:
                    case MultiValueOpcode.C_P:
                    case MultiValueOpcode.STM:
                    case MultiValueOpcode.LES:
                    case MultiValueOpcode.ADD:
                    case MultiValueOpcode.SUB:
                    case MultiValueOpcode.MUL:
                    case MultiValueOpcode.EQU:
                    case MultiValueOpcode.NOT:
                    case MultiValueOpcode.RET:
                    case MultiValueOpcode.AND:
                    case MultiValueOpcode.NEG:
                    case MultiValueOpcode.ORR:
                    case MultiValueOpcode.BAN:
                    case MultiValueOpcode.RZ_:
                    case MultiValueOpcode.NLE:
                    case MultiValueOpcode.GRE:
                    case MultiValueOpcode.DIV:
                    case MultiValueOpcode.BOR:
                    case MultiValueOpcode.NGR:
                    case MultiValueOpcode.BNT:
                    {
                        break;
                    }

                    case MultiValueOpcode.S_V:
                    {
                        this.Output.WriteValueU32((uint)item.Arg);
                        break;
                    }

                    case MultiValueOpcode.INT:
                    case MultiValueOpcode.JZ_:
                    case MultiValueOpcode.J__:
                    {
                        this.Output.WriteValueS64((long)item.Arg);
                        break;
                    }

                    case MultiValueOpcode.FLT:
                    {
                        this.Output.WriteValueF64((double)item.Arg);
                        break;
                    }

                    case MultiValueOpcode.STR:
                    case MultiValueOpcode.FUN:
                    case MultiValueOpcode.OBJ:
                    case MultiValueOpcode.IDS:
                    case MultiValueOpcode.RP_:
                    {
                        this.Output.WriteStringPascalUncapped((string)item.Arg);
                        break;
                    }

                    default: throw new NotSupportedException("unhandled opcode in multival");
                }
            }
        }

        public void SerializeValueByteEnum<T>(ref T value)
        {
            this.Output.WriteValueU8((byte)Convert.ChangeType(value, typeof(byte)));
        }

        public void SerializeArrayByteEnum<T>(ref T[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListByteEnum<T>(ref List<T> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueInt16Enum<T>(ref T value)
        {
            this.Output.WriteValueS32((int)((short)Convert.ChangeType(value, typeof(short))));
        }

        public void SerializeArrayInt16Enum<T>(ref T[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListInt16Enum<T>(ref List<T> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueInt32Enum<T>(ref T value)
        {
            this.Output.WriteValueS32((int)Convert.ChangeType(value, typeof(int)));
        }

        public void SerializeArrayInt32Enum<T>(ref T[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListInt32Enum<T>(ref List<T> list)
        {
            if (list.Count > 800000)
            {
                throw new ArgumentException("list has too many items", "list");
            }

            this.Output.WriteValueS32(list.Count);
            foreach (var item in list)
            {
                this.Output.WriteValueS32((int)Convert.ChangeType(item, typeof(int)));
            }
        }

        public void SerializeValueBitEnum<T>(ref T value)
        {
            this.Output.WriteValueU32((uint)Convert.ChangeType(value, typeof(uint)));
        }

        public void SerializeArrayBitEnum<T>(ref T[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListBitEnum<T>(ref List<T> list)
        {
            throw new NotImplementedException();
        }

        #region ICrypticFileStream Members


        public void SerializeValueInt32Packed(ref int value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
