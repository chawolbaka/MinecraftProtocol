using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets
{

    /// <summary>
    /// 一个Packet的包装器，用于防止ID和Data被修改（如果修改原始Packet中的内容会导致这边也被修改）
    /// </summary>
    public class ReadOnlyPacket : IPacket, IEnumerable<byte>
    {
        public int ID => _packet.ID;
        public int Count => _packet.Count;
        private Packet _packet;

        byte IPacket.this[int index] { get => _packet[index]; set => throw new NotSupportedException(); }
        public byte this[int index] => _packet[index];
        

        public bool IsReadToEnd => offset >= _packet.Count;
        public int Position
        {
            get => offset;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Position), "不能使用负数");
                if (value > _packet.Count)
                    throw new ArgumentOutOfRangeException(nameof(Position), "Position不能大于Data的长度");
                offset = value;
            }
        }


        private int offset;

        public ReadOnlyPacket(Packet packet)
        {
            _packet = packet;
        }


        public virtual byte[] Pack(int compress = -1) => _packet.Pack(compress);


        public bool ReadBoolean() => _packet[offset++] == 0x01;

        public sbyte ReadByte() => (sbyte)_packet[offset++];

        public byte ReadUnsignedByte() => _packet[offset++];

        public short ReadShort() => (short)(_packet[offset++] << 8 | _packet[offset++]);

        public ushort ReadUnsignedShort() => (ushort)(_packet[offset++] << 8 | _packet[offset++]);

        public int ReadInt() =>
                _packet[offset++] << 24 |
                _packet[offset++] << 16 |
                _packet[offset++] << 08 |
                _packet[offset++];

        public long ReadLong() =>
                ((long)_packet[offset++]) << 56 |
                ((long)_packet[offset++]) << 48 |
                ((long)_packet[offset++]) << 40 |
                ((long)_packet[offset++]) << 32 |
                ((long)_packet[offset++]) << 24 |
                ((long)_packet[offset++]) << 16 |
                ((long)_packet[offset++]) << 08 |
                _packet[offset++];

        public float ReadFloat()
        {
            const int size = sizeof(float);
            byte[] buffer = new byte[size];
            for (int i = 0; i < size; i++)
                buffer[i] = _packet[offset + 3 - i];
            offset += size;
            return BitConverter.ToSingle(buffer);
        }

        public double ReadDouble()
        {
            const int size = sizeof(double);
            byte[] buffer = new byte[size];
            for (int i = 0; i < size; i++)
                buffer[i] = _packet[offset + 3 - i];
            offset += size;
            return BitConverter.ToDouble(buffer);
        }

        public string ReadString()
        {
            int length = ReadVarInt();
            var x = _packet.AsSpan().Slice(offset, length);
            string result = Encoding.UTF8.GetString(x);
            offset += length;
            return result;
        }

        public int ReadVarShort()
        {
            int result = VarShort.Read(_packet.AsSpan().Slice(offset), out int length);
            offset += length;
            return result;
        }

        public int ReadVarInt()
        {
            int result = VarInt.Read(_packet.AsSpan().Slice(offset), out int length);
            offset += length;
            return result;
        }

        public long ReadVarLong()
        {
            long result = VarLong.Read(_packet.AsSpan().Slice(offset), out int length);
            offset += length;
            return result;
        }

        public UUID ReadUUID()
        {
            return new UUID(ReadLong(), ReadLong());
        }

        public byte[] ReadByteArray(int protocolVersion)
        {
            int ArrayLength = protocolVersion >= ProtocolVersionNumbers.V14w21a ? ReadVarInt() : ReadShort();
            byte[] result = _packet.AsSpan().Slice(offset, ArrayLength).ToArray();
            offset += ArrayLength;
            return result;
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            return _packet.AsSpan();
        }
        
        public byte[] ReadAll()
        {
            offset = _packet.Count;
            return _packet.AsSpan().ToArray();
        }

        public override int GetHashCode()
        {
            return _packet.GetHashCode();
        }

        public IEnumerator<byte> GetEnumerator()
        {
            return ((IEnumerable<byte>)_packet).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_packet).GetEnumerator();
        }

        public byte[] ToArray()
        {
            return ((IPacket)_packet).ToArray();
        }
    }
}
