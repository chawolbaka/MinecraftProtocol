using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Drawing;
using MinecraftProtocol.IO.Extensions;

namespace MinecraftProtocol.Compression
{
    /// <summary>
    /// Quick Zlib compression handling for network packet compression.
    /// Note: Underlying compression handling is taken from the DotNetZip Library.
    /// This library is open source and provided under the Microsoft Public License.
    /// More info about DotNetZip at dotnetzip.codeplex.com.
    /// </summary>
    public static class ZlibUtils
    {
        public static uint Adler32(ReadOnlySpan<byte> data)
        {
            const uint MOD_ADLER = 65521;
            uint a = 1, b = 0;

            for (int i = 0; i < data.Length; i++)
            {
                a = (a + data[i]) % MOD_ADLER;
                b = (b + a) % MOD_ADLER;
            }

            return (b << 16) | a;
        }
        public static uint Adler32(ReadOnlyMemory<byte> data)
        {
            return Adler32(data.Span);
        }

        public static byte[] Compress(ReadOnlySpan<byte> to_compress)
        {
            using (MemoryStream memstream = new MemoryStream())
            {
                memstream.WriteByte(0x78);
                memstream.WriteByte(0x9C);
                using (DeflateStream stream = new DeflateStream(memstream, CompressionMode.Compress, true))
                {
                    stream.Write(to_compress);
                }
                uint adler32 = Adler32(to_compress);
                memstream.WriteByte((byte)(adler32 >> 24));
                memstream.WriteByte((byte)(adler32 >> 16));
                memstream.WriteByte((byte)(adler32 >> 8));
                memstream.WriteByte((byte)adler32);
                return memstream.ToArray();
            }
        }

        public static int Decompress(ReadOnlySpan<byte> input, Span<byte> output)
        {
            using MemoryStream ms = new MemoryStream(input.Slice(2, input.Length - 6).ToArray());
            using DeflateStream stream = new DeflateStream(ms, CompressionMode.Decompress);
            int read = stream.Read(output);
            while (read < output.Length)
            {
                read += stream.Read(output.Slice(read));
            }

#if DEBUG
            if (Adler32(output) != (uint)input.Slice(input.Length - 4).AsInt())
                throw new InvalidDataException("adler32 verify failed");
#endif       
            return read;
        }

        public static async Task<byte[]> CompressAsync(ReadOnlyMemory<byte> to_compress, CancellationToken cancellationToken = default)
        {
            using (MemoryStream memstream = new MemoryStream())
            {
                memstream.WriteByte(0x78);
                memstream.WriteByte(0x9C);
                using (DeflateStream stream = new DeflateStream(memstream, CompressionMode.Compress, true))
                {
                    await stream.WriteAsync(to_compress, cancellationToken);
                }
                uint adler32 = Adler32(to_compress);
                memstream.WriteByte((byte)(adler32 >> 24));
                memstream.WriteByte((byte)(adler32 >> 16));
                memstream.WriteByte((byte)(adler32 >> 8));
                memstream.WriteByte((byte)adler32);
                return memstream.ToArray();
            }
        }

        public static async ValueTask<int> DecompressAsync(ReadOnlyMemory<byte> input, Memory<byte> output, CancellationToken cancellationToken = default)
        {
            using MemoryStream ms = new MemoryStream(input.Slice(2).ToArray());
            using DeflateStream stream = new DeflateStream(ms, CompressionMode.Decompress);
            int read = await stream.ReadAsync(output, cancellationToken);
            while (read < output.Length)
            {
                read += await stream.ReadAsync(output.Slice(read), cancellationToken);
            }

#if DEBUG
            if (Adler32(output) != (uint)input.Span.Slice(input.Length - 4).AsInt())
                throw new InvalidDataException("adler32 verify failed");
#endif       
            return read;
        }
    }
}
