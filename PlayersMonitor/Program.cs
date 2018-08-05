using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MinecraftProtocol;
using MinecraftProtocol.DataType;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace PlayersMonitor
{
    
    class Program
    {
        public static string QAQ = 
            "这个程序抄袭了这个项目的大部分代码(无脑Ctrl C/V):http://www.mcbbs.net/thread-723161-1-1.html";
        private static List<PlayerInfo> Players = new List<PlayerInfo>();
        private static int PlayerIndex = 0;
        private static int BanTime { get; set; } = 18200;
        static void Main(string[] args)
        {
            #region 一堆很想删除的代码

#if (DEBUG == true)
            //Settings.IPAddress.Destination = "120.41.42.92";
            //Settings.Port = 23533;
            Settings.IPAddress.Destination = "mc-sm.com";
            Settings.Port = 23533;
            Settings.Sleep = 0;
            //Configure.IP = "120.77.58.174";
            //Configure.Port = 25567;
            //Settings.AutoSetBlood = true;
#endif
            Settings.Initialization(args);

#if (DEBUG == false)
            if (Settings.IPAddress.Destination==null)
            {
                Console.Write("服务器地址:");
                string IP = Console.ReadLine();
                Settings.IPAddress.Destination = IP;
                string QAQ = @"^\s*(\S+\.\S+)(：|:)([1-9]\d{0,3}|[1-5]\d{0,4}|6[0-4]\d{3}|65[0-4]\d{2}|655[0-2]\d|6553[0-5])\s*$";
                var m = Regex.Match(IP,QAQ );
                if (m.Success==true)
                {
                    Settings.IPAddress.Destination = Regex.Replace(IP, QAQ, "$1");
                    Settings.Port = ushort.Parse(Regex.Replace(IP, QAQ, "$3"));
                }
                else if(Settings.Port == 25565)
                {
                    string tmp_port;
                    int NumberofRetries = 0;
                    while (true)
                    {
                        if (NumberofRetries > 30000)
                        {
                            Console.WriteLine("QAQ你有毒吗,试这么多次还不能输对.生气啦！(关闭程序.gif)");
                            Console.ReadLine();
                            System.Environment.Exit(0);
                        }
                        NumberofRetries++;
                        Console.Write("服务器端口号(默认:25565):");
                        tmp_port = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(tmp_port))
                        {
                            Settings.Port = 25565;
                            break;
                        }
                        if (ushort.TryParse(tmp_port, out ushort _port))
                        {
                            Settings.Port = _port;
                            break;
                        }
                        Console.Clear();
                        Console.WriteLine("无法将你输入的端口转换为ushort,请重新输入端口(范围:1-65535)");
                    }
                }
            }
#endif

            #endregion
            Ping p = new Ping(Settings.IPAddress.Destination, Settings.Port);
            if (Settings.IPAddress.Source != null)
                p.ClientIPEndPoint = new IPEndPoint(IPAddress.Parse(Settings.IPAddress.Source), 0);
            int reConnectTime = 5000;
            int reConnectCount = 0;
            Console.Title = "正在连接服务器...";
            while (true)
            {
                PlayerIndex = 1;
                #region ping服务器
                try
                {
                    p.Send();
                }
                catch (SocketException se)
                {

                    if (se.ErrorCode == (int)SocketError.HostNotFound)//突然看见的一个错误,突然就想加上去(虽然好像没什么用)
                    {
                        Console.WriteLine("地址错误:无法找到主机名");
                        Environment.Exit(-0);
                    }
                    else if (se.ErrorCode == (int)SocketError.ConnectionRefused || se.ErrorCode == 111)
                    {//处理一下服务器重启不至于让我手动重启整个程序

                        if (reConnectCount == 0)
                        {
                            Console.Clear();
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("[Error]被服务器拒绝了连接,请检测双方网络状态 IP/端口是否正确.");
                            Console.WriteLine($"ErrorCode:{se.ErrorCode}");
                            Console.ForegroundColor = ConsoleColor.Green;
                            //这块以后换成一个方法,用来输出各种信息来诊断错误原因
                            //(其实这个错误也不太需要诊断啦,大概就是服务器关了,端口输错了,DNS缓存还没有到期可是IP变了(这个问题的可以试试看直接找根域服务器去解析地址)
                            Console.WriteLine("配置信息:");//这名字看着好奇怪,以后换了吧.
                            Console.WriteLine($"IP:{Settings.IPAddress.Destination}:{Settings.Port}\n\r");
                            //这边加个dns解析,这样可以看到是不是dns缓存的锅(两个对比,路由器给的DNS服务器和根域的
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine($"将在{reConnectTime / 1000}秒后尝试重连...");
                        }
                        else if (reConnectCount > 255)
                        {
                            Console.WriteLine("已到达最大重连次数(255),程序关闭.");
                            Environment.Exit(-0);
                        }
                        else
                        {
                            Console.Title = $"已重连:{reConnectCount}次";
                            reConnectTime += new Random().Next(100, 8000);
                            Console.WriteLine($"重连失败,下次重连将在{(float)reConnectTime / 1000}秒后开始");
                        }
                        Thread.Sleep(reConnectTime);
                        reConnectCount++;
                        p = new Ping(Settings.IPAddress.Destination, Settings.Port);
                        if (Settings.IPAddress.Source != null)
                            p.ClientIPEndPoint = new IPEndPoint(IPAddress.Parse(Settings.IPAddress.Source), 0);
                        continue;
                    }
                    else if (se.ErrorCode == (int)SocketError.TimedOut || se.ErrorCode == 110)
                    {
                        Console.WriteLine("连接超时.");
                        Environment.Exit(-0);
                    }
                    else
                        throw;
                }
                try
                {
                    p.ResolveAndEnd();
                }
                catch (SocketException se)
                {
                    Console.Clear();
                    if (se.ErrorCode == (int)SocketError.TimedOut || se.ErrorCode == 110)
                    {
                        Debug.Print("SocketException:Receive Time out");
                        Console.WriteLine("SocketException:Receive Time out");
                        Console.WriteLine($"Please Restart or Wait {BanTime}ms");
                        Thread.Sleep(BanTime);
                    }
                    else if(se.ErrorCode == (int)SocketError.ConnectionReset)
                    {
                        Console.WriteLine("服务器强制关闭了连接.");
                        Console.WriteLine("程序的继续执行下去,但是不保证可以正常运行(可能会死循环)");
                    }
                    else
                        throw;
                }
                finally
                {
                    if (p.ProtocolVersion!=default(int))
                    {
                        reConnectCount = 0;
                        reConnectTime = reConnectTime += new Random().Next(255, 1024);
                    }
                }
                #endregion
                if (p.CurrentPlayerCount>0&&p.OnlinePlayers!=null)
                {
                    foreach (var player in p.OnlinePlayers)
                    {
                        var result = Players.Find(delegate (PlayerInfo PF) { return player.UUID == PF.UUID; });
                        if (result == null)
                        {
                            Players.Add(new PlayerInfo()
                            {
                                UUID = player.UUID,
                                Name = player.Name,
                                Blood = GetBloodValue(p, Settings.AutoSetBlood)
                            });
                            PlayerJoin(new PlayerInfo() { UUID = player.UUID, Name = player.Name });
                        }
                        else if (result != null)
                            result.Blood = GetBloodValue(p, Settings.AutoSetBlood);
                    }
                    LifeTime(Players);
                }
                if (p.CurrentPlayerCount==0&&Players.Count>0)
                {
                    foreach (var downLinePlayer in Players)
                    {
                        PlayerDownLine(downLinePlayer);
                    }
                    Players.RemoveAll(n => true);
                }
                Console.Clear();
                Console.WriteLine($"服务端版本:{p.ServerVersion}");
                Console.WriteLine($"在线人数:{p.CurrentPlayerCount}/{p.MaxPlayerCount}");
                foreach (var player in Players)
                {
                    PrintPlayerName(player);
                    PlayerIndex++;
                }
                if (Settings.ShowPing == true)
                    Console.Title = $"Ping:{p.Ping}ms";
                Thread.Sleep((int)Settings.Sleep);
            }
        }
        static int GetBloodValue(Ping info,bool auto=false)
        {
            if (auto==false)
                return info.CurrentPlayerCount <= 12 ? 2 : Settings.Blood;
            else//不会数学写的一脸绝望
            {
                if (info.CurrentPlayerCount>Players.Count)
                    return info.CurrentPlayerCount <= 12 ? 2 : info.CurrentPlayerCount - 7;
                else if(Players.Count> info.CurrentPlayerCount)
                    return info.CurrentPlayerCount - 13;
                else if(info.CurrentPlayerCount<=17)         
                    return info.CurrentPlayerCount <= 12 ? 2 : info.CurrentPlayerCount - 9;
                else
                    return info.CurrentPlayerCount <= 12 ? 2 : info.CurrentPlayerCount - 12;
            }
        }
        static void PlayerJoin(PlayerInfo player)
        {
            StreamWriter sw = File.AppendText("Player.log");
            sw.WriteLine($"[{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")}]玩家：{player.Name} 上线");
            sw.Flush();
            sw.Close();
        }
        static void PlayerDownLine(PlayerInfo player)
        {
            StreamWriter sw = File.AppendText("Player.log");
            sw.WriteLine($"[{DateTime.Now.ToString("yyyy/MM/dd hh:mm:ss")}]玩家：{player.Name} 下线");
            sw.Flush();
            sw.Close();
        }
        static void PrintPlayerName(PlayerInfo player, params string[] hightLight)
        {
            if (hightLight.Length > 0)
            {
                foreach (var hl in hightLight)
                {
                    if (player.Name == hl || hl == "@allplayers")
                    {
                        Console.Write($"[{PlayerIndex.ToString("D2")}/{player.Blood}]Name:");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"{ player.Name}");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($"({player.UUID})");
                        return;
                    }
                }
            }
            Console.WriteLine($"[{PlayerIndex.ToString("D2")}/{player.Blood}]Name:{player.Name}({player.UUID})");
        }
        static void ReName(string oldName ,string newName)
        {
            var destPlayer = Players.Find(delegate (PlayerInfo PF) { return PF.Name == oldName; });
            if (destPlayer != null)
                destPlayer.Name = newName;
        }
        static void LifeTime(List<PlayerInfo> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                players[i].Blood--;
                if (players[i].Blood == 0)
                {
                    PlayerDownLine(players[i]);
                    players.Remove(players[i]);
                }

            }
        }
    }
}
