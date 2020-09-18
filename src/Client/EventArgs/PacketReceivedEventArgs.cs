using MinecraftProtocol.Protocol.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{
    public class PacketReceivedEventArgs : PacketEventArgs
    {
        public virtual TimeSpan RoundTripTime { get; }
        public virtual ReadOnlyPacket Packet { get; }

        public PacketReceivedEventArgs(ReadOnlyPacket packet, TimeSpan roundTripTime) : this(packet, roundTripTime, DateTime.Now) { }
        public PacketReceivedEventArgs(ReadOnlyPacket packet, TimeSpan roundTripTime, DateTime time) : base(time)
        {
            this.Packet = packet;
            this.RoundTripTime = roundTripTime;
        }
    }
}
