using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets
{
    public interface ICompatiblePacket : IPacket, ICompatible
    {
        int CompressionThreshold { get; }

        CompatibleByteReader AsCompatibleByteReader();
    }
}