using System;

namespace MinecraftProtocol.Packets
{
    public class PacketNotFoundException:PacketException
    {
        public PacketNotFoundException() : base() { }
        public PacketNotFoundException(string message) : base(message) { }
        public PacketNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
