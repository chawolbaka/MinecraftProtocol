using System;
using System.Collections.Generic;
using System.Net.Sockets;
using MinecraftProtocol.Protocol;
using MinecraftProtocol.DataType;
using System.Net;
using System.Text.RegularExpressions;

namespace MinecraftProtocol
{
    public static class Login
    {
        /// <returns>如果登陆成功,会返回LoginSuccess包和数据包压缩阀值(如果有)</returns>
        public static (Packet LoginSuccess, ConnectionPayload ConnectInfo) Start(IPEndPoint IPAndPort, string playerName, string password,TcpClient tcpClient)
        {

            PingReply ReplyInfo = new Utils.ServerListPing(IPAndPort).Send();

            ConnectionPayload Connect = new ConnectionPayload();
            Connect.Session = tcpClient != null ? tcpClient : throw new ArgumentNullException("tcpClient");
            Connect.ProtocolVersion = ReplyInfo.Version.Protocol;

            SendPacket.Handshake(IPAndPort.Address.ToString(),(ushort)IPAndPort.Port, new VarInt(ReplyInfo.Version.Protocol),new VarInt(2),Connect);

            SendPacket.LoginStart(playerName, Connect);
            
            int MaxReceiveCount = 2;//这个等我到时候查查最多会有几个包
            for (int i = 0; i < MaxReceiveCount; i++)
            {
                Packet TempPacket = ProtocolHandler.ReceivePacket(Connect.Session.Client,Connect.CompressionThreshold);
                Console.WriteLine("接收到了一个包,总接次数:" + i);
                Console.WriteLine($"PacketID:{TempPacket.ID}");
                object Type = ProtocolHandler.GetPacketType(TempPacket, Connect.ProtocolVersion);
                //数据包压缩阀值
                if (Connect.CompressionThreshold == -1 && Type is PacketType.Server && (PacketType.Server)Type == PacketType.Server.SetCompression)
                    Connect.CompressionThreshold = new VarInt(TempPacket.Data.ToArray(),0).ToInt();

                else if (false)
                {
                    //这边要开始写加密了
                }
                else if (Type is PacketType.Server && (PacketType.Server)Type == PacketType.Server.LoginSuccess)
                {
                    return (TempPacket, Connect);
                }
                else
                {
#if DEBUG
                    throw new Exception($"接收到了不该出现在登陆流程中的包.PacketID:{TempPacket.ID}");
#else
                                    
                   Console.WriteLine($"接收到了不该出现在登陆流程中的包.PacketID:{TempPacket.ID}");              
#endif
                }

            }

            throw new Exception("Can not Get LoginSuccess Packet");
        }
        public static (Packet LoginSuccess, ConnectionPayload ConnectInfo) Start(string host, ushort port, string playerName, string password, TcpClient tcpClient)
        {
            IPEndPoint IPAndEndPort;
            if (!Regex.Match(host, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$").Success)
                IPAndEndPort = new IPEndPoint(Dns.GetHostEntry(host).AddressList[0], port);
            else
                IPAndEndPort = new IPEndPoint(IPAddress.Parse(host), port);
            return Login.Start(IPAndEndPort, playerName, password,tcpClient);
        }
        public static (Packet LoginSuccess, ConnectionPayload ConnectInfo) Start(IPEndPoint IPEndPort, string playerName,string password)
        {
            TcpClient tmp = new TcpClient();
            tmp.Connect(IPEndPort);
            var result = Login.Start(IPEndPort, playerName, password,tmp);
            return (result.LoginSuccess,result.ConnectInfo);
        }
        public static (Packet LoginSuccess, ConnectionPayload ConnectInfo) Start(string host, ushort port, string playerName, string password)
        {
            IPEndPoint IPAndPort;
            if (!Regex.Match(host, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$").Success)
                IPAndPort = new IPEndPoint(Dns.GetHostEntry(host).AddressList[0], port);
            else
                IPAndPort = new IPEndPoint(IPAddress.Parse(host), port);

            return Login.Start(IPAndPort,playerName,password);
        }
    }
}
