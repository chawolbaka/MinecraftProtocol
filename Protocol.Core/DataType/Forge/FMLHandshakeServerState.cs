using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType.Forge
{
    /// <summary>
    /// Copy of the forge enum for client states.
    /// https://github.com/MinecraftForge/MinecraftForge/blob/ebe9b6d4cbc4a5281c386994f1fbda04df5d2e1f/src/main/java/net/minecraftforge/fml/common/network/handshake/FMLHandshakeServerState.java
    /// </summary>
    public enum FMLHandshakeServerState:byte
    {
        START        = 0,
        HELLO        = 1,
        WAITINGCACK  = 2,
        COMPLETE     = 3,
        DONE         = 4,
        ERROR        = 5
    }
}
