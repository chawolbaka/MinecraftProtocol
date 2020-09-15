using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType
{
    public interface IClientSettings
    {
        string Locale { get; }
        sbyte ViewDistance { get; }
        ClientChatMode ChatMode { get; }
        bool ChatColors { get; }
        DisplayedSkinParts DisplayedSkinParts { get; }
        MainHand? MainHandDefine { get; }
    }
}
