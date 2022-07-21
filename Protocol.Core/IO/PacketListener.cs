using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using MinecraftProtocol.Compression;
using MinecraftProtocol.Crypto;
using MinecraftProtocol.IO.Pools;
using MinecraftProtocol.Packets;

namespace MinecraftProtocol.IO
{

    /// <summary>
    /// Minecraft数据包监听器
    /// <para>一直从传入的Socket读取Minecraft的数据包并通过<see cref="PacketReceived"/>事件吐出</para>
    /// </summary>
    public class PacketListener : NetworkListener, IPacketListener
    {
        public int CompressionThreshold
        {
            get => ThrowIfDisposed(_compressionThreshold);
            set => ThrowIfDisposed(() => _compressionThreshold = value);
        }

        public int ProtocolVersion
        {
            get => ThrowIfDisposed(_protocolVersion);
            set => ThrowIfDisposed(() => _protocolVersion = value);
        }
        

        public CryptoHandler Crypto => ThrowIfDisposed(_crypto);

        public event EventHandler<PacketReceivedEventArgs> PacketReceived;

        private static IPool<CompatiblePacket> _CPPool = new CompatiblePacketPool(true);
        private static IPool<PacketReceivedEventArgs> PREAPool = new ObjectPool<PacketReceivedEventArgs>();
        private CryptoHandler _crypto;
        private int _compressionThreshold;
        private int _protocolVersion;
        private int _packetLength;
        private byte _packetLengthOffset;
        private int _packetDataOffset;

        private byte[] _packetData;
        private ReadState _state;
        private enum ReadState
        {
            PacketLength,
            PacketData
        }


        public PacketListener(Socket socket) : this(socket, socket.ReceiveBufferSize) { }
        public PacketListener(Socket socket, int receiveBufferSize) : this(socket, receiveBufferSize, false) { }
        public PacketListener(Socket socket, int receiveBufferSize, bool disablePool) : base(socket, receiveBufferSize,disablePool)
        {
            _compressionThreshold = -1;
            _protocolVersion = -1;
            _crypto = new CryptoHandler();
        }

        public override void Start(CancellationToken token = default)
        {
            base.Start(token);
            _state = ReadState.PacketLength;
        }


        protected override void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            int bytesTransferred = e.BytesTransferred;
            //如果是加密数据就先将buffer解密再继续处理
            if (!_disposed && !_internalToken.IsCancellationRequested && _crypto.Enable)
                _crypto.Decrypt(_buffer, 0, bytesTransferred).AsSpan(0, bytesTransferred).CopyTo(_buffer);

