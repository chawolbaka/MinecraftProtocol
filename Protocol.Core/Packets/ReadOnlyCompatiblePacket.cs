using MinecraftProtocol.Compatible;
using MinecraftProtocol.DataType;
using MinecraftProtocol.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Packets
{
    public struct ReadOnlyCompatiblePacket : ICompatiblePacket
    {     
        public int Id => _cpacket.Id;
        public int Count => _cpacket.Count;
        public int ProtocolVersion => _cpacket.ProtocolVersion;
        public int CompressionThreshold => _cpacket.CompressionThreshold;

        byte IPacket.this[int index] { get => _cpacket[index]; set => throw new NotSupportedException("Read only"); }
        public byte this[int index] => _cpacket[index];


        public bool IsReadOnly => true;

        internal CompatiblePacket _cpacket;

        public ReadOnlyCompatiblePacket(CompatiblePacket packet)
        {
            _cpacket = packet;
        }

        public byte[] Pack() => _cpacket.Pack(CompressionThreshold);
        public byte[] Pack(int compressionThreshold) => _cpacket.Pack(compressionThreshold);
        public ByteReader AsByteReader() => _cpacket.AsByteReader();
        public CompatibleByteReader AsCompatibleByteReader() => _cpacket.AsCompatibleByteReader();
        object ICloneable.Clone() => new ReadOnlyCompatiblePacket(_cpacket);

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyCompatiblePacket rop)
                return _cpacket.Equals(rop._cpacket);

            else if (obj is Packet p)
                return _cpacket.Equals(p);
            else
                return false;
        }
        public override string ToString()
        {
            return _cpacket.ToString();
        }

        public override int GetHashCode()
        {
            return _cpacket.GetHashCode();
        }

        public byte[] ToArray()
        {
            return ((IPacket)_cpacket).ToArray();
        }

    }
}
