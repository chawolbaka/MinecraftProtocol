using MinecraftProtocol.Compatible;
using MinecraftProtocol.IO;
using System;
using System.Text;

namespace MinecraftProtocol.Packets
{
    public abstract class DefinedPacket : Packet
    {
        /// <summary>
        /// 当前版本没有的包
        /// </summary>
        protected const int UnsupportPacketId = -25;
        protected virtual int ProtocolVersion { get; set; }
        protected virtual ByteReader Reader { get; private set; }

        protected DefinedPacket(int id, int protocolVersion) : this(id, null, protocolVersion) {}
        protected DefinedPacket(int id, byte[] data, int protocolVersion) : base(id, data)
        {
            ProtocolVersion = protocolVersion;
        }

        protected DefinedPacket(ReadOnlyPacket packet,int protcolVersion)
        {
            //id由自动生成的代码设置
            _size = packet.Count;
            _data = new byte[packet.Count];
            packet.AsSpan().CopyTo(_data);
            Reader = packet;
            ProtocolVersion = protcolVersion;
        }

        protected virtual void CheckProperty()
        {
            if (ProtocolVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(ProtocolVersion), $"无法使用负数的{nameof(ProtocolVersion)}创建Packet");
        }
        protected abstract void Write();
        protected abstract void Read();

        public new static Packet Depack(ReadOnlySpan<byte> data, int compress = -1)
        {
            throw new NotImplementedException("请调用TryRead或通过构造函数创建包");
        }
    }
}
