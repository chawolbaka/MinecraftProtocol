using System;

namespace MinecraftProtocol.Packets
{
    public class FakeLazyCompatiblePacket : LazyCompatiblePacket
    {
        public FakeLazyCompatiblePacket(CompatiblePacket packet)
        {
            _id = packet.Id;
            _packet = packet;
            _isCreated = true;

            ProtocolVersion = packet.ProtocolVersion;
            CompressionThreshold = packet.CompressionThreshold;
        }

        protected override CompatiblePacket InitializePacket()
        {
            throw new NotImplementedException();
        }
    }
}
