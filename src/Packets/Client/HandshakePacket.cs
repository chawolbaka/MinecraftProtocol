using System;
using System.Collections.Generic;

namespace MinecraftProtocol.Protocol.Packets.Client
{

    /// <summary>
    /// http://wiki.vg/Server_List_Ping#Handshake
    /// </summary>
    public class HandshakePacket : Packet
    {
        //握手包永远不会变ID(大概)
        private const int id = 0x00;
        public enum State : int
        {
            GetStatus = 1,
            Login = 2
        }
        public int ProtocolVersion { get; }
        public string ServerAddress { get; }
        public ushort ServerPort { get; }
        public State NextState { get; }

        private HandshakePacket(ReadOnlyPacket packet, string serverAddress, ushort port, int protocolVersion, State nextState) : base(packet)
        {
            this.ServerAddress = serverAddress;
            this.ServerPort = port;
            this.ProtocolVersion = protocolVersion;
            this.NextState = nextState;
        }
        /// <param name="protocolVersion">
        /// The version that the client plans on using to connect to the server (which is not important for the ping).
        /// If the client is pinging to determine what version to use, by convention -1 should be set.
        /// </param>
        public HandshakePacket(string serverAddress, ushort port, int protocolVersion, State nextState) : base(id)
        {
            if (string.IsNullOrEmpty(serverAddress))
                throw new ArgumentNullException(nameof(serverAddress));     

            this.ServerAddress = serverAddress;
            this.ServerPort = port;
            this.ProtocolVersion = protocolVersion;
            this.NextState = nextState;
            WriteVarInt(protocolVersion);
            WriteString(serverAddress);
            WriteUnsignedShort(port);
            WriteVarInt((int)nextState);
        }

        public static int GetPacketID() => id;

        public static bool Verify(ReadOnlyPacket packet) => Verify(packet, out _);
        public static bool Verify(ReadOnlyPacket packet, out HandshakePacket hp)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));

            hp = null;
            if (packet.ID != id)
                return false;

            try
            {
                int ProtocolVersion = packet.ReadVarInt();
                string ServerAddress = packet.ReadString();
                ushort ServerPort = packet.ReadUnsignedShort();
                int NextState = packet.ReadVarInt();

                if (packet.IsReadToEnd)
                    hp = new HandshakePacket(packet, ServerAddress, ServerPort, ProtocolVersion, (State)NextState);
                return !(hp is null);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }      
    }
}
