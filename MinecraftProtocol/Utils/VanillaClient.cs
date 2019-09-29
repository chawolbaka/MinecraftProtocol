using System;
using System.Collections.Generic;
using System.Net.Sockets;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Protocol.Packets;

namespace MinecraftProtocol.Utils
{
    public class VanillaClient : MinecraftClient
    {
        private string Host { get; set; }
        public Player Player { get; }
        public bool Connected { get; }
        private PingReply ServerStatus;
        private TcpClient TCPConnect;
        public int CompressionThreshold { get; set; }
        public int ProtocolVersion { get; set; }

        public VanillaClient(string serverHost, ushort serverPort)
        {
            throw new NotImplementedException();
        }

        public override void Connect()
        {
            throw new NotImplementedException();
        }

        public void Join(string playerName)
        {
            throw new NotImplementedException();
        }
        //自动处理数据包压缩和加密
        public void SendPacket(Packet packet)
        {
            if (TCPConnect.Connected)
                TCPConnect.Client.Send(packet.ToBytes(CompressionThreshold));
            else
                Connect();//这边是不是直接报错好一点?
        }
        private Packet ReceivePacket()
        {
            //之后还要处理解密
            return Protocol.ProtocolHandler.ReceivePacket(TCPConnect.Client, CompressionThreshold);
        }

        public PingReply GetStatus() => GetStatus(false);
        private PingReply GetStatus(bool rePing)
        {
            if (rePing || ServerStatus == null)
            {
                ServerListPing slp = new ServerListPing(Host, ServerPort);
                this.ServerIP = slp.ServerIP;
                return slp.Send();
            }
            else
            {
                return ServerStatus;
            }
        }
    }
}