using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BouncyCastle.Crypto;
using BouncyCastle.Crypto.Engines;
using BouncyCastle.Crypto.Modes;
using BouncyCastle.Crypto.Parameters;

namespace MinecraftProtocol.IO
{
    public class MinecraftCryptoStream : MinecraftStream
    {
        public override bool Encrypted => true;

        private BufferedBlockCipher Encrypt;
        private BufferedBlockCipher Decrypt;

        public MinecraftCryptoStream(NetworkStream ns, byte[] secretKey) : base(ns)
        {
            Encrypt = new BufferedBlockCipher(new CfbBlockCipher(new AesFastEngine(), 8));
            Encrypt.Init(true, new ParametersWithIV(new KeyParameter(secretKey), secretKey, 0, 16));

            Decrypt = new BufferedBlockCipher(new CfbBlockCipher(new AesFastEngine(), 8));
            Decrypt.Init(false, new ParametersWithIV(new KeyParameter(secretKey), secretKey, 0, 16));
        }

        public override int ReadByte() => Decrypt.ProcessByte((byte)base.ReadByte())[0];
    
        public override int Read(byte[] buffer, int offset, int size)
        {
            int read = base.Read(buffer, offset, size);
            Decrypt.ProcessBytes(buffer, offset, size, buffer, offset);
            return read;
        }
        public override int Read(Span<byte> buffer)
        {
            int read = base.Read(buffer);
            Decrypt.ProcessBytes(buffer, 0, buffer.Length, buffer, 0);
            return read;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotImplementedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public override void WriteByte(byte value) => base.WriteByte(Encrypt.ProcessByte(value)[0]);

        public override void Write(byte[] buffer, int offset, int size) => base.Write(Encrypt.ProcessBytes(buffer, offset, size), offset, size);
        
        public override void Write(ReadOnlySpan<byte> buffer) => base.Write(Encrypt.ProcessBytes(buffer));

        public override Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken) => base.WriteAsync(Encrypt.ProcessBytes(buffer, offset, size), offset, size, cancellationToken);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => base.WriteAsync(Encrypt.ProcessBytes(buffer.ToArray()), cancellationToken);
        
    }
}
