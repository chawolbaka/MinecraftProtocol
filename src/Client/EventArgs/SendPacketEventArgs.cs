using MinecraftProtocol.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{
    public class SendPacketEventArgs : PacketEventArgs
    {
        public virtual IPacket Packet { get; }

        public SendPacketEventArgs(IPacket packet) : this(packet, DateTime.Now) { }
        public SendPacketEventArgs(IPacket packet, DateTime time) : base(time)
        {
            this.Packet = packet;
        }

    }
}
