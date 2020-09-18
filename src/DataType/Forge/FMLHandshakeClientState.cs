using System;

namespace MinecraftProtocol.DataType.Forge
{
    /// <summary>
    /// Copy of the forge enum for client states.
    /// https://github.com/MinecraftForge/MinecraftForge/blob/ebe9b6d4cbc4a5281c386994f1fbda04df5d2e1f/src/main/java/net/minecraftforge/fml/common/network/handshake/FMLHandshakeClientState.java
    /// </summary>
    public enum FMLHandshakeClientState : byte
    {
        START                   = 0,
        HELLO                   = 1,
        WAITINGSERVERDATA       = 2,
        WAITINGSERVERCOMPLETE   = 3,
        PENDINGCOMPLETE         = 4,
        COMPLETE                = 5,
        DONE                    = 6,
        ERROR                   = 7
    }
}
