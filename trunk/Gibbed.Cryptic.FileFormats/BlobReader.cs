﻿/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
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
    public class BlobReader : ICrypticFileStream
    {
        private Stream Input;

        public BlobReader(Stream input)
        {
            this.Input = input;
        }

        private class ResourceLoader<TType> : ICrypticStructure
            where TType : ICrypticStructure, new()
        {
            public List<TType> List;

            public void Serialize(ICrypticFileStream stream)
            {
                stream.SerializeListStructure<TType>(ref this.List);
            }

            public void Serialize(ICrypticPacketReader reader, bool unknownFlag)
            {
                throw new NotSupportedException();
            }
        }

        public static List<TType> LoadResource<TType>(Stream input)
            where TType: ICrypticStructure, new()
        {
            var loader = new ResourceLoader<TType>();

            var reader = new BlobReader(input);
            reader.SerializeValueStructure(ref loader, false);

            return loader.List;
        }

        public void SerializeValueByte(ref byte value)
        {
            value = this.Input.ReadValueU8();
        }

        public void SerializeArrayByte(ref byte[] array, int count)
        {
            array = new byte[count];
            this.Input.Read(array, 0, count);
        }

        public void SerializeListByte(ref List<byte> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueInt16(ref short value)
        {
            value = (short)this.Input.ReadValueS32();
        }

        public void SerializeArrayInt16(ref short[] array, int count)
        {
            array = new short[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = (short)this.Input.ReadValueS32();
            }
        }

        public void SerializeListInt16(ref List<short> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueInt32(ref int value)
        {
            value = this.Input.ReadValueS32();
        }

        public void SerializeArrayInt32(ref int[] array, int count)
        {
            array = new int[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = this.Input.ReadValueS32();
            }
        }

        public void SerializeListInt32(ref List<int> list)
        {
            var count = this.Input.ReadValueU32();
            if (count > 800000)
            {
                throw new FormatException();
            }

            list = new List<int>();
            for (uint i = 0; i < count; i++)
            {
                list.Add(this.Input.ReadValueS32());
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
            value = this.Input.ReadValueF32();
        }

        public void SerializeArrayFloat(ref float[] array, int count)
        {
            array = new float[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = this.Input.ReadValueF32();
            }
        }

        public void SerializeListFloat(ref List<float> list)
        {
            throw new NotImplementedException();
        }

        public void SerializeValueString(ref string value)
        {
            value = this.Input.ReadStringPascalUncapped();
        }

        public void SerializeArrayString(ref string[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListString(ref List<string> list)
        {
            var count = this.Input.ReadValueU32();
            if (count > 800000)
            {
                throw new FormatException();
            }

            list = new List<string>();
            for (uint i = 0; i < count; i++)
            {
                list.Add(this.Input.ReadStringPascalUncapped());
            }
        }

        public void SerializeValueCurrentFile(ref string value)
        {
            value = this.Input.ReadStringPascalUncapped();
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
            value = this.Input.ReadStringPascalUncapped();
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

        private ICrypticStructure SerializeValueStructure(Type type, bool optional)
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

            var instance = (ICrypticStructure)Activator.CreateInstance(type);
            instance.Serialize(this);

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

        public void SerializeValueStructure<TType>(ref TType value, bool optional)
            where TType : ICrypticStructure, new()
        {
            value = (TType)SerializeValueStructure(typeof(TType), optional);
        }

        public void SerializeArrayStructure<TType>(ref TType[] array, int count)
            where TType : ICrypticStructure, new()
        {
            throw new NotImplementedException();
        }

        public void SerializeListStructure<TType>(ref List<TType> list)
            where TType : ICrypticStructure, new()
        {
            var count = this.Input.ReadValueU32();
            if (count > 800000)
            {
                throw new FormatException();
            }

            list = new List<TType>();
            for (uint i = 0; i < count; i++)
            {
                TType instance = default(TType);
                this.SerializeValueStructure<TType>(ref instance, false);
                list.Add(instance);
            }
        }

        public void SerializeValuePolymorph(ref object value, bool optional, Type[] validTypes)
        {
            if (optional == true)
            {
                var hasValue = this.Input.ReadValueU32();
                if (hasValue == 0)
                {
                    value = null;
                    return;
                }
                else if (hasValue != 1)
                {
                    throw new FormatException();
                }
            }

            var index = this.Input.ReadValueS32();
            var type = validTypes[index];
            value = SerializeValueStructure(type, false);
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
            value = this.Input.ReadValueU32();
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
            var count = this.Input.ReadValueU32();
            if (count > 800000)
            {
                throw new FormatException();
            }

            list = new List<MultiValue>();
            for (uint i = 0; i < count; i++)
            {
                object arg;

                var name = this.Input.ReadString(4, true, Encoding.ASCII);
                MultiValueOpcode op;
                if (MultiValue.TryParseOpcode(name, out op) == false)
                {
                    throw new FormatException();
                }
                
                switch (op)
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
                        arg = null;
                        break;
                    }

                    case MultiValueOpcode.S_V:
                    {
                        arg = this.Input.ReadValueU32();
                        break;
                    }

                    case MultiValueOpcode.INT:
                    case MultiValueOpcode.JZ_:
                    case MultiValueOpcode.J__:
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
                    case MultiValueOpcode.FUN:
                    case MultiValueOpcode.OBJ:
                    case MultiValueOpcode.IDS:
                    case MultiValueOpcode.RP_:
                    {
                        arg = this.Input.ReadStringPascalUncapped();
                        break;
                    }

                    default: throw new NotSupportedException("unhandled opcode in multival");
                }

                list.Add(new MultiValue()
                {
                    Op = op,
                    Arg = arg,
                });
            }
        }

        public void SerializeValueByteEnum<T>(ref T value)
        {
            value = (T)Enum.ToObject(typeof(T), this.Input.ReadValueU8());
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
            value = (T)Enum.ToObject(typeof(T), (short)this.Input.ReadValueS32());
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
            value = (T)Enum.ToObject(typeof(T), this.Input.ReadValueS32());
        }

        public void SerializeArrayInt32Enum<T>(ref T[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListInt32Enum<T>(ref List<T> list)
        {
            var count = this.Input.ReadValueU32();
            if (count > 800000)
            {
                throw new FormatException();
            }

            list = new List<T>();
            for (uint i = 0; i < count; i++)
            {
                list.Add((T)Enum.ToObject(typeof(T), this.Input.ReadValueS32()));
            }
        }

        public void SerializeValueBitEnum<T>(ref T value)
        {
            var x = this.Input.ReadValueU32();
            value = (T)Enum.ToObject(typeof(T), x);
        }

        public void SerializeArrayBitEnum<T>(ref T[] array, int count)
        {
            throw new NotImplementedException();
        }

        public void SerializeListBitEnum<T>(ref List<T> list)
        {
            throw new NotImplementedException();
        }
    }
}
