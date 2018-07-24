using System;
using System.Collections.Generic;
using System.Net.Sockets;
using MinecraftProtocol.DataType;

namespace MinecraftProtocol
{
    public class Login
    {
        private TcpClient tcp;
        private ServerInfo _serverInfo;
        public Login(TcpClient session,ServerInfo serverInfo)
        {
            if (session != null)
                this.tcp = session;
            else
                throw new ArgumentNullException($"Param \"{nameof(session)}\" is null.");
            if (serverInfo != null)
                this._serverInfo = serverInfo;
            else
                throw new ArgumentNullException($"Param \"{nameof(serverInfo)}\" is null.");
            //if (!tcp.Client.Connected)
            //    tcp.Connect(_serverInfo.ServerIPAddress, _serverInfo.ServerPort);
        }
        public void Handshake()
        {
            Protocol.Packet handler = new Protocol.Packet();
            handler.WriteVarInt(0x00);//Packet ID 
            handler.WriteVarInt(_serverInfo.ProtocolVersion);//Field:ProtocolVersion
            handler.WriteString(_serverInfo.ServerIPAddress);//Field:Server Address
            handler.WriteUnsignedShort(_serverInfo.ServerPort);//Field:Server Port
            handler.WriteVarInt(2);//Field:Next State(1 for status, 2 for login)
            tcp.Client.Send(handler.GetPacket());
        }
        public void LoginStart(string playerName)
        {
            Protocol.Packet handler = new Protocol.Packet();
            handler.WriteVarInt(0x00);//Packet ID
            handler.WriteString(playerName);//Player Name
            tcp.Client.Send(handler.GetPacket());
        }
        public void EncryptionResponse()
        {
            throw new NotImplementedException();
        }
    }
}
