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
    public class BlobReader : ICrypticStream
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

            public void Serialize(ICrypticStream stream)
            {
                stream.SerializeListStructure<TType>(ref this.List);
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

        public void SerializeValueInt16(ref short value)
        {
            value = (short)this.Input.ReadValueS32();
        }

        public void SerializeValueInt32(ref int value)
        {
            value = this.Input.ReadValueS32();
        }

        public void SerializeValueFloat(ref float value)
        {
            value = this.Input.ReadValueF32();
        }

        public void SerializeValueString(ref string value)
        {
            value = this.Input.ReadStringPascalUncapped();
        }

        public void SerializeValueReference(ref string value)
        {
            value = this.Input.ReadStringPascalUncapped();
        }

        public void SerializeValueCurrentFile(ref string value)
        {
            value = this.Input.ReadStringPascalUncapped();
        }

        private object SerializeValueStructure(Type type, bool optional)
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

        public void SerializeValueBit(ref uint value)
        {
            value = this.Input.ReadValueU32();
        }

        public void SerializeArrayInt16(ref short[] array, int count)
        {
            array = new short[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = (short)this.Input.ReadValueS32();
            }
        }

        public void SerializeArrayInt32(ref int[] array, int count)
        {
            array = new int[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = this.Input.ReadValueS32();
            }
        }

        public void SerializeArrayFloat(ref float[] array, int count)
        {
            array = new float[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = this.Input.ReadValueF32();
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

        // todo: create real multival handling
        public void SerializeListMultiValue(ref List<MultiValueInstruction> list)
        {
            var count = this.Input.ReadValueU32();
            if (count > 800000)
            {
                throw new FormatException();
            }

            list = new List<MultiValueInstruction>();
            for (uint i = 0; i < count; i++)
            {
                object arg;

                var name = this.Input.ReadString(4, true, Encoding.ASCII);
                switch (name)
                {
                    case "NON":
                    case "O_P":
                    case "C_P":
                    case "STM":
                    case "LES":
                    case "ADD":
                    case "SUB":
                    case "MUL":
                    case "EQU":
                    case "NOT":
                    case "RET":
                    case "AND":
                    case "NEG":
                    case "ORR":
                    case "BAN":
                    case "RZ_":
                    case "NLE":
                    case "GRE":
                    case "DIV":
                    case "BOR":
                    case "NGR":
                    case "BNT":
                    {
                        arg = null;
                        break;
                    }

                    case "S_V":
                    {
                        arg = this.Input.ReadValueU32();
                        break;
                    }

                    case "INT":
                    case "JZ_":
                    case "J__":
                    {
                        arg = this.Input.ReadValueS64();
                        break;
                    }

                    case "FLT":
                    {
                        arg = this.Input.ReadValueF64();
                        break;
                    }

                    case "STR":
                    case "FUN":
                    case "OBJ":
                    case "IDS":
                    case "RP_":
                    {
                        arg = this.Input.ReadStringPascalUncapped();
                        break;
                    }

                    default: throw new NotSupportedException("unhandled opcode in multival");
                }

                list.Add(new MultiValueInstruction()
                    {
                        Op = name,
                        Arg = arg,
                    });
            }
        }
    }
}
