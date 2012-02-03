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

using System.Reflection.Emit;

namespace Gibbed.StarTrekOnline.GenerateSerializer
{
    internal static class ILGeneratorHelpers
    {
        private static OpCode[] Predefined =
        {
            OpCodes.Ldc_I4_0,
            OpCodes.Ldc_I4_1,
            OpCodes.Ldc_I4_2,
            OpCodes.Ldc_I4_3,
            OpCodes.Ldc_I4_4,
            OpCodes.Ldc_I4_5,
            OpCodes.Ldc_I4_6,
            OpCodes.Ldc_I4_7,
            OpCodes.Ldc_I4_8,
        };

        public static void EmitConstant(this ILGenerator msil, int value)
        {
            if (value == -1)
            {
                msil.Emit(OpCodes.Ldc_I4_M1);
            }
            else if (value < Predefined.Length)
            {
                msil.Emit(Predefined[value]);
            }
            else if (value >= -127 && value <= 128)
            {
                msil.Emit(OpCodes.Ldc_I4_S, (byte)value);
            }
            else
            {
                msil.Emit(OpCodes.Ldc_I4, value);
            }
        }
    }
}
