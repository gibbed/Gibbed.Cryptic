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

namespace Gibbed.Cryptic.FileFormats.ParseSchema
{
    /* If this is updated, make sure to update
     * Gibbed.Cryptic.ExportSchemas.Program.FormatNames
     */

    public enum ColumnFormat
    {
        // ReSharper disable InconsistentNaming
        None,
        IP, // 1
        UNSIGNED, // 2
        DATESS2000, // 3
        PERCENT, // 4
        HSV, // 5
        TEXTURE, // 7
        COLOR, // 8
        FRIENDLYDATE, // 9
        FRIENDLYSS2000, // 10
        KBYTES, // 11
        FLAGS, // 12
        INT_WAS_BOOL, // 13
        // ReSharper restore InconsistentNaming
    }
}
