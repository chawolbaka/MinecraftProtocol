using System;

namespace MinecraftProtocol.DataType.Forge
{
    /// <summary>
    /// Causes the client to recomplete the entire handshake from the start. There is no payload beyond the discriminator byte.
    /// The normal forge server does not ever use this packet, but it is used when connecting through a BungeeCord instance, specifically when transitioning from a vanilla server to a modded one or from a modded server to another modded server.
    /// </summary>
    public struct HandshakeReset : IForgeStructure
    {
        /// <summary>Always -2 (254) for HandshakeReset</summary>
        public const byte Discriminator = 254;

        public byte[] ToBytes() => new byte[] { Discriminator };
    }
}
