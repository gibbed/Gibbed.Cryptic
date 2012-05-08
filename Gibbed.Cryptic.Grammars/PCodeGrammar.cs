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
using Irony.Parsing;

namespace Gibbed.Cryptic.Grammars
{
    [Language("Cryptic PCode")]
    public class PCodeGrammar : Grammar
    {
        public PCodeGrammar()
            : base(false)
        {
            // ReSharper disable InconsistentNaming
            this.LanguageFlags = LanguageFlags.NewLineBeforeEOF;

            var integerLiteral = new NumberLiteral("INTEGER");
            integerLiteral.Options |= NumberOptions.IntOnly;
            integerLiteral.DefaultIntTypes = new[]
            {
                TypeCode.Int64
            };
            integerLiteral.AddPrefix("0x", NumberOptions.Hex);

            var floatLiteral = new NumberLiteral("FLOAT")
            {
                DefaultIntTypes = new[]
                {
                    TypeCode.Double
                },
            };

            var stringLiteral = new StringLiteral("STRING",
                                                  "\"",
                                                  StringOptions.AllowsLineBreak |
                                                  StringOptions.AllowsOctalEscapes |
                                                  StringOptions.AllowsXEscapes);

            var name = new IdentifierTerminal("NAME", IdOptions.IsNotKeyword);

            var comment = new CommentTerminal("comment", "#", "\n");

            var comma = ToTerm(",", "comma");
            var colon = ToTerm(":", "colon");

            var staticLiteral = new NonTerminal("STATIC")
            {
                Rule = this.ToTerm("Activation") | "AdjustLevel" | "Application" |
                       "ClickableTracker" | "Contact" | "Context" | "CurNode" |
                       "CurrentState" | "curStateTracker" | "dependencyVal" |
                       "Encounter" | "Encounter2" | "EncounterDef" | "EncounterTemplate" |
                       "Entity" | "Forever" | "GenData" | "GenInstanceColumn" |
                       "GenInstanceColumnCount" | "GenInstanceCount" | "GenInstanceData" |
                       "GenInstanceNumber" | "GenInstanceRow" | "GenInstanceRowCount" |
                       "HP" | "HPMax" | "iLevelINTERNAL_LayerFSM" | "IsDisabled" |
                       "IsSelectable" | "IsVisible" | "me" | "Mission" |
                       "MissionClickable" | "MissionDef" | "Mod" | "ModDef" | "MouseX" |
                       "MouseY" | "MouseZ" | "NewTreeLevel" | "NumTeamMembers" |
                       "ParentValue" | "pathOldValue" | "Player" | "Power" | "PowerDef" |
                       "PowerMax" | "PowerVolumeData" | "Prediction" | "RowData" |
                       "Self" | "Source" | "SpawnLocation" | "TableValue" | "Target" |
                       "TargetEnt" | "TeamHP" | "TeamHPMax" | "Volume",
            };

            var PROGRAM = new NonTerminal("PROGRAM");
            var LINE = new NonTerminal("LINE");
            var STATEMENT = new NonTerminal("STATEMENT");
            var VALUE = new NonTerminal("VALUE");
            var LABEL = new NonTerminal("LABEL");

            var LABEL_OPT = new NonTerminal("LABEL_OPT");
            var STATEMENT_OPT = new NonTerminal("STATEMENT_OPT");
            var COMMENT_OPT = new NonTerminal("COMMENT_OPT");

            var BINARY = new NonTerminal("BINARY");

            var OP_NON = new NonTerminal("OP_NON");
            var OP_INT = new NonTerminal("OP_INT");
            var OP_FLT = new NonTerminal("OP_FLT");
            var OP_INS = new NonTerminal("OP_INS");
            var OP_FLS = new NonTerminal("OP_FLS");
            var OP_VEC = new NonTerminal("OP_VEC");
            var OP_VC4 = new NonTerminal("OP_VC4");
            var OP_MAT = new NonTerminal("OP_MAT");
            var OP_QAT = new NonTerminal("OP_QAT");
            var OP_STR = new NonTerminal("OP_STR");
            var OP_FIL = new NonTerminal("OP_FIL");
            var OP_ENT = new NonTerminal("OP_ENT");

            var OP_PTR = new NonTerminal("OP_PTR");
            var OP_ADD = new NonTerminal("OP_ADD");
            var OP_SUB = new NonTerminal("OP_SUB");
            var OP_NEG = new NonTerminal("OP_NEG");
            var OP_MUL = new NonTerminal("OP_MUL");
            var OP_DIV = new NonTerminal("OP_DIV");
            var OP_EXP = new NonTerminal("OP_EXP");
            var OP_BAN = new NonTerminal("OP_BAN");
            var OP_BOR = new NonTerminal("OP_BOR");
            var OP_BNT = new NonTerminal("OP_BNT");
            var OP_BXR = new NonTerminal("OP_BXR");
            var OP_O_P = new NonTerminal("OP_O_P");
            var OP_C_P = new NonTerminal("OP_C_P");
            var OP_O_B = new NonTerminal("OP_O_B");
            var OP_C_B = new NonTerminal("OP_C_B");
            var OP_EQU = new NonTerminal("OP_EQU");
            var OP_LES = new NonTerminal("OP_LES");
            var OP_NGR = new NonTerminal("OP_NGR");
            var OP_GRE = new NonTerminal("OP_GRE");
            var OP_NLE = new NonTerminal("OP_NLE");
            var OP_FUN = new NonTerminal("OP_FUN");
            var OP_IDS = new NonTerminal("OP_IDS");
            var OP_S_V = new NonTerminal("OP_S_V");
            var OP_COM = new NonTerminal("OP_COM");
            var OP_AND = new NonTerminal("OP_AND");
            var OP_ORR = new NonTerminal("OP_ORR");
            var OP_NOT = new NonTerminal("OP_NOT");
            var OP_IF_ = new NonTerminal("OP_IF_");
            var OP_ELS = new NonTerminal("OP_ELS");
            var OP_ELF = new NonTerminal("OP_ELF");
            var OP_EIF = new NonTerminal("OP_EIF");
            var OP_RET = new NonTerminal("OP_RET");
            var OP_RZ_ = new NonTerminal("OP_RZ_");
            var OP_J__ = new NonTerminal("OP_J__");
            var OP_JZ_ = new NonTerminal("OP_JZ_");
            var OP_CON = new NonTerminal("OP_CON");
            var OP_STM = new NonTerminal("OP_STM");
            var OP_RP_ = new NonTerminal("OP_RP_");
            var OP_OBJ = new NonTerminal("OP_OBJ");
            var OP_L_M = new NonTerminal("OP_L_M");
            var OP_L_S = new NonTerminal("OP_L_S");

            this.Root = PROGRAM;

            VALUE.Rule = integerLiteral | stringLiteral;

            PROGRAM.Rule = MakePlusRule(PROGRAM, LINE);
            LINE.Rule = LABEL_OPT + STATEMENT_OPT + COMMENT_OPT + NewLine;

            LABEL_OPT.Rule = LABEL | Empty;
            STATEMENT_OPT.Rule = STATEMENT | Empty;
            COMMENT_OPT.Rule = comment | Empty;

            STATEMENT.Rule =
                OP_NON | OP_INT | OP_FLT |
                OP_INS | OP_FLS |
                OP_VEC | OP_VC4 |
                /*OP_MAT | OP_QAT |*/
                OP_STR | /*OP_FIL |*/
                /*OP_ENT | OP_PTR |*/
                OP_ADD | OP_SUB | OP_NEG | OP_MUL | OP_DIV | OP_EXP |
                OP_BAN | OP_BOR | OP_BNT | OP_BXR |
                OP_O_P | OP_C_P |
                OP_O_B | OP_C_B |
                OP_EQU | OP_LES | OP_NGR | OP_GRE | OP_NLE |
                OP_FUN |
                OP_IDS | OP_S_V |
                OP_COM |
                OP_AND | OP_ORR | OP_NOT |
                OP_IF_ | OP_ELS | OP_ELF | OP_EIF |
                OP_RET | OP_RZ_ |
                OP_J__ | OP_JZ_ |
                OP_CON |
                OP_STM |
                OP_RP_ |
                OP_OBJ |
                OP_L_M | OP_L_S;

            LABEL.Rule = name + ":";

            BINARY.Rule = "binary";

            OP_NON.Rule = "null";
            OP_INT.Rule = "int" + integerLiteral;
            OP_FLT.Rule = "float" + floatLiteral;
            OP_INS.Rule = "ints" + stringLiteral; // todo: fixme
            OP_FLS.Rule = "floats" + stringLiteral; // todo: fixme
            OP_VEC.Rule = "vec3" + stringLiteral; // todo: fixme
            OP_VC4.Rule = "vec4" + stringLiteral; // todo: fixme
            OP_STR.Rule = "str" + stringLiteral;

            OP_ADD.Rule = "add";
            OP_SUB.Rule = "sub";
            OP_NEG.Rule = "neg";
            OP_MUL.Rule = "mul";
            OP_DIV.Rule = "div";
            OP_EXP.Rule = "exp";
            OP_BAN.Rule = BINARY + "and";
            OP_BOR.Rule = BINARY + "or";
            OP_BNT.Rule = BINARY + "not";
            OP_BXR.Rule = BINARY + "xor";
            OP_O_P.Rule = "(";
            OP_C_P.Rule = ")";
            OP_O_B.Rule = "[";
            OP_C_B.Rule = "]";
            OP_EQU.Rule = "equals";
            OP_LES.Rule = "less";
            OP_NGR.Rule = "notgreater";
            OP_GRE.Rule = "greater";
            OP_NLE.Rule = "notless";
            OP_FUN.Rule = "call" + stringLiteral;
            OP_IDS.Rule = "ident" + stringLiteral;
            OP_S_V.Rule = "static" + staticLiteral;
            OP_COM.Rule = "comma";
            OP_AND.Rule = "and";
            OP_ORR.Rule = "or";
            OP_NOT.Rule = "not";
            OP_IF_.Rule = "if";
            OP_ELS.Rule = "else";
            OP_ELF.Rule = "elif";
            OP_EIF.Rule = "endif";
            OP_RET.Rule = "return";
            OP_RZ_.Rule = "retifzero";
            OP_J__.Rule = "j" + name;
            OP_JZ_.Rule = "jz" + name;
            OP_CON.Rule = "continue";
            OP_STM.Rule = ";";
            OP_RP_.Rule = "rootpath" + stringLiteral;
            OP_OBJ.Rule = "objpath" + stringLiteral;
            OP_L_M.Rule = "locm" + stringLiteral; // todo: fixme
            OP_L_S.Rule = "locs" + stringLiteral; // todo: fixme

            this.MarkPunctuation(",", ":");
            this.MarkTransient(
                VALUE,
                LABEL_OPT,
                STATEMENT_OPT,
                COMMENT_OPT,
                staticLiteral);
            // ReSharper restore InconsistentNaming
        }
    }
}
