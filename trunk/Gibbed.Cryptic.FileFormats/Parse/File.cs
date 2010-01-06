using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gibbed.Cryptic.Parse
{
    public class File
    {
        public string Name;
        public UInt32 Unknown;

        public override string ToString()
        {
            return this.Name + " / " + this.Unknown.ToString("X8");
        }
    }
}
