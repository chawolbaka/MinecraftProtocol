using System;
using System.Collections.Generic;

namespace MinecraftProtocol.DataType
{
    public class ServerInfo:ServerBasePayload
    {
        public ServerInfo()
        { }
        /// <summary>
        /// 初始化所以属性
        /// </summary>
        public ServerInfo(
            string serverIPAddress,
            ushort serverPort,
            int currentPlayerCount,
            int maxPlayerCount,
            int protocolVersion,
            string serverVersion,
            string motd,
            List<Player> onlinePlayers,
            byte[] icon
            )
        {
            this.ServerIPAddress = serverIPAddress;
            this.ServerPort = serverPort;
            this.MaxPlayerCount = maxPlayerCount;
            this.CurrentPlayerCount = currentPlayerCount;
            this.ProtocolVersion = protocolVersion;
            this.MOTD = motd;
            this.OnlinePlayers = onlinePlayers;
            this.Icon = icon;
        }



        /// <summary>
        /// 服务器的在线人数
        /// </summary>
        public int CurrentPlayerCount { get; protected set; }

        /// <summary>
        /// 服务器的最大玩家数量
        /// </summary>
        public int MaxPlayerCount { get; protected set; }

        /// <summary>
        /// MC协议版本号
        /// </summary>
        public int ProtocolVersion { get; protected set; }

        /// <summary>
        /// 服务端版本
        /// </summary>
        public string ServerVersion { get; protected set; }

        /// <summary>
        /// 服务器MOTD
        /// </summary>
        public string MOTD { get; protected set; }

        /// <summary>
        /// 获取服务器Forge信息（如果可用）
        /// </summary>
        //public ForgeInfo ForgeInfo { get; protected set; }

        /// <summary>
        /// 服务器在线玩家的uuid,name（如果可用）
        /// </summary>
        public List<Player> OnlinePlayers { get; protected set; }
        public byte[] Icon { get; protected set; }

        /// <summary>
        /// ping一下服务器(ICMP)
        /// </summary>
        public double Ping
        {
            
            get
            {
                try
                {
                    using (System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping())
                    {
                        //long tmp_date = DateTime.Now.Ticks;
                        var pingResulr = ping.Send(this.ServerIPAddress, 1000 * 10);
                        //好像会造成1毫秒左右的精度丢失,所以我直接减了0.7
                        //double PingTime = ((DateTime.Now.Ticks - tmp_date) / 10000.0)-0.7;
                        if (pingResulr.Status == System.Net.NetworkInformation.IPStatus.Success)
                        {
                            //if (pingResulr.RoundtripTime == 0)//如果是0的话就使用这种低精度的(起码不用看见0了QAQ
                            //    return PingTime;
                            //else
                                return pingResulr.RoundtripTime;
                        }
                        return -1;
                    }
                }
                catch (System.Net.NetworkInformation.PingException pe)
                {
                    Console.WriteLine($"Exception:{pe.Message}");
                    Console.WriteLine(pe.HelpLink);
                    return -1;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return -1;
                }

            }
        }

    }
}
