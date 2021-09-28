using System;
using System.Collections.Generic;

namespace MinecraftProtocol.Packets.Client
{

    /// <summary>
    /// http://wiki.vg/Server_List_Ping#Handshake
    /// </summary>
    public partial class HandshakePacket : DefinedPacket
    {
        //握手包永远不会变ID(大概)
        private const int id = 0x00;
        public enum State : int
        {
            GetStatus = 1,
            Login = 2
        }

        [PacketProperty]
        private string _serverAddress;
        [PacketProperty]
        private ushort _serverPort;
        [PacketProperty]
        private State _nextState;

        protected override void CheckProperty()
        {
            if (string.IsNullOrEmpty(_serverAddress))
                throw new ArgumentNullException(nameof(ServerAddress));
        }

        protected override void Write()
        {
            WriteVarInt(ProtocolVersion);
            WriteString(_serverAddress);
            WriteUnsignedShort(_serverPort);
            WriteVarInt((int)_nextState);
        }

        protected override void Read()
        {
            ProtocolVersion = Reader.ReadVarInt();
            _serverAddress = Reader.ReadString();
            _serverPort = Reader.ReadUnsignedShort();
            _nextState = (State)Reader.ReadVarInt();
        }

        public static int GetPacketId(int protocolVersion) => id;
        public static int GetPacketId() => id;

    }
}
