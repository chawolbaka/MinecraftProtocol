using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace MinecraftProtocol.DataType
{
    //向服务器通信的核心信息
    public class ConnectionPayload
    {
        public TcpClient Session { get; set; }
        public int CompressionThreshold { get; set; } = -1;
        public int ProtocolVersion { get; set; }
    }
}