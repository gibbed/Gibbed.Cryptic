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

namespace Gibbed.Cryptic.FileFormats
{
    public enum StaticVariableType : uint
    {
        Activation = 0,
        AdjustLevel = 1,
        Application = 2,
        ClickableTracker = 3,
        Contact = 4,
        Context = 5,
        CurNode = 6,
        CurrentState = 7,
        curStateTracker = 8,
        dependencyVal = 9,
        Encounter = 10,
        Encounter2 = 11,
        EncounterDef = 12,
        EncounterTemplate = 13,
        Entity = 14,
        Forever = 15,
        GenData = 16,
        GenInstanceColumn = 17,
        GenInstanceColumnCount = 18,
        GenInstanceCount = 19,
        GenInstanceData = 20,
        GenInstanceNumber = 21,
        GenInstanceRow = 22,
        GenInstanceRowCount = 23,
        HP = 24,
        HPMax = 25,
        iLevelINTERNAL_LayerFSM = 26,
        IsDisabled = 27,
        IsSelectable = 28,
        IsVisible = 29,
        me = 30,
        Mission = 31,
        MissionClickable = 32,
        MissionDef = 33,
        Mod = 34,
        ModDef = 35,
        MouseX = 36,
        MouseY = 37,
        MouseZ = 38,
        NewTreeLevel = 39,
        NumTeamMembers = 40,
        ParentValue = 41,
        pathOldValue = 42,
        Player = 43,
        Power = 44,
        PowerDef = 45,
        PowerMax = 46,
        PowerVolumeData = 47,
        Prediction = 48,
        RowData = 49,
        Self = 50,
        Source = 51,
        SpawnLocation = 52,
        TableValue = 53,
        Target = 54,
        TargetEnt = 55,
        TeamHP = 56,
        TeamHPMax = 57,
        Volume = 58,
    }
}
