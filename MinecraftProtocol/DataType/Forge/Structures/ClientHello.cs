using System;
using System.Collections.Generic;

namespace MinecraftProtocol.DataType.Forge
{
    /// <summary>
    /// Response from the client to the ServerHello packet.
    /// </summary>
    public struct ClientHello : IForgeStructure
    {
        /// <summary>Always 1 for ClientHello. </summary>
        public const byte Discriminator = 1;
        /// <summary>Determined from NetworkRegistery. Currently 2.</summary>
        public readonly byte FMLProtocolVersion;

        public ClientHello(byte fmlProtocolVersion)
        {
            FMLProtocolVersion = fmlProtocolVersion;
        }

        public byte[] ToBytes() => new byte[] { Discriminator, FMLProtocolVersion };

        public static ClientHello Read(ReadOnlySpan<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length != 2)
                throw new ArgumentOutOfRangeException(nameof(data), "data length must be 2");
            if (data[0] != Discriminator)
                throw new InvalidCastException($"Invalid Discriminator {data[0]}");

            return new ClientHello(data[1]);
        }

        public static ClientHello Read(List<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Count != 2)
                throw new ArgumentOutOfRangeException(nameof(data), "data length must be 2");
            if (data[0] != ClientHello.Discriminator)
                throw new InvalidCastException($"Invalid Discriminator {data[0]}");

            return new ClientHello(data[1]);
        }

    }
}
