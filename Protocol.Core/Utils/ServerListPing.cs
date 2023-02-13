using System;
using System.Collections.Generic;
using System.Net;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;
using System.Net.Sockets;
using System.Diagnostics;
using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Chat;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace MinecraftProtocol.Utils
{
    /// <summary>
    /// Support Version: 1.7 - 1.19.3
    /// </summary>
    /// <remarks>http://wiki.vg/Server_List_Ping</remarks>
    public class ServerListPing
    {
        private const ushort DEFAULT_PORT = 25565;

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
            
            Host = host;
            ServerPort = port;
            EnableDnsRoundRobin = useDnsRoundRobin;
            if (!IPAddress.TryParse(host, out IPAddress ip))
            {
                IPAddressList = (IPAddress[])Dns.GetHostEntry(host).AddressList.Clone();
                if (IPAddressList.Length > 0)
                    ServerIP = IPAddressList[0];
                else
                    throw new ArgumentException("incorrect host supplied");
            }
            else
            {
               ServerIP = ip;
            }
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
        public Task<PingReply> SendAsync(CancellationToken cancellationToken = default) => SendAsync(new Socket(ServerIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp), cancellationToken);
            
        
        
        /// <summary>
        /// 向服务器发送ServerListPing相关的包并解析返回的json
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="InvalidPacketException"/>
        /// <exception cref="SocketException"/>
        /// <exception cref="PacketException"/>
        /// <exception cref="JsonException"/>
        public async Task<PingReply> SendAsync(Socket socket, CancellationToken cancellationToken = default)
        {
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));


            if (EnableDnsRoundRobin && IPAddressList != null && IPAddressList.Length > 1)
                DnsRoundRobinHandler();
            if (!socket.Connected)
                await socket.ConnectAsync(ServerIP, ServerPort, cancellationToken);
            if (ReceiveTimeout != default)
                socket.ReceiveTimeout = ReceiveTimeout;

            //Send Ping Packet
            Packet Handshake = new HandshakePacket(string.IsNullOrWhiteSpace(Host) ? ServerIP.ToString() : Host, ServerPort, HandshakePacket.State.GetStatus, -1);
            socket.Send(Handshake.Pack());
            Packet PingRequest = new PingRequestPacket();
            socket.Send(PingRequest.Pack());

            //Receive Packet
            Packet ResponsePacket = await ProtocolUtils.ReceivePacketAsync(socket, -1, cancellationToken);
            if (PingResponsePacket.TryRead(ResponsePacket, -1, out PingResponsePacket PingResponse) && !string.IsNullOrWhiteSpace(PingResponse.Content))
            {
                PingReply PingResult = ResolveJson(PingResponse.Content);
                PingResult.Elapsed = EnableDelayDetect ? await GetTimeAsync(socket, cancellationToken) : null;
                PingResult.Json = PingResponse.Content;
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                return PingResult;
            }
            else
                throw new InvalidPacketException("Invalid response packet", ResponsePacket);
            
        }

        
        /// <summary>
        /// 解析服务器响应的Json
        /// </summary>
        /// <exception cref="JsonException"/>
        public static PingReply ResolveJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            PingReply PingInfo = JsonSerializer.Deserialize<PingReply>(json, jsonSerializerOptions);
            PingInfo.Json = json;
            return PingInfo;
            
        }
        private static JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { Converters = { new ChatComponentConverter(), new PingReply.Converter() } };

        private async Task<TimeSpan?> GetTimeAsync(Socket socket, CancellationToken cancellationToken = default)
        {
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));

            Stopwatch sw = new Stopwatch();
            try
            {
                //http://wiki.vg/Server_List_Ping#Ping
                long code = DateTime.Now.Millisecond;
                byte[] RequestPacket = new PingPacket(code).Pack();
                sw.Start();
                socket.Send(RequestPacket);

                //http://wiki.vg/Server_List_Ping#Pong
                byte[] ResponesPacket = await NetworkUtils.ReceiveDataAsync(socket,ProtocolUtils.ReceivePacketLength(socket), cancellationToken);
                sw.Stop();

                //校验
                if (ResponesPacket.Length != 9 || ResponesPacket[0] != 0x01)
                    return null;
                if (new ReadOnlySpan<byte>(ResponesPacket).Slice(1).AsLong() != code)
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
