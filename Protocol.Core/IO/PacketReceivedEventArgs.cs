using System;
using MinecraftProtocol.Compression;
using MinecraftProtocol.IO.Pools;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Utils;

namespace MinecraftProtocol.IO
{
    public class PacketReceivedEventArgs : CancelEventArgs, IDisposable
    {
        /// <summary>
        /// 数据被完整的接收到的时间
        /// </summary>
        public DateTime ReceivedTime { get; private set; }

        /// <summary>
        /// 接收到的Packet
        /// </summary>
        public LazyCompatiblePacket Packet { get; private set; }
        
        /// <summary>
        /// 完整的数据包
        /// </summary>
        public Memory<Memory<byte>> RawData;

        private bool _usePool;
        private bool _disposed = false;

        private Memory<byte>[] _dataBlock;
        private byte[][] _bufferBlock;
        private ushort _bufferBlockLength;

        private static IPool<CompatiblePacket> _CPPool = new CompatiblePacketPool(true);
        internal PacketReceivedEventArgs Setup(Memory<byte>[] dataBlock, ushort dataBlockLength, byte[][] bufferBlock, ushort bufferBlockLength, int packetDataStartBlockIndex, int packetDataStartIndex, int packetLength, PacketListener listener)
        {
            Packet = new PREAPacket(dataBlock, dataBlockLength, packetDataStartBlockIndex, packetDataStartIndex, packetLength, listener);
            RawData = new Memory<Memory<byte>>(dataBlock, 0, dataBlockLength);
            ReceivedTime = DateTime.Now;
            _usePool = listener._usePool;
            _disposed = false;
            _isCancelled = false;
            _dataBlock = dataBlock;
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
            bool disposed = _disposed;
            if (disposed)
                return;
            else
                _disposed = true;

            try
            {
                if (_usePool)
                {
                    LazyCompatiblePacket packet = Packet;
                    Packet = null; RawData = null;
                    if (packet.IsCreated)
                        _CPPool.Return(packet.Get()); 
                    
                    for (int i = 0; i < _bufferBlockLength; i++)
                        NetworkListener._bufferPool.Return(_bufferBlock[i]);
                    
                    //虽然一般buffer会从上面返回数组池，但有极少部分不会回去，因此为了防止那极少数的buffer被block长期占着导致无法被GC回收，所以这边需要每次都清空block
                    PacketListener._dataBlockPool.Return(_dataBlock, true);
                    PacketListener._bufferBlockPool.Return(_bufferBlock, true);
                    _bufferBlock = null; _dataBlock = null; 
                    PacketListener.PREAPool.Return(this); //这个必须在最后，只有当所有资源回收完成才能送回池内，否则在极端情况下会存在线程安全问题
                }
            }
#if !DEBUG //防止因为非数组池的数组返回池内导致的异常
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
            private Memory<byte>[] _dataBlock;
            private ushort _dataBlockLength;
            private int _packetLength;
            private int _blockX, _blockY;            
            private int _idOffset;
            private PacketListener _listener;

            public PREAPacket(Memory<byte>[] dataBlock, ushort dataBlockLength, int packetDataStartBlockIndex, int packetDataStartIndex, int packetLength, PacketListener listener)
            {
                _dataBlock = dataBlock;
                _dataBlockLength = dataBlockLength;
                _packetLength = packetLength;
                _listener = listener;
                _blockY = packetDataStartBlockIndex;
                _blockX = packetDataStartIndex;

                _protocolVersion = _listener.ProtocolVersion;
                _compressionThreshold = _listener.CompressionThreshold;

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
                        return;
                    }
                }
                _id = VarInt.Read(ReadByte, out _idOffset);
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
                CompatiblePacket packet = null;
                if (_listener._usePool)
                {
                    packet = _CPPool.Rent();
                    packet.ProtocolVersion = ProtocolVersion;
                    packet.CompressionThreshold = CompressionThreshold;
                }
                else
                {
                    packet = new CompatiblePacket(-1, ProtocolVersion, CompressionThreshold);
                }
                return packet;
            }

            void CheckBounds()
            {
                if (_blockX >= _dataBlock[_blockY].Length)
                {
                    _blockY++;
                    _blockX = 0;
                }
            }
            void SkipByte()
            {
                CheckBounds();
                _blockX++;
            }
            byte ReadByte()
            {
                CheckBounds();
                return _dataBlock[_blockY].Span[_blockX++];
            }
        }
    }
}
