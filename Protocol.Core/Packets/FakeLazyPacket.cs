using System;

namespace MinecraftProtocol.Packets
{
    public class FakeLazyPacket<T> : LazyPacket<T> where T : Packet
    {
        public FakeLazyPacket(T packet)
        {
            _id = packet.Id;
            _packet = packet;
            _isCreated = true;
        }

        protected override T InitializePacket()
        {
            throw new NotImplementedException();
        }
    }

}
