using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets
{
    public interface ICompatiblePacket : IPacket
    {
        int CompressionThreshold { get; }
        int ProtocolVersion { get; }
    }
}