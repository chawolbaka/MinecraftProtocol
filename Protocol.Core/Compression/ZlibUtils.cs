using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zlib;

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
        /// <summary>
        /// Compress a byte array into another bytes array using Zlib compression
        /// </summary>
        /// <param name="to_compress">Data to compress</param>
        /// <returns>Compressed data as a byte array</returns>
        public static byte[] Compress(byte[] to_compress, int offset, int length)
        {
            byte[] data;
            using (MemoryStream memstream = new MemoryStream())
            {
                using (ZlibStream stream = new ZlibStream(memstream, CompressionMode.Compress))
                {
                    stream.Write(to_compress, offset, length);
                }
                data = memstream.ToArray();
            }
            return data;
        }

        /// <summary>
        /// Decompress a byte array into another byte array of the specified size
        /// </summary>
        /// <param name="to_decompress">Data to decompress</param>
        /// <param name="size_uncompressed">Size of the data once decompressed</param>
        /// <returns>Decompressed data as a byte array</returns>
        public static byte[] Decompress(byte[] to_decompress, int offset, int size_uncompressed)
        {
            using (ZlibStream stream = new ZlibStream(new MemoryStream(to_decompress, false), CompressionMode.Decompress))
            {
                byte[] packetData_decompressed = new byte[size_uncompressed];
                stream.Read(packetData_decompressed, offset, size_uncompressed);
                return packetData_decompressed;
            }
        }

        /// <summary>
        /// Decompress a byte array into another byte array of a potentially unlimited size (!)
        /// </summary>
        /// <param name="to_decompress">Data to decompress</param>
        /// <returns>Decompressed data as byte array</returns>
        public static byte[] Decompress(byte[] to_decompress)
        {
            using (ZlibStream stream = new ZlibStream(new MemoryStream(to_decompress, false), CompressionMode.Decompress))
            {
                byte[] buffer = new byte[16 * 1024];
                using (MemoryStream decompressedBuffer = new MemoryStream())
                {
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        decompressedBuffer.Write(buffer, 0, read);
                    return decompressedBuffer.ToArray();
                }
            }
        }

        public static async Task<byte[]> CompressAsync(Memory<byte> to_compress, CancellationToken cancellationToken = default)
        {
            byte[] data;
            using MemoryStream ms = new MemoryStream();
            using ZlibStream stream = new ZlibStream(ms, CompressionMode.Compress);
            await stream.WriteAsync(to_compress, cancellationToken).ConfigureAwait(false);
            data = ms.ToArray();
            return data;
        }

        public static async Task<byte[]> DecompressAsync(ReadOnlyMemory<byte> to_decompress, int size_uncompressed)
        {
            byte[] packetData_decompressed = new byte[size_uncompressed];
            using MemoryStream ms = new MemoryStream(to_decompress.Span.ToArray(), false);
            using ZlibStream stream = new ZlibStream(ms, CompressionMode.Decompress);
            await stream.ReadAsync(packetData_decompressed, 0, size_uncompressed).ConfigureAwait(false);
            return packetData_decompressed;
        }
    }
}
