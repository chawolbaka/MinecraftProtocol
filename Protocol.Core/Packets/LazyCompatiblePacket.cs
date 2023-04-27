using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets
{

    public abstract class LazyCompatiblePacket : LazyPacket<CompatiblePacket>, ICompatible
    {
        public virtual int CompressionThreshold { get; set; }
        public virtual int ProtocolVersion { get; set; }
    }
}
