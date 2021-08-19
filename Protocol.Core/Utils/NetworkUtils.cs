﻿using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MinecraftProtocol.Utils
{
    public static class NetworkUtils
    {

        /// <summary>
        /// 一直接收，直到length全部被读取完
        /// </summary>
        public static byte[] ReceiveData(int length, Socket tcp)
        {
            byte[] buffer = new byte[length]; 
            int read = 0;
            int count = 0;
            while (read < length)
            {
                if (count >= 26)
                {
                    if (!CheckConnect(tcp))
                    {
                        tcp.Disconnect(false);
                        throw new SocketException((int)SocketError.ConnectionReset);
                    }
                    else
                        count /= 2;
                }
                else
                {
                    read += tcp.Receive(buffer, read, length - read, SocketFlags.None);
                    count++;
                }
            }
            return buffer;
        }

        public static bool CheckConnect(Socket tcp)
        {
            try
            {
                if (tcp.Available > 0)
                    return true;

                //wiki说连接关闭后Available会直接抛出异常，但我从来没见过它在断开连接后抛出过异常
                //还是这种方法的准确率高一点，不过看着效率没有Available高，所以暂时只在Available<1的时候使用
                var connections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections()
                    .Where(x => x.LocalEndPoint.Equals(tcp.LocalEndPoint) && x.RemoteEndPoint.Equals(tcp.RemoteEndPoint));
                return connections != null && connections.Any() && connections.First().State == TcpState.Established;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        /// <summary>
        /// 检查IP是否是保留地址
        /// </summary>
        public static bool IsReserved(this IPAddress ip)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return IsAddressReserved(ip.GetAddressBytes());
            else
                throw new NotSupportedException("不支持IPv6");
        }

        public static bool IsAddressReserved(byte[] ip)
        {
            //数据维基百科:https://zh.wikipedia.org/wiki/%E4%BF%9D%E7%95%99IP%E5%9C%B0%E5%9D%80

            //效用域:专用网络

            //用于专用网络内的本地通信。
            if (ip[0] == 10) return true;
            //用于在电信级NAT环境中服务提供商与其用户通信。[3]
            else if (ip[0] == 100 && (ip[1] & 0xC0) == 64) return true;
            //用于专用网络中的本地通信。
            else if (ip[0] == 172 && (ip[1] & 0xF0) == 16) return true;
            //用于专用网络中的本地通信。
            else if (ip[0] == 192 && ip[1] == 168) return true;
            //用于IANA的IPv4特殊用途地址表。
            else if (ip[0] == 192 && ip[1] == 0 && ip[2] == 0) return true;
            //用于测试两个不同的子网的网间通信。
            else if (ip[0] == 192 && (ip[1] & 0xFE) == 18) return true;


            //效用域:主机

            //用于到本地主机的环回地址。
            else if (ip[0] == 127) return true;


            //效用域:文档

            //分配为用于文档和示例中的“TEST-NET”（测试网），它不应该被公开使用。
            else if (ip[0] == 192 && ip[1] == 0 && ip[2] == 2) return true;
            //分配为用于文档和示例中的“TEST-NET-2”（测试-网-2），它不应该被公开使用。
            else if (ip[0] == 192 && ip[1] == 51 && ip[2] == 100) return true;
            //分配为用于文档和示例中的“TEST-NET-3”（测试-网-3），它不应该被公开使用。
            else if (ip[0] == 203 && ip[1] == 0 && ip[2] == 113) return true;

            //效用域:子网

            //用于单链路的两个主机之间的本地链路地址，而没有另外指定IP地址，例如通常从DHCP服务器所检索到的IP地址。
            else if (ip[0] == 169 && ip[1] == 254) return true;
            else return false;
        }
    }
}
