using MinecraftProtocol.Compression;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Protocol.VersionCompatible;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.Protocol.Packets
{

    public class ReadOnlyPacket
    {
        public int ID => _packet.ID;
        public ReadOnlyPacketData Data { get; }
        public int Length => _packet.Length;

        private Packet _packet;

        public bool IsReadToEnd => offset >= Data.Count;

        public int Position
        {
            get => offset;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Position), "不能使用负数");
                if (value > Data.Count)
                    throw new ArgumentOutOfRangeException(nameof(Position), "Position不能大于Data的长度");
                this.offset = value;
            }
        }

        private int offset;

        public ReadOnlyPacket(Packet packet)
        {
            this._packet = packet;
            this.Data = new ReadOnlyPacketData(packet.Data);
        }

        public virtual byte[] ToBytes(int compress = -1) => _packet.ToBytes(compress);
        public virtual List<byte> ToList(int compress = -1) => _packet.ToList(compress);

        public bool ReadBoolean() => Data[offset++] == 0x01;

        public sbyte ReadByte() => (sbyte)Data[offset++];

        public byte ReadUnsignedByte() => Data[offset++];

        public short ReadShort() => (short)(Data[offset++] << 8 | Data[offset++]);

        public ushort ReadUnsignedShort() => (ushort)(Data[offset++] << 8 | Data[offset++]);

        public int ReadInt() =>
                Data[offset++] << 24 |
                Data[offset++] << 16 |
                Data[offset++] << 08 |
                Data[offset++];

        public long ReadLong() =>
                ((long)Data[offset++]) << 56 |
                ((long)Data[offset++]) << 48 |
                ((long)Data[offset++]) << 40 |
                ((long)Data[offset++]) << 32 |
                ((long)Data[offset++]) << 24 |
                ((long)Data[offset++]) << 16 |
                ((long)Data[offset++]) << 08 |
                Data[offset++];

        public float ReadFloat()
        {
            const int size = sizeof(float);
            byte[] buffer = new byte[size];
            for (int i = 0; i < size; i++)
                buffer[i] = Data[offset + 3 - i];
            offset += size;
            return BitConverter.ToSingle(buffer);
        }

        public double ReadDouble()
        {
            const int size = sizeof(double);
            byte[] buffer = new byte[size];
            for (int i = 0; i < size; i++)
                buffer[i] = Data[offset + 3 - i];
            offset += size;
            return BitConverter.ToDouble(buffer);
        }

        public string ReadString()
        {
            int length = ReadVarInt();
            string result = Encoding.UTF8.GetString(Data.Slice(offset, length));
            offset += length;
            return result;
        }

        public int ReadVarInt()
        {
            int result = VarInt.Read(Data.ToSpan(), offset, out int length);
            offset += length;
            return result;
        }

        public long ReadVarLong()
        {
            long result = VarLong.Read(Data.ToSpan(), offset, out int length);
            offset += length;
            return result;
        }

        public UUID ReadUUID() => new UUID(ReadLong(), ReadLong());

        public byte[] ReadByteArray(int protocolVersion)
        {
            int ArrayLength = protocolVersion >= ProtocolVersionNumbers.V14w21a ? ReadVarInt() : ReadShort();
            byte[] result = Data.Slice(offset, ArrayLength).ToArray();
            offset += ArrayLength;
            return result;
        }
    }
}
