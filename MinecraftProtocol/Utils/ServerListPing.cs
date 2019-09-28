using System;
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

        public string Host { get; set; }
        public IPAddress ServerIP { get; set; }
        public ushort ServerPort { get; set; }
        public int ReceiveTimeout { get; set; }
        public bool EnableDelayDetect { get; set; } = true;
        public bool EnableDnsRoundRobin { get; set; }

        private string JsonResult;
        private IPAddress[] IPAddressList;


        /// <param name="host">服务器IP或域名(如果是域名会被拿去解析IP)</param>
        /// <param name="port">服务器端口号</param>
        /// <param name="useDnsRoundRobin">轮流使用解析域名的时候解析到的IP</param>
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
                IPAddressList = (IPAddress[])Dns.GetHostEntry(host).AddressList.Clone();
                if (IPAddressList.Length > 0)
                    ServerIP = IPAddressList[0];
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
        /// <param name="host">服务器域名,会被写入握手包用于查询使用了反向代理的服务器(不会被拿去问dns服务器要IP)</param>
        /// <param name="address">服务器IP地址,用于连接服务器</param>
        /// <param name="port">服务器端口号</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public ServerListPing(string host, IPAddress address, ushort port)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host));
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (port == 0)
                throw new ArgumentOutOfRangeException(nameof(port), port, "incorrect port supplied");

            this.ServerIP = new IPAddress(address.GetAddressBytes());
            this.Host = host;
            this.ServerPort = port;
        }
        /// <param name="host">服务器域名,会被写入握手包用于查询使用了反向代理的服务器(不会被拿去问dns服务器要IP)</param>
        /// <param name="addressList">服务器地址列表,用于连接服务器(启用DnsRoundRobin会轮流使用里面的IP去连接)</param>
        /// <param name="port">服务器端口号</param>
        /// <param name="useDnsRoundRobin">轮流使用服务器地址列表里面的IP地址去查询服务器</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public ServerListPing(string host, IPAddress[] addressList, ushort port, bool useDnsRoundRobin = false)
        {
            if (addressList == null)
                throw new ArgumentNullException(nameof(addressList));
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host));
            if (addressList.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(addressList), "AddressList length is 0");
            if (port == 0)
                throw new ArgumentOutOfRangeException(nameof(port), port, "incorrect port supplied");

            this.Host = host;
            this.IPAddressList = (IPAddress[])addressList.Clone();
            this.ServerIP = IPAddressList[0];
            this.ServerPort = port;
            this.EnableDnsRoundRobin = useDnsRoundRobin;
        }
        /// <exception cref="ArgumentNullException"/>
        public ServerListPing(IPAddress serverIP, ushort serverPort)
        {
            this.ServerIP = serverIP ?? throw new ArgumentNullException(nameof(serverIP));
            this.Host = serverIP.ToString();
            this.ServerPort = serverPort;
        }
        /// <exception cref="ArgumentNullException"/>
        public ServerListPing(IPEndPoint remoteEP)
        {
            this.ServerIP = remoteEP.Address ?? throw new ArgumentNullException(nameof(remoteEP));
            this.Host = remoteEP.Address.ToString();
            this.ServerPort = (ushort)remoteEP.Port;
        }
        /// <summary>向服务器发送ServerListPing相关的包并解析返回的json</summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidPacketException"/>
        /// <exception cref="PacketException"/>
        /// <exception cref="SocketException"/>
        /// <exception cref="JsonException"/>
        public PingReply Send()
        {
            using (Socket socket = new Socket(ServerIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                return Send(socket, false);
        }
        /// <summary>向服务器发送ServerListPing相关的包并解析返回的json</summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidPacketException"/>
        /// <exception cref="SocketException"/>
        /// <exception cref="PacketException"/>
        /// <exception cref="JsonException"/>
        public PingReply Send(TcpClient tcp)=>Send(tcp.Client, true);
        /// <summary>向服务器发送ServerListPing相关的包并解析返回的json</summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidPacketException"/>
        /// <exception cref="SocketException"/>
        /// <exception cref="PacketException"/>
        /// <exception cref="JsonException"/>
        public PingReply Send(Socket socket, bool reuseSocket)
        {
            PingReply PingResult;
            if (EnableDnsRoundRobin&&IPAddressList!=null&&IPAddressList.Length>1)
                DnsRoundRobinHandler();
            if(!socket.Connected)
                socket.Connect(ServerIP, this.ServerPort);
            if (ReceiveTimeout != default)
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
                if (PacketID != PingResponsePacket.GetPacketID())
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
                throw new PacketException($"Response Packet Length too Small (PacketLength:{PacketLength})");
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
            ServerIP = IPAddressList[0];
            if (IPAddressList.Length == 2)
            {
                //这边单独处理只有2个地址的情况是因为我产生了这样子效率高一点的错觉
                IPAddressList[0] = IPAddressList[1];
                IPAddressList[1] = ServerIP;
            }
            else if (IPAddressList.Length > 1)
            {
                for (int i = 0; i < IPAddressList.Length - 1; i++)
                {
                    IPAddressList[i] = IPAddressList[i + 1];
                }
                IPAddressList[IPAddressList.Length - 1] = ServerIP;
            }
        }

        /// <summary>返回使用了Send方法后接收到的json</summary>
        public override string ToString()
        {
            return JsonResult;
        }
    }
}
