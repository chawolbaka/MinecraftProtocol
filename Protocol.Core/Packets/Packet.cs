using System;
using System.Threading.Tasks;
using MinecraftProtocol.IO;
using MinecraftProtocol.Compression;
using System.Text;
using System.Runtime.InteropServices;

namespace MinecraftProtocol.Packets
{
    public class Packet : ByteWriter, IPacket, IEquatable<Packet>
    {

        public virtual bool IsEmpty => ID < 0 || _data is null;

        public virtual bool IsReadOnly => false;

        public virtual int ID { get => _id; set { _id = value; _version++; } }

        private int _id;

        public Packet() : this(-1) { }
        public Packet(int packetId) : this(packetId, DEFUALT_CAPACITY) { }
        public Packet(int packetId, ref int size, ref byte[] packetData) : base(ref size, ref packetData)
        {
            ID = packetId;
        }
        public Packet(int packetId, int capacity) : base(capacity)
        {
            ID = packetId;
            RerentData(capacity);
        }

        //packetData如果传入null会变成空的Span所以不需要null检测
        //（顺便如果不写base那么还是会调用基类的空构造函数结果就是从池里取出一个默认长度的，但这边很有可能创建不一样的所以那个16的不处理就回不去池内了处理了也浪费性能）
        public Packet(int packetId, ReadOnlySpan<byte> packetData) : base(packetData.Length) 
        {
            ID = packetId;
            if (packetData.Length > 0)
            {
                _size = packetData.Length;
                packetData.CopyTo(_data.AsSpan().Slice(0, _size));
            }
        }

        internal Packet(ICompatiblePacket compatiblePacket)
        {
            ID = compatiblePacket.ID;
            if (compatiblePacket is null)
                throw new ArgumentNullException(nameof(compatiblePacket));
            if (compatiblePacket is CompatiblePacket cp)
                _data = cp._data;
            else if (compatiblePacket is ReadOnlyCompatiblePacket rcp)
                _data = rcp._cpacket._data;
            else
                _data = compatiblePacket.ToArray();

        }
        public Packet(IPacket packet) : this(packet.ID, packet.ToArray()) { }

        /// <summary>
        /// 生成发送给服务端的包
        /// </summary>
        /// <param name="compressionThreshold">数据包压缩的阚值</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="IndexOutOfRangeException"/>       
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="PacketException"/>
        public virtual byte[] Pack(int compressionThreshold)
        {
            if (IsEmpty)
                throw new PacketException("Packet is empty");
            ThrowIfDisposed();

            byte[] PackedData;
            int uncompressLength = VarInt.GetLength(ID) + Count;
            int offset;
            if (compressionThreshold > 0 && _size >= compressionThreshold)
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
                if (compressionThreshold > 0)
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
        public virtual byte[] Pack()
        {
            return Pack(-1);
        }


        public static Packet Depack(ReadOnlySpan<byte> data) => Depack(data);
        public static Packet Depack(ReadOnlySpan<byte> data, int compressionThreshold)
        {
            if (compressionThreshold > 0)
            {
                int size = VarInt.Read(data, out int SizeOffset);
                data = data.Slice(SizeOffset);
                if (size != 0) //如果是0的话就代表这个数据包没有被压缩
                    data = ZlibUtils.Decompress(data.ToArray(),0, size);
            }
            return new Packet(VarInt.Read(data, out int IdOffset), data.Slice(IdOffset));
        }

        public static Task<Packet> DepackAsync(ReadOnlyMemory<byte> data) => DepackAsync(data, -1);
        public static async Task<Packet> DepackAsync(ReadOnlyMemory<byte> data, int compressionThreshold)
        {
            if (compressionThreshold > 0)
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

        /// <summary>
        /// 从一个CompatiblePacket中取出信息后并使用当前packet的data和id创建一个CompatiblePacket
        /// </summary>
        public virtual CompatiblePacket AsCompatible(ICompatiblePacket compatible) => new CompatiblePacket(this, compatible.ProtocolVersion, compatible.CompressionThreshold);
        public virtual CompatiblePacket AsCompatible(int protocolVersion, int compressionThreshold) => new CompatiblePacket(this, protocolVersion, compressionThreshold);
        public static implicit operator ReadOnlyPacket(Packet packet) => packet.AsReadOnly();

        public virtual Packet Clone() => ThrowIfDisposed(new Packet(ID, AsSpan()));
        object ICloneable.Clone() => ThrowIfDisposed(Clone());

        public override string ToString()
        {
            ThrowIfDisposed();
            StringBuilder stringBuilder = new StringBuilder(Count*3);
            byte[] temp = _data; 
            stringBuilder.Append($"{_id:X2}: ");
            for (int i = 0; i < _size; i++)
            {
                stringBuilder.Append($"{temp[i]:X2} ");
            }
            stringBuilder.Append($"({_size})");
            return stringBuilder.ToString();
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
            if (packet is null || ID != packet.ID || _size != packet._size) return false;
            if (ReferenceEquals(this, packet) || ReferenceEquals(this._data, packet._data)) return true;
            for (int i = 0; i < packet._size; i++)
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
            {
                for (int i = 0; i < _size; i++)
                {
                    code.Add(_data[i]);
                }
            }
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
