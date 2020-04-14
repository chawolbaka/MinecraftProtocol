using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType.Chat
{
    [Flags]
    public enum ChatFormat : byte
    {
        None            = 0b0000_0000,
        Bold            = 0b0000_0001,
        Italic          = 0b0000_0010,
        Underline       = 0b0000_0100,
        Strikethrough   = 0b0000_1000,
        Obfuscated      = 0b0001_0000,
    }
}
