using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        public CompatiblePacket Packet { get; private set; }
        
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
            RawData = new Memory<Memory<byte>>(dataBlock, 0, dataBlockLength);
            ReceivedTime = DateTime.Now;

            CompatiblePacket _packet;
            _usePool = listener._usePool;
            _disposed = false;
            _isCancelled = false;
            _dataBlock = dataBlock;
            _bufferBlock = bufferBlock;
            _bufferBlockLength = bufferBlockLength;

            int blockIndex = packetDataStartBlockIndex, blockOffset = packetDataStartIndex, IdOffset;
            CheckBounds();
            if (_usePool)
            {
                _packet = _CPPool.Rent();
                _packet.ProtocolVersion = listener.ProtocolVersion;
                _packet.CompressionThreshold = listener.CompressionThreshold;
            }
            else
            {
                _packet = new CompatiblePacket(-1, listener.ProtocolVersion, listener.CompressionThreshold);
            }

            //如果要解压那么就先组合data块并解压，如果不需要解压那么就直接从data块中复制到Packet内避免多余的内存复制
            if (listener.CompressionThreshold > 0)
            {
                int size = VarInt.Read(ReadByte, out int sizeCount);
                if (size != 0)
                {
                    //组合data块
                    byte[] buffer = new byte[packetLength - sizeCount];
                    int count = 0;
                    for (; blockIndex < dataBlockLength; blockIndex++)
                    {
                        if (blockOffset > 0)
                        {
                            _dataBlock[blockIndex].Span.Slice(blockOffset).CopyTo(buffer);
                            count -= blockOffset;
                            blockOffset = 0;
                        }
                        else
                        {
                            _dataBlock[blockIndex].Span.CopyTo(buffer.AsSpan(count));
                        }
                        count += _dataBlock[blockIndex].Length;
                    }

                    _packet.Capacity = size;
                    ZlibUtils.Decompress(buffer, _packet._data.AsSpan(0, size));
                    _packet.Id = VarInt.Read(_packet._data, out IdOffset);
                    _packet._start = IdOffset;
                    _packet._size = size - IdOffset;
                    Packet = _packet;
                    return this;
                }
            }

            _packet.Id = VarInt.Read(ReadByte, out IdOffset);
            _packet.Capacity = packetLength - IdOffset;

            CheckBounds();
            for (int i = blockIndex; i < dataBlockLength - blockIndex; i++)
            {
                _packet.WriteBytes(_dataBlock[i].Span.Slice(blockOffset));
                if (blockOffset != 0)
                    blockOffset = 0;
            }
            Packet = _packet;
            return this;

            void CheckBounds()
            {
                if (blockOffset >= _dataBlock[blockIndex].Length)
                {
                    blockIndex++;
                    blockOffset = 0;
                }
            }
            byte ReadByte()
            {
                CheckBounds();
                return _dataBlock[blockIndex].Span[blockOffset++];
            }
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
                    //_CPPool.Return(Packet); Packet = null;
                    //for (int i = 0; i < _bufferBlockLength; i++)
                    //{
                    //    NetworkListener._bufferPool.Return(_bufferBlock[i]);
                    //}
                    ////虽然一般buffer会从上面返回数组池，但有极少部分不会回去，因此为了防止那极少数的buffer被block长期占着导致无法被GC回收，所以这边需要每次都清空block
                    //PacketListener._dataBlockPool.Return(_dataBlock, true);
                    //PacketListener._bufferBlockPool.Return(_bufferBlock, true);
                    //_bufferBlock = null; _dataBlock = null; RawData = null;
                    //PacketListener.PREAPool.Return(this); //这个必须在最后，只有当所有资源回收完成才能送回池内，否则在极端情况下会存在线程安全问题
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
    }
}
