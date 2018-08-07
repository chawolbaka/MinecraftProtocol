using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Protocol;

namespace MinecraftProtocol.Utils
{
    public class Ping:ServerBasePayload
    {
        /* 
         * Support Version:1.7 - 1.12.2
         * 参考资料(Main):
         * http://wiki.vg/Server_List_Ping
         * https://gist.github.com/csh/2480d14fbbb33b4bbae3
        */
        private string JsonResult;
        private ConnectionPayload ConnectInfo = new ConnectionPayload();
        /// <summary>
        /// Not Support Legacy Ping(https://wiki.vg/Server_List_Ping#1.6)
        /// </summary>
        /// <param name="host">Server IP Address or Domain Name</param>
        public Ping(string host, ushort port)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException("Parameter \"serverIP\" is null or WhiteSpace");
            if (Regex.Match(host, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$").Success == false)//域名的正则我写不出来...(这个都是抄来的)
            {
                IPHostEntry hostInfo = Dns.GetHostEntry(host);
                this.ServerIPAddress = hostInfo.AddressList[0].ToString();//为什么有不止一条的记录?
            }
            else
                this.ServerIPAddress = host;
            this.ServerPort = port;
        }
        public PingReply Send()
        {
            
            ConnectInfo.Session = new System.Net.Sockets.TcpClient();
            ConnectInfo.Session.Connect(this.ServerIPAddress, this.ServerPort);
            //Send Ping Packet
            SendPacket.Handshake(this.ServerIPAddress, this.ServerPort, -1, 1, ConnectInfo);
            SendPacket.PingRequest(ConnectInfo);
            //Receive Packet
            int PacketLength = ProtocolHandler.GetPacketLength(ConnectInfo.Session);
            if (PacketLength > 0)
            {
                List<byte> Packet = new List<byte>(ProtocolHandler.ReceiveData(0, PacketLength,ConnectInfo.Session));
                int PacketID = ProtocolHandler.ReadNextVarInt(Packet);
                JsonResult = ProtocolHandler.ReadNextString(Packet);
                PingReply tmp =  ResolveJson(this.JsonResult);
                tmp.Time = GetTime();
                ConnectInfo.Session.Dispose();
                ConnectInfo.Session.Close();
                return tmp;
            }
            else
                throw new Exception("Response Packet Length too Small (<=0) ");
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
            if (ConnectInfo != null)
            {
                //http://wiki.vg/Server_List_Ping#Ping
                int code = new Random().Next(1, 25565);
                Packet RequestPacket = new Packet();
                RequestPacket.PacketID = 0x01;
                RequestPacket.WriteLong(code);
                DateTime TmpTime = DateTime.Now;
                ConnectInfo.Session.Client.Send(RequestPacket.GetPacket());

                //http://wiki.vg/Server_List_Ping#Pong
                int PacketLenght = ProtocolHandler.GetPacketLength(ConnectInfo.Session);
                Time = DateTime.Now.Ticks - TmpTime.Ticks;
                List<byte> ResponesPacket = new List<byte>(
                    ProtocolHandler.ReceiveData(0, PacketLenght, ConnectInfo.Session));

                //校验
                try
                {
                    if (ProtocolHandler.ReadNextVarInt(ResponesPacket) != 0x01)
                        return null;
                    if (ResponesPacket.Count != 8 && ProtocolHandler.ReadNextLong(ResponesPacket) != code)
                        return null;
                }
                catch {
                    return null;
                } //因为这个不是关键参数,读不到就读不到吧.不能因为拿不到延迟就导致拿不到其它数据了
            }
            else throw new NullReferenceException("Do You Used Method \"Send\"?");
            return Time;
        }
        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(JsonResult))
                return JsonResult;
            return base.ToString();
        }
    }
}
