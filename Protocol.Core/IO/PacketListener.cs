using System;
using System.Buffers;
using System.IO;
using System.Net;
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
        public int ProtocolVersion { get; set; }

        public int CompressionThreshold { get; set; }

        public CryptoHandler CryptoHandler { get; init; }

        public event CommonEventHandler<object, PacketReceivedEventArgs> PacketReceived;
        
        internal static IPool<PacketReceivedEventArgs> PREAPool = new ObjectPool<PacketReceivedEventArgs>();
        internal static ArrayPool<Memory<byte>> _dataBlockPool = new SawtoothArrayPool<Memory<byte>>(1024 * 8, 2048);
        internal static ArrayPool<byte[]> _bufferBlockPool = new SawtoothArrayPool<byte[]>(1024 * 8, 512);
        
        private Memory<byte>[] _dataBlock;
        private byte[][] _bufferBlock;
        private ushort _dataBlockIndex, _bufferBlockIndex;

        private int _packetLength;
        private int _packetLengthOffset;
        private int _packetLengthCount;
        private int _packetDataBlockIndex;
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
            CompressionThreshold = -1;
            ProtocolVersion = -1;
            CryptoHandler ??= new CryptoHandler();
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
            _bufferBlockIndex = 0;
            _dataBlock = _usePool ? _dataBlockPool.Rent(DEFUALT_SIZE) : new Memory<byte>[DEFUALT_SIZE];
            _bufferBlock = _usePool ? _bufferBlockPool.Rent(DEFUALT_SIZE) : new byte[DEFUALT_SIZE][];
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
            if (_bufferBlockIndex + 1 > _bufferBlock.Length)
            {
                byte[][] newBufferBlock = _usePool ? _bufferBlockPool.Rent(_dataBlock.Length * 2) : new byte[_dataBlock.Length * 2][];
                byte[][] oldBufferBlock = _bufferBlock;
                oldBufferBlock.CopyTo(newBufferBlock.AsSpan());
                _bufferBlock = newBufferBlock;
                _bufferBlockPool.Return(oldBufferBlock, true);
            }

            _bufferBlock[_bufferBlockIndex++] = _buffer;
            _buffer = null;
        }

        protected override void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            int bytesTransferred = e.BytesTransferred;
            //如果是加密数据就先将buffer解密再继续处理
            if (!_disposed && !_internalToken.IsCancellationRequested && CryptoHandler.Enable)
                CryptoHandler.Decrypt(_buffer.AsSpan(0, bytesTransferred));

            while (!_disposed && !_internalToken.IsCancellationRequested)
            {
   
                //读取Packet的长度
                if (_state == ReadState.PacketLength)
                {
                    for (; _packetLengthCount < 5;)
                    {
                        //如果当前buffer不足以读取一个完整的varint就等待缓存区刷新后继续向_readLength写入
                        if (_disposed)
                            return;

                        byte b = _buffer[_bufferOffset++];
                        _packetLength |= (b & 0b0111_1111) << _packetLengthCount++ * 7;
                        _packetLengthOffset++;
                        if ((b & 0b1000_0000) == 0) //varint结束符
                            _state = ReadState.PacketData; //Packet长度读取完成，开始读取数据部分

                        if (_bufferOffset >= bytesTransferred)
                        {
                            if (_packetLengthOffset > 0)
                            {
                                AddData(new Memory<byte>(_buffer, _bufferOffset - _packetLengthOffset, _packetLengthOffset));
                                
                                TransferBuffer();
                                _packetLengthOffset = 0;
                                _packetDataBlockIndex = _dataBlockIndex;
                            }
                            ReceiveNextBuffer(e);
                            return;
                        }

                        if (_state == ReadState.PacketData)
                        {
                            _packetDataBlockIndex = _dataBlockIndex;
                            break;
                        }
                            
                    }
                    if (_packetLengthCount > 5)
                        throw new OverflowException("varint too big");
                }
                if (_disposed)
                    return;

                if (bytesTransferred - _bufferOffset >= _packetLength - _packetDataOffset)
                {
                    int start = _packetDataOffset > 0 ? _bufferOffset : _bufferOffset - _packetLengthOffset;
                    int length = _packetDataOffset > 0 ? _packetLength - _packetDataOffset : _packetLengthOffset + _packetLength;
                    if (start < 0)
                        throw new OverflowException();



                    AddData(new Memory<byte>(_buffer, start, length));
                    _bufferOffset += _packetLength - _packetDataOffset;
                    _packetDataOffset = 0;

                    if (_bufferOffset >= bytesTransferred)
                    {
                        TransferBuffer();
                        InvokeReceived();
                        ReceiveNextBuffer(e);
                        return;
                    }
                    else
                    {
                        InvokeReceived();
                        continue;
                    }
                }
                else
                {
                    if (_packetDataOffset == 0)
                    {
                        int start = _bufferOffset - _packetLengthOffset;
                        int length = bytesTransferred - start;
                        if (start < 0)
                            throw new OverflowException();

                        AddData(new Memory<byte>(_buffer, start, length));
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
            if (dataLength >= _packetLengthCount + _packetLength && _packetLength >= (CompressionThreshold > 0 ? 3 : 1))
            {
                try
                {
                    PacketReceivedEventArgs prea = _usePool ? PREAPool.Rent() : new PacketReceivedEventArgs();
                    prea.Setup(_dataBlock, _dataBlockIndex, _bufferBlock, _bufferBlockIndex, _packetDataBlockIndex, _packetLengthOffset, _packetLength, this);

                    EventUtils.InvokeCancelEvent(PacketReceived, this, prea);
                }
                catch (Exception ex)
                {
                    if (!InvokeUnhandledException(ex))
                        throw;
                }
            }

            _state = ReadState.PacketLength;
            _packetLengthOffset = 0;
            _packetLength = 0;
            _packetLengthCount = 0;
            if (_usePool)
                ResetBlock();
        }
    
    }
}
