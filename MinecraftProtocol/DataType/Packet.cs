using System;
using System.Text;
using System.Collections.Generic;
using MinecraftProtocol.Protocol;

namespace MinecraftProtocol.DataType
{
    public class Packet
    {
        public List<byte> Data { get; set; } = new List<byte>();
        public int ID
        {
            //解释一下为什么要这样子写
            //我的想法是这样子的,PacketID不是必须写的参数,如果不写的话在GetPacket的时候就不在前面加PacketID了
            //但是我直接往属性上面加个?的话外面调用PacketID可能需要一个强制转换才能使用,所以就写了这块代码
            //(其实我一开始的想法是去掉这个强制转换的,然后写了后发现还是需要一个强制转换.不过起码现在不用在外部强制转换了)
            get {
                if (_PacketID == null)
                    throw new Exception("PacketID is null , Please set a value");
                else 
                    return (int)_PacketID;
            }
            set {
                _PacketID = value;
            }
        }
        private int? _PacketID = null;

        public Packet()
        {
        }
        public Packet(int packetID, List<byte> packetData)
        {
            this.ID = packetID;
            this.Data = packetData;
        }
        public Packet(int packetID,params byte[] packetData)
        {
            this.ID = packetID;
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
        public byte[] GetPacket(int compress = -1)
        {
            byte[] DataPacket;
            if (_PacketID.HasValue)
                DataPacket = ProtocolHandler.ConcatBytes(new VarInt(_PacketID.Value).ToBytes(), this.Data.ToArray());
            else
                DataPacket = this.Data.ToArray();

            if (compress > 0)
            {
                if (this.Data.Count >= compress)
                    DataPacket = ProtocolHandler.ConcatBytes(new VarInt(DataPacket.Length).ToBytes(), ZlibUtils.Compress(DataPacket));
                else
                    DataPacket = ProtocolHandler.ConcatBytes(new VarInt(0).ToBytes(), DataPacket);
                return ProtocolHandler.ConcatBytes(new VarInt(DataPacket.Length).ToBytes(), DataPacket);
            }
            else
                return ProtocolHandler.ConcatBytes(new VarInt(DataPacket.Length).ToBytes(),DataPacket);
            
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
    }
}
