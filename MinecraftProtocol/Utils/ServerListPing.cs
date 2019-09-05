﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Protocol;
using MinecraftProtocol.Protocol.Packets;
using MinecraftProtocol.Protocol.Packets.Client;
using MinecraftProtocol.Protocol.Packets.Server;
using System.Net.Sockets;


namespace MinecraftProtocol.Utils
{
    /// <summary>
    /// Support Version:1.7 - 1.12.2
    /// More See:http://wiki.vg/Server_List_Ping
    /// </summary>
    public class ServerListPing
    {
        /* 
         * Support Version:1.7 - 1.12.2
         * Reference(Main):
         * http://wiki.vg/Server_List_Ping
         * https://gist.github.com/csh/2480d14fbbb33b4bbae3
         * (Not Support Legacy Ping(See:https://wiki.vg/Server_List_Ping#1.6))
        */

        private const string REG_IPv4 = @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$";

        public IPAddress ServerIP { get; set; }
        public ushort ServerPort { get; set; }
        public int ReceiveTimeout { get; set; }
        public bool EnableDelayDetect { get; set; } = true;
        public bool EnableDnsRoundRobin { get; set; }

        private string JsonResult;
        private string Host;
        private IPHostEntry IPEntry;

        /// <summary> Warning:don't use this constructor,if you want fast run for your program</summary>
        /// <param name="host">Server IP Address or Domain Name</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="SocketException"/>
        public ServerListPing(string host, ushort port, bool useDnsRoundRobin=false)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host));
            if (port == 0)
                throw new ArgumentOutOfRangeException(nameof(port), port, "incorrect port supplied");

            EnableDnsRoundRobin = useDnsRoundRobin;

            if (!Regex.Match(host, REG_IPv4).Success)//域名的正则我写不出来...(这个都是抄来的)
            {
                IPEntry = Dns.GetHostEntry(host);
                if (IPEntry.AddressList.Length > 0)
                    ServerIP = IPEntry.AddressList[0];
                else //IPv6 or error addr
                    ServerIP = IPAddress.Parse(host);
            }
            else
            {
                ServerIP = IPAddress.Parse(host);
            }
            Host = host;
            ServerPort = port;
        }
        /// <exception cref="ArgumentNullException"/>
        public ServerListPing(IPAddress serverIP, ushort serverPort)
        {
            ServerIP = serverIP ?? throw new ArgumentNullException(nameof(serverIP));
            ServerPort = serverPort;
        }
        /// <exception cref="ArgumentNullException"/>
        public ServerListPing(IPEndPoint ipEndPoint)
        {
            if (ipEndPoint != null)
            {
                ServerIP = ipEndPoint.Address;
                ServerPort = (ushort)ipEndPoint.Port;
            }
            else
                throw new ArgumentNullException(nameof(ipEndPoint));
        }

        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="SocketException"/>
        /// <exception cref="JsonException"/>
        /// <exception cref="Exception"/>
        public PingReply Send()
        {
            using (Socket socket = new TcpClient().Client)
                return Send(socket, false);
        }
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="SocketException"/>
        /// <exception cref="JsonException"/>
        /// <exception cref="Exception"/>
        public PingReply Send(TcpClient tcp)
        {
            return Send(tcp.Client, true);
        }
        public PingReply Send(Socket socket,bool reuseSocket)
        {
            PingReply PingResult;
            if (EnableDnsRoundRobin&&IPEntry!=null&&IPEntry.AddressList.Length>1)
                DnsRoundRobinHandler();
            if(!socket.Connected)
                socket.Connect(ServerIP, this.ServerPort);
            if (ReceiveTimeout != default(int))
                socket.ReceiveTimeout = ReceiveTimeout;

            //Send Ping Packet
            Packet Handshake = new HandshakePacket(string.IsNullOrWhiteSpace(Host) ? ServerIP.ToString() : Host, this.ServerPort, -1, HandshakePacket.NextState.GetStatus);
            socket.Send(Handshake.GetPacket());
            Packet PingRequest = new PingRequestPacket();
            socket.Send(PingRequest.GetPacket());

            //Receive Packet
            int PacketLength = ProtocolHandler.GetPacketLength(socket);
            if (PacketLength > 0)
            {
                List<byte> Packet = new List<byte>(ProtocolHandler.ReceiveData(0, PacketLength, socket));
                int PacketID = ProtocolHandler.ReadVarInt(Packet);
                if (PacketID != PingResponsePacket.PacketID)
                    throw new InvalidPacketException("Invalid ping response packet id ", new Packet(PacketID, Packet));
                JsonResult = ProtocolHandler.ReadString(Packet);
                if (!string.IsNullOrWhiteSpace(JsonResult))
                {
                    PingResult = ResolveJson(JsonResult);
                    PingResult.Time = EnableDelayDetect ? GetTime(socket) : null;
                }
                else
                {
                    PingResult = null;
                }
                socket.Shutdown(SocketShutdown.Both);
                socket.Disconnect(reuseSocket);
            }
            else
                throw new Exception($"Response Packet Length too Small (PacketLength:{PacketLength})");
            return PingResult;
        }

        /// <exception cref="JsonException"/>
        public static PingReply ResolveJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            PingReply PingInfo = JsonConvert.DeserializeObject<PingReply>(json);

            //我不知道怎么直接反序列化motd,不是每个服务器给的json都长的一样的,我也查不到具体的标准.
            //所以我现在只能尽量去兼容已知的种类
            if (JObject.Parse(json).ContainsKey("description"))
            {
                PingInfo.Motd = new PingReply.Description();
                var Description = JObject.Parse(json)["description"];

                if (Description.HasValues)
                {
                    foreach (JProperty property in Description.Children())
                    {
                        if (property.Name == "text" || property.Name == "translate")
                        {
                            PingInfo.Motd.Text = property.Value.ToString();

                        }
                        else if (property.Name == "extra")
                        {
                            PingInfo.Motd.Extra = new List<PingReply.ExtraPayload>();

                            foreach (var ja in (JArray)property.First)
                            {
                                PingReply.ExtraPayload Extra = new PingReply.ExtraPayload();
                                foreach (JProperty extraItem in ja)
                                {
                                    switch(extraItem.Name)
                                    {
                                        case "color":
                                            Extra.Color = extraItem.Value.ToString(); break;
                                        case "strikethrough":
                                            Extra.Strikethrough = bool.Parse(extraItem.Value.ToString().Trim()); break;
                                        case "bold":
                                            Extra.Bold = bool.Parse(extraItem.Value.ToString().Trim()); break;
                                        case "text":
                                            Extra.Text = extraItem.Value.ToString(); break;
                                    }
                                }
                                PingInfo.Motd.Extra.Add(Extra);
                            }
                        }
                    }
                }
                else
                {
                    PingInfo.Motd.Text = Description.ToString();
                }
            }
            return PingInfo;
            
        }
        private long? GetTime(Socket socket)
        {
            long? Time = 0;

            if (socket != null)
            {
                try
                {
                    //http://wiki.vg/Server_List_Ping#Ping
                    int code = new Random().Next(1, 25565);
                    Packet RequestPacket = new Packet();
                    RequestPacket.ID = 0x01;
                    RequestPacket.WriteLong(code);
                    DateTime StartTime = DateTime.Now;
                    socket.Send(RequestPacket.GetPacket());

                    //http://wiki.vg/Server_List_Ping#Pong
                    int PacketLength = ProtocolHandler.GetPacketLength(socket);
                    Time = DateTime.Now.Ticks - StartTime.Ticks;
                    List<byte> ResponesPacket = new List<byte>(
                        ProtocolHandler.ReceiveData(0, PacketLength, socket));

                    //校验
                    if (ProtocolHandler.ReadVarInt(ResponesPacket) != 0x01)
                        return null;
                    if (ResponesPacket.Count != 8 && ProtocolHandler.ReadLong(ResponesPacket) != code)
                        return null;
                }
                catch
                {

#if DEBUG
                    throw;
#else
                    return null;//在正式发布的时候不能因为获取延迟时发生异常就影响到整个程序的运行
#endif

                }
            }
            else throw new NullReferenceException("Do you used method \"Send\"?");
            return Time;
        }

        private void DnsRoundRobinHandler()
        {
            ServerIP = IPEntry.AddressList[0];
            if (IPEntry.AddressList.Length == 2)
            {
                //这边单独处理只有2个地址的情况是因为我产生了这样子效率高一点的错觉
                IPEntry.AddressList[0] = IPEntry.AddressList[1];
                IPEntry.AddressList[1] = ServerIP;
            }
            else if (IPEntry.AddressList.Length > 1)
            {
                for (int i = 0; i < IPEntry.AddressList.Length - 1; i++)
                {
                    IPEntry.AddressList[i] = IPEntry.AddressList[i + 1];
                }
                IPEntry.AddressList[IPEntry.AddressList.Length - 1] = ServerIP;
            }
        }


        /// <summary>
        /// Return json(if it exists)
        /// </summary>
        public override string ToString()
        {
            return JsonResult;
        }
    }
}
