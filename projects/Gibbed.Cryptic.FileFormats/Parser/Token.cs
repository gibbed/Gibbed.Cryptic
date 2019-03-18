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

using System.IO;
using System.Xml;

namespace Gibbed.Cryptic.FileFormats.Parser
{
    public abstract class Token
    {
        public virtual StorageCompatability Storage { get { return StorageCompatability.None; } }

        public virtual string NameDirectValue        { get { return null; } }
        public virtual string NameDirectFixedArray   { get { return null; } }
        public virtual string NameDirectArray        { get { return null; } }
        public virtual string NameIndirectValue      { get { return null; } }
        public virtual string NameIndirectFixedArray { get { return null; } }
        public virtual string NameIndirectArray      { get { return null; } }

        public abstract ColumnParameter GetParameter(ColumnFlags flags, int index);

        public abstract void Deserialize(Stream input, ParserSchema.Column column, XmlWriter output);
    }
}
