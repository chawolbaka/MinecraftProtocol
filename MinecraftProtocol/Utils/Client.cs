using System;
using System.Collections.Generic;
using System.Net.Sockets;
using MinecraftProtocol.DataType;

namespace MinecraftProtocol.Utils
{
    public class Client
    {
        public string HostName { get; }
        public ushort Port { get; }
        public PlayerEntity Player { get;}
        //添加一个接收到包的事件
        private PingReply PingPayload;
        private ConnectionPayload ConnectionInfo = new ConnectionPayload();

        public Client(string hostName,ushort port)
        {
            //throw new NotImplementedException("占位符,无实现。");
            Ping tmp = new Ping(hostName, port);
            PingPayload = tmp.Send();
            ConnectionInfo.ProtocolVersion = PingPayload.Version.Protocol;
            //Player = new PlayerEntity(playerName, ConnectionInfo);
            //本来打算在这边连接到服务器的,不过好像这样不太好,这里初始化就好啦,连接放在其它方法里面吧
        }

        public void Connect()
        {
            ConnectionInfo.Session = new TcpClient(HostName, Port);
            //这边要开始监听包啦
        }

        public void Join(string playerName, string password = null)
        {
            throw new NotImplementedException("");
        }
    }
}
