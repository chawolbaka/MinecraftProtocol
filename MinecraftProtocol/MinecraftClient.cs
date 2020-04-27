using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MinecraftProtocol.DataType.Chat;
using MinecraftProtocol.IO;
using MinecraftProtocol.Protocol.Packets;

namespace MinecraftProtocol
{
    public class MinecraftClientEventArgs: EventArgs
    {
        public virtual DateTime Time { get; }
        public virtual ReadOnlyPacket Packet { get; }
        public MinecraftClientEventArgs() { this.Time = DateTime.Now; }
        public MinecraftClientEventArgs(ReadOnlyPacket packet) { this.Time = DateTime.Now; this.Packet = packet; }
        public MinecraftClientEventArgs(DateTime time, ReadOnlyPacket packet) { this.Time = time; this.Packet = packet; }
    }

    public abstract class MinecraftClient
    {
        public virtual string ServerHost { get; protected set; }
        public virtual IPAddress ServerIP { get; protected set; }
        public virtual ushort ServerPort { get; protected set; }
        public virtual int CompressionThreshold { get; set; } = -1;
        public virtual int ProtocolVersion { get; set; } = -1;

        /// <summary>
        /// 接收到包的事件(登录期间的包无法获取)
        /// </summary>
        public abstract event PacketReceivedEventHandler PacketReceived;
        public delegate void PacketReceivedEventHandler(MinecraftClient sender, MinecraftClientEventArgs args);

        /// <summary>
        /// 获取连接状态
        /// </summary>
        public abstract bool Connected { get; }

        /// <summary>
        /// 连接服务器
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// 发送登录服务器的请求
        /// </summary>
        /// <param name="playerName">玩家名</param>
        /// <returns>如果是false代表登陆失败</returns>
        public virtual bool Join(string playerName) => Join(playerName, out _);

        /// <summary>
        /// 发送登录服务器的请求
        /// </summary>
        /// <param name="playerName">玩家名</param>
        /// <param name="disconnectReason">无法加入服务器的原因</param>
        /// <returns>如果是false代表登陆失败</returns>
        public abstract bool Join(string playerName, out ChatMessage disconnectReason);

        /// <summary>
        /// 登录服务器
        /// </summary>
        /// <param name="playerName">玩家名</param>
        /// <param name="disconnectReason">无法加入服务器的原因</param>
        /// <returns>如果是false代表登陆失败</returns>
        public virtual bool Login(string playerName, out ChatMessage disconnectReason)
        {
            Connect();
            return Join(playerName, out disconnectReason);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public abstract void Disconnect();
        public virtual void DisconnectAsync() => Task.Factory.StartNew(Disconnect);

        /// <summary>
        /// 开始监听数据包
        /// </summary>
        public abstract Thread StartListen();

        /// <summary>
        /// 请求停止监听数据包
        /// </summary>
        public abstract void StopListen();

        public abstract void SendPacket(IPacket packet);
        public virtual Task SendPacketAsync(IPacket packet) => Task.Factory.StartNew(() => SendPacket(packet));

        /// <summary>
        /// 获取MinecraftStream,用于实现自己的的数据包监听(也就是说调用了StartListen就不要在获取了,不然不安全)
        /// </summary>
        public abstract MinecraftStream GetStream();

        /// <summary>
        /// 获取服务器地址
        /// </summary>
        public override string ToString() => $"{ServerHost ?? (ServerIP != null ? ServerIP.ToString() : "Unknown")}:{ServerPort}";

    }
}
