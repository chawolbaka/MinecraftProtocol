using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{
    public enum ForgeLoginStatus
    {
        ServerRegisterChannel,
        ServerHello,
        ClientRegisterChannel,
        ClientHello,
        SendModList,
        ReceiveModList,
        RegistryData,
        HandshakeAck,
        Success
    }
}
