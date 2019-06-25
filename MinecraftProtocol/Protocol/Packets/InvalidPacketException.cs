using System;

namespace MinecraftProtocol.Protocol.Packets
{
    public class InvalidPacketException:Exception
    {
        Packet Packet { get; }

        public InvalidPacketException(Packet packet) : base()
        {
            this.Packet = packet;
        }
        public InvalidPacketException(string message, Packet packet) : base(message)
        {
            this.Packet = new Packet(packet.ID,Packet.Data);
        }
    }
}
