/* Copyright (c) 2021 Rick (rick 'at' gibbed 'dot' us)
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Gibbed.Cryptic.ExportSchemas.Runtime
{
    internal abstract class RuntimeBase : IDisposable
    {
        private readonly string _ObjectName;
        private bool _Disposed;

        public RuntimeBase(string objectName)
        {
            this._ObjectName = objectName ?? throw new ArgumentNullException(nameof(objectName));
        }

        public abstract bool Is32Bit { get; }

        ~RuntimeBase()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            this._Disposed = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract int Read(IntPtr address, byte[] buffer, int offset, int length);

        public byte[] ReadBytes(IntPtr address, int length)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            var buffer = new byte[length];
            if (this.Read(address, buffer, 0, length) != length)
            {
                throw new InvalidOperationException();
            }
            return buffer;
        }

        public byte ReadValueU8(IntPtr address)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            return this.ReadBytes(address, 1)[0];
        }

        public sbyte ReadValueS8(IntPtr address)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            return (sbyte)this.ReadBytes(address, 1)[0];
        }

        public ushort ReadValueU16(IntPtr address)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            var buffer = this.ReadBytes(address, 2);
            return BitConverter.ToUInt16(buffer, 0);
        }

        public short ReadValueS16(IntPtr address)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            var buffer = this.ReadBytes(address, 2);
            return BitConverter.ToInt16(buffer, 0);
        }

        public uint ReadValueU32(IntPtr address)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            var buffer = this.ReadBytes(address, 4);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public int ReadValueS32(IntPtr address)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            var buffer = this.ReadBytes(address, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public ulong ReadValueU64(IntPtr address)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            var buffer = this.ReadBytes(address, 8);
            return BitConverter.ToUInt64(buffer, 0);
        }

        public long ReadValueS64(IntPtr address)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            var buffer = this.ReadBytes(address, 8);
            return BitConverter.ToInt64(buffer, 0);
        }

        public float ReadValueF32(IntPtr address)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            var buffer = this.ReadBytes(address, 4);
            return BitConverter.ToSingle(buffer, 0);
        }

        public double ReadValueF64(IntPtr address)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            var buffer = this.ReadBytes(address, 8);
            return BitConverter.ToDouble(buffer, 0);
        }

        public string ReadString(IntPtr address, int length, Encoding encoding)
        {
            if (address == default)
            {
                return default;
            }

            if (length == 0)
            {
                return "";
            }

            int characterSize = encoding.GetByteCount("e");
            Debug.Assert(characterSize == 1 || characterSize == 2 || characterSize == 4);
            char characterEnd = '\0';

            var data = this.ReadBytes(address, characterSize * length);
            var str = encoding.GetString(data, 0, data.Length);

            var index = str.IndexOf(characterEnd);
            if (index >= 0)
            {
                str = str.Substring(0, index);
            }

            return str;
        }

        public string ReadStringZ(IntPtr address, Encoding encoding)
        {
            if (address == default)
            {
                return default;
            }

            int characterSize = encoding.GetByteCount("e");
            Debug.Assert(characterSize == 1 || characterSize == 2 || characterSize == 4);
            string characterEnd = '\0'.ToString(CultureInfo.InvariantCulture);

            int i = 0;
            var data = new byte[128 * characterSize];

            while (true)
            {
                if (i + characterSize > data.Length)
                {
                    Array.Resize(ref data, data.Length + (128 * characterSize));
                }

                int read = this.Read(address, data, i, characterSize);
                Debug.Assert(read == characterSize);

                if (encoding.GetString(data, i, characterSize) == characterEnd)
                {
                    break;
                }

                address += read;
                i += characterSize;
            }

            if (i == 0)
            {
                return "";
            }

            return encoding.GetString(data, 0, i);
        }

        public T ReadStructure<T>(IntPtr address)
        {
            var structureSize = Marshal.SizeOf(typeof(T));
            var buffer = this.ReadBytes(address, structureSize);
            if (buffer.Length != structureSize)
            {
                throw new EndOfStreamException("could not read all of data for structure");
            }
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return structure;
        }

        public T[] ReadStructureArray<T>(IntPtr address, int count)
        {
            var structureSize = Marshal.SizeOf(typeof(T));
            var bufferSize = count * structureSize;
            var buffer = this.ReadBytes(address, bufferSize);
            if (buffer.Length != bufferSize)
            {
                throw new EndOfStreamException("could not read all of data for structures");
            }
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var current = handle.AddrOfPinnedObject();
            var items = new T[count];
            for (var o = 0; o < count; o++)
            {
                items[o] = (T)Marshal.PtrToStructure(current, typeof(T));
                current += structureSize;
            }
            handle.Free();
            return items;
        }

        public IntPtr ReadPointer(IntPtr address)
        {
            return this.Is32Bit == false
                ? new IntPtr(this.ReadValueS64(address))
                : new IntPtr(this.ReadValueU32(address));
        }

        public abstract int Write(IntPtr address, byte[] buffer, int offset, int length);

        public void WriteBytes(IntPtr address, byte[] buffer)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (this.Write(address, buffer, 0, buffer.Length) != buffer.Length)
            {
                throw new InvalidOperationException();
            }
        }

        public void WriteValueU8(IntPtr address, byte value)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            this.WriteBytes(address, new[] { value });
        }

        public void WriteValueS8(IntPtr address, sbyte value)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            this.WriteBytes(address, new[] { (byte)value });
        }

        public void WriteValueU16(IntPtr address, ushort value)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            this.WriteBytes(address, BitConverter.GetBytes(value));
        }

        public void WriteValueS16(IntPtr address, short value)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            this.WriteBytes(address, BitConverter.GetBytes(value));
        }

        public void WriteValueU32(IntPtr address, uint value)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            this.WriteBytes(address, BitConverter.GetBytes(value));
        }

        public void WriteValueS32(IntPtr address, int value)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            this.WriteBytes(address, BitConverter.GetBytes(value));
        }

        public void WriteValueU64(IntPtr address, ulong value)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            this.WriteBytes(address, BitConverter.GetBytes(value));
        }

        public void WriteValueS64(IntPtr address, long value)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            this.WriteBytes(address, BitConverter.GetBytes(value));
        }

        public void WriteValueF32(IntPtr address, float value)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            this.WriteBytes(address, BitConverter.GetBytes(value));
        }

        public void WriteValueF64(IntPtr address, double value)
        {
            if (this._Disposed == true)
            {
                throw new ObjectDisposedException(this._ObjectName);
            }
            this.WriteBytes(address, BitConverter.GetBytes(value));
        }

        public void WriteStructure<T>(IntPtr address, T structure)
        {
            if (address == default)
            {
                throw new ArgumentNullException(nameof(address));
            }
            var structureSize = Marshal.SizeOf(typeof(T));
            var buffer = new byte[structureSize];
            GCHandle handle = default;
            try
            {
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                Marshal.StructureToPtr(structure, handle.AddrOfPinnedObject(), false);
            }
            finally
            {
                if (handle.IsAllocated == true)
                {
                    handle.Free();
                }
            }
            this.WriteBytes(address, buffer);
        }

        public void WriteStructureArray<T>(IntPtr address, T[] items)
        {
            if (address == default)
            {
                throw new ArgumentNullException(nameof(address));
            }
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            if (items.Length == 0)
            {
                return;
            }
            var structureSize = Marshal.SizeOf(typeof(T));
            var bufferSize = items.Length * structureSize;
            var buffer = new byte[bufferSize];
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var current = handle.AddrOfPinnedObject();
            for (var i = 0; i < items.Length; i++)
            {
                Marshal.StructureToPtr(items[i], current, false);
                current += structureSize;
            }
            handle.Free();
            this.WriteBytes(address, buffer);
        }

        public void WritePointer(IntPtr address, IntPtr value)
        {
            if (this.Is32Bit == false)
            {
                this.WriteValueS64(address, value.ToInt64());
            }
            else
            {
                this.WriteValueU32(address, (uint)value.ToInt64());
            }
        }
    }
}
