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

namespace Gibbed.Cryptic.FileFormats.ParserSchema
{
    public enum ColumnFormat
    {
        // ReSharper disable InconsistentNaming
        None = 0,
        IP = 1,
        Unsigned = 2,
        DatesS2000 = 3,
        Percent = 4,
        HSV = 5,
        Unknown6 = 6,
        Texture = 7,
        Color = 8,
        FriendlyDate = 9,
        FriendlySS2000 = 10,
        KBytes = 11,
        Flags = 12,
        // ReSharper restore InconsistentNaming
    }
}
