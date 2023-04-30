using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets
{

    public abstract class LazyCompatiblePacket : LazyPacket<CompatiblePacket>, ICompatible
    {
        public virtual int CompressionThreshold { get => _isCreated ? _packet.CompressionThreshold : _compressionThreshold;}
        public virtual int ProtocolVersion      { get => _isCreated ? _packet.ProtocolVersion : _protocolVersion;}

        protected int _compressionThreshold;
        protected int _protocolVersion;
    }
}
