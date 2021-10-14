using System;
using MinecraftProtocol.Packets;

namespace MinecraftProtocol.Client
{
    public class PacketReceivedEventArgs : PacketEventArgs
    {
        /// <summary>
        /// Packet从服务端发生到被客户端接收消耗的时间（因为存在缓存部分可能不准确）
        /// </summary>
        public virtual TimeSpan RoundTripTime { get; }
        /// <summary>
        /// 用于读取的 <see cref="ReadOnlyCompatiblePacket">ReadOnlyCompatiblePacket</see> 事件结束后会被Dispose，如果有异步或存储的需求请调用Clone
        /// </summary>
        public virtual ReadOnlyCompatiblePacket Packet { get; }

        public PacketReceivedEventArgs(ReadOnlyCompatiblePacket packet, TimeSpan roundTripTime) : this(packet, roundTripTime, DateTime.Now) { }
        public PacketReceivedEventArgs(ReadOnlyCompatiblePacket packet, TimeSpan roundTripTime, DateTime time) : base(time)
        {
            this.Packet = packet;
            this.RoundTripTime = roundTripTime;
        }
    }
}
