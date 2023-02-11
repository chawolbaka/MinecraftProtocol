using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.DataType
{
    public enum ChatPosition : byte
    {
        ChatMessage = 0,
        SystemMessage = 1,
        GameInfo = 2
    }
}
