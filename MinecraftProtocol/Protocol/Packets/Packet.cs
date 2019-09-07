using System;
using System.Text;
using System.Collections.Generic;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Compression;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol.Packets
{
    public class Packet : IEquatable<Packet>, IWriteMinecraftDataType
    {
        public int ID { get; set; }
        public List<byte> Data { get; set; }
        public int Length => VarInt.GetLength(ID) + Data.Count;

        public Packet() : this(-1) { }
        public Packet(int packetID)
        {
            this.ID = packetID;
            this.Data = new List<byte>();
        }
        public Packet(int packetID, List<byte> packetData)
        {
            this.ID = packetID;
            this.Data = new List<byte>(packetData);
        }
        public Packet(int packetID, params byte[] packetData)
        {
            this.ID = packetID;
            this.Data = new List<byte>();
            foreach (var data in packetData)
            {
                this.Data.Add(data);
            }
        }

        /// <summary>
        /// 获取可以用于发送的完整包
        /// </summary>
        /// <param name="compress">数据包压缩的阚值</param>
        /// <returns></returns>
        public virtual byte[] GetPacket(int compress = -1)
        {
            byte[] PacketData = ProtocolHandler.ConcatBytes(VarInt.GetBytes(ID), this.Data.ToArray());

            if (compress > 0)
            {
                if (this.Data.Count >= compress)
                    PacketData = ProtocolHandler.ConcatBytes(VarInt.GetBytes(PacketData.Length), ZlibUtils.Compress(PacketData));
                else
                    PacketData = ProtocolHandler.ConcatBytes(new byte[] { 0 }, PacketData);
            }
            return ProtocolHandler.ConcatBytes(VarInt.GetBytes(PacketData.Length), PacketData);
        }


        public void WriteBoolean(bool boolean)
        {
            if (boolean)
                WriteUnsignedByte(0x01);
            else
                WriteUnsignedByte(0x00);
        }
        public void WriteByte(sbyte value)
        {
            Data.Add((byte)value);
        }
        public void WriteUnsignedByte(byte value)
        {
            Data.Add(value);
        }
        public void WriteString(string value)
        {
            byte[] str = Encoding.UTF8.GetBytes(value);
            WriteBytes(ProtocolHandler.ConcatBytes(VarInt.GetBytes(str.Length), str));
        }
        public void WriteShort(short value)
        {
            byte[] data = new byte[2];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            WriteBytes(data);
        }
        public void WriteUnsignedShort(ushort value)
        {
            byte[] data = new byte[2];
            //byte[] datax = BitConverter.GetBytes(value);
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            //Array.Reverse(data);
            WriteBytes(data);
        }
        public void WriteInt(int value)
        {
            byte[] data = new byte[4];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            //Array.Reverse(data);
            WriteBytes(data);
        }
        public void WriteLong(long value)
        {
            byte[] data = new byte[8];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            //Array.Reverse(data);
            WriteBytes(data);
        }
        public void WriteFloat(float value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            WriteBytes(data);
        }
        public void WriteDouble(double value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            WriteBytes(data);
        }
        public void WriteVarInt(int value)
        {
            WriteBytes(VarInt.GetBytes(value));
        }
        public void WriteVarLong(long value)
        {
            WriteBytes(VarLong.GetBytes(value));
        }
        public void WriteUUID(UUID value)
        {
            Guid uuid = value.ToGuid();
            WriteBytes(uuid.ToByteArray());
        }
        public void WriteBytes(List<byte> value) => Data.AddRange(value);
        public void WriteBytes(params byte[] value) => Data.AddRange(value);
        public void WriteByteArray(byte[] array, int protocolVersion)
        {
            //14w21a: All byte arrays have VarInt length prefixes instead of short
            if (protocolVersion >= ProtocolVersionNumbers.V14w21a)
                WriteVarInt(array.Length);
            else
                WriteShort((short)array.Length);
            WriteBytes(array);
        }


        public override string ToString()
        {
            return $"PacketID: {ID} PacketLength: {Data.Count}";
        }
        public override bool Equals(object obj)
        {
            if (obj is Packet packet)
                return Equals(packet);
            else
                return false;
        }
        public static bool operator ==(Packet left, Packet right)
        {
            if (object.ReferenceEquals(left, null))
                return object.ReferenceEquals(right, null);
            else
                return left.Equals(right);
        }
        public static bool operator !=(Packet left, Packet right) => !(left == right);
        public bool Equals(Packet packet)
        {
            if (this.ID != packet.ID)
                return false;
            if (this.Data.Count != packet.Data.Count)
                return false;
            if (object.ReferenceEquals(this, packet))
                return true;
            for (int i = 0; i < packet.Data.Count; i++)
            {
                if (this.Data[i] != packet.Data[i])
                    return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            //return ID ^ Data.GetHashCode();
            return HashCode.Combine(ID, Data);
        }
    }
}
