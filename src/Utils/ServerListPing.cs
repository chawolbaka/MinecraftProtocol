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
using System.Diagnostics;
using MinecraftProtocol.IO.Extensions;

namespace MinecraftProtocol.Utils
{
    /// <summary>
    /// Support Version: 1.7 - 1.15.2
    /// </summary>
    /// <remarks>http://wiki.vg/Server_List_Ping</remarks>
    public class ServerListPing
    {
        private const ushort DEFAULT_PORT = 25565;
        private const string REG_IPv4 = @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$";

        public string Host { get; set; }
        public IPAddress ServerIP { get; set; }
        public ushort ServerPort { get; set; }
        public int ReceiveTimeout { get; set; }
        public bool EnableDelayDetect { get; set; } = true;
        public bool EnableDnsRoundRobin { get; set; }

        private IPAddress[] IPAddressList;

        /// <summary>
        /// 初始化ServerListPing
        /// </summary>
        /// <param name="host">服务器IP或域名(如果是域名会被拿去解析IP)</param>
        /// <param name="port">服务器端口号</param>
        /// <param name="useDnsRoundRobin">轮流使用解析域名的时候解析到的IP</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="SocketException"/>
        public ServerListPing(string host, ushort port = DEFAULT_PORT, bool useDnsRoundRobin=false)
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

        /// <summary>
        /// 初始化ServerListPing
        /// </summary>
        /// <param name="host">服务器域名,会被写入握手包用于查询使用了反向代理的服务器(不会被拿去问dns服务器要IP)</param>
        /// <param name="address">服务器IP地址,用于连接服务器</param>
        /// <param name="port">服务器端口号</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public ServerListPing(string host, IPAddress address, ushort port = DEFAULT_PORT)
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

        /// <summary>
        /// 初始化ServerListPing
        /// </summary>
        /// <param name="host">服务器域名,会被写入握手包用于查询使用了反向代理的服务器(不会被拿去问dns服务器要IP)</param>
        /// <param name="addressList">服务器地址列表,用于连接服务器(启用DnsRoundRobin会轮流使用里面的IP去连接)</param>
        /// <param name="port">服务器端口号</param>
        /// <param name="useDnsRoundRobin">轮流使用服务器地址列表里面的IP地址去查询服务器</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public ServerListPing(string host, IPAddress[] addressList, ushort port = DEFAULT_PORT, bool useDnsRoundRobin = false)
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

        /// <summary>
        /// 初始化ServerListPing
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public ServerListPing(IPAddress ipAddress, ushort port = DEFAULT_PORT)
        {
            this.ServerIP = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            this.Host = ipAddress.ToString();
            this.ServerPort = port;
        }
   
        /// <summary>
        /// 初始化ServerListPing
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public ServerListPing(IPEndPoint remoteEP)
        {
            this.ServerIP = remoteEP.Address ?? throw new ArgumentNullException(nameof(remoteEP));
            this.Host = remoteEP.Address.ToString();
            this.ServerPort = (ushort)remoteEP.Port;
        }
        
        
        /// <summary>
        /// 向服务器发送ServerListPing相关的包并解析返回的json
        /// </summary>
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
        
        
        /// <summary>
        /// 向服务器发送ServerListPing相关的包并解析返回的json
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidPacketException"/>
        /// <exception cref="SocketException"/>
        /// <exception cref="PacketException"/>
        /// <exception cref="JsonException"/>
        public PingReply Send(Socket tcp) => Send(tcp, true);


        /// <summary>
        /// 向服务器发送ServerListPing相关的包并解析返回的json
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidPacketException"/>
        /// <exception cref="SocketException"/>
        /// <exception cref="PacketException"/>
        /// <exception cref="JsonException"/>
        public PingReply Send(Socket socket, bool reuseSocket)
        {
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));

            PingReply PingResult; string JsonResult;           
            if (EnableDnsRoundRobin&&IPAddressList!=null&&IPAddressList.Length>1)
                DnsRoundRobinHandler();
            if(!socket.Connected)
                socket.Connect(ServerIP, ServerPort);
            if (ReceiveTimeout != default)
                socket.ReceiveTimeout = ReceiveTimeout;

            //Send Ping Packet
            Packet Handshake = new HandshakePacket(string.IsNullOrWhiteSpace(Host) ? ServerIP.ToString() : Host, ServerPort, -1, HandshakePacket.State.GetStatus);
            socket.Send(Handshake.ToBytes());
            Packet PingRequest = new PingRequestPacket();
            socket.Send(PingRequest.ToBytes());

            //Receive Packet
            int PacketLength = ProtocolHandler.GetPacketLength(socket);
            if (PacketLength > 0)
            {
                List<byte> Packet = new List<byte>(NetworkUtils.ReceiveData(PacketLength, socket));
                int PacketID = ProtocolHandler.ReadVarInt(Packet);
                if (PacketID != PingResponsePacket.GetPacketID())
                    throw new InvalidPacketException("Invalid ping response packet id ", new Packet(PacketID, Packet.ToArray()));
           
                JsonResult = ProtocolHandler.ReadString(Packet);
                if (!string.IsNullOrWhiteSpace(JsonResult))
                {
                    PingResult = ResolveJson(JsonResult);
                    PingResult.Elapsed = EnableDelayDetect ? GetTime(socket) : null;
                    PingResult.Json = JsonResult;
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

        /// <summary>
        /// 解析服务器响应的Json
        /// </summary>
        /// <exception cref="JsonException"/>
        public static PingReply ResolveJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            PingReply PingInfo = JsonConvert.DeserializeObject<PingReply>(json);
            PingInfo.Json = json;

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
        private TimeSpan? GetTime(Socket socket)
        {
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));

            Stopwatch sw = new Stopwatch();
            try
            {
                //http://wiki.vg/Server_List_Ping#Ping
                long code = DateTime.Now.Millisecond;
                byte[] RequestPacket = new PingPacket(code).ToBytes();
                sw.Start();
                socket.Send(RequestPacket);

                //http://wiki.vg/Server_List_Ping#Pong
                ReadOnlySpan<byte> ResponesPacket = NetworkUtils.ReceiveData(ProtocolHandler.GetPacketLength(socket), socket);
                sw.Stop();

                //校验
                if (ResponesPacket.Length != 9 || ResponesPacket[0] != 0x01)
                    return null;
                if (ResponesPacket.Slice(1).AsLong() != code)
                    return null;
            }
            catch
            {

#if DEBUG
                throw;
#else
                //有一些服务端或反向代理会导致最后的这两个测量延迟的包没被发送
                //但json应该是拿到了的，不能因为拿不到延迟就报错导致拿不到json
                return null;
#endif

            }

            return sw.Elapsed;
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

        /// <summary>
        /// 获取服务器地址
        /// </summary>
        public override string ToString()
        {
            if (ServerPort == DEFAULT_PORT)
                return Host;
            else
                return $"{Host}:{ServerPort}";
        }
    }
}
