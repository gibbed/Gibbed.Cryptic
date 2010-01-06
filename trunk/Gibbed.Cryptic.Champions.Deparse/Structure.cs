using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gibbed.Cryptic.Champions.Deparse
{
    public class Structure
    {
        public string Type;
        public StructureMode Mode;
        public List<KeyValuePair<string, object>> Datums = new List<KeyValuePair<string, object>>();
    }
}
