using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.IO;

namespace MinecraftProtocol.Packets
{

    /// <summary>
    /// 一个Packet的包装器，用于防止ID和Data被修改
    /// </summary>
    public class ReadOnlyPacket : ByteReader, IPacket
    {
        public int ID => _packet.ID;
        public int Count => _data.Length;
        public virtual bool IsReadOnly => true;
        private Packet _packet;

        byte IPacket.this[int index] { get => _packet[index]; set => throw new NotSupportedException(); }
        public byte this[int index] => _packet[index];

        public ReadOnlyPacket(Packet packet) : base(new ReadOnlyMemory<byte>(packet._data).Slice(0, packet._size))
        {
            _packet = packet;
        }

        public virtual byte[] Pack(int compress = -1) => _packet.Pack(compress);
        object ICloneable.Clone() => new ReadOnlyPacket(_packet) { offset = base.offset };


        /// <summary>
        /// 把只读包转换回可写入的包，仅用于一些特殊情况下
        /// （这种转换非常不安全，因为只读包的目的是让一个包可以被多个线程同时读取，如果出现互相修改很容易出现问题，所以只允许程序集内使用，如果外部需要使用请使用ExpressionTreeUtils或反射）
        /// </summary>
        internal Packet AsPacket()
        {
            return _packet;
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            return _data.Span;
        }

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
