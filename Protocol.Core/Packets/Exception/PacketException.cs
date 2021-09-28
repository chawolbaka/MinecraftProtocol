using System;

namespace MinecraftProtocol.Packets
{
    public class PacketException : Exception
    {
        public IPacket Packet => _packet;
        protected IPacket _packet;

        public PacketException() : base() { }
        public PacketException(string message) : base(message) { }
        public PacketException(string message, Exception innerException) : base(message, innerException) { }
        public PacketException(string message, IPacket packet) : base(message)
        {
            _packet = packet.Clone() as IPacket;
        }
        public PacketException(string message, IPacket packet, Exception innerException) : base(message, innerException)
        {
            _packet = packet.Clone() as IPacket;
        }
    }
}
