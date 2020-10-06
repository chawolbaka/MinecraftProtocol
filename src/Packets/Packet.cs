using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Compression;
using MinecraftProtocol.Compatible;
using System.Linq;

namespace MinecraftProtocol.Packets
{
    public class Packet : IPacket, IEnumerable<byte>, IEquatable<Packet>
    {
        public virtual bool IsEmpty => ID < 0 || _data is null;

        public virtual int ID { get; set; }
      
        public virtual int Count => _size;

        public virtual byte this[int index]
        {
            get
            {
                if (index > _size)
                    throw new IndexOutOfRangeException("Index was outside the bounds of the packet.");
                else
                    return _data[index];
            }
            set
            {
                if (index > _size)
                    throw new IndexOutOfRangeException("Index was outside the bounds of the packet.");
                else
                    _data[index] = value;
            }
        }

        public virtual int Capacity
        {
            get => _data.Length;
            set
            {
                if (value < _size)
                    throw new ArgumentOutOfRangeException(nameof(Capacity), "Capacity was less than the current size.");
                if (value == _size)
                    return;

                byte[] temp = new byte[value];
                Array.Copy(_data, temp, _size);
                _data = temp;
            }
        }

        /// <summary>
        /// 和List内部的_items一样后面可能有空白的部分
        /// </summary>
        protected byte[] _data;
        private int _size = 0;

        private const int DEFUALT_CAPACITY = 16;

        public Packet() : this(-1) { }
        public Packet(int packetID) : this(packetID, DEFUALT_CAPACITY) { }
        public Packet(int packetID, int capacity)
        {
            ID = packetID;
            _data = new byte[capacity];
        }
        public Packet(int packetID, byte[] packetData)
        {
            ID = packetID;
            _data = packetData;
            _size = _data.Length;
        }
        public Packet(int packetID, ReadOnlySpan<byte> packetData) : this(packetID, packetData.ToArray()) { }
        public Packet(IPacket packet) : this(packet.ID, packet.ToArray()) { }

        /// <summary>
        /// 生成发送给服务端的包
        /// </summary>
        /// <param name="compress">数据包压缩的阚值</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="IndexOutOfRangeException"/>
        /// <exception cref="PacketException"/>
        public virtual byte[] Pack(int compress = -1)
        {
            if (IsEmpty)
                throw new PacketException("Packet is empty");

            byte[] PacketData;
            int Length = VarInt.GetLength(ID) + Count, offset;
            if (compress > 0 && _size >= compress)
            {
                /*
                 * 压缩的数据包:
                 *
                 * 名称       类型    备注
                 * 数据包长度 Varint  等于下方两者解压前的总字节数
                 * 解压后长度 Varint  若为0则表示本包未被压缩
                 * 被压缩的数据       经Zlib压缩.开头是一个VarInt字段,代表数据包ID,然后是数据包数据.
                 */
                
                //拼接PacketID(VarInt)和PacketData(ByteArray)并塞入ZlibUtils.Compress去压缩
                byte[] uncompressed = new byte[Length];
                AsSpan().CopyTo(uncompressed.AsSpan().Slice(VarInt.WriteTo(ID, uncompressed)));
                //Array.Copy(_data, 0, uncompressed, VarInt.WriteTo(ID, uncompressed), uncompressed.Length);
                byte[] compressed = ZlibUtils.Compress(uncompressed);

                PacketData = new byte[VarInt.GetLength(VarInt.GetLength(uncompressed.Length) + compressed.Length) + VarInt.GetLength(uncompressed.Length) + compressed.Length];
                //写入第一个VarInt(解压长度+压缩后的长度）
                offset = VarInt.WriteTo(VarInt.GetLength(uncompressed.Length) + compressed.Length, PacketData);
                //写入第二个VarInt(未压缩长度)
                offset += VarInt.WriteTo(uncompressed.Length, PacketData.AsSpan().Slice(offset));
                //写入被压缩的数据
                compressed.CopyTo(PacketData, offset);
                return PacketData;
            }
            else
            {
                if (compress > 0)
                {
                    //写入数据包长度和解压后的长度(0=未压缩)
                    PacketData = new byte[VarInt.GetLength(Length+1) + Length+1];
                    offset = VarInt.WriteTo(Length+1, PacketData);
                        PacketData[offset++] = 0;
                }
                else
                {
                    //写入数据包长度
                    PacketData = new byte[VarInt.GetLength(Length) + Length];
                    offset = VarInt.WriteTo(Length, PacketData);
                }
                //写入ID和Data
                offset += VarInt.WriteTo(ID, PacketData.AsSpan().Slice(offset));
                if (_size > 0)
                    Array.Copy(_data, 0, PacketData, offset, PacketData.Length - offset);
                return PacketData;
            }
        }
        public static Packet Depack(ReadOnlySpan<byte> data,int compress = -1)
        {
            if (compress > 0)
            {
                int size = VarInt.Read(data, out int SizeOffset);
                data = data.Slice(SizeOffset);
                if (size != 0) //如果是0的话就代表这个数据包没有被压缩
                    data = ZlibUtils.Decompress(data.ToArray(), size);
            }
            return new Packet(VarInt.Read(data, out int IdOffset), data.Slice(IdOffset));
        }


