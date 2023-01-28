using System;
using System.Runtime.InteropServices;
using MinecraftProtocol.Compression;
using MinecraftProtocol.IO.Pools;
using MinecraftProtocol.Packets;

namespace MinecraftProtocol.IO
{
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
        /// 完整的数据包
        /// </summary>
        public Memory<Memory<byte>> RawData;

        private bool _usePool;
        private bool _disposed = false;

        private Memory<byte>[] _dataBlock;
        private GCHandle[] _dataGCHandle;
        private ushort _dataGCHandleBlockLength;

        private static IPool<CompatiblePacket> _CPPool = new CompatiblePacketPool(true);

        //那堆ref是为了减少值类型的内存复制，因为性能不好开始病态起来了
        internal PacketReceivedEventArgs Setup(GCHandle[] dataGCHandleBlock, ref ushort dataGCHandleBlockLength, ref Memory<byte>[] dataBlock, ref ushort dataBlockLength, ref byte headLength, ref int bodyLength, ref int protocolVersion, int compressionThreshold, bool usePool)
        {
            RawData = new Memory<Memory<byte>>(dataBlock, 0, dataBlockLength);
            ReceivedTime = DateTime.Now;

            _usePool = usePool;
            _disposed = false;
            _dataBlock = dataBlock;
            _dataGCHandle = dataGCHandleBlock;
            _dataGCHandleBlockLength = dataGCHandleBlockLength;

            int blockIndex = 0, blockOffset = 0, IdOffset;


            if (dataBlock[0].Length > headLength)
            {
                blockOffset = headLength;
            }
            else
            {
                for (; blockOffset < headLength; blockOffset++)
                {
                    if (blockOffset > dataBlock[blockIndex].Length)
                    {
                        blockIndex++;
                        blockOffset = 0;
                    }
                }
            }


            if (usePool)
            {
                Packet = _CPPool.Rent();
                Packet.ProtocolVersion = protocolVersion;
                Packet.CompressionThreshold = compressionThreshold;
            }
            else
            {
                Packet = new CompatiblePacket(-1, protocolVersion, compressionThreshold);
            }

            //如果要解压那么就先组合data块并解压，如果不需要解压那么就直接从data块中复制到Packet内避免多余的内存复制
            if (compressionThreshold > 0)
            {
                int size = VarInt.Read(ReadByte);
                if (size != 0)
                {
                    //组合data块
                    byte[] buffer = new byte[bodyLength];
                    int count = 0;
                    for (; blockIndex < dataBlockLength; blockIndex++)
                    {
                        if (blockOffset > 0)
                        {
                            _dataBlock[blockIndex].Span.Slice(blockOffset).CopyTo(buffer.AsSpan().Slice(count));
                            count -= blockOffset;
                            blockOffset = 0;
                        }
                        else
                        {
                            _dataBlock[blockIndex].Span.CopyTo(buffer.AsSpan().Slice(count));
                        }
                        count += _dataBlock[blockIndex].Length;
                    }

                    Span<byte> decompressed = ZlibUtils.Decompress(buffer, 0, size);
                    Packet.ID = VarInt.Read(decompressed, out IdOffset);
                    Packet.Capacity = decompressed.Length - IdOffset;
                    Packet.WriteBytes(decompressed.Slice(IdOffset));
                    return this;
                }
            }

            Packet.ID = VarInt.Read(ReadByte, out IdOffset);
            Packet.Capacity = bodyLength - IdOffset;

            for (int i = blockIndex; i < dataBlockLength - blockIndex; i++)
            {
                Packet.WriteBytes(_dataBlock[i].Span.Slice(blockOffset));
                if (blockOffset != 0)
                    blockOffset = 0;
            }

            return this;

            byte ReadByte()
            {
                if (blockOffset > _dataBlock[blockIndex].Length)
                {
                    blockIndex++;
                    blockOffset = 0;
                }
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
                    _CPPool.Return(Packet);
                    for (int i = 0; i < _dataGCHandleBlockLength; i++)
                    {
                        PacketListener._dataPool.Return(_dataGCHandle[i]);
                    }
                    PacketListener._dataBlockPool.Return(_dataBlock);
                    PacketListener._gcHandleBlockPool.Return(_dataGCHandle);
                    PacketListener.PREAPool.Return(this);
                }
                else
                {
                    for (int i = 0; i < _dataGCHandle.Length; i++)
                    {
                        _dataGCHandle[i].Free();
                    }
                }
            }
            catch (ArgumentException)
            {

            }
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
