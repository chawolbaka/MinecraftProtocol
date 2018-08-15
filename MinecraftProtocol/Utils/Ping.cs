using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Protocol;
using System.Diagnostics;

namespace MinecraftProtocol.Utils
{
    public class Ping
    {
        /* 
         * Support Version:1.7 - 1.12.2
         * 参考资料(Main):
         * http://wiki.vg/Server_List_Ping
         * https://gist.github.com/csh/2480d14fbbb33b4bbae3
        */


        public string ServerIP { get; set; }
        
        public ushort ServerPort { get; set; }

        private string JsonResult;
        private ConnectionPayload Connect = new ConnectionPayload();

        /// <summary>
        /// Not Support Legacy Ping(https://wiki.vg/Server_List_Ping#1.6)
        /// </summary>
        /// <param name="host">Server IP Address or Domain Name</param>
        /// <param name="UseDnsRoundRobin">
        /// 一种拿DNS来负载均衡的方法
        /// 大概是这样的:如果多条同名记录的话 DNS会循环提供这些记录(全部都会提供,循环的是顺序)
        /// 禁用这个选项的话会只使用最前面的那条记录中的IP
        /// (不禁用的话会使用ICMP的Ping来检测所以记录中哪条的延迟最低,并使用那条记录来进行接下来的工作)
        /// </param>
        public Ping(string host, ushort port,bool UseDnsRoundRobin=true)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException("host");
            if (port == 0)
                throw new ArgumentOutOfRangeException("port","0", "port 0 is not allowed");

            if (Regex.Match(host, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$").Success == false)//域名的正则我写不出来...(这个都是抄来的)
            {
                IPHostEntry hostInfo = Dns.GetHostEntry(host);
                if (UseDnsRoundRobin == true && hostInfo.AddressList.Length > 1)
                {
                    //一种拿DNS来负载均衡的方法,如果多条同名记录的话 DNS会循环提供这些记录的IP
                    //但是这些IP只是位置会循环变更,并不是只能查询到一条记录,所以我这边使用ICMP的ping来查询那条记录延迟最低
                    //我不确定这个会不会有什么严重的bug,所以现在暂时只在这边使用
                    
                    System.Net.NetworkInformation.PingException buff=null;
                    long? MinTime=null;
                    foreach (var ip in hostInfo.AddressList)
                    {
                        try
                        {
                            using (System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping())
                            {
                                var pingResulr = ping.Send(ip, 1000 * 10);
                                if (pingResulr.Status == System.Net.NetworkInformation.IPStatus.Success)
                                {
                                    if (MinTime == null|| pingResulr.RoundtripTime < MinTime)
                                    {
                                        MinTime = pingResulr.RoundtripTime;
                                        this.ServerIP = ip.ToString();
                                        //太多了的话就不一个个来检测了,只要找到一个能使用的就用这个吧
                                        if (hostInfo.AddressList.Length > 16 && MinTime > 300 || hostInfo.AddressList.Length > 32)
                                            break;
                                    }
                                    else
                                        continue;
                                }
                            }
                        }
                        catch (System.Net.NetworkInformation.PingException e)
                        {
                            //这一条Ping不通没关系,继续Ping下一条.
                            //(缓存起来是为了防止出现所以IP都不可用的情况,那种情况下的话遍历结束后会重新抛出异常)
                            buff = e;
                            continue;
                        }
                    }
                    if (string.IsNullOrWhiteSpace(ServerIP) && buff != null)
                        throw buff;
                    else if (string.IsNullOrWhiteSpace(ServerIP) && buff == null)
                        throw new Exception("DNS记录中没有可用的IP");
                }
                else
                    this.ServerIP = hostInfo.AddressList[0].ToString();
            }
            else
                this.ServerIP = host;
            this.ServerPort = port;
        }
        public Ping(IPEndPoint IP, bool UseDnsRoundRobin = true) :this(IP.Address.ToString(), (ushort) IP.Port, UseDnsRoundRobin)
        { }
        public PingReply Send()
        {
            Connect.Session = new System.Net.Sockets.TcpClient();
            Connect.Session.Connect(this.ServerIP, this.ServerPort);
            //Send Ping Packet
            SendPacket.Handshake(this.ServerIP, this.ServerPort, -1, 1, Connect);
            SendPacket.PingRequest(Connect);
            //Receive Packet
            int PacketLength = ProtocolHandler.GetPacketLength(Connect.Session);
            if (PacketLength > 0)
            {
                List<byte> Packet = new List<byte>(ProtocolHandler.ReceiveData(0, PacketLength,Connect.Session));
                int PacketID = ProtocolHandler.ReadNextVarInt(Packet);
                JsonResult = ProtocolHandler.ReadNextString(Packet);
                PingReply tmp =  ResolveJson(this.JsonResult);
                tmp.Time = GetTime();
                Connect.Session.Dispose();
                Connect.Session.Close();
                return tmp;
            }
            else
                throw new Exception($"Response Packet Length too Small (PacketLength:{PacketLength})");
        }
        public static PingReply ResolveJson(string json)
        {
            PingReply result = JsonConvert.DeserializeObject<PingReply>(json);
            //因为motd有两种,然后我不知道怎么直接反序列化,所以就这样写了.
            var Description = JObject.Parse(json)["description"];
            if (Description.HasValues == false)
                result.Motd = Description.ToString();
            else
                result.Motd = Description["text"].ToString();
            return result;
        }
        private long? GetTime()
        {
            long? Time = 0;
            if (Connect != null)
            {
                //http://wiki.vg/Server_List_Ping#Ping
                int code = new Random().Next(1, 25565);
                Packet RequestPacket = new Packet();
                RequestPacket.PacketID = 0x01;
                RequestPacket.WriteLong(code);
                DateTime TmpTime = DateTime.Now;
                Connect.Session.Client.Send(RequestPacket.GetPacket());

                //http://wiki.vg/Server_List_Ping#Pong
                int PacketLenght = ProtocolHandler.GetPacketLength(Connect.Session);
                Time = DateTime.Now.Ticks - TmpTime.Ticks;
                List<byte> ResponesPacket = new List<byte>(
                    ProtocolHandler.ReceiveData(0, PacketLenght, Connect.Session));

                //校验
                try {
                    if (ProtocolHandler.ReadNextVarInt(ResponesPacket) != 0x01)
                        return null;
                    if (ResponesPacket.Count != 8 && ProtocolHandler.ReadNextLong(ResponesPacket) != code)
                        return null;
                }
                catch {

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

        /// <summary>
        /// Return Json(if it exists)
        /// </summary>
        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(JsonResult))
                return JsonResult;
            return base.ToString();
        }
    }
}
