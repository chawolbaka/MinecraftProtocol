using System;
using System.Threading.Tasks;
using MinecraftProtocol.IO;
using MinecraftProtocol.Compression;


namespace MinecraftProtocol.Packets
{
    public class Packet : ByteWriter, IPacket, IEquatable<Packet>
    {
        public virtual bool IsEmpty => ID < 0 || _data is null;

        public virtual int ID { get; set; }

        public virtual bool IsReadOnly => false;

        public Packet() : this(-1) { }
        public Packet(int packetID) : this(packetID, DEFUALT_CAPACITY) { }
        public Packet(int packetID, int capacity)
        {
            ID = packetID;
            _data = _dataPool.Rent(capacity);
        }
        public Packet(int packetID, ReadOnlySpan<byte> packetData)
        {
            ID = packetID;
            if (packetData.Length > 0)
            {
                _size = packetData.Length;
                _data = _dataPool.Rent(packetData.Length);
                packetData.CopyTo(_data.AsSpan().Slice(0,_size));
            }
            else
            {
                _size = 0;
                _data = _dataPool.Rent(DEFUALT_CAPACITY);
            }
        }
        public Packet(IPacket packet) : this(packet.ID, packet.ToArray()) { }

        /// <summary>
        /// 生成发送给服务端的包
        /// </summary>
        /// <param name="compress">数据包压缩的阚值</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="IndexOutOfRangeException"/>       
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="PacketException"/>
        public virtual byte[] Pack(int compress = -1)
        {
            if (IsEmpty)
                throw new PacketException("Packet is empty");
            ThrowIfDisposed();

            byte[] PackedData;
            int uncompressLength = VarInt.GetLength(ID) + Count;
            int offset;
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
                byte[] uncompressed = _dataPool.Rent(uncompressLength);
                Array.Copy(_data, 0, uncompressed, VarInt.WriteTo(ID, uncompressed),_size);
                byte[] compressed = ZlibUtils.Compress(uncompressed, 0, uncompressLength);
                _dataPool.Return(uncompressed);

                PackedData = new byte[VarInt.GetLength(VarInt.GetLength(uncompressLength) + compressed.Length) + VarInt.GetLength(uncompressLength) + compressed.Length];
                //写入第一个VarInt(解压长度+压缩后的长度）
                offset = VarInt.WriteTo(VarInt.GetLength(uncompressLength) + compressed.Length, PackedData);
                //写入第二个VarInt(未压缩长度)
                offset += VarInt.WriteTo(uncompressLength, PackedData.AsSpan().Slice(offset));
                //写入被压缩的数据
                compressed.CopyTo(PackedData, offset);
                return PackedData;
            }
            else
            {
                if (compress > 0)
                {
                    //写入数据包长度和解压后的长度(0=未压缩)
                    PackedData = new byte[VarInt.GetLength(uncompressLength + 1) + uncompressLength + 1];
                    offset = VarInt.WriteTo(uncompressLength + 1, PackedData);
                    PackedData[offset++] = 0;
                }
                else
                {
                    //写入数据包长度
                    PackedData = new byte[VarInt.GetLength(uncompressLength) + uncompressLength];
                    offset = VarInt.WriteTo(uncompressLength, PackedData);
                }
                //写入ID和Data
                offset += VarInt.WriteTo(ID, PackedData.AsSpan().Slice(offset));
                if (_size > 0)
                    Array.Copy(_data, 0, PackedData, offset, PackedData.Length - offset);
                return PackedData;
            }
        }
        public static Packet Depack(ReadOnlySpan<byte> data,int compress = -1)
        {
            if (compress > 0)
            {
                int size = VarInt.Read(data, out int SizeOffset);
                data = data.Slice(SizeOffset);
                if (size != 0) //如果是0的话就代表这个数据包没有被压缩
                    data = ZlibUtils.Decompress(data.ToArray(),0, size);
            }
            return new Packet(VarInt.Read(data, out int IdOffset), data.Slice(IdOffset));
        }

        public static async Task<Packet> DepackAsync(ReadOnlyMemory<byte> data, int compress = -1)
        {
            if (compress > 0)
            {
                int size = VarInt.Read(data.Span, out int SizeOffset);
                data = data.Slice(SizeOffset);
                if (size != 0) //如果是0的话就代表这个数据包没有被压缩
                    data = await ZlibUtils.DecompressAsync(data, size);
            }
            return new Packet(VarInt.Read(data.Span, out int IdOffset), data.Span.Slice(IdOffset));
        }


        
        /// <summary>
        /// 浅拷贝一个受保护的只读Packet
        /// </summary>
        public virtual ReadOnlyPacket AsReadOnly() => ThrowIfDisposed(new ReadOnlyPacket(this));
        public static implicit operator ReadOnlyPacket(Packet packet) => packet.AsReadOnly();

        public virtual Packet Clone() => ThrowIfDisposed(new Packet(ID, AsSpan()));
        object ICloneable.Clone() => ThrowIfDisposed(Clone());

        public override string ToString()
        {
            ThrowIfDisposed();
            return $"ID: {ID}, Count: {Count}";
        }

        public override bool Equals(object obj)
        {
            ThrowIfDisposed();
            if (obj is Packet packet)
                return Equals(packet);
            else
                return false;
        }

        public virtual bool Equals(Packet packet)
        {
            ThrowIfDisposed();
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
            ThrowIfDisposed();
            HashCode code = new HashCode();
            code.Add(ID);
            if (_size > 0)
                for (int i = 0; i < _size; i++)
                    code.Add(_data[i]);

            return code.ToHashCode();
        }

        public void CopyTo(byte[] array, int arrayIndex)
        {
            ThrowIfDisposed();
            if (arrayIndex == 0)
                _data.AsSpan().Slice(0, _size).CopyTo(array);
            else
                _data.AsSpan().Slice(0, _size).CopyTo(array.AsSpan().Slice(arrayIndex));
        }

        byte[] IPacket.ToArray()
        {
            ThrowIfDisposed();
            if (_size > 0)
                return AsSpan().ToArray();
            else
                return new byte[0];
        }

    }
}
