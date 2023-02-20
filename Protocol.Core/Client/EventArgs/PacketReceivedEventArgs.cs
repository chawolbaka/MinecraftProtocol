using System;
using MinecraftProtocol.Packets;

namespace MinecraftProtocol.Client
{
    public class PacketReceivedEventArgs : PacketEventArgs
    {
        /// <summary>
        /// 用于读取的 <see cref="ReadOnlyCompatiblePacket">ReadOnlyCompatiblePacket</see> (如果有异步或存储的需求请调用Clone)
        /// </summary>
        public virtual ReadOnlyCompatiblePacket Packet => (ReadOnlyCompatiblePacket)_packet.AsCompatibleReadOnly();

        private CompatiblePacket _packet;

        internal readonly IO.PacketReceivedEventArgs RawEventArgs;

        public PacketReceivedEventArgs(CompatiblePacket packet) : this(packet, DateTime.Now) { }
        public PacketReceivedEventArgs(CompatiblePacket packet, DateTime time) : base(time)
        {
            _packet = packet;
        }

        public PacketReceivedEventArgs(IO.PacketReceivedEventArgs prea) : base(prea.ReceivedTime)
        {
            _packet = prea.Packet;
        }
    }
}
