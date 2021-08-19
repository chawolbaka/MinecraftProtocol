using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Packets.Both
{
    [Flags]
    public enum Bound
    {
        /// <summary>从客户端发出去的包</summary>
        Client = 1,
        /// <summary>从服务端发出来的包</summary>
        Server = 2
    }
}
