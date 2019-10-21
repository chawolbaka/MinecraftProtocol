using System;
using System.Text;
using System.Collections.Generic;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Compression;
using MinecraftProtocol.Protocol.VersionCompatible;

namespace MinecraftProtocol.Protocol.Packets
{
    public class Packet : IEquatable<Packet>
    {
        public virtual int ID { get; set; }
        public virtual List<byte> Data { get; set; }
        public virtual int Length => VarInt.GetLength(ID) + Data.Count;

        public Packet() : this(-1) { }
        public Packet(int packetID)
        {
            this.ID = packetID;
            this.Data = new List<byte>();
        }
        public Packet(int packetID, IEnumerable<byte> packetData)
        {
            this.ID = packetID;
            this.Data = new List<byte>(packetData);
        }

        /// <summary>
        /// 生成发送给服务端的包
        /// </summary>
        /// <param name="compress">数据包压缩的阚值</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public virtual byte[] ToBytes(int compress = -1)
        {
            byte[] PacketData;
            if (compress > 0)
            {
                /*
                 * 压缩的数据包:
                 *
                 * 名称       类型    备注
                 * 数据包长度 Varint  等于下方两者解压前的总字节数
                 * 解压后长度 Varint  若为0则表示本包未被压缩
                 * 被压缩的数据       经Zlib压缩.开头是一个VarInt字段,代表数据包ID,然后是数据包数据.
                 */
                if (Data.Count >= compress)
                {
                    //拼接PacketID(VarInt)和PacketData(ByteArray)并塞入ZlibUtils.Compress去压缩
                    byte[] uncompressed = new byte[Length];
                    Data.CopyTo(uncompressed, VarInt.WriteTo(ID, uncompressed));
                    byte[] compressed = ZlibUtils.Compress(uncompressed);

                    PacketData = new byte[VarInt.GetLength(VarInt.GetLength(uncompressed.Length) + compressed.Length) + VarInt.GetLength(uncompressed.Length) + compressed.Length];
                    //写入第一个VarInt(解压长度+压缩后的长度）
                    int offset = VarInt.WriteTo(VarInt.GetLength(uncompressed.Length) + compressed.Length, PacketData);
                    //写入第二个VarInt(未压缩长度)
                    offset += VarInt.WriteTo(uncompressed.Length, PacketData.AsSpan().Slice(offset));
                    //写入被压缩的数据
                    compressed.CopyTo(PacketData, offset);
                }
                else
                {
                    PacketData = new byte[VarInt.GetLength(Length+1) + Length + 1];
                    int offset = VarInt.WriteTo(Length + 1, PacketData);
                    PacketData[offset++] = 0;
                    offset += VarInt.WriteTo(ID, PacketData.AsSpan().Slice(offset));
                    if (Data.Count > 0)
                        Data.CopyTo(PacketData, offset);
                }
            }
            else
            {
                PacketData = new byte[VarInt.GetLength(Length) + Length];
                int offset = VarInt.WriteTo(Length, PacketData);
                offset += VarInt.WriteTo(ID, PacketData.AsSpan().Slice(offset));
                if (Data.Count > 0)
                    Data.CopyTo(PacketData, offset);
            }
            return PacketData;
        }
        /// <summary>
        /// 生成发送给服务端的包
        /// </summary>
        /// <param name="compress">数据包压缩的阚值</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public virtual List<byte> ToList(int compress = -1)
        {
            List<byte> PacketData = new List<byte>();
            if (compress > 0)
            {
                if (Data.Count >= compress)
                {
                    PacketData.AddRange(VarInt.GetBytes(Length));
                    int IdLength = VarInt.GetLength(ID);
                    byte[] buffer = new byte[IdLength + Data.Count];
                    Array.Copy(VarInt.GetBytes(ID), 0, buffer, 0, IdLength);
                    Data.CopyTo(buffer, IdLength);
                    PacketData.AddRange(ZlibUtils.Compress(buffer));
                    PacketData.InsertRange(0, VarInt.GetBytes(PacketData.Count));
                }
                else
                {
                    PacketData.AddRange(VarInt.GetBytes(Length + 1));
                    PacketData.Add(0);
                    PacketData.AddRange(VarInt.GetBytes(ID));
                    PacketData.AddRange(Data);
                }
            }
            else
            {
                PacketData.AddRange(VarInt.GetBytes(Length));
                PacketData.AddRange(VarInt.GetBytes(ID));
                PacketData.AddRange(Data);

            }
            return PacketData;
        }
        
        public virtual void WriteBoolean(bool boolean)
        {
            if (boolean)
                WriteUnsignedByte(0x01);
            else
                WriteUnsignedByte(0x00);
        }
        public virtual void WriteByte(sbyte value)
        {
            Data.Add((byte)value);
        }
        public virtual void WriteUnsignedByte(byte value)
        {
            Data.Add(value);
        }
        public virtual void WriteString(string value)
        {
            byte[] str = Encoding.UTF8.GetBytes(value);
            WriteBytes(ProtocolHandler.ConcatBytes(VarInt.GetBytes(str.Length), str));
        }
        public virtual void WriteShort(short value)
        {
            byte[] data = new byte[2];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            WriteBytes(data);
        }
        public virtual void WriteUnsignedShort(ushort value)
        {
            byte[] data = new byte[2];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            WriteBytes(data);
        }
        public virtual void WriteInt(int value)
        {
            byte[] data = new byte[4];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            WriteBytes(data);
        }
        public virtual void WriteLong(long value)
        {
            byte[] data = new byte[8];
            for (int i = data.Length; i > 0; i--)
            {
                data[i - 1] |= (byte)value;
                value >>= 8;
            }
            WriteBytes(data);
        }
        public virtual void WriteFloat(float value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            WriteBytes(data);
        }
        public virtual void WriteDouble(double value)
        {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            WriteBytes(data);
        }
        public virtual void WriteVarInt(int value)
        {
            WriteBytes(VarInt.GetBytes(value));
        }
        public virtual void WriteVarLong(long value)
        {
            WriteBytes(VarLong.GetBytes(value));
        }
        public virtual void WriteUUID(UUID value)
        {
            WriteLong(value.Most);
            WriteLong(value.Least);
        }
        public virtual void WriteBytes(IEnumerable<byte> value) => Data.AddRange(value);
        public virtual void WriteBytes(params byte[] value) => Data.AddRange(value);
        public virtual void WriteByteArray(byte[] array, int protocolVersion)
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
            if (packet is null) return false;
            if (this.ID != packet.ID) return false;
            if (this.Data.Count != packet.Data.Count) return false;
            if (object.ReferenceEquals(this, packet)) return true;
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
