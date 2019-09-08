using System;

namespace MinecraftProtocol.Protocol.Packets
{
    public class InvalidPacketException : PacketException
    {
        public InvalidPacketException(Packet packet) : base()
        {
            this._packet = new Packet(packet.ID, packet.Data);
        }
        public InvalidPacketException(string message) : base(message) { }
        public InvalidPacketException(string message, Exception innerException) : base(message, innerException) { }
        public InvalidPacketException(string message, Packet packet) : base(message, packet) { }
        public InvalidPacketException(string message, Packet packet, Exception innerException) : base(message, packet, innerException) { }
    }
}