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

namespace Gibbed.Cryptic.FileFormats
{
    public enum MultiValueOpcode : ushort
    {
        InstructionMask = 0xFF00,
        TypeMask = 0x00FF,

        INV = 0xFFFF, // invalid

        NON = 0x0000, // none
        StaticVariable = 0x0001, // internal type associated with S_V
        INT = 0x0002, // int
        FLT = 0x0004, // flt
        INS = 0x0005, // intarray
        FLS = 0x0006, // floatarray
        VEC = 0x0007, // Vec3
        VC4 = 0x0008, // Vec4
        MAT = 0x0009, // Mat4
        QAT = 0x000A, // Quat
        STR = 0x000B, // str
        FIL = 0x000C, // multivalarray
        ENT = 0x0080, // entityarray
        PTR = 0x0081, // ptr

        ADD = 0x0100, // +
        SUB = 0x0200, // minus
        NEG = 0x0300, // neg
        MUL = 0x0400, // *
        DIV = 0x0500, // /
        EXP = 0x0600, // ^
        BAN = 0x0700, // &
        BOR = 0x0800, // |
        BNT = 0x0900, // ~
        BXR = 0x0A00, // BXR
        O_P = 0x0B00, // (
        C_P = 0x0C00, // )
        O_B = 0x0D00, // [
        C_B = 0x0E00, // ]
        EQU = 0x0F00, // =
        LES = 0x1000, // <
        NGR = 0x1100, // <=
        GRE = 0x1200, // >
        NLE = 0x1300, // >=
        FUN = 0x140B, // func
        IDS = 0x160B, // ident
        S_V = 0x1701, // staticvar
        COM = 0x1800, // ,
        AND = 0x1900, // and
        ORR = 0x1A00, // or
        NOT = 0x1B00, // not
        IF_ = 0x1C00, // if
        ELS = 0x1D00, // else
        ELF = 0x1E00, // elif
        EIF = 0x1F00, // endif
        RET = 0x2000, // return
        RZ_ = 0x2100, // retifzero
        J__ = 0x2202, // j
        JZ_ = 0x2302, // jz
        CON = 0x2400, // continuation
        STM = 0x2500, // ;
        RP_ = 0x260B, // rootpath
        OBJ = 0x270B, // objpath
        L_M = 0x2809, // loc (mat4)
        L_S = 0x280B, // loc (str)
    }
}
