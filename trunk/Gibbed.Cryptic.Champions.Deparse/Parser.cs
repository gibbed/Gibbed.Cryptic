using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Gibbed.Cryptic.Champions.Deparse
{
    public class Parser
    {
        public class Member
        {
            #region public enum DataType
            public enum DataType
            {
                Ignore = 0,
                Start = 1,
                End = 2,
                U8 = 3,
                S16 = 4,
                S32 = 5,
                S64 = 6,
                F32 = 7,
                String = 8,
                CurrentFile = 9,
                TimeStamp = 10,
                LineNum = 11,
                Boolean = 12,
                Flags = 13,
                BooleanFlag = 14,
                Quatpyr = 15,
                Matpyr = 16,
                FileName = 17,
                Reference = 18,
                FunctionCall = 19,
                Structure = 20,
                Polymorph = 21,
                StashTable = 22,
                Bit = 23,
                Multival = 24,
                Command = 25,
            }
            #endregion
            #region public enum DataMode
            public enum DataMode
            {
                Value,
                Dynamic,
                Static,
            }
            #endregion

            public int Index;
            public string Name;
            public DataType Type;
            public DataMode Mode;
            public int ExplicitCount;
            public string Reference;
            public Dictionary<string, string> PossibleNames = new Dictionary<string, string>();
        }

        public UInt32 Id;
        public string Name;
        public StructureMode Mode;
        public List<Member> Members = new List<Member>();
    }
}
