using System;

namespace MinecraftProtocol.Packets
{
    public class InvalidPacketException : PacketException
    {
        public InvalidPacketException(IPacket packet) : base()
        {
            _packet = packet.Clone() as IPacket;
        }
        public InvalidPacketException(string message) : base(message) { }
        public InvalidPacketException(string message, Exception innerException) : base(message, innerException) { }
        public InvalidPacketException(string message, IPacket packet) : base(message, packet) { }
        public InvalidPacketException(string message, IPacket packet, Exception innerException) : base(message, packet, innerException) { }
    }
}