﻿using System;
using System.Collections.Generic;
using MinecraftProtocol.DataType;

namespace MinecraftProtocol.Protocol
{

    /*
     * 翻译来源:
     * https://github.com/bangbang93/minecraft-protocol/blob/b043535794164f6c9d536adb81de8ed9559e7a09/protocol.md
     * 机翻
     * 参考资料:
     * http://wiki.vg/Protocol
     * https://github.com/bangbang93/minecraft-protocol
     * 参考(抄袭)项目:
     * https://github.com/ORelio/Minecraft-Console-Client
    */
    
    public static class SendPacket
    {
        
        public static void Handshake(string serverIP,ushort port, int protocolVersion, int nextState,ConnectionPayload info)
        {
            //http://wiki.vg/Server_List_Ping#Handshake
            Packet packet = new Packet();
            packet.PacketID = 0x00;
            packet.WriteVarInt(protocolVersion);//Field:ProtocolVersion
            packet.WriteString(serverIP);//Field:Server Address
            packet.WriteUnsignedShort(port);//Field:Server Port
            packet.WriteVarInt(nextState);//Field:Next State(1 for status, 2 for login)
            info.Session.Client.Send(packet.GetPacket(info.CompressionThreshold));
        }
        /// <summary>
        /// Ping的请求包
        /// (就是一个长度和空字节而已,为什么要写这个?因为我怕以后改包ID呀)
        /// http://wiki.vg/Server_List_Ping#Request
        /// </summary>
        public static void PingRequest(ConnectionPayload info)
        {
            Packet packet = new Packet();
            packet.PacketID = 0x00;
            info.Session.Client.Send(packet.GetPacket(info.CompressionThreshold));
        }
        public static void LoginStart(string playerName,ConnectionPayload connect)
        {
            Packet LoginStartPacket = new Packet();
            LoginStartPacket.PacketID = PacketType.GetPacketID(
                PacketType.Client.LoginStart, connect.ProtocolVersion);
            LoginStartPacket.WriteString(playerName);
            connect.Session.Client.Send(LoginStartPacket.GetPacket(connect.CompressionThreshold));
        }

        //加入游戏后的包


        /// <summary>
        /// 服务端将会不断发送包含了一个随机数字标识符的保持在线，客户端必须以相同的数据包回复。
        /// 如果客户端超过30s没有回复，服务端将会踢出玩家。反之，如果服务端超过20s没有发送任何保持在线，那么客户端将会断开连接并产生一个“Timed out”（超时）异常。
        /// </summary>
        /// <param name="data">服务端发送的随机数字</param>
        /// <param name="connection"></param>
        public static void KeepAlive(List<byte> data, ConnectionPayload connection)
        {
            Packet packet = new Packet();
            packet.WriteVarInt(PacketType.GetPacketID(PacketType.Client.KeepAlive,connection.ProtocolVersion));
            packet.WriteBytes(data.ToArray());
            connection.Session.Client.Send(packet.GetPacket(connection.CompressionThreshold));
        }
        /// <summary>
        /// 发送聊天消息(在加入服务器后马上发送可能会导致被服务器踢下线)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="connection"></param>
        public static void ChatMessage(string message, ConnectionPayload connection)
        {
            Packet packet = new Packet();
            packet.PacketID = PacketType.GetPacketID(PacketType.Client.ChatMessage, connection.ProtocolVersion);
            packet.WriteString(message);
            connection.Session.Client.Send(packet.GetPacket(connection.CompressionThreshold));
        }
    }
}
