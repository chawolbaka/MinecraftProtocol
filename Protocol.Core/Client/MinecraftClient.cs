﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Utils;
using MinecraftProtocol.Entity;
using MinecraftProtocol.Auth;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Client
{

    public abstract class MinecraftClient : ICompatible
    {
        public const ushort DefaultServerPort = 25565;

        public virtual string ServerHost { get; protected set; }
        public virtual IPAddress ServerIP { get; protected set; }
        public virtual ushort ServerPort { get; protected set; }
        public virtual int CompressionThreshold { get; set; } = -1;
        public virtual int ProtocolVersion { get; set; } = -1;
        
        /// <summary>
        /// 接收到包事件
        /// </summary>
        public abstract event CommonEventHandler<MinecraftClient, PacketReceivedEventArgs> PacketReceived;

        /// <summary>
        /// 发送包事件
        /// </summary>
        public abstract event CommonEventHandler<MinecraftClient, SendPacketEventArgs> PacketSend;

        /// <summary>
        /// 登陆成功事件
        /// </summary>
        public abstract event CommonEventHandler<MinecraftClient, LoginEventArgs> LoginSuccess;

        /// <summary>
        /// TCP断开连接事件
        /// </summary>
        public abstract event CommonEventHandler<MinecraftClient, DisconnectEventArgs> Disconnected;

        /// <summary>
        /// TCP连接状态
        /// </summary>
        public abstract bool Connected { get; }

        /// <summary>
        /// 连接TCP
        /// </summary>
        /// <returns>连接是否成功</returns>
        public abstract bool Connect();

        /// <summary>
        /// 异步连接TCP
        /// </summary>
        /// <returns>连接是否成功</returns>
        public virtual Task<bool> ConnectAsync() => Task.Run(Connect);

        /// <summary>
        /// 玩家是否已进入服务器
        /// </summary>
        public abstract bool Joined { get; }

        /// <summary>
        /// 发送登录服务器的请求
        /// </summary>
        /// <param name="playerName">玩家名</param>
        /// <returns>登录是否成功</returns>
        public abstract Task<bool> JoinAsync(string playerName);

        /// <summary>
        /// 发送登录服务器的请求
        /// </summary>
        /// <param name="session">通过微软或Mojang账号获取的SessionToken，用于正版登录</param>
        /// <returns>登录是否成功</returns>
        public abstract Task<bool> JoinAsync(SessionToken session);

        /// <summary>
        /// 断开连接
        /// </summary>
        public abstract void Disconnect();
        public virtual Task DisconnectAsync() => Task.Run(Disconnect);

        /// <summary>
        /// 开始监听数据包
        /// </summary>
        public abstract void StartListen(CancellationTokenSource cancellationToken = default);

        /// <summary>
        /// 请求停止监听数据包
        /// </summary>
        public abstract void StopListen();

        /// <summary>
        /// 发送数据包
        /// </summary>
        public abstract void SendPacket(IPacket packet);
        public virtual Task SendPacketAsync(IPacket packet)
        {
            Task task = Task.Run(() =>
             {
                 IPacket p = packet;
                 SendPacket(p);
             });
            return task;
        }

        /// <summary>
        /// 获取Socket,用于实现自己的的包监听(也就是说调用了StartListen就不要在获取了,不然不安全)
        /// </summary>
        public abstract Socket GetSocket();

        /// <summary>
        /// 获取玩家
        /// </summary>
        public abstract Player GetPlayer();

        /// <summary>
        /// 获取服务器地址
        /// </summary>
        public override string ToString()
        {
            ReadOnlySpan<char> host = (ServerHost ?? (ServerIP != null ? ServerIP.ToString() : "Unknown")).AsSpan();
            int offset = host.IndexOf('\0');
            if (offset > 0)
                host = host.Slice(0, offset);

            if (ServerPort != DefaultServerPort)
                return $"{host}:{ServerPort}";
            else
                return host.ToString();

        }

    }
}