            while (!_disposed && !_internalToken.IsCancellationRequested)
            {
                //读取Packet的长度
                if (_state == ReadState.PacketLength)
                {
                    if (_packetLengthOffset == 0)
                        _packetLength = 0;

                    for (; _packetLengthOffset < 5;)
                    {
                        //如果当前buffer不足以读取一个完整的varint就等待缓存区刷新后继续向_readLength写入
                        if (!_disposed && _bufferOffset + 1 > bytesTransferred)
                        {
                            ReceiveNextBuffer(e);
                            return;
                        }

                        byte b = _buffer[_bufferOffset++];
                        _packetLength |= (b & 0b0111_1111) << _packetLengthOffset++ * 7;
                        if ((b & 0b1000_0000) == 0) //varint结束符
                        {
                            
                            _packetData = AllocateByteArray(_packetLengthOffset + _packetLength);
                            if (bytesTransferred - _packetLengthOffset > 0)
                                Array.Copy(_buffer, _bufferOffset - _packetLengthOffset, _packetData, 0, _packetLengthOffset);
                            else
                                VarInt.WriteTo(_packetLength, _packetData);

                            //Packet长度读取完成，开始读取数据部分
                            _state = ReadState.PacketData;
                            break;
                        }
                    }
                    if (_packetLengthOffset > 5)
                        throw new OverflowException("varint too big");
                }

                //开始读取Packet的数据部分
                if (!_disposed && _packetDataOffset == 0 && bytesTransferred - _bufferOffset >= _packetLength)
                {
                    //缓存区的数据足够读取一个完整的包
                    Array.Copy(_buffer, _bufferOffset , _packetData, _packetLengthOffset, _packetLength);
                    InvokeReceived(_packetData, 0, ref _packetLengthOffset, ref _packetLength);
                    _bufferOffset += _packetLength;
                }
                else
                {
                    //包的大小超出缓冲区，先会执行到else把现在缓冲区的数据都复制到_packetData内
                    //然后在下次接收到数据时继续回到这边，如果缓冲内的足够读取一个包了就进入下面，否则继续进else把接收到的填充进_packetData已被填充的数据后面
                    //（上面的read == 0确保数据收到后能回到这边而不是被上面的读取导致炸掉）
                    if (!_disposed && _packetLength - _packetDataOffset < bytesTransferred - _bufferOffset)
                    {
                        Array.Copy(_buffer, _bufferOffset, _packetData, _packetDataOffset, _packetLength - _packetDataOffset);
                        InvokeReceived(_packetData, 0, ref _packetLengthOffset, ref _packetLength);
                        _bufferOffset += _packetLength - _packetDataOffset;
                        _packetDataOffset = 0;
                    }
                    else
                    {
                        Array.Copy(_buffer, _bufferOffset, _packetData, _packetDataOffset == 0 ? _packetLengthOffset : _packetDataOffset, bytesTransferred - _bufferOffset);
                        _packetDataOffset += bytesTransferred - _bufferOffset;
                        ReceiveNextBuffer(e);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 通过读取完成的数据包触发<see cref="PacketReceived"/>事件
        /// </summary>
        protected virtual void InvokeReceived(byte[] data, ushort startIndex, ref byte headLength, ref int bodyLength)
        {
            if (data.Length >= startIndex + headLength + bodyLength && bodyLength >= (_compressionThreshold > 0 ? 3 : 1)) //varint(size)+varint(decompressSize)+varint(id)+data 这是一个包最小的尺寸，不知道什么mod还是插件竟然会在玩家发送聊天消息后发个比这还小的东西过来...
                PacketReceived?.Invoke(this, (_usePool ? PREAPool.Rent() : new PacketReceivedEventArgs()).Setup(data, ref startIndex, ref headLength, ref bodyLength, ref _protocolVersion, _compressionThreshold, _usePool));
            else if (_usePool)
                _dataPool.Return(data);
            _state = ReadState.PacketLength;

            _packetLengthOffset = 0;
            _packetData = null;
        }

        protected override void Dispose(bool disposing)
        {
            bool disposed = _disposed;
            if (disposed)
                return;

            _disposed = true;

            try
            {
                if (_usePool && _packetData is not null)
                    _dataPool.Return(_packetData);
                if (_usePool && _buffer is not null)
                    _dataPool.Return(_buffer);
            }
            catch (ArgumentException) { }
            if (!disposed && disposing)
            {
                _packetData = null;
                _buffer = null;
                _socket = null;
                _crypto = null;
                GC.SuppressFinalize(this);
            }
        }

        public class PacketReceivedEventArgs : EventArgs, IDisposable
        {
            /// <summary>
            /// 数据被完整的接收到的时间
            /// </summary>
            public DateTime ReceivedTime { get; private set; }

            /// <summary>
            /// 接收到的Packet
            /// </summary>
            public CompatiblePacket Packet { get; private set; }

            /// <summary>
            /// 完整的数据包（不可直接进行Packet.Depack）
            /// </summary>
            public Memory<byte> RawData => new Memory<byte>(_data, _startIndex, _headLength + _bodyLength);

            private bool _disposed = false;
            private byte[] _data;
            private ushort _startIndex;
            private byte _headLength;
            private int _bodyLength;
            private bool _usePool;

            internal PacketReceivedEventArgs Setup(byte[] data, ref ushort startIndex, ref byte headLength, ref int bodyLength, ref int protocolVersion, int compressionThreshold, bool usePool)
            {
                //上面那堆ref是为了减少值类型的内存复制，赋值给全局变量时会复制所以最终并不是相同地址的
                ReceivedTime = DateTime.Now; _data = data; _startIndex = startIndex; _headLength = headLength; _bodyLength = bodyLength; _usePool = usePool;

                _disposed = false;
                if (usePool)
                {
                    Packet = _CPPool.Rent();
                    //以下代码复制自CompatiblePacket.Depack
                    Span<byte> buffer = data.AsSpan().Slice(startIndex + headLength, bodyLength);
                    if (compressionThreshold > 0)
                    {
                        int size = VarInt.Read(buffer, out int SizeOffset);
                        buffer = buffer.Slice(SizeOffset);
                        if (size != 0)
                            buffer = ZlibUtils.Decompress(buffer.ToArray(), 0, size);
                    }
                    Packet.ID = VarInt.Read(buffer, out int IdOffset);
                    Packet.Capacity = buffer.Length - IdOffset;
                    Packet.WriteBytes(buffer.Slice(IdOffset));
                    Packet.ProtocolVersion = protocolVersion;
                    Packet.CompressionThreshold = compressionThreshold;
                }
                else
                {
                    Packet = CompatiblePacket.Depack(RawData.Span, protocolVersion, compressionThreshold);
                }
                return this;
            }

            /// <summary>
            /// 如果未禁止对象池调用该实例和内部的对象都将返回池内，请不要滞留该实例和内部的任何成员。
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
            }
            private void Dispose(bool disposing)
            {
                bool disposed = _disposed;
                if (disposed)
                    return;
                else
                    _disposed = true;

                try
                {
                    if (_usePool && _data != null)
                    {
                        _CPPool.Return(Packet);
                        _dataPool.Return(_data);
                        _data = null;
                        PREAPool.Return(this);
                    }
                }
                catch (ArgumentException)
                {

                }
                finally
                {
                    if(disposing)
                        GC.SuppressFinalize(this);
                }
            }

            ~PacketReceivedEventArgs()
            {
                Dispose(false);
            }
        }
    }
}