        public virtual Packet WriteBoolean(bool boolean)
        {
            if (boolean)
                WriteUnsignedByte(0x01);
            else
                WriteUnsignedByte(0x00);
            return this;
        }
        public virtual Packet WriteByte(sbyte value)
        {
            TryAllocateCapacity(sizeof(sbyte));
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual Packet WriteUnsignedByte(byte value)
        {
            TryAllocateCapacity(sizeof(byte));
            _data[_size++] = value;
            return this;
        }
        public virtual Packet WriteString(string value)
        {
            byte[] str = Encoding.UTF8.GetBytes(value);
            TryAllocateCapacity(VarInt.GetLength(str.Length) + str.Length);
            WriteVarInt(str.Length);
            WriteBytes(str);
            return this;
        }
        public virtual Packet WriteShort(short value)
        {
            TryAllocateCapacity(2);
            _data[_size++] = (byte)(value >> 8);
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual Packet WriteUnsignedShort(ushort value)
        {
            TryAllocateCapacity(2);
            _data[_size++] = (byte)(value >> 8);
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual Packet WriteInt(int value)
        {
            TryAllocateCapacity(4);
            _data[_size++] = (byte)(value >> 24);
            _data[_size++] = (byte)(value >> 16);
            _data[_size++] = (byte)(value >> 8);
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual Packet WriteLong(long value)
        {
            TryAllocateCapacity(8);
            _data[_size++] = (byte)(value >> 54);
            _data[_size++] = (byte)(value >> 48);
            _data[_size++] = (byte)(value >> 40);
            _data[_size++] = (byte)(value >> 32);
            _data[_size++] = (byte)(value >> 24);
            _data[_size++] = (byte)(value >> 16);
            _data[_size++] = (byte)(value >> 8);
            _data[_size++] = (byte)value;
            return this;
        }
        public virtual Packet WriteFloat(float value)
        {
            TryAllocateCapacity(sizeof(float));
            BitConverter.GetBytes(value).CopyTo(_data,_size);
            _data.AsSpan().Slice(_size, sizeof(float)).Reverse();
            _size+=sizeof(float);
            return this;
        }
        public virtual Packet WriteDouble(double value)
        {
            TryAllocateCapacity(sizeof(double));
            BitConverter.GetBytes(value).CopyTo(_data, _size);
            _data.AsSpan().Slice(_size, sizeof(double)).Reverse();
            _size += sizeof(double);
            return this;
        }
        public virtual Packet WriteVarShort(int value)
        {
            WriteBytes(VarShort.GetBytes(value));
            return this;
        }
        public virtual Packet WriteVarInt(int value)
        {
            WriteBytes(VarInt.GetBytes(value));
            return this;
        }
        public virtual Packet WriteVarLong(long value)
        {
            WriteBytes(VarLong.GetBytes(value));
            return this;
        }
        public virtual Packet WriteUUID(UUID value)
        {
            TryAllocateCapacity(sizeof(long)*2);
            WriteLong(value.Most);
            WriteLong(value.Least);
            return this;
        }
        public virtual Packet WriteBytes(params ICollection<byte>[] collections)
        {
            int length = 0;
            foreach (var conllection in collections)
                length += conllection.Count;
            TryAllocateCapacity(length);

            foreach (var collection in collections)
            {
                collection.CopyTo(_data, _size);
                _size += collection.Count;
            }
            return this;
        }
        public virtual Packet WriteBytes(params byte[] value)
        {
            TryAllocateCapacity(value.Length);
            Array.Copy(value, 0, _data, _size, value.Length);
            _size += value.Length;
            return this;
        }

        public virtual Packet WriteBytes(ReadOnlySpan<byte> value)
        {
            TryAllocateCapacity(value.Length);
            value.CopyTo(_data.AsSpan(_size));
            _size += value.Length;
            return this;
        }
        public virtual Packet WriteByteArray(byte[] array, int protocolVersion)
        {
            //14w21a: All byte arrays have VarInt length prefixes instead of short
            if (protocolVersion >= ProtocolVersionNumbers.V14w21a)
                WriteVarInt(array.Length);
            else
                WriteShort((short)array.Length);
            WriteBytes(array);
            return this;
        }

        private bool ExtraAllocate = IntPtr.Size > 4;
        protected virtual void TryAllocateCapacity(int writeLength)
        {
            //这样子是不是有点浪费内存?
            if (writeLength + _size > Capacity)
                Capacity += ExtraAllocate && writeLength < 128 ? writeLength * 2 : writeLength;
        }


        public static implicit operator ReadOnlyPacket(Packet packet) => packet.AsReadOnly();
        public virtual ReadOnlyPacket AsReadOnly() => new ReadOnlyPacket(this);

        public Span<byte> AsSpan() => _data.AsSpan(0, _size);
        public Packet Clone() => new Packet(ID, AsSpan());

        public override string ToString()
        {
            return $"ID: {ID}, Count: {Count}";
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
            if (packet is null) return false;
            if (ID != packet.ID) return false;
            if (Count != packet.Count) return false;
            if (ReferenceEquals(this, packet)) return true;
            for (int i = 0; i < packet.Count; i++)
            {
                if (this[i] != packet[i])
                    return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            HashCode code = new HashCode();
            code.Add(ID);
            if (_size > 0)
                for (int i = 0; i < _size; i++)
                    code.Add(_data[i]);

            return code.ToHashCode();
        }

        public void CopyTo(byte[] array, int arrayIndex)
        {
            if (arrayIndex == 0)
                _data.AsSpan().Slice(0, _size).CopyTo(array);
            else
                _data.AsSpan().Slice(0, _size).CopyTo(array.AsSpan().Slice(arrayIndex));
        }

        byte[] IPacket.ToArray()
        {
            if (_size > 0)
                return AsSpan().ToArray();
            else
                return new byte[0];
        }

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            if (_size <= 0)
                yield break;

            for (int i = 0; i < _size; i++)
            {
                yield return _data[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_size <= 0)
                yield break;
            
            for (int i = 0; i < _size; i++)
            {
                yield return _data[i];
            }
        }
    }
}
