using System;
using System.Buffers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Crypto;
using MinecraftProtocol.IO.Pools;
using MinecraftProtocol.Utils;

namespace MinecraftProtocol.IO
{

    /// <summary>
    /// Minecraft数据包监听器
    /// <para>一直从传入的Socket读取Minecraft的数据包并通过<see cref="PacketReceived"/>事件吐出</para>
    /// </summary>
    public partial class PacketListener : NetworkListener, IPacketListener, ICompatible
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

        public CryptoHandler CryptoHandler 
        { 
            get => ThrowIfDisposed(_cryptoHandler);
            init => _cryptoHandler = value;
        }

        public event CommonEventHandler<object, PacketReceivedEventArgs> PacketReceived;
        
        internal static IPool<PacketReceivedEventArgs> PREAPool = new ObjectPool<PacketReceivedEventArgs>();
        internal static ArrayPool<Memory<byte>> _dataBlockPool = new SawtoothArrayPool<Memory<byte>>(1024 * 8, 2048);
        internal static ArrayPool<GCHandle> _gcHandleBlockPool = new SawtoothArrayPool<GCHandle>(1024 * 8, 512);
        
        private Memory<byte>[] _dataBlock;
        private GCHandle[] _gcHandleBlock;
        private ushort _dataBlockIndex, _gcHandleBlockIndex;

        private CryptoHandler _cryptoHandler;
        private int _compressionThreshold;
        private int _protocolVersion;
        private int _packetLength;
        private byte _packetLengthOffset;
        private int _packetDataOffset;

        private ReadState _state;
        private enum ReadState
        {
            PacketLength,
            PacketData
        }


        public PacketListener(Socket socket) : this(socket, false) { }
        public PacketListener(Socket socket, bool disablePool) : base(socket, disablePool)
        {
            _compressionThreshold = -1;
            _protocolVersion = -1;
            _cryptoHandler ??= new CryptoHandler();
        }

        public override void Start(CancellationToken token = default)
        {
            if (_internalToken != default)
                throw new InvalidOperationException("started");

            _state = ReadState.PacketLength;
            ResetBlock();
            base.Start(token); //必须在后面，因为内部直接就会读取数据包
        }


        private void ResetBlock()
        {
            const int DEFUALT_SIZE = 16;
            _dataBlockIndex = 0;
            _gcHandleBlockIndex = 0;
            _dataBlock = _usePool ? _dataBlockPool.Rent(DEFUALT_SIZE) : new Memory<byte>[DEFUALT_SIZE];
            _gcHandleBlock = _usePool ? _gcHandleBlockPool.Rent(DEFUALT_SIZE) : new GCHandle[DEFUALT_SIZE];
        }

        private void AddData(Memory<byte> data)
        {
            if (_dataBlockIndex + 1 > _dataBlock.Length)
            {
                Memory<byte>[] newDataBlock = _usePool ? _dataBlockPool.Rent(_dataBlock.Length * 2) : new Memory<byte>[_dataBlock.Length * 2];
                Memory<byte>[] oldDataBlock = _dataBlock;
                oldDataBlock.CopyTo(newDataBlock.AsSpan());
                _dataBlock = newDataBlock;
                _dataBlockPool.Return(oldDataBlock, true);
            }
            _dataBlock[_dataBlockIndex++] = data;
        }

        private void TransferBuffer()
        {
            if (_gcHandleBlockIndex + 1 > _gcHandleBlock.Length)
            {
                GCHandle[] newGCHandleBlock = _usePool ? _gcHandleBlockPool.Rent(_dataBlock.Length * 2) : new GCHandle[_dataBlock.Length * 2];
                GCHandle[] oldGCHandleBlock = _gcHandleBlock;
                oldGCHandleBlock.CopyTo(newGCHandleBlock.AsSpan());
                _gcHandleBlock = newGCHandleBlock;
                _gcHandleBlockPool.Return(oldGCHandleBlock, true);
            }

            _gcHandleBlock[_gcHandleBlockIndex++] = _bufferGCHandle;

        }


        protected override void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            int bytesTransferred = e.BytesTransferred;
            //如果是加密数据就先将buffer解密再继续处理
            if (!_disposed && !_internalToken.IsCancellationRequested && _cryptoHandler.Enable)
                _cryptoHandler.Decrypt(_buffer.AsSpan(0, bytesTransferred));

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
                        if (_disposed)
                            return;
                        else if (_bufferOffset + 1 > bytesTransferred)
                        {
                            if(_packetLengthOffset > 0)
                            {
                                AddData(new Memory<byte>(_buffer, _packetLengthOffset - _packetLengthOffset, _packetLengthOffset));
                                TransferBuffer();
                            }
                            ReceiveNextBuffer(e);
                            return;
                        }

