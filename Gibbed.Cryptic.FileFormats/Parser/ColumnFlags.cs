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

namespace Gibbed.Cryptic.FileFormats.Parser
{
    [Flags]
    public enum ColumnFlags : ulong
    {
        None = 0ul,

        TokenMask          = 0x00FFul,
        NumBitsMask        = 0xFF00ul,

        POOL_STRING        = 1ul << 16,
        ESTRING            = 1ul << 17,
        EARRAY             = 1ul << 18,
        FIXED_ARRAY        = 1ul << 19,
        INDIRECT           = 1ul << 20,
        OBJECTTYPE         = 1ul << 21,
        ALIAS              = 1ul << 22, // REDUNDANT?
        STRUCTPARAM        = 1ul << 23,
        ALWAYS_ALLOC       = 1ul << 24,
        NON_NULL_REF       = 1ul << 25,
        REQUIRED           = 1ul << 26,
        NO_WRITE           = 1ul << 27,
        NO_NETSEND         = 1ul << 28,
        FLATEMBED          = 1ul << 29,
        NO_TEXT_SAVE       = 1ul << 30,
        GLOBAL_NAME        = 1ul << 31,
        UNKNOWN_32         = 1ul << 32,
        USEDFIELD          = 1ul << 33,
        USEROPTIONBIT_1    = 1ul << 34,
        USEROPTIONBIT_2    = 1ul << 35,
        USEROPTIONBIT_3    = 1ul << 36,
        POOL_STRING_DB     = 1ul << 37,
        DEFAULT_FIELD      = 1ul << 38,
        DEMO_IGNORE        = 1ul << 39,
        PUPPET_NO_COPY     = 1ul << 40,
        SUBSCRIBE          = 1ul << 41,
        SERVER_ONLY        = 1ul << 42,
        CLIENT_ONLY        = 1ul << 43,
        SELF_ONLY          = 1ul << 44,
        SELF_AND_TEAM_ONLY = 1ul << 45,
        LOGIN_SUBSCRIBE    = 1ul << 46,
        KEY                = 1ul << 47,
        PERSIST            = 1ul << 48,
        NO_TRANSACT        = 1ul << 49,
        SOMETIMES_TRANSACT = 1ul << 50,
        VITAL_REF          = 1ul << 51,
        NON_NULL_REF__ERROR_ONLY = 1ul << 52,
        NO_LOG             = 1ul << 53,
        DIRTY_BIT          = 1ul << 54,
        NO_INHERIT         = 1ul << 55,
        IGNORE_STRUCT      = 1ul << 56,
        SPECIAL_DEFAULT    = 1ul << 57,
        PARSETABLE_INFO    = 1ul << 58,
        INHERITANCE_STRUCT = 1ul << 59,
        STRUCT_NORECURSE   = 1ul << 60,
        CASE_SENSITIVE     = 1ul << 61,
        EDIT_ONLY          = 1ul << 62,
        NO_INDEX           = 1ul << 63,
    }
}
