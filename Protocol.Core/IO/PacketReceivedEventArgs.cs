using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using MinecraftProtocol.Compression;
using MinecraftProtocol.IO.Pools;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Utils;

namespace MinecraftProtocol.IO
{
    public class PacketReceivedEventArgs : CancelEventArgs, IDisposable
    {
        private static IPool<CompatiblePacket> CompatiblePacketPool = new CompatiblePacketPool(true);
        private static IPool<PREAPacket> PREAPacketPool = new ObjectPool<PREAPacket>();

        /// <summary>
        /// 数据被完整的接收到的时间
        /// </summary>
        public DateTime ReceivedTime { get; private set; }

        /// <summary>
        /// 接收到的Packet
        /// </summary>
        public LazyCompatiblePacket Packet => _packet;

        /// <summary>
        /// 完整的数据包
        /// </summary>
        public ReadOnlySpan<Memory<byte>> RawData => new ReadOnlySpan<Memory<byte>>(_packet._dataBlock, 0, _packet._dataBlockLength);


        /// <summary>
        /// 数据包的原始长度
        /// </summary>
        public int PacketLength { get; private set; }

        private PREAPacket _packet;
        private byte[][] _bufferBlock;
        private byte _bufferBlockLength;
        
        private bool _usePool;
        private bool _disposed;
        private SpinLock _lock = new SpinLock();

        internal PacketReceivedEventArgs Setup(ref Memory<byte>[] dataBlock, ref byte dataBlockLength, ref byte[][] bufferBlock, ref byte bufferBlockLength, ref int packetDataStartBlockIndex, ref int packetDataStartIndex, ref int packetLength, PacketListener listener)
        {
            _packet = (listener._usePool ? PREAPacketPool.Rent() : new PREAPacket()).Setup(ref dataBlock, ref dataBlockLength, ref packetDataStartBlockIndex, ref packetDataStartIndex, ref packetLength, ref listener);
            
            ReceivedTime = DateTime.Now;
            PacketLength = VarInt.GetLength(PacketLength) + packetLength;
            _usePool = listener._usePool;
            _disposed = false;
            _isCancelled = false;
            _bufferBlock = bufferBlock;
            _bufferBlockLength = bufferBlockLength;
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
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                if (_disposed)
                    return;
                else
                    _disposed = true;

            }
            finally
            {
                if (lockTaken)
                    _lock.Exit();
            }
            
            try
            {
                if (_usePool)
                {
                    PREAPacket packet = _packet;
                    _packet = null;
                    if (packet.IsCreated)
                        CompatiblePacketPool.Return(packet.Get());
                   

                    for (int i = 0; i < _bufferBlockLength; i++)
                        NetworkListener._bufferPool.Return(_bufferBlock[i]);
                    
                    //虽然一般buffer会从上面返回数组池，但有极少部分不会回去，因此为了防止那极少数的buffer被block长期占着导致无法被GC回收，所以这边需要每次都清空block
                    PacketListener._dataBlockPool.Return(packet._dataBlock, true);
                    PacketListener._bufferBlockPool.Return(_bufferBlock, true);
                    _bufferBlock = null; packet.Reset();


                    PREAPacketPool.Return(packet);
                    PacketListener.PREAPool.Return(this); //这个必须在最后，只有当所有资源回收完成才能送回池内，否则在极端情况下会存在线程安全问题
                }
            }
#if DEBUG //防止因为非数组池的数组返回池内导致的异常(DEBUG模式下还是需要显示的)
            catch (ArgumentException)
            {
                
            }
#endif
            finally
            {
                if (disposing)
                    GC.SuppressFinalize(this);
            }
        }

        ~PacketReceivedEventArgs()
        {
            Dispose(false);
        }

        private class PREAPacket : LazyCompatiblePacket
        {
            internal Memory<byte>[] _dataBlock;
            internal byte _dataBlockLength;
            private int _packetLength;
            private int _blockX, _blockY;
            private int _idOffset;
            private bool _usePool;

            public PREAPacket Setup(ref Memory<byte>[] dataBlock, ref byte dataBlockLength, ref int packetDataStartBlockIndex, ref int packetDataStartIndex, ref int packetLength, ref PacketListener listener)
            {
                _dataBlock = dataBlock;
                _dataBlockLength = dataBlockLength;
                _packetLength = packetLength;
                _blockY = packetDataStartBlockIndex;
                _blockX = packetDataStartIndex;

                _usePool = listener._usePool;
                _protocolVersion = listener.ProtocolVersion;
                _compressionThreshold = listener.CompressionThreshold;

                if (_compressionThreshold > 0)
                {
                    int size = VarInt.Read(ReadByte, out int sizeCount);
                    CheckBounds();
                    if (size > 0)
                    {
                        //如果数据包是被压缩的那么就立刻序列化，因为我暂时还无法在不完整复制的情况下解压出开头的几个字节
                        CompatiblePacket packet = CreatePacket();
                        byte[] buffer = new byte[_packetLength - sizeCount];
                        int count = 0;
                        for (; _blockY < _dataBlockLength; _blockY++)
                        {
                            if (_blockX > 0)
                            {
                                _dataBlock[_blockY].Span.Slice(_blockX).CopyTo(buffer);
                                count -= _blockX;
                                _blockX = 0;
                            }
                            else
                            {
                                _dataBlock[_blockY].Span.CopyTo(buffer.AsSpan(count));
                            }
                            count += _dataBlock[_blockY].Length;
                        }

                        packet.Capacity = size;
                        ZlibUtils.Decompress(buffer, packet._data.AsSpan(0, size));
                        packet.Id = VarInt.Read(packet._data, out _idOffset);
                        packet._start = _idOffset;
                        packet._size = size - _idOffset;
                        _packet = packet;
                        _isCreated = true;
                        return this;
                    }
                }
                _id = VarInt.Read(ReadByte, out _idOffset);
                return this;
            }

            public void Reset()
            {
                bool lockTaken = false;
                try
                {
                    _getLock.Enter(ref lockTaken);
                    _isCreated = false;
                    _dataBlock = null;
                    _packet = null;
                }
                finally
                {
                    if (lockTaken)
                        _getLock.Exit();
                }
#if DEBUG
                _getLock = new SpinLock(Debugger.IsAttached);
#else
                _getLock = new SpinLock(false);
#endif
            }

            protected override CompatiblePacket InitializePacket()
            {
                CompatiblePacket packet = CreatePacket();
                packet.Id = _id;
                packet.Capacity = _packetLength - _idOffset;
                CheckBounds();
                for (int i = _blockY; i < _dataBlockLength - _blockY; i++)
                {
                    packet.WriteBytes(_dataBlock[i].Span.Slice(_blockX));
                    if (_blockX != 0)
                        _blockX = 0;
                }
                return packet;
            }

            private CompatiblePacket CreatePacket()
            {
                CompatiblePacket packet;
                if (_usePool)
                {
                    packet = CompatiblePacketPool.Rent();
                    packet.ProtocolVersion = _protocolVersion;
                    packet.CompressionThreshold = _compressionThreshold;
                }
                else
                {
                    packet = new CompatiblePacket(-1, _protocolVersion, _compressionThreshold);
                }
                return packet;
            }

            byte ReadByte()
            {
                CheckBounds();
                return _dataBlock[_blockY].Span[_blockX++];
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void CheckBounds()
            {
                if (_blockX >= _dataBlock[_blockY].Length)
                {
                    _blockY++;
                    _blockX = 0;
                }
            }

        }
    }
}
