using System;
using MinecraftProtocol.IO;

namespace MinecraftProtocol.Packets
{

    /// <summary>
    /// 一个Packet的包装器，用于防止ID和Data被修改
    /// </summary>
    public struct ReadOnlyPacket : IPacket
    {
        public int Id => _packet.Id;

        public int Count => _packet.Count;
                
        public bool IsReadOnly => true;

        byte IPacket.this[int index] { get => _packet[index]; set => throw new NotSupportedException("Read only"); }
        public byte this[int index] => _packet[index];

        internal Packet _packet;


        public ReadOnlyPacket(Packet packet)
        {
            _packet = packet;
        }

        public byte[] Pack() => _packet.Pack();
      
        public byte[] Pack(int compressionThreshold) => _packet.Pack(compressionThreshold);
        
        public ByteReader AsByteReader() => _packet.AsByteReader();
        
        object ICloneable.Clone() => new ReadOnlyPacket(_packet);


        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyPacket rop)
                return _packet.Equals(rop._packet);

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
