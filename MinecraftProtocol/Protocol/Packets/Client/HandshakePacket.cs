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

        private HandshakePacket(Packet packet, string serverAddress, ushort port, int protocolVersion, State nextState) : base(packet.ID, packet.Data)
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

        public static bool Verify(Packet packet) => Verify(packet, out _);
        public static bool Verify(Packet packet, out HandshakePacket hp)
        {
            hp = null;
            if (packet.ID != id)
                return false;

            try
            {
                int ProtocolVersion = ProtocolHandler.ReadVarInt(packet.Data, 0, out int offset, true);
                string ServerAddress = ProtocolHandler.ReadString(packet.Data, offset, out offset, true);
                ushort ServerPort = ProtocolHandler.ReadUnsignedShort(packet.Data, offset, out offset, true);
                int NextState = ProtocolHandler.ReadVarInt(packet.Data, offset, out offset, true);

                if (packet.Data.Count == offset)
                    hp = new HandshakePacket(packet, ServerAddress, ServerPort, ProtocolVersion, (State)NextState);
                return !(hp is null);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }      
    }
}
