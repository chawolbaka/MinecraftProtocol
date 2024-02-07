using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using MinecraftProtocol.IO;
using MinecraftProtocol.IO.Extensions;

namespace MinecraftProtocol.DataType.Forge
{
    /// <summary>
    /// Starts the handshake.
    /// </summary>
    public struct ServerHello : IForgeStructure
    {

        /// <summary>Always 0 for ServerHello</summary>
        public const byte Discriminator = 0;
        /// <summary>Determined from NetworkRegistery. Currently 2.</summary>
        public readonly byte FMLProtocolVersion;
        /// <summary>Only sent if protocol version is greater than 1 (Optional Int)</summary>
        public readonly int? OverrideDimension;


        public ServerHello(byte fmlProtocolVersion, int? overrideDimension)
        {
            FMLProtocolVersion = fmlProtocolVersion;
            OverrideDimension = overrideDimension;
        }

        public byte[] ToBytes()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteUnsignedByte(Discriminator);
            writer.WriteUnsignedByte(FMLProtocolVersion);
            if (OverrideDimension.HasValue)
                writer.WriteInt(OverrideDimension.Value);
            return writer.AsSpan().ToArray();
        }

        public static ServerHello Read(ReadOnlySpan<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(data), "data length too short");
            if (data[0] != Discriminator)
                throw new InvalidCastException($"Invalid Discriminator {data[0]}");

            int version = data[1];
            if (version > 1)
                return new ServerHello(data[1], BinaryPrimitives.ReadInt32BigEndian(data.Slice(2, 4)));
            else
                return new ServerHello(data[1], null);
        }
    }
}
