using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PlayersMonitor
{
    public static class Settings
    {
        public static bool Initialization(string[] args)
        {
            if (args == null || args.Length == 0)
                return false;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-help" || args[i] == "-h" || args[i] == "--help")
                {
                    Console.WriteLine($"默认值:IP:{IPAddress.Destination} Port:{Port} Blood:{Blood} Sleep:{Sleep}ms");
                    Console.WriteLine("-ip 设置IP");
                    Console.WriteLine("-p Number(0-65535) 设置端口");
                    Console.WriteLine("-b Number(0-4294967295) or auto(无效) 设置生命周期");
                    //Console.WriteLine("Minecraft的Ping包给的玩家列表最多只有12个,如果服务器超过了12个人就随机给12个人的名字。");
                    //Console.WriteLine("所以这个参数是这样的,如果一个玩家随机x次都随机不出来就当成它下线了");
                    Console.WriteLine("-s Number(0-4294967295) 设置发包频率(ms)\r\n");
                    Console.WriteLine("ps:发包频率过低会导致服务器返回包了,然后程序就卡死了.");
                    Console.WriteLine("这个版本没有做任何异常处理.");
                    Console.WriteLine("所以如果不刷新了请直接重启程序,我对这个问题没有做任何处理(我也不知道怎么处理)");
                    return true;
                }
                if (args[i] == "-ip" && i != args.Length)
                    IPAddress.Destination = Regex.Replace(args[i + 1],@"\s","");
                if (args[i] == "--IPSource" && i != args.Length)
                    IPAddress.Source = Regex.Replace(args[i + 1], @"\s", "");
                if (args[i] == "-p" || args[i] == "-port" && i != args.Length)
                {
                    if (ushort.TryParse(args[i + 1], out _Port) == false)
                    {
                        Console.WriteLine($"参数错误{Environment.NewLine}Use:-p (0-65535)");
                        System.Environment.Exit(0);
                    }
                }
                if (args[i] == "-b"||args[i] == "-blood" && i != args.Length)
                {
                    if (args[i + 1] == "auto")
                        AutoSetBlood = true;
                    else if (int.TryParse(args[i + 1], out _Blood) == false)
                    {
                        Console.WriteLine($"参数错误{Environment.NewLine}Use:-b ({int.MinValue}-{int.MaxValue})");
                        System.Environment.Exit(0);
                    }
                }
                if (args[i] == "-s"|| args[i] == "-sleeptime" && i != args.Length)
                {
                    if (uint.TryParse(args[i + 1], out _Sleep) == false)
                    {
                        Console.WriteLine($"参数错误{Environment.NewLine}Use:-s (0-{uint.MaxValue})");
                        System.Environment.Exit(0);
                    }
                }
            }
            return false;
        }
        public static (string Source, string Destination) IPAddress = (Source:null,Destination:null);
        public static ushort Port { get { return _Port ; } set { _Port= value; } }
        private static ushort _Port = 25565;
        public static int Blood { get { return _Blood; } set { _Blood = value; } }
        private static int _Blood = 10;
        public static uint Sleep { get { return _Sleep; } set { _Sleep = value; } }
        private static uint _Sleep = 1000;

        public static bool ShowPing { get; set; } = true;
        public static bool AutoSetBlood { get; set; } = false;
    }
}
