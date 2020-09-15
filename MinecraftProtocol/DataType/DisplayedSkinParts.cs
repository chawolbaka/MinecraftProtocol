using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType
{
    [Flags]
    public enum DisplayedSkinParts : byte
    {
        None = 0x0,
        Cape = 0x1,
        Jacket = 0x02,
        LeftSleeve = 0x04,
        RightSleeve = 0x08,
        LeftPantsLeg = 0x10,
        RightPantsLeg = 0x20,
        Hat = 0x40,
        All = Cape | Jacket | LeftSleeve | RightSleeve | LeftPantsLeg | RightPantsLeg | Hat
    }
}
