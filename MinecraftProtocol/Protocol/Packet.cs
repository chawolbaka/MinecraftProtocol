using MinecraftProtocol.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace MinecraftProtocol.Protocol
{
    public class Packet
    {
        private List<byte> packetData;
        public int? PacketID { get; set; }
        public Packet()
        {
            packetData = new List<byte>();
        }
        public void WriteBoolean(bool value)
        {
            if (value == true)
                WriteUnsignedByte(0x01);
            else
                WriteUnsignedByte(0x00);
        }
        public void WriteByte(sbyte value)
        {
            packetData.Add((byte)value);
        }
        public void WriteUnsignedByte(byte value)
        {
            packetData.Add(value);
        }
        public void WriteString(string value)
        {
            byte[] tmp = Encoding.UTF8.GetBytes(value);
            WriteBytes(ProtocolHandler.ConcatBytes(VarInt.Write(tmp.Length),tmp));
        }
        public void WriteVarInt(int value)
        {
            WriteBytes(VarInt.Write(value));
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
                data[i-1] |= (byte)value;
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
                packetData.Add(item);
            }
        }
        public byte[] GetPacket(int compress=-1)
        {
            byte[] tmp_packet = ProtocolHandler.ConcatBytes(VarInt.Write(PacketID), this.packetData.ToArray());
            if (compress > 0)
            {
                if (this.packetData.Count >= compress)
                    tmp_packet = ProtocolHandler.ConcatBytes(VarInt.Write(tmp_packet.Length), ZlibUtils.Compress(tmp_packet));
                else
                    tmp_packet = ProtocolHandler.ConcatBytes(VarInt.Write(0), tmp_packet);
                return ProtocolHandler.ConcatBytes(VarInt.Write(tmp_packet.Length), tmp_packet);
            }
            else
                return ProtocolHandler.ConcatBytes(VarInt.Convert(tmp_packet.Length),tmp_packet);
            
        }
        
    }
}
