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
using System.Runtime.InteropServices;

namespace Gibbed.Cryptic.ExportSchemas
{
    internal static class Win32
    {
        private const string DllName = "kernel32";
        private const CallingConvention CC = CallingConvention.Winapi;

        public static readonly IntPtr InvalidHandleValue = (IntPtr)(-1);

        public static uint Synchronize = 0x00100000u;
        public static uint StandardRightsRequired = 0x000F0000u;
        public static uint ProcessAllAccess = StandardRightsRequired | Synchronize | 0xFFFu;
        public static uint ThreadAllAccess = StandardRightsRequired | Synchronize | 0x3FFu;

        [DllImport(DllName, SetLastError = true, CallingConvention = CC)]
        public static extern IntPtr OpenProcess(
            [In] uint desiredAccess,
            [In][MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            [In] uint processId);

        [DllImport(DllName, SetLastError = true, CallingConvention = CC)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(
            [In] IntPtr process,
            [In] IntPtr baseAddress,
            [In] IntPtr buffer,
            [In] uint size,
            [Out] out uint numberOfBytesRead);

        [DllImport(DllName, SetLastError = true, CallingConvention = CC)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteProcessMemory(
            [In] IntPtr process,
            [In] IntPtr baseAddress,
            [In] IntPtr buffer,
            [In] uint size,
            [Out] out uint numberOfBytesWritten);

        [DllImport(DllName, SetLastError = true, CallingConvention = CC)]
        public static extern IntPtr OpenThread(
            [In] uint desiredAccess,
            [In][MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            [In] uint threadId);

        [DllImport(DllName, SetLastError = true, CallingConvention = CC)]
        public static extern uint SuspendThread([In] IntPtr thread);

        [DllImport(DllName, SetLastError = true, CallingConvention = CC)]
        public static extern uint ResumeThread([In] IntPtr thread);

        [DllImport(DllName, SetLastError = true, CallingConvention = CC)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle([In] IntPtr handle);

        [DllImport(DllName, SetLastError = true, CallingConvention = CC)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
    }
}
