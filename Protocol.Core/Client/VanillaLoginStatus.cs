using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{
    /// <summary>
    /// 全部是发生后的
    /// </summary>
    public enum VanillaLoginStatus
    {
        Connected,
        Handshake,
        LoginStart,
        EncryptionRequest,
        EncryptionResponse,
        SetCompression,
        Success,
        Failed
    }
}
