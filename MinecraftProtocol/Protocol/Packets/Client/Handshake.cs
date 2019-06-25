using System;
using System.Collections.Generic;
using MinecraftProtocol.DataType;

namespace MinecraftProtocol.Protocol.Packets.Client
{

    /// <summary>
    /// http://wiki.vg/Server_List_Ping#Handshake
    /// </summary>
    public class Handshake:Packet
    {
        //握手包永远不会变ID(大概)
        public const int PacketID = 0x00;
        public enum NextState : int
        {
            GetStatus = 1,
            Login = 2
        }
        public int ProtocolVersion { get; }
        public string ServerAddress { get; }
        public ushort ServerPort { get; }
        public NextState Next { get; }

        /// <param name="protocolVersion">
        /// The version that the client plans on using to connect to the server (which is not important for the ping).
        /// If the client is pinging to determine what version to use, by convention -1 should be set.
        /// </param>
        public Handshake(string serverIP, ushort port, int protocolVersion, NextState nextState)
        {
            this.ID = PacketID;
            this.ServerAddress = serverIP;
            this.ServerPort = port;
            this.ProtocolVersion = protocolVersion;
            this.Next = nextState;
            WriteVarInt(new VarInt(protocolVersion));
            WriteString(ServerAddress);
            WriteUnsignedShort(ServerPort);
            WriteVarInt(new VarInt((int)Next));
        }
        public Handshake(Packet handshakePacket)
        {
            if(Handshake.Verify(handshakePacket))
            {
                List<byte> buffer = new List<byte>(handshakePacket.Data);
                this.ID = handshakePacket.ID;
                this.Data = new List<byte>(handshakePacket.Data);
                this.ProtocolVersion = ProtocolHandler.ReadNextVarInt(buffer);
                this.ServerAddress = ProtocolHandler.ReadNextString(buffer);
                this.ServerPort = ProtocolHandler.ReadNextUnsignedShort(buffer);
                this.Next = (NextState)ProtocolHandler.ReadNextVarInt(buffer);
            }
            else
            {
                throw new InvalidPacketException("Not a handshake packet", handshakePacket);
            }
        }
        public static bool Verify(Packet packet) => Verify(packet, -1);
        public static bool Verify(Packet packet, NextState nextState) => Verify(packet, (int)nextState);
        private static bool Verify(Packet packet, int nextState)
        {
            if (packet.ID != PacketID)
                return false;

            List<byte> verifyPacket = new List<byte>(packet.Data);
            try
            {
                ProtocolHandler.ReadNextVarInt(verifyPacket); //Protocol Version
                ProtocolHandler.ReadNextString(verifyPacket); //Server Address
                ProtocolHandler.ReadNextUnsignedShort(verifyPacket); //Server Port
                int Next_State = ProtocolHandler.ReadNextVarInt(verifyPacket);
                if (nextState == -1)
                    return verifyPacket.Count == 0 && (Next_State == (int)NextState.GetStatus || Next_State == (int)NextState.Login);
                else
                    return verifyPacket.Count == 0 && Next_State == nextState;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }      
    }
}
