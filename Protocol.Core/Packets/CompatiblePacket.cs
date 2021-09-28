using MinecraftProtocol.Compression;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftProtocol.Packets
{
    /// <summary>
    /// 带有对各种版本mc兼容信息的Packet，由于避免一些参数被输入
    /// </summary>
    public class CompatiblePacket : Packet, ICompatiblePacket
    {
        //这边带了协议版本后我就可以直接加一个PacketType
        //然后做 packet == PacketType.xxx的操作了
        //内部是packet.id == xxx.GetPacketId(packet.ProtocolVersion);
        //同时也可以if（packet == PacketType.xxx）
        // xxx = new xxx(packet);
        public int ProtocolVersion { get; set; }
        //至于这个，就是单纯的方便直接socket.send(packet.Pack());
        //但一般还是直接调用Client.SendPacket()不会用那么原始的方法
        public int CompressionThreshold { get; set; }

        public CompatiblePacket(int packetID, ReadOnlySpan<byte> packetData, int protocolVersion, int compressionThreshold) : base(packetID, packetData)
        {
            ProtocolVersion = protocolVersion;
            CompressionThreshold = compressionThreshold;
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
                    data = ZlibUtils.Decompress(data.ToArray(), size);
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
