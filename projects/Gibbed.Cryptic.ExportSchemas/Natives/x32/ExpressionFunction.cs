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

namespace Gibbed.Cryptic.ExportSchemas.Natives.x32
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct ExpressionFunction
    {
        public int ArgumentCount; // Argc
        public int ExprCodeEnum; // Exprcodeenum
        public uint Handler;
        public uint Unknown010;
        public uint NamePointer; // FuncName

        // CO doesn't have these for some reason.
        //public uint CommentPointer; // Comment
        //public uint SourceFilePointer; // Sourcefile
        //public uint LineNumber; // Linenum

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public ExpressionArgument[] Arguments; // Args

        public ExpressionArgument ReturnType; // ReturnType

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public uint[] TagPointers; // Tags

        public float Cost; // Cost
        public uint ExprCodeNamePointer; // Exprcodename
        public uint Unknown318;
        public uint Flags; // Funcflags
        public uint ParsedTagsPointer;

        public x64.ExpressionFunction Upgrade()
        {
            var arguments = new x64.ExpressionArgument[this.Arguments.Length];
            for (int i = 0; i < this.Arguments.Length; i++)
            {
                arguments[i] = this.Arguments[i].Upgrade();
            }
            var tagPointers = new IntPtr[this.TagPointers.Length];
            for (int i = 0; i < this.TagPointers.Length; i++)
            {
                tagPointers[i] = new IntPtr(this.TagPointers[i]);
            }
            return new x64.ExpressionFunction()
            {
                ArgumentCount = this.ArgumentCount,
                ExprCodeEnum = this.ExprCodeEnum,
                Handler = new IntPtr(this.Handler),
                Unknown010 = new IntPtr(this.Unknown010),
                NamePointer = new IntPtr(this.NamePointer),
                CommentPointer = default,//new IntPtr(this.CommentPointer),
                SourceFilePointer = default,//new IntPtr(this.SourceFilePointer),
                LineNumber = default,//this.LineNumber,
                Arguments = arguments,
                ReturnType = this.ReturnType.Upgrade(),
                TagPointers = tagPointers,
                Cost = this.Cost,
                ExprCodeNamePointer = new IntPtr(this.ExprCodeNamePointer),
                Unknown318 = new IntPtr(this.Unknown318),
                Flags = this.Flags,
                ParsedTagsPointer = new IntPtr(this.ParsedTagsPointer),
            };
        }
    }
}
