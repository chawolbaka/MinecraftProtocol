using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Protocol;

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

        private readonly string REG_IPv4 = @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$";


        public IPAddress ServerIP { get; set; }
        public ushort ServerPort { get; set; }
        public int ReceiveTimeout { get; set; }
        public bool EnableDelayDetect { get; set; } = true;
        public bool EnableDnsRoundRobin { get; set; }

        private string JsonResult;
        private IPHostEntry Host;
        private ConnectionPayload Connect = new ConnectionPayload();

        /// <summary> Warning:don't use this constructor,if you want fast run for your program</summary>
        /// <param name="host">Server IP Address or Domain Name</param>
        public ServerListPing(string host, ushort port, bool useDnsRoundRobin=false)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host));
            if (port == 0)
                throw new ArgumentOutOfRangeException(nameof(port), port, "incorrect port supplied");

            EnableDnsRoundRobin = useDnsRoundRobin;

            if (!Regex.Match(host, REG_IPv4).Success)//域名的正则我写不出来...(这个都是抄来的)
            {
                Host = Dns.GetHostEntry(host);
                if (Host.AddressList.Length > 0)
                    ServerIP = Host.AddressList[0];
                else //IPv6 or error addr
                    ServerIP = IPAddress.Parse(host);

            }
            else
            {
                ServerIP = IPAddress.Parse(host);
            }
            ServerPort = port;
        }
        public ServerListPing(IPAddress serverIP, ushort serverPort)
        {
            ServerIP = serverIP ?? throw new ArgumentNullException(nameof(serverIP));
            ServerPort = serverPort;
        }
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

        public PingReply Send()
        {
            PingReply PingResult;
            if (EnableDnsRoundRobin&&Host.AddressList.Length>1) DnsRoundRobinHandler();
            Connect.Session = new System.Net.Sockets.TcpClient();
            Connect.Session.Connect(ServerIP, this.ServerPort);
            if (ReceiveTimeout != default(int))
                Connect.Session.ReceiveTimeout = ReceiveTimeout;

            //Send Ping Packet
            SendPacket.Handshake(ServerIP.ToString(), this.ServerPort, new VarInt(-1),new VarInt(1), Connect);
            SendPacket.PingRequest(Connect);

            //Receive Packet
            int PacketLength = ProtocolHandler.GetPacketLength(Connect.Session);
            if (PacketLength > 0)
            {
                List<byte> Packet = new List<byte>(ProtocolHandler.ReceiveData(0, PacketLength, Connect.Session));
                int PacketID = ProtocolHandler.ReadNextVarInt(Packet);
                JsonResult = ProtocolHandler.ReadNextString(Packet);
                if (!string.IsNullOrWhiteSpace(JsonResult))
                {
                    PingResult = ResolveJson(JsonResult);
                    PingResult.Time = EnableDelayDetect ? GetTime() : null;
                }
                else
                {
                    //这边我是想抛异常的,可是找不到什么适合的异常又懒的自己写一个就直接返回了null
                    PingResult = null;
                }
                Connect.Session.Client.Dispose();
                Connect.Session.Close();
            }
            else
                throw new Exception($"Response Packet Length too Small (PacketLength:{PacketLength})");
            return PingResult;
        }
        public static PingReply ResolveJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            PingReply result = JsonConvert.DeserializeObject<PingReply>(json);

            //因为motd有两种,然后我不知道怎么直接反序列化,所以就这样写了.
            if (JObject.Parse(json).ContainsKey("description"))
            {

                var Description = JObject.Parse(json)["description"];
                if (Description.HasValues && Description.First is JProperty)
                    result.Motd = ((JProperty)Description.First).Value.ToString();
                else
                    result.Motd = Description.ToString();
            }
            return result;


        }
        private long? GetTime()
        {
            long? Time = 0;
            
            if (Connect != null)
            {
                try
                {
                    //http://wiki.vg/Server_List_Ping#Ping
                    int code = new Random().Next(1, 25565);
                    Packet RequestPacket = new Packet();
                    RequestPacket.ID = 0x01;
                    RequestPacket.WriteLong(code);
                    DateTime TmpTime = DateTime.Now;
                    Connect.Session.Client.Send(RequestPacket.GetPacket());

                    //http://wiki.vg/Server_List_Ping#Pong
                    int PacketLenght = ProtocolHandler.GetPacketLength(Connect.Session);
                    Time = DateTime.Now.Ticks - TmpTime.Ticks;
                    List<byte> ResponesPacket = new List<byte>(
                        ProtocolHandler.ReceiveData(0, PacketLenght, Connect.Session));

                    //校验
                    if (ProtocolHandler.ReadNextVarInt(ResponesPacket) != 0x01)
                        return null;
                    if (ResponesPacket.Count != 8 && ProtocolHandler.ReadNextLong(ResponesPacket) != code)
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
            else throw new NullReferenceException("Do You Used Method \"Send\"?");
            return Time;
        }

        private void DnsRoundRobinHandler()
        {
            ServerIP = Host.AddressList[0];
            if (Host.AddressList.Length == 2)
            {
                //这边单独处理只有2个地址的情况是因为我产生了这样子效率高一点的错觉
                Host.AddressList[0] = Host.AddressList[1];
                Host.AddressList[1] = ServerIP;
            }
            else if (Host.AddressList.Length > 1)
            {
                ServerIP = Host.AddressList[0];
                for (int i = 0; i < Host.AddressList.Length - 1; i++)
                {
                    Host.AddressList[i] = Host.AddressList[i + 1];
                }
                Host.AddressList[Host.AddressList.Length - 1] = ServerIP;
            }
        }


        /// <summary>
        /// Return json(if it exists)
        /// </summary>
        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(JsonResult))
                return JsonResult;
            else
                return string.Empty;
        }
    }
}
