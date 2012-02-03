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

namespace Gibbed.Cryptic.FileFormats.Parser
{
    public enum Opcode : ushort
    {
        TypeMask = 0x00FF,

        INV = 0xFFFF, // invalid

        NON = 0x0000, // null?
        Register = 0x0001,
        INT = 0x0002, // integer
        FLT = 0x0004, // float
        INS = 0x0005, // ?
        FLS = 0x0006, // ?
        VEC = 0x0007, // vector3
        VC4 = 0x0008, // vector4
        MAT = 0x0009, // matpyr
        QAT = 0x000A, // quatpyr
        STR = 0x000B, // string
        FIL = 0x000C, // filename
        ENT = 0x0080, // ?
        PTR = 0x0081, // pointer
        
        ADD = 0x0100, // add
        SUB = 0x0200, // subtract
        NEG = 0x0300, // negate
        MUL = 0x0400, // multiply
        DIV = 0x0500, // divide
        EXP = 0x0600, // exponent
        BAN = 0x0700, // binary and
        BOR = 0x0800, // binary or
        BNT = 0x0900, // binary not
        BXR = 0x0A00, // binary xor
        O_P = 0x0B00, // open paranthesis
        C_P = 0x0C00, // close parentesis
        O_B = 0x0D00, // open brace
        C_B = 0x0E00, // close brace
        EQU = 0x0F00, // equals
        LES = 0x1000, // ?
        NGR = 0x1100, // ?
        GRE = 0x1200, // ?
        NLE = 0x1300, // ?
        FUN = 0x1400, // function call
        IDS = 0x160B, // ?
        S_V = 0x1701, // ?
        COM = 0x1800, // ?
        AND = 0x1900, // ?
        ORR = 0x1A00, // ?
        NOT = 0x1B00, // ?
        IF_ = 0x1C00, // ?
        ELS = 0x1D00, // ?
        ELF = 0x1E00, // ?
        EIF = 0x1F00, // ?
        RET = 0x2000, // ?
        RZ_ = 0x2100, // ?
        J__ = 0x2202, // ?
        JZ_ = 0x2302, // ?
        CON = 0x2400, // ?
        STM = 0x2500, // ?
        RP_ = 0x260B, // ?
        OBJ = 0x270B, // ?
        L_M = 0x2809, // ?
        L_S = 0x280B, // ?
    }
}
