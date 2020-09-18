using System;

namespace MinecraftProtocol.Packets
{
    public class PacketException : Exception
    {
        public Packet Packet => _packet;
        protected Packet _packet;
        public PacketException() : base() { }
        public PacketException(string message) : base(message) { }
        public PacketException(string message, Exception innerException) : base(message, innerException) { }
        public PacketException(string message, Packet packet) : base(message)
        {
            _packet = packet.Clone();
        }
        public PacketException(string message, Packet packet, Exception innerException) : base(message, innerException)
        {
            _packet = packet.Clone();
        }
    }
}
