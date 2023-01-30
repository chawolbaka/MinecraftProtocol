using System;
using MinecraftProtocol.Packets;

namespace MinecraftProtocol.Client
{
    public class PacketReceivedEventArgs : PacketEventArgs
    {
        /// <summary>
        /// 用于读取的 <see cref="ReadOnlyCompatiblePacket">ReadOnlyCompatiblePacket</see> (如果有异步或存储的需求请调用Clone)
        /// </summary>
        public virtual ReadOnlyCompatiblePacket Packet { get; }

        public virtual IO.PacketReceivedEventArgs RawEventArgs { get; }

        public PacketReceivedEventArgs(ReadOnlyCompatiblePacket packet) : this(packet, DateTime.Now) { }
        public PacketReceivedEventArgs(ReadOnlyCompatiblePacket packet, DateTime time) : base(time)
        {
            this.Packet = packet;
        }

        public PacketReceivedEventArgs(IO.PacketReceivedEventArgs prea):base(prea.ReceivedTime)
        {
            Packet = prea.Packet;
            RawEventArgs = prea;
        }
    }
}
