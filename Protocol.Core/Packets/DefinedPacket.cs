using MinecraftProtocol.Compatible;
using MinecraftProtocol.DataType;
using MinecraftProtocol.IO;
using System;

namespace MinecraftProtocol.Packets
{
    public abstract class DefinedPacket : Packet
    {
        /// <summary>
        /// 当前版本没有的包
        /// </summary>
        public const int UnsupportPacketId = -25;
        public virtual int ProtocolVersion { get; protected set; }

        protected virtual CompatibleByteReader Reader => reader ??= new CompatibleByteReader(AsMemory(), ProtocolVersion);
        private CompatibleByteReader reader;

        protected DefinedPacket(int id, int protocolVersion) : this(id, null, protocolVersion) { }
        protected DefinedPacket(int id, byte[] data, int protocolVersion) : base(id, data)
        {
            ProtocolVersion = protocolVersion;
        }

        internal protected DefinedPacket(int id, int size, ref byte[] data, int protcolVersion) : base(id, ref size, ref data)
        {
            ProtocolVersion = protcolVersion;
        }

        protected DefinedPacket(int id, ref int size, ref byte[] data, int protcolVersion) : base(id, ref size, ref data)
        {
            ProtocolVersion = protcolVersion;
        }

        protected DefinedPacket(ReadOnlyPacket packet, int protcolVersion)
        {
            //id由自动生成的代码设置
            _size = packet.Count;
            RerentData(packet.Count);
            packet.AsSpan().CopyTo(_data);
            reader = new CompatibleByteReader(packet.AsMemory(), protcolVersion);
            ProtocolVersion = protcolVersion;
        }

        public virtual ByteWriter WritePosition(Position position) => WritePosition(position, ProtocolVersion);
        public virtual ByteWriter WriteByteArray(ReadOnlySpan<byte> bytes) => WriteByteArray(bytes, ProtocolVersion);
        public virtual ByteWriter WriteOptionalByteArray(ReadOnlySpan<byte> bytes) => WriteOptionalByteArray(bytes, ProtocolVersion);


        protected virtual void CheckProperty()
        {
            if (ProtocolVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(ProtocolVersion), $"无法使用负数的{nameof(ProtocolVersion)}创建Packet");
        }
        protected virtual void SetProperty(string propertyName, object newValue)
        {
            _size = 0;
            Write();
        }
        protected abstract void Write();
        protected abstract void Read();


        public new static Packet Depack(ReadOnlySpan<byte> data, int compress = -1)
        {
            throw new NotImplementedException("请调用TryRead或通过构造函数创建包");
        }
    }
}
