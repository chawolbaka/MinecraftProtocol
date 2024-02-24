using System;
using System.Threading.Tasks;
using MinecraftProtocol.IO;
using MinecraftProtocol.Compression;
using System.Text;


namespace MinecraftProtocol.Packets
{
    public class Packet : ByteWriter, IPacket, IEquatable<Packet>
    {
        public virtual bool IsEmpty => Id < 0 || _data is null;

        public virtual bool IsReadOnly => false;

        public virtual int Id { get => _id; set { _version++; _id = value; } }

        private int _id;

        public Packet() : this(-1) { }
        public Packet(int packetId) : base(DEFUALT_CAPACITY)
        {
            Id = packetId;
        }
        internal Packet(int packetId, ref int start, ref int size, ref byte[] packetData) : base(ref start, ref size, ref packetData)
        {
            Id = packetId;
        }
        public Packet(int packetId, int start, int size, ref byte[] packetData) : base(ref start, ref size, ref packetData)
        {
            Id = packetId;
        }
        public Packet(int packetId, int capacity) : base(capacity)
        {
            Id = packetId;
        }

        //packetData如果传入null会变成空的Span所以不需要null检测
        //（顺便如果不写base那么还是会调用基类的空构造函数结果就是从池里取出一个默认长度的，但这边很有可能创建不一样的所以那个16的不处理就回不去池内了处理了也浪费性能）
        public Packet(int packetId, ReadOnlySpan<byte> packetData) : base(packetData.Length) 
        {
            Id = packetId;
            if (packetData.Length > 0)
            {
                _size = packetData.Length;
                packetData.CopyTo(AsSpan());
            }
        }

        internal Packet(ICompatiblePacket compatiblePacket)
        {
            Id = compatiblePacket.Id;
            if (compatiblePacket is null)
                throw new ArgumentNullException(nameof(compatiblePacket));
            if (compatiblePacket is CompatiblePacket cp)
                _data = cp._data;
            else if (compatiblePacket is ReadOnlyCompatiblePacket rcp)
                _data = rcp._cpacket._data;
            else
                _data = compatiblePacket.ToArray();
            _returnToPool = false;
            
            _size = compatiblePacket.Count;

        }
        public Packet(IPacket packet) : this(packet.Id, packet is ByteWriter bw ? bw.AsSpan() : packet.ToArray()) { }



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
            ThrowIfDisposed();

            if (IsEmpty)
                throw new PacketException("Packet is empty");


            byte[] packedData;
            int uncompressLength = VarInt.GetLength(Id) + _size;
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
                Array.Copy(_data, _start, uncompressed, VarInt.WriteTo(Id, uncompressed), _size);

                byte[] compressed = ZlibUtils.Compress(uncompressed.AsSpan(0, uncompressLength));
                _dataPool.Return(uncompressed);

                packedData = new byte[VarInt.GetLength(VarInt.GetLength(uncompressLength) + compressed.Length) + VarInt.GetLength(uncompressLength) + compressed.Length];
                //写入第一个VarInt(解压长度+压缩后的长度）
                offset = VarInt.WriteTo(VarInt.GetLength(uncompressLength) + compressed.Length, packedData);
                //写入第二个VarInt(未压缩长度)
                offset += VarInt.WriteTo(uncompressLength, packedData.AsSpan(offset));
                //写入被压缩的数据
                compressed.CopyTo(packedData, offset);
                
