﻿/* Copyright (c) 2021 Rick (rick 'at' gibbed 'dot' us)
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

namespace Gibbed.Cryptic.FileFormats.Parse
{
    public abstract class BasicValueToken : Token
    {
        public override ColumnParameter GetParameter(ColumnFlags flags, int index)
        {
            switch (index)
            {
                case 0:
                {
                    if (flags.HasAny(ColumnFlags.FIXED_ARRAY) == true)
                    {
                        return ColumnParameter.NumberOfElements;
                    }

                    if (flags.HasAny(ColumnFlags.EARRAY) == true)
                    {
                        return ColumnParameter.None;
                    }

                    return ColumnParameter.Default;
                }

                case 1:
                {
                    return ColumnParameter.StaticDefineList;
                }

                default:
                {
                    return ColumnParameter.None;
                }
            }
        }
    }
}
