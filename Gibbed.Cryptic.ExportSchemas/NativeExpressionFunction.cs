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

using System.Runtime.InteropServices;

namespace Gibbed.Cryptic.ExportSchemas
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeExpressionFunction
    {
        public int ArgumentCount; // args
        public int ExprCodeEnum;
        public uint Handler;
        public uint Unknown00C;
        public uint NamePointer; // funcName
        public uint CommentPointer; // comment
        public uint SourceFilePointer; // SourceFile
        public uint LineNumber; // LineNum

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public NativeExpressionArgument[] Arguments; // args
        
        public NativeExpressionArgument ReturnType; // returnType

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public uint[] Tags; // tags
        
        public float Cost; // cost
        public uint ExprCodeNamePointer; // ExprCodeName
        public uint Unknown1B8;
        public uint Flags; // funcFlags
        public uint ParsedTags; // parsedTags
    }
}