                return packedData;
            }
            else
            {
                if (compressionThreshold > 0)
                {
                    //写入数据包长度和解压后的长度(0=未压缩)
                    packedData = new byte[VarInt.GetLength(uncompressLength + 1) + uncompressLength + 1];
                    offset = VarInt.WriteTo(uncompressLength + 1, packedData);
                    packedData[offset++] = 0;
                }
                else
                {
                    //写入数据包长度
                    packedData = new byte[VarInt.GetLength(uncompressLength) + uncompressLength];
                    offset = VarInt.WriteTo(uncompressLength, packedData);
                }
                //写入ID和Data
                offset += VarInt.WriteTo(Id, packedData.AsSpan(offset));
                if (_size > 0)
                    Array.Copy(_data, _start, packedData, offset, packedData.Length - offset);
                return packedData;
            }
        }
        public virtual byte[] Pack() => Pack(-1);

        public static Packet Depack(ReadOnlySpan<byte> data) => Depack(data, -1);
        public static Packet Depack(ReadOnlySpan<byte> data, int compressionThreshold)
        {
            int IdOffset;
            if (compressionThreshold > 0)
            {
                int size = VarInt.Read(data, out int SizeOffset);
                data = data.Slice(SizeOffset);
                if (size != 0) //如果是0的话就代表这个数据包没有被压缩
                {
                    Packet packet = new Packet(-1, size);
                    packet.Capacity = size;
                    ZlibUtils.Decompress(data, packet._data);
                    packet.Id = VarInt.Read(packet._data, out IdOffset);
                    packet._start = IdOffset;
                    packet._size = size - IdOffset;
                    return packet;
                }
            }
            return new Packet(VarInt.Read(data, out IdOffset), data.Slice(IdOffset));
        }

        public static Task<Packet> DepackAsync(ReadOnlyMemory<byte> data) => DepackAsync(data, -1);
        public static async Task<Packet> DepackAsync(ReadOnlyMemory<byte> data, int compressionThreshold)
        {
            int IdOffset;
            if (compressionThreshold > 0)
            {
                int size = VarInt.Read(data.Span, out int SizeOffset);
                data = data.Slice(SizeOffset);
                if (size != 0) //如果是0的话就代表这个数据包没有被压缩
                {
                    Packet packet = new Packet(-1, size);
                    await ZlibUtils.DecompressAsync(data, packet._data);
                    packet.Id = VarInt.Read(packet._data, out IdOffset);
                    packet._start = IdOffset;
                    packet._size = size - IdOffset;
                    return packet;
                }
                    
            }
            return new Packet(VarInt.Read(data.Span, out IdOffset), data.Span.Slice(IdOffset));
        }
        
        /// <summary>
        /// 浅拷贝一个受保护的只读Packet
        /// </summary>
        public virtual IPacket AsReadOnly() => ThrowIfDisposed(new ReadOnlyPacket(this));

        /// <summary>
        /// 从一个<see cref="ICompatiblePacket"/>中取出信息后使用当前对象的data和id创建一个<see cref="CompatiblePacket"/>
        /// </summary>
        public virtual CompatiblePacket AsCompatible(ICompatiblePacket compatible) => new CompatiblePacket(this, compatible.ProtocolVersion, compatible.CompressionThreshold);
        public virtual CompatiblePacket AsCompatible(int protocolVersion, int compressionThreshold) => new CompatiblePacket(this, protocolVersion, compressionThreshold);

        public virtual ByteReader AsByteReader()
        {
            return new ByteReader(AsSpan());
        }

        public virtual Packet Clone() => ThrowIfDisposed(new Packet(Id, AsSpan()));

        object ICloneable.Clone() => Clone();

        public override bool Equals(object obj) => Equals(obj as Packet);
        
        public virtual bool Equals(Packet packet)
        {
            ThrowIfDisposed();
            if (packet is null || Id != packet.Id || _size != packet._size) 
                return false;

            return ReferenceEquals(this, packet) || ReferenceEquals(_data, packet._data) || MemoryExtensions.SequenceEqual(AsSpan(), packet.AsSpan());
        }

        public override string ToString()
        {
            ThrowIfDisposed();
            StringBuilder stringBuilder = new StringBuilder(Count * 3);
            byte[] temp = _data;
            stringBuilder.Append($"{_id:X2}: ");
            for (int i = 0; i < _size; i++)
            {
                stringBuilder.Append($"{temp[_start + i]:X2} ");
            }
            stringBuilder.Append($"({_size})");
            return stringBuilder.ToString();
        }

        public override int GetHashCode()
        {
            ThrowIfDisposed();
            HashCode code = new HashCode();
            code.Add(Id);
            if (_size > 0)
                code.AddBytes(AsSpan());
            
            return code.ToHashCode();
        }

        public void CopyTo(byte[] array, int arrayIndex)
        {
            ThrowIfDisposed();
            AsSpan().CopyTo(array.AsSpan(arrayIndex));
        }

        byte[] IPacket.ToArray()
        {
            ThrowIfDisposed();
            if (_size > 0)
                return AsSpan().ToArray();
            else
                return Array.Empty<byte>();
        }

    }
}
