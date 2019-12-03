using MinecraftProtocol.Protocol;
using MinecraftProtocol.Protocol.Packets.Both;
using System;
using System.Collections.Generic;

namespace MinecraftProtocol.DataType.Forge
{

    /// <summary>
    /// Confirms that the client is able to use the settings sent previously.
    /// </summary>
    public struct HandshakeAck : IForgeStructure
    {
        /// <summary>Always -1 (255) for HandshakeAck</summary>
        public const byte Discriminator = 255; //java那边的byte好像是有符号的,但是C#这边不是所有255=java那边的-1
        /// <summary>
        /// The current phase, which is the ordinal (0-indexed) in the FMLHandshakeClientState or FMLHandshakeServerState enums 
        /// (if the server is sending it, it is in the ServerState enum, and if the client is sending it, it is the ClientState enum).
        /// </summary>
        public readonly byte Phase;

        public HandshakeAck(FMLHandshakeClientState phase) : this((byte)phase) { }
        public HandshakeAck(byte phase)
        {
            Phase = phase;
        }
        public byte[] ToBytes()
        {
            return new byte[] { Discriminator, Phase };
        }

        public static HandshakeAck Read(ReadOnlySpan<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length != 2)
                throw new ArgumentOutOfRangeException(nameof(data), "data length must be 2");
            if (data[0] != Discriminator)
                throw new InvalidCastException($"Invalid Discriminator {data[0]}");

            return new HandshakeAck(data[1]);
        }
        public static HandshakeAck Read(List<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Count != 2)
                throw new ArgumentOutOfRangeException(nameof(data), "data length must be 2");
            if (data[0] != Discriminator)
                throw new InvalidCastException($"Invalid Discriminator {data[0]}");

            return new HandshakeAck(data[1]);
        }

        public static bool operator ==(FMLHandshakeClientState left, HandshakeAck right) => right.Phase == (byte)left;
        public static bool operator ==(FMLHandshakeServerState left, HandshakeAck right) => right.Phase == (byte)left;
        public static bool operator !=(FMLHandshakeClientState left, HandshakeAck right) => !(left == right);
        public static bool operator !=(FMLHandshakeServerState left, HandshakeAck right) => !(left == right);
        public static bool operator ==(HandshakeAck left, FMLHandshakeClientState right) => left.Phase == (byte)right;
        public static bool operator ==(HandshakeAck left, FMLHandshakeServerState right) => left.Phase == (byte)right;
        public static bool operator !=(HandshakeAck left, FMLHandshakeClientState right) => !(left == right);
        public static bool operator !=(HandshakeAck left, FMLHandshakeServerState right) => !(left == right);
        public static bool operator ==(HandshakeAck left, HandshakeAck right) => left.Phase == right.Phase;
        public static bool operator !=(HandshakeAck left, HandshakeAck right) => !(left == right);
        public override int GetHashCode() => Phase;
        public override bool Equals(object obj)
        {
            if (obj is HandshakeAck ack)
                return this.Phase == ack.Phase;
            if (obj is FMLHandshakeClientState hcs)
                return this.Phase == (byte)hcs;
            if (obj is FMLHandshakeServerState hss)
                return this.Phase == (byte)hss;
            else
                return false;
        }

    }
}
