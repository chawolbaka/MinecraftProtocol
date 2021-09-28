using MinecraftProtocol.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Client
{
    public class PacketReceivedEventArgs : PacketEventArgs
    {
        public virtual TimeSpan RoundTripTime { get; }
        public virtual ReadOnlyCompatiblePacket Packet { get; }

        public PacketReceivedEventArgs(ReadOnlyCompatiblePacket packet, TimeSpan roundTripTime) : this(packet, roundTripTime, DateTime.Now) { }
        public PacketReceivedEventArgs(ReadOnlyCompatiblePacket packet, TimeSpan roundTripTime, DateTime time) : base(time)
        {
            this.Packet = packet;
            this.RoundTripTime = roundTripTime;
        }
    }
}
