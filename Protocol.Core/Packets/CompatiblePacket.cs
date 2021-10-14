using System;
using System.Threading.Tasks;
using MinecraftProtocol.Compression;

namespace MinecraftProtocol.Packets
{
    /// <summary>
    /// 带有对各种版本mc兼容信息的Packet，用于避免一些参数被输入
    /// </summary>
    public class CompatiblePacket : Packet, ICompatiblePacket
    {
        private int _compressionThreshold;
        private int _protocolVersion;

        public int CompressionThreshold  => ThrowIfDisposed(_compressionThreshold);
        public int ProtocolVersion => ThrowIfDisposed(_protocolVersion);

        public CompatiblePacket(int packetID, ReadOnlySpan<byte> packetData, int protocolVersion, int compressionThreshold) : base(packetID, packetData)
        {
            _protocolVersion = protocolVersion;
            _compressionThreshold = compressionThreshold;
        }

        public virtual byte[] Pack()
        {
            return base.Pack(CompressionThreshold);
        }

        public override ReadOnlyPacket AsReadOnly() => AsCompatibleReadOnly();
        public virtual ReadOnlyCompatiblePacket AsCompatibleReadOnly() => new ReadOnlyCompatiblePacket(this);
        public static implicit operator ReadOnlyPacket(CompatiblePacket packet) => packet.AsCompatibleReadOnly();

        public static implicit operator ReadOnlyCompatiblePacket(CompatiblePacket packet) => packet.AsCompatibleReadOnly();



        public static new Packet Depack(ReadOnlySpan<byte> data, int compress = -1) => throw new NotSupportedException();
        public static CompatiblePacket Depack(ReadOnlySpan<byte> data, int protocolVersion, int compress = -1)
        {
            if (compress > 0)
            {
                int size = VarInt.Read(data, out int SizeOffset);
                data = data.Slice(SizeOffset);
                if (size != 0) //如果是0的话就代表这个数据包没有被压缩
                    data = ZlibUtils.Decompress(data.ToArray(), 0, size);
            }
            return new CompatiblePacket(VarInt.Read(data, out int IdOffset), data.Slice(IdOffset), protocolVersion, compress);
        }

        public static new Task<CompatiblePacket> DepackAsync(ReadOnlyMemory<byte> data, int compress = -1) => throw new NotSupportedException();
        public static async Task<CompatiblePacket> DepackAsync(ReadOnlyMemory<byte> data, int protocolVersion, int compress = -1)
        {
            if (compress > 0)
            {
                int size = VarInt.Read(data.Span, out int SizeOffset);
                data = data.Slice(SizeOffset);
                if (size != 0) //如果是0的话就代表这个数据包没有被压缩
                    data = await ZlibUtils.DecompressAsync(data, size);
            }
            return new CompatiblePacket(VarInt.Read(data.Span, out int IdOffset), data.Span.Slice(IdOffset), protocolVersion, compress);
        }
    }
}
