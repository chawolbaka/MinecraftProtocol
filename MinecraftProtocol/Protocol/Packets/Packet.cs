using System;
using System.Text;
using System.Collections.Generic;
using MinecraftProtocol.DataType;

namespace MinecraftProtocol.Protocol.Packets
{
    public class Packet:IEquatable<Packet>
    {
        public List<byte> Data { get; set; }
        public int ID { get; set; }

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
            byte[] DataPacket = ProtocolHandler.ConcatBytes(new VarInt(ID).ToBytes(), this.Data.ToArray());

            if (compress > 0)
            {
                if (this.Data.Count >= compress)
                    DataPacket = ProtocolHandler.ConcatBytes(new VarInt(DataPacket.Length).ToBytes(), ZlibUtils.Compress(DataPacket));
                else
                    DataPacket = ProtocolHandler.ConcatBytes(new VarInt(0).ToBytes(), DataPacket);
                return ProtocolHandler.ConcatBytes(new VarInt(DataPacket.Length).ToBytes(), DataPacket);
            }
            else
                return ProtocolHandler.ConcatBytes(new VarInt(DataPacket.Length).ToBytes(), DataPacket);
            
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
            byte[] tmp = Encoding.UTF8.GetBytes(value);
            WriteBytes(ProtocolHandler.ConcatBytes(new VarInt(tmp.Length).ToBytes(), tmp));
        }
        public void WriteVarInt(VarInt value)
        {
            WriteBytes(value.ToBytes());
        }
        public void WriteVarLong(long value)
        {
            WriteBytes(VarLong.Write(value));
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
        public void WriteBytes(params byte[] value)
        {
            foreach (var item in value)
            {
                Data.Add(item);
            }
        }

        public override string ToString()
        {
#if DEBUG
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"PacketID:{ID},PacketLength:{Data.Count}");
            sb.Append("PacketData:");
            foreach (var BYTE in Data)
                sb.Append(BYTE.ToString("X2"));
            return sb.ToString();
#else
            return $"PacketID:{ID},PacketLength:{Data.Count}";
#endif
        }
        public override bool Equals(object obj)
        {
            if (obj is Packet packet)
                return Equals(packet);
            else
                return false;
        }
        public bool Equals(Packet packet)
        {
            if (this.ID != packet.ID)
                return false;
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
