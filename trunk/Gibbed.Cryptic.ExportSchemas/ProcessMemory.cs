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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Gibbed.Cryptic.ExportSchemas
{
    public class ProcessMemory : IDisposable
    {
        #region Native Imports / Defines
        protected struct Native
        {
            public static Int32 TRUE = 1;
            public static Int32 FALSE = 0;

            public static IntPtr NULL = (IntPtr)(0);
            public static IntPtr INVALID_HANDLE_VALUE = (IntPtr)(-1);

            public static UInt32 SYNCHRONIZE = 0x00100000;
            public static UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
            public static UInt32 PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF;
            public static UInt32 THREAD_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x3FF;

            [DllImport("kernel32.dll")]
            public static extern UInt32 GetLastError();

            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Int32 bInheritHandle, Int32 dwProcessId);

            [DllImport("kernel32.dll")]
            public static extern Int32 ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] lpBuffer, UInt32 nSize, out UInt32 lpNumberOfBytesRead);

            [DllImport("kernel32.dll")]
            public static extern Int32 WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] lpBuffer, UInt32 nSize, out UInt32 lpNumberOfBytesWritten);

            [DllImport("kernel32.dll")]
            public static extern IntPtr OpenThread(UInt32 dwDesiredAccess, Int32 bInheritHandle, UInt32 dwThreadId);

            [DllImport("kernel32.dll")]
            public static extern UInt32 SuspendThread(IntPtr hThread);

            [DllImport("kernel32.dll")]
            public static extern UInt32 ResumeThread(IntPtr hThread);

            [DllImport("kernel32.dll")]
            public static extern Int32 CloseHandle(IntPtr hObject);
        };
        #endregion

        protected Encoding Encoding;
        protected Process Process;
        protected IntPtr Handle;

        public IntPtr MainModuleAddress
        {
            get { return this.Process.MainModule.BaseAddress; }
        }

        public int MainModuleSize
        {
            get { return this.Process.MainModule.ModuleMemorySize; }
        }

        public ProcessMemory()
        {
            this.Process = null;
            this.Handle = Native.NULL;
            this.Encoding = new ASCIIEncoding();
        }

        ~ProcessMemory()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected void Dispose(bool managed)
        {
            if (this.Handle != Native.NULL)
            {
                this.Close();
            }
        }

        public bool Open(Process process)
        {
            if (this.Handle != Native.NULL)
            {
                Native.CloseHandle(this.Handle);
                this.Process = null;
                this.Handle = Native.INVALID_HANDLE_VALUE;
            }

            IntPtr theProcess;
            theProcess = Native.OpenProcess(Native.PROCESS_ALL_ACCESS, Native.FALSE, process.Id);

            if (theProcess == Native.NULL)
            {
                return false;
            }

            this.Process = process;
            this.Handle = theProcess;
            return true;
        }

        public bool Close()
        {
            Int32 result;

            if (this.Handle == Native.NULL)
            {
                throw new InvalidOperationException("process handle is invalid");
            }

            result = Native.CloseHandle(this.Handle);

            this.Process = null;
            this.Handle = Native.NULL;

            return result == Native.TRUE ? true : false;
        }

        public uint Search(ByteSearch pattern)
        {
			const int blockSize = 0x00A00000;
            var data = new byte[blockSize];

            var address = this.MainModuleAddress;

			for (int i = 0; i < this.MainModuleSize; i += (blockSize - pattern.Size))
			{
				int size = (int)Math.Min(blockSize, this.MainModuleSize - i);

				this.Read(this.MainModuleAddress + i, data, size);

				uint result = pattern.Match(data, size);
                if (result != uint.MaxValue)
                {
                    var target = (uint)((
                        this.MainModuleAddress + i).ToInt32());
                    return target + result;
                }
			}

            return uint.MaxValue;
		}

        private int Read(IntPtr address, byte[] data, int size)
        {
            int result;
            uint read;

            if (this.Handle == Native.NULL)
            {
                throw new InvalidOperationException("process handle is invalid");
            }

            result = Native.ReadProcessMemory(this.Handle, address, data, (uint)size, out read);

            if (result == 0)
            {
                throw new NativeException("error " + Native.GetLastError().ToString());
            }

            if (read != (uint)size)
            {
                throw new InvalidOperationException("only read " + read.ToString() + " instead of " + size);
            }

            return (int)read;
        }

        public byte[] ReadBytes(uint address, int size)
        {
            var data = new byte[size];
            if (this.Read((IntPtr)address, data, size) != size)
            {
                throw new InvalidOperationException();
            }
            return data;
        }

        public T ReadStructure<T>(uint address)
        {
            GCHandle handle;
            int structureSize;
            byte[] buffer;

            structureSize = Marshal.SizeOf(typeof(T));
            
            buffer = this.ReadBytes(address, structureSize);
            if (buffer.Length != structureSize)
            {
                throw new EndOfStreamException("could not read all of data for structure");
            }

            handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));

            handle.Free();

            return structure;
        }

        public byte ReadU8(uint address)
        {
            var data = this.ReadBytes(address, 1);
            return data[0];
        }

        public sbyte ReadS8(uint address)
        {
            var data = this.ReadBytes(address, 1);
            return (sbyte)data[0];
        }

        public ushort ReadU16(uint address)
        {
            var data = this.ReadBytes(address, 2);
            return BitConverter.ToUInt16(data, 0);
        }

        public short ReadS16(uint address)
        {
            var data = this.ReadBytes(address, 2);
            return BitConverter.ToInt16(data, 0);
        }

        public uint ReadU32(uint address)
        {
            var data = this.ReadBytes(address, 4);
            return BitConverter.ToUInt32(data, 0);
        }

        public int ReadS32(uint address)
        {
            var data = this.ReadBytes(address, 4);
            return BitConverter.ToInt32(data, 0);
        }

        public ulong ReadU64(uint address)
        {
            var data = this.ReadBytes(address, 8);
            return BitConverter.ToUInt64(data, 0);
        }

        public long ReadS64(uint address)
        {
            var data = this.ReadBytes(address, 8);
            return BitConverter.ToInt64(data, 0);
        }

        public float ReadF32(uint address)
        {
            var data = this.ReadBytes(address, 4);
            return BitConverter.ToSingle(data, 0);
        }

        public double ReadF64(uint address)
        {
            var data = this.ReadBytes(address, 8);
            return BitConverter.ToDouble(data, 0);
        }

        public string ReadString(uint address, int length)
        {
            if (address == 0)
            {
                return null;
            }

            var data = this.ReadBytes(address, length);

            int end = 0;
            for (int i = 0; i < length; i++)
            {
                if (data[i] == 0)
                {
                    break;
                }
                end = i + 1;
            }

            return Encoding.ASCII.GetString(data, 0, end);
        }

        public string ReadStringZ(uint address)
        {
            if (address == 0)
            {
                return null;
            }

            var data = new byte[0];
            var current = 0;

            while (true)
            {
                var block = this.ReadBytes(address + (uint)current, 256);

                Array.Resize(ref data, data.Length + block.Length);
                Array.Copy(block, 0, data, current, block.Length);

                for (int i = current; i < data.Length; i++)
                {
                    if (data[i] == 0)
                    {
                        return Encoding.ASCII.GetString(data, 0, i);
                    }
                }

                current += block.Length;

                if (current >= 102480)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        protected bool SuspendThread(int id)
        {
            IntPtr handle = Native.OpenThread(Native.THREAD_ALL_ACCESS, Native.FALSE, (uint)id);

            if (handle == Native.NULL)
            {
                return false;
            }

            if (Native.SuspendThread(handle) == 0xFFFFFFFF)
            {
                return false;
            }

            Native.CloseHandle(handle);
            return true;
        }

        protected bool ResumeThread(int id)
        {
            IntPtr handle = Native.OpenThread(Native.THREAD_ALL_ACCESS, Native.FALSE, (uint)id);

            if (handle == Native.NULL)
            {
                return false;
            }

            if (Native.ResumeThread(handle) == 0xFFFFFFFF)
            {
                return false;
            }

            Native.CloseHandle(handle);
            return true;
        }

        public bool Suspend()
        {
            bool result = true;
            for (int i = 0; i < this.Process.Threads.Count; i++)
            {
                if (this.SuspendThread(this.Process.Threads[i].Id) == false)
                {
                    result = false;
                }
            }
            return result;
        }

        public bool Resume()
        {
            bool result = true;
            for (int i = 0; i < this.Process.Threads.Count; i++)
            {
                if (this.ResumeThread(this.Process.Threads[i].Id) == false)
                {
                    result = false;
                }
            }
            return result;
        }
    }
}
