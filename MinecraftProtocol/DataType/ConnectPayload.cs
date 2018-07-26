using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace MinecraftProtocol.DataType
{
    /// <summary>
    /// 一些传输包或者接收包时候需要的信息,我不知道怎么取名字,先这样吧.
    /// 存放一个连接的重要数据?
    /// </summary>
    public class ConnectPayload
    {
        public TcpClient Session { get; set; }
        public int CompressionThreshold { get; set; } = -1;
        public int ProtocolVersion { get; set; }
    }
}