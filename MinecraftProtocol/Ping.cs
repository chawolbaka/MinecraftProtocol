using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Protocol;
using System.Text.RegularExpressions;
using System.Net;

namespace MinecraftProtocol
{
    [Obsolete]
    public class Ping : ServerInfo
    {
        private TcpClient tcp;
        private string JsonResult;
        //PingReply 考虑改名
        public IPEndPoint ClientIPEndPoint { get; set; } = null;
        public int Timeot { get; set; } = 4000;
        
        public Ping(string serverIP, ushort port)
        {
            var isIPAddress =  Regex.Match(serverIP, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
            if (isIPAddress.Success == false)//域名的正则我写不出来...(上面那个都是抄的)
            {
                IPHostEntry hostInfo = Dns.GetHostEntry(serverIP);
                this.ServerIPAddress = hostInfo.AddressList[0].ToString();//为什么有不止一条的记录?
            }
            else
                this.ServerIPAddress = serverIP;
            this.ServerPort = port;
        }
        /// <summary>
        /// 向服务器发送握手包和Ping请求(无异常处理)
        /// </summary>
        public void Send()
        {
            tcp = new TcpClient();
            if (ClientIPEndPoint != null)
                tcp.Client.Bind(ClientIPEndPoint);
            tcp.Connect(this.ServerIPAddress, this.ServerPort);
            tcp.ReceiveBufferSize = 1024 * 1024;
            tcp.Client.ReceiveTimeout = this.Timeot;

            //Handshake
            byte[] Packet_id =  VarInt.Convert(0x00);
            byte[] Protocol_version = VarInt.Convert(-1);
            byte[] Server_ip = Encoding.UTF8.GetBytes(this.ServerIPAddress);
            byte[] Server_ip_length = VarInt.Convert(Server_ip.Length);
            byte[] Server_port = BitConverter.GetBytes(this.ServerPort); Array.Reverse(Server_port);//为什么BitConverter这个方法转出来的是反的呀,还好是抄别人的代码.不然怕是要被坑很久
            byte[] Next_state = VarInt.Convert(1);
            byte[] tmp = ProtocolHandler.ConcatBytes(Packet_id, Protocol_version, Server_ip_length, Server_ip, Server_port, Next_state);
            byte[] Handshake_Packet = ProtocolHandler.ConcatBytes(VarInt.Convert(tmp.Length), tmp);
            tcp.Client.Send(Handshake_Packet, SocketFlags.None);

            //Request
            //Wiki:The client follows up with a Request packet. This packet has no fields.
            byte[] Request_Packet = new byte[3];
            Request_Packet[0] = 1;//Packet Length
            Request_Packet[1] = 0x00;//Packet ID
            Request_Packet[2] = 0;//Packet Data(可省略,我为了看着像wiki才写上的)
            tcp.Client.Send(Request_Packet, SocketFlags.None);
        }
        /// <summary>
        /// 解析服务器返回的包,并结束这次连接
        /// </summary>
        public void ResolveAndEnd()
        {
            if(tcp!=null&&tcp.Client.Connected)
            {
                int Packet_Lenght = ProtocolHandler.GetPacketLength(this.tcp);
                if (Packet_Lenght > 0)
                {
                    byte[] tmp_date = new byte[Packet_Lenght];
                    ProtocolHandler.Receive(tmp_date, 0, Packet_Lenght, SocketFlags.None,this.tcp);
                    List<byte> Packet_Date = new List<byte>(tmp_date);
                    if (ProtocolHandler.ReadNextVarInt(Packet_Date) == 0x00)
                        this.JsonResult = Encoding.UTF8.GetString
                            (ProtocolHandler.ReadData(ProtocolHandler.ReadNextVarInt(Packet_Date), Packet_Date));
                    SetInfoFromJsonText(this.JsonResult);
                }
                else
                    new Exception("包的长度小于0");
            }
            tcp.Client.Disconnect(true);
            tcp.Close();
        }
        /// <summary>
        /// 仅解析玩家列表
        /// </summary>
        /// <returns></returns>
        public List<PlayerInfo> GetPlayers()
        {
            List<PlayerInfo> players = new List<PlayerInfo>();

            int Packet_Lenght = ProtocolHandler.GetPacketLength(this.tcp);
            if (Packet_Lenght > 0)
            {
                byte[] tmp_date = new byte[Packet_Lenght];
                ProtocolHandler.Receive(tmp_date, 0, Packet_Lenght, SocketFlags.None,this.tcp);
                List<byte> Packet_Date = new List<byte>(tmp_date);
                if (ProtocolHandler.ReadNextVarInt(Packet_Date) == 0x00)
                {
                    Json.JsonData jsonData = Json.ParseJson
                        (Encoding.UTF8.GetString(ProtocolHandler.ReadData(ProtocolHandler.ReadNextVarInt(Packet_Date), Packet_Date)));
                    Json.JsonData playerData = jsonData.Properties["players"];
                    if (playerData.Properties.ContainsKey("sample"))
                    {
                        this.OnlinePlayers = new List<PlayerInfo>();
                        foreach (Json.JsonData name in playerData.Properties["sample"].DataArray)
                        {
                            string playerUUID = name.Properties["id"].StringValue;
                            string playerName = name.Properties["name"].StringValue;
                            players.Add(new PlayerInfo(playerName,playerUUID));
                        }
                    }
                }
            }
            else
                new Exception("包的长度小于0");
            tcp.Client.Disconnect(true);
            tcp.Close();
            return players;
        }
        private void SetInfoFromJsonText(string jsonText)
        {
            if (!string.IsNullOrWhiteSpace(jsonText) && jsonText.StartsWith("{") && jsonText.EndsWith("}"))
            {
                Json.JsonData jsonData = Json.ParseJson(jsonText);
                if (jsonData.Properties.ContainsKey("version"))
                {
                    Json.JsonData varsionDate = jsonData.Properties["version"];
                    this.ServerVersion = varsionDate.Properties["name"].StringValue;
                    this.ProtocolVersion = int.Parse(varsionDate.Properties["protocol"].StringValue);
                }
                if (jsonData.Properties.ContainsKey("players"))
                {
                    Json.JsonData playerData = jsonData.Properties["players"];
                    if (playerData.Properties.ContainsKey("max"))
                        this.MaxPlayerCount = int.Parse(playerData.Properties["max"].StringValue);
                    if (playerData.Properties.ContainsKey("online"))
                        this.CurrentPlayerCount = int.Parse(playerData.Properties["online"].StringValue);
                    if (playerData.Properties.ContainsKey("sample"))
                    {
                        this.OnlinePlayers = new List<PlayerInfo>();
                        foreach (Json.JsonData name in playerData.Properties["sample"].DataArray)
                        {
                            string playerUUID = name.Properties["id"].StringValue;
                            string playerName = name.Properties["name"].StringValue;
                            this.OnlinePlayers.Add(new PlayerInfo(playerUUID,playerName));
                        }
                    }
                }


            }
        }       
       /// <summary>
       /// 从TCP在协议栈中的缓存里取出指定范围内的数据
       /// </summary>
       /// <param name="buffer">取出来的数据</param>
       /// <param name="start">开始</param>
       /// <param name="offset">结束</param>
       /// <returns>错误码</returns>
        //private int Receive(byte[] buffer, int start, int offset, SocketFlags flags)
        //{
        //    int read = 0;
        //    while (read < offset)
        //    {
        //        //我看不懂这个.这是在读取所以数据吗
        //        try
        //        {
        //            read += tcp.Client.Receive(buffer, start + read, offset - read, flags);
        //        }
        //        catch(SocketException se)
        //        {
        //            if (se.ErrorCode == (int)SocketError.TimedOut || se.ErrorCode == 110)
        //            {
        //                Console.WriteLine("SocketException:Receive Time out");
        //                Console.WriteLine("Please Restart or Wait.");
        //                return -1;
        //            }
        //            else
        //                throw se;
        //        }
        //    }
        //    return 0;
        //}
        public ServerInfo ToServerInfo() => new ServerInfo(
                serverIPAddress: this.ServerIPAddress,
                serverPort: this.ServerPort,
                maxPlayerCount: this.MaxPlayerCount,
                currentPlayerCount: this.CurrentPlayerCount,
                protocolVersion: this.ProtocolVersion,
                serverVersion: this.ServerVersion,
                motd: this.MOTD,
                onlinePlayers: this.OnlinePlayers,
                icon: this.Icon
            );

        
    }
}