                        byte b = _buffer[_bufferOffset++];
                        _packetLength |= (b & 0b0111_1111) << _packetLengthOffset++ * 7;
                        if ((b & 0b1000_0000) == 0) //varint结束符
                        {
                            _state = ReadState.PacketData; //Packet长度读取完成，开始读取数据部分
                            break;
                        }
                    }
                    if (_packetLengthOffset > 5)
                        throw new OverflowException("varint too big");
                }

                if(!_disposed && bytesTransferred - _bufferOffset >= _packetLength - _packetDataOffset)
                {
                    int start = _bufferOffset - _packetLengthOffset;
                    if (start < 0)
                        start = 0;
                    if (_dataBlockIndex > 0)
                        AddData(new Memory<byte>(_buffer, _bufferOffset, _packetLength - _packetDataOffset));
                    else
                        AddData(new Memory<byte>(_buffer, start, _packetLengthOffset + _packetLength - _packetDataOffset));
                    _bufferOffset += _packetLength - _packetDataOffset;
                    _packetDataOffset = 0;

                    if (_bufferOffset + 1 > bytesTransferred)
                        TransferBuffer();
                    InvokeReceived();
                }
                else
                {
                    if (_packetDataOffset == 0 && _dataBlockIndex == 0)
                    {
                        int start = _bufferOffset - _packetLengthOffset;
                        if (_packetDataOffset == 0 && start < 0)
                            start = 0;
                        AddData(new Memory<byte>(_buffer, start, bytesTransferred - start));
                    }
                    else
                    {
                        AddData(new Memory<byte>(_buffer, _bufferOffset, bytesTransferred - _bufferOffset));
                    }
                    _packetDataOffset += bytesTransferred - _bufferOffset;
                    TransferBuffer(); //因为剩下的都是这个数据包的了所以当然归他来回收当前的buffer
                    ReceiveNextBuffer(e);
                    return;
                }
            }
        }

        /// <summary>
        /// 通过读取完成的数据包触发<see cref="PacketReceived"/>事件
        /// </summary>
        protected virtual void InvokeReceived()
        {
            int dataLength = 0;
            for (int i = 0; i < _dataBlockIndex; i++)
            {
                dataLength += _dataBlock[i].Length;
            }


            //varint(size)+varint(decompressSize)+varint(id)+data 这是一个包最小的尺寸，不知道什么mod还是插件竟然会在玩家发送聊天消息后发个比这还小的东西过来...
            if (dataLength >= _packetLengthOffset + _packetLength && _packetLength >= (_compressionThreshold > 0 ? 3 : 1))
            {
                try
                {
                    PacketReceivedEventArgs prea = _usePool ? PREAPool.Rent() : new PacketReceivedEventArgs();
                    prea.Setup(_gcHandleBlock, ref _gcHandleBlockIndex, ref _dataBlock, ref _dataBlockIndex, ref _packetLengthOffset, ref _packetLength, ref _protocolVersion, _compressionThreshold, _usePool);

                    EventUtils.InvokeCancelEvent(PacketReceived, this, prea);
                }
                catch (Exception ex)
                {
                    if (!InvokeUnhandledException(ex))
                        throw;
                }
            }
            else if (_usePool)
            {
                for (int i = 0; i < _gcHandleBlockIndex; i++)
                {
                    if (_gcHandleBlock[i] == default)
                        break;

                    _dataPool.Return(_gcHandleBlock[i]);
                }
            }
                
            _state = ReadState.PacketLength;

            _packetLengthOffset = 0;
            if (_usePool)
                ResetBlock();
        }

        protected override void Dispose(bool disposing)
        {
            bool disposed = _disposed;
            if (disposed)
                return;

            _disposed = true;

            try
            {
                if (_usePool && _buffer is not null)
                    _dataPool.Return(_bufferGCHandle);
            }
            catch (ArgumentException) { }
            if (!disposed && disposing)
            {
                _buffer = null;
                _socket = null;
                _cryptoHandler = null;
                GC.SuppressFinalize(this);
            }
        }
    
    }
}
