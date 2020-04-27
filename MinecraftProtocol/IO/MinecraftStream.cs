using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MinecraftProtocol.Compression;
using MinecraftProtocol.Protocol.Packets;

namespace MinecraftProtocol.IO
{
    public class MinecraftStream : Stream
    {
        public virtual bool Encrypted => false;

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => _baseStream.CanWrite;

        public override bool CanTimeout => _baseStream.CanTimeout;
        
        //下面几个好像一定会抛出异常的样子?
        public override long Length => _baseStream.Length;

        public override long Position { get => _readCount; set => throw new NotSupportedException("Read only"); }

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

        public override void SetLength(long value) => _baseStream.SetLength(value);

        private NetworkStream _baseStream;
        private long _readCount;

        
        public MinecraftStream(NetworkStream ns)
        {
            this._baseStream = ns;
        }
        public virtual MinecraftCryptoStream ToCryptoStream(byte[] secretKey)
        {
            return new MinecraftCryptoStream(_baseStream, secretKey);
        }

        public virtual Packet ReadPacket(int compressionThreshold)
        {
            int PacketLength = VarInt.Read(this);
            if (PacketLength >= 0)
            {
                Span<byte> PacketData = new byte[PacketLength];
                if (Read(PacketData) != PacketLength)
                    throw new InvalidDataException("未读取到包的所有数据");
                if (compressionThreshold > 0)
                {
                    int size = VarInt.Read(PacketData, out int SizeOffset);
                    PacketData = PacketData.Slice(SizeOffset);
                    //如果是0的话就代表这个数据包没有被压缩
                    if (size != 0)
                        PacketData = ZlibUtils.Decompress(PacketData.ToArray(), size);
                }
                return new Packet(VarInt.Read(PacketData, out int IdOffset), PacketData.Slice(IdOffset));
            }
            else
                throw new IndexOutOfRangeException("数据包长度低于1");
        }

        public override int ReadByte() => _baseStream.ReadByte();

        public override int Read(Span<byte> buffer)
        {
            int read = _baseStream.Read(buffer);
            _readCount += read;
            while (read < buffer.Length)
            {
                read += _baseStream.Read(buffer.Slice(read));
                _readCount += read;
            }
            return read;
        }
        public override int Read(byte[] buffer, int offset, int size)
        {
            int read = 0;
            while (read < size)
            {
                read += _baseStream.Read(buffer, offset + read, size - read);
                _readCount += read;
            }   
            return read;
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
            //int read = 0;
            //while (read < offset)
            //    read += await _baseStream.ReadAsync(buffer, offset + read, count - read, cancellationToken);
            //return read;
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
            //int read = await _baseStream.ReadAsync(buffer);
            //while (read < buffer.Length)
            //    read += await _baseStream.ReadAsync(buffer.Slice(read));
            //return read;
        }

        public override void WriteByte(byte value) => _baseStream.WriteByte(value);

        public override void Write(byte[] buffer, int offset, int size) => _baseStream.Write(buffer, offset, size);

        public override void Write(ReadOnlySpan<byte> buffer) => _baseStream.Write(buffer);

        public override Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken) => _baseStream.WriteAsync(buffer, offset, size, cancellationToken);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => _baseStream.WriteAsync(buffer, cancellationToken);

        public override void Flush() => _baseStream.Flush();

        public override void Close() => _baseStream.Close();

    }
}
