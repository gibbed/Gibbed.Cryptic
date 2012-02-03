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
    public class BlobWriter : ICrypticStream
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

            public void Serialize(ICrypticStream stream)
            {
                stream.SerializeListStructure<TType>(ref this.List);
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

        public void SerializeValueInt16(ref short value)
        {
            this.Output.WriteValueS32(value);
        }

        public void SerializeValueInt32(ref int value)
        {
            this.Output.WriteValueS32(value);
        }

        public void SerializeValueFloat(ref float value)
        {
            this.Output.WriteValueF32(value);
        }

        public void SerializeValueString(ref string value)
        {
            this.Output.WriteStringPascalUncapped(value);
        }

        public void SerializeValueReference(ref string value)
        {
            Output.WriteStringPascalUncapped(value);
        }

        public void SerializeValueCurrentFile(ref string value)
        {
            this.Output.WriteStringPascalUncapped(value);
        }

        private void SerializeValueStructure(object value, bool optional)
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
                ((ICrypticStructure)value).Serialize(new BlobWriter(data));
                data.Position = 0;
                this.Output.WriteValueU32((uint)data.Length);
                this.Output.WriteFromStream(data, data.Length);
            }
#else
            var size = this.Output.Position;
            this.Output.Seek(4, SeekOrigin.Current);

            var start = this.Output.Position;
            ((ICrypticStructure)value).Serialize(this);

            var end = this.Output.Position;

            this.Output.Seek(size, SeekOrigin.Begin);
            Output.WriteValueU32((uint)(end - start));
            this.Output.Seek(end, SeekOrigin.Begin);
#endif
        }

        public void SerializeValueStructure<TType>(ref TType value, bool optional)
            where TType : ICrypticStructure, new()
        {
            SerializeValueStructure(value, optional);
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
            SerializeValueStructure(value, false);
        }

        public void SerializeValueBit(ref uint value)
        {
            this.Output.WriteValueU32(value);
        }

        public void SerializeArrayInt16(ref short[] array, int count)
        {
            if (array.Length != count)
            {
                throw new InvalidOperationException();
            }

            for (int i = 0; i < count; i++)
            {
                this.Output.WriteValueS32(array[i]);
            }
        }

        public void SerializeArrayInt32(ref int[] array, int count)
        {
            if (array.Length != count)
            {
                throw new InvalidOperationException();
            }

            for (int i = 0; i < count; i++)
            {
                this.Output.WriteValueS32(array[i]);
            }
        }

        public void SerializeArrayFloat(ref float[] array, int count)
        {
            if (array.Length != count)
            {
                throw new InvalidOperationException();
            }

            for (int i = 0; i < count; i++)
            {
                this.Output.WriteValueF32(array[i]);
            }
        }

        public void SerializeListInt32(ref List<int> list)
        {
            if (list.Count > 800000)
            {
                throw new InvalidOperationException();
            }

            this.Output.WriteValueS32(list.Count);
            foreach (var item in list)
            {
                this.Output.WriteValueS32(item);
            }
        }

        public void SerializeListString(ref List<string> list)
        {
            if (list.Count > 800000)
            {
                throw new InvalidOperationException();
            }

            this.Output.WriteValueS32(list.Count);
            foreach (var item in list)
            {
                this.Output.WriteStringPascalUncapped(item);
            }
        }

        public void SerializeListStructure<TType>(ref List<TType> list)
            where TType : ICrypticStructure, new()
        {
            if (list.Count > 800000)
            {
                throw new InvalidOperationException();
            }

            this.Output.WriteValueS32(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                this.SerializeValueStructure<TType>(ref item, false);
            }
        }

        // todo: create real multival handling
        public void SerializeListMultiValue(ref List<MultiValueInstruction> list)
        {
            if (list.Count > 800000)
            {
                throw new InvalidOperationException();
            }

            this.Output.WriteValueS32(list.Count);
            foreach (var item in list)
            {
                this.Output.WriteString(item.Op, 4, Encoding.ASCII);

                switch (item.Op)
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
                        break;
                    }

                    case "S_V":
                    {
                        this.Output.WriteValueU32((uint)item.Arg);
                        break;
                    }

                    case "INT":
                    case "JZ_":
                    case "J__":
                    {
                        this.Output.WriteValueS64((long)item.Arg);
                        break;
                    }

                    case "FLT":
                    {
                        this.Output.WriteValueF64((double)item.Arg);
                        break;
                    }

                    case "STR":
                    case "FUN":
                    case "OBJ":
                    case "IDS":
                    case "RP_":
                    {
                        this.Output.WriteStringPascalUncapped((string)item.Arg);
                        break;
                    }

                    default:
                    throw new NotSupportedException("unhandled opcode in multival");
                }
            }
        }
    }
}
