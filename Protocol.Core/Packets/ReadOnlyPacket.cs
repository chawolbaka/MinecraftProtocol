using System;
using MinecraftProtocol.IO;

namespace MinecraftProtocol.Packets
{

    /// <summary>
    /// 一个Packet的包装器，用于防止ID和Data被修改
    /// </summary>
    public class ReadOnlyPacket : ByteReader, IPacket
    {
        public int ID => _packet.ID;
        public virtual bool IsReadOnly => true;
        internal Packet _packet;

        byte IPacket.this[int index] { get => _packet[index]; set => throw new NotSupportedException("Read only"); }
        public override byte this[int index] => _packet[index];

        public ReadOnlyPacket(Packet packet) : base(new ReadOnlyMemory<byte>(packet._data).Slice(0, packet._size))
        {
            _packet = packet;
        }

        public virtual byte[] Pack() => _packet.Pack();
        public virtual byte[] Pack(int compressionThreshold) => _packet.Pack(compressionThreshold);
        object ICloneable.Clone() => new ReadOnlyPacket(_packet) { offset = base.offset };

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyPacket rop)
                return offset == rop.offset && _packet.Equals(rop._packet);

            else if (obj is Packet p)
                return _packet.Equals(p);
            else
                return false;
        }
        public override string ToString()
        {
            return _packet.ToString();
        }

        public override int GetHashCode()
        {
            return _packet.GetHashCode();
        }

        public byte[] ToArray()
        {
            return ((IPacket)_packet).ToArray();
        }
    }
}
