using MinecraftProtocol.IO.Extensions;
using MinecraftProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

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
            if (OverrideDimension.HasValue)
                return ProtocolHandler.ConcatBytes(new byte[] { Discriminator, FMLProtocolVersion }, ProtocolHandler.GetBytes(OverrideDimension.Value));
            else
                return new byte[] { Discriminator, FMLProtocolVersion };
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
                return new ServerHello(data[1], data.Slice(2, 4).AsInt());
            else
                return new ServerHello(data[1], null);
        }
        public static ServerHello Read(List<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Count < 1)
                throw new ArgumentOutOfRangeException(nameof(data), "data length too short");
            if (data[0] != Discriminator)
                throw new InvalidCastException($"Invalid Discriminator {data[0]}");

            int version = data[1];
            if (version > 1)
                return new ServerHello(data[1], ProtocolHandler.ReadInt(data, 2, true));
            else
                return new ServerHello(data[1], null);
        }
    }
}
