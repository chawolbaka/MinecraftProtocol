using System;
using System.Threading.Tasks;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType;
using MinecraftProtocol.IO;

namespace MinecraftProtocol.Packets
{
    /// <summary>
    /// 带有对各种版本mc兼容信息的Packet，用于避免一些参数被输入
    /// </summary>
    public class CompatiblePacket : Packet, ICompatiblePacket
    {
        public int CompressionThreshold  { get => ThrowIfDisposed(_compressionThreshold); set => _compressionThreshold = ThrowIfDisposed(value); }
        public int ProtocolVersion       { get => ThrowIfDisposed(_protocolVersion);      set => _protocolVersion = ThrowIfDisposed(value); }

        private int _compressionThreshold;
        private int _protocolVersion;
        private Packet _packet;


        internal CompatiblePacket(Packet packet, int protocolVersion, int compressionThreshold) : base(packet.Id, ref packet._start, ref packet._size, ref packet._data)
        {
            _packet = packet;
            _protocolVersion = protocolVersion;
            _compressionThreshold = compressionThreshold;
        }
        public CompatiblePacket(int packetId, int start, int size, ref byte[] packetData, int protocolVersion, int compressionThreshold) : this(packetId, ref start, ref size, ref packetData, protocolVersion, compressionThreshold) { }
        internal CompatiblePacket(int packetId, ref int start, ref int size, ref byte[] packetData, int protocolVersion, int compressionThreshold) : base(packetId, ref start, ref size, ref packetData)
        {
            _protocolVersion = protocolVersion;
            _compressionThreshold = compressionThreshold;
        }


        public CompatiblePacket(int packetId, ReadOnlySpan<byte> packetData, int protocolVersion, int compressionThreshold) : base(packetId, packetData)
        {
            _protocolVersion = protocolVersion;
            _compressionThreshold = compressionThreshold;
        }


        public CompatiblePacket(int packetId, int protocolVersion, int compressionThreshold) : base(packetId)
        {
            _protocolVersion = protocolVersion;
            _compressionThreshold = compressionThreshold;
        }

        public CompatiblePacket(int packetId, int capacity, int protocolVersion, int compressionThreshold) : base(packetId, capacity)
        {
            _protocolVersion = protocolVersion;
            _compressionThreshold = compressionThreshold;
        }


        public virtual ByteWriter WritePosition(Position position) => WritePosition(position, ProtocolVersion);
        public virtual ByteWriter WriteByteArray(ReadOnlySpan<byte> bytes) => WriteByteArray(bytes, ProtocolVersion);
        public virtual ByteWriter WriteOptionalByteArray(ReadOnlySpan<byte> bytes) => WriteOptionalByteArray(bytes, ProtocolVersion);


        public override byte[] Pack() => Pack(CompressionThreshold);

        public override Packet Clone() => base.Clone().AsCompatible(this);

        public override IPacket AsReadOnly() => AsCompatibleReadOnly();

        /// <summary>
        /// 从CompatiblePacket转换到ReadOnlyCompatiblePacket（浅拷贝）
        /// </summary>
        public virtual ICompatiblePacket AsCompatibleReadOnly() => new ReadOnlyCompatiblePacket(this);

        /// <summary>
        /// 从CompatiblePacket转换回CompatiblePacket（浅拷贝）
        /// </summary>
        public virtual Packet AsPacket() => new Packet(this);
        
        public virtual CompatibleByteReader AsCompatibleByteReader()
        {
            return new CompatibleByteReader(AsSpan(), ProtocolVersion);
        }

        public static new Packet Depack(ReadOnlySpan<byte> data) => throw new NotSupportedException();

        public static new Task<CompatiblePacket> DepackAsync(ReadOnlyMemory<byte> data) => throw new NotSupportedException();

        public static new CompatiblePacket Depack(ReadOnlySpan<byte> data, int protocolVersion) => Depack(data, protocolVersion, -1);
        public static CompatiblePacket Depack(ReadOnlySpan<byte> data, int protocolVersion, int compressionThreshold)
        {
            Packet packet = Packet.Depack(data);
            return packet.AsCompatible(protocolVersion, compressionThreshold);
        }

        public static new Task<CompatiblePacket> DepackAsync(ReadOnlyMemory<byte> data, int protocolVersion) => DepackAsync(data, protocolVersion, -1);
        public static async Task<CompatiblePacket> DepackAsync(ReadOnlyMemory<byte> data, int protocolVersion, int compressionThreshold)
        {
            Packet packet = await Packet.DepackAsync(data);
            return packet.AsCompatible(protocolVersion, compressionThreshold);
        }

        protected override void ThrowIfDisposed()
        {
            if (_packet != null && _packet._disposed)
                throw new ObjectDisposedException(GetType().FullName);
            else
                base.ThrowIfDisposed();
        }
        protected override T ThrowIfDisposed<T>(T value)
        {
            if (_packet != null)
            {
                if (_packet._disposed)
                    throw new ObjectDisposedException(GetType().FullName);
                else
                    return value;
            }
            else
            {
                return base.ThrowIfDisposed(value);
            }
        }

    }
}
