using System;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;

namespace MinecraftProtocol.Packets
{
    public class PacketType
    {
        public string Name { get; }
        public Func<int, int> GetId { get; }
        
        public PacketType(string name, Func<int, int> getId)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            GetId = getId ?? throw new ArgumentNullException(nameof(getId));
        }


        public static bool operator ==(PacketType left, LazyCompatiblePacket right) => !ReferenceEquals(left, null) && !ReferenceEquals(right, null) && right.Id == left.GetId(right.ProtocolVersion);
        public static bool operator !=(PacketType left, LazyCompatiblePacket right) => !(left == right);
        public static bool operator ==(LazyCompatiblePacket left, PacketType right) => !ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Id == right.GetId(left.ProtocolVersion);
        public static bool operator !=(LazyCompatiblePacket left, PacketType right) => !(left == right);


        public static bool operator ==(PacketType left, CompatiblePacket right) => !ReferenceEquals(left, null) && !ReferenceEquals(right, null) && right.Id == left.GetId(right.ProtocolVersion);
        public static bool operator !=(PacketType left, CompatiblePacket right) => !(left == right);
        public static bool operator ==(CompatiblePacket left, PacketType right) => !ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Id == right.GetId(left.ProtocolVersion);
        public static bool operator !=(CompatiblePacket left, PacketType right) => !(left == right);


        public static bool operator ==(PacketType left, ReadOnlyCompatiblePacket right) => !ReferenceEquals(left, null) && !ReferenceEquals(right, null) && right.Id == left.GetId(right.ProtocolVersion);
        public static bool operator !=(PacketType left, ReadOnlyCompatiblePacket right) => !(left == right);
        public static bool operator ==(ReadOnlyCompatiblePacket left, PacketType right) => !ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Id == right.GetId(left.ProtocolVersion);
        public static bool operator !=(ReadOnlyCompatiblePacket left, PacketType right) => !(left == right);


        public override bool Equals(object obj)
        {
            return obj is PacketType pt && pt.Name == Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public static readonly PacketType Handshake = new PacketType(nameof(HandshakePacket), (ver) => HandshakePacket.GetPacketId());

        public static class Status
        {
            /// <summary>从服务端发送到客户端Packet</summary>
            public static class Server
            {
                public static readonly PacketType Response = new PacketType(nameof(PingResponsePacket), (ver) => PingResponsePacket.GetPacketID());
                public static readonly PacketType Pong     = new PacketType(nameof(PongPacket), (ver) => PongPacket.GetPacketId());
            }

            /// <summary>从客户端发送到服务端Packet</summary>
            public static class Client
            {
                public static readonly PacketType Request = new PacketType(nameof(PingRequestPacket), (ver) => PingRequestPacket.GetPacketId());
                public static readonly PacketType Ping    = new PacketType(nameof(PingPacket), (ver) => PingPacket.GetPacketId());
            }
        }

        public static class Login
        {
            /// <summary>从服务端发送到客户端Packet</summary>
            public static class Server
            {
                public static readonly PacketType Disconnect         = new PacketType(nameof(DisconnectLoginPacket), (ver) => DisconnectLoginPacket.GetPacketId(ver));
                public static readonly PacketType LoginSuccess       = new PacketType(nameof(LoginSuccessPacket), (ver) => LoginSuccessPacket.GetPacketId(ver));
                public static readonly PacketType SetCompression     = new PacketType(nameof(SetCompressionPacket), (ver) => SetCompressionPacket.GetPacketId(ver));
                public static readonly PacketType EncryptionRequest  = new PacketType(nameof(EncryptionRequestPacket), (ver) => EncryptionRequestPacket.GetPacketId(ver));
                public static readonly PacketType LoginPluginRequest = new PacketType(nameof(LoginPluginRequestPacket), (ver) => LoginPluginRequestPacket.GetPacketId(ver));
            }

            /// <summary>从客户端发送到服务端Packet</summary>
            public static class Client
            {
                public static readonly PacketType LoginStart          = new PacketType(nameof(LoginStartPacket), (ver) => LoginStartPacket.GetPacketId(ver));
                public static readonly PacketType EncryptionResponse  = new PacketType(nameof(EncryptionResponsePacket), (ver) => EncryptionResponsePacket.GetPacketId(ver));
                public static readonly PacketType LoginPluginResponse = new PacketType(nameof(LoginPluginResponsePacket), (ver) => LoginPluginResponsePacket.GetPacketId(ver));
            }
        }

        public static class Play
        {
            /// <summary>从服务端发送到客户端Packet</summary>
            public static class Server
            {
                public static readonly PacketType PluginChannel         = new PacketType(nameof(ServerPluginChannelPacket), (ver) => ServerPluginChannelPacket.GetPacketId(ver));
                public static readonly PacketType KeepAlive             = new PacketType(nameof(KeepAliveRequestPacket), (ver) => KeepAliveRequestPacket.GetPacketId(ver));
                public static readonly PacketType ChatMessage           = new PacketType(nameof(ServerChatMessagePacket), (ver) => ServerChatMessagePacket.GetPacketId(ver));
                public static readonly PacketType DisguisedChatMessage  = new PacketType(nameof(DisguisedChatMessagePacket), (ver) => DisguisedChatMessagePacket.GetPacketId(ver));
                public static readonly PacketType SystemChatMessage     = new PacketType(nameof(SystemChatMessagePacket), (ver) => SystemChatMessagePacket.GetPacketId(ver));
                public static readonly PacketType PlayerChatMessage     = new PacketType(nameof(PlayerChatMessagePacket), (ver) => PlayerChatMessagePacket.GetPacketId(ver));
                public static readonly PacketType Disconnect            = new PacketType(nameof(DisconnectPacket), (ver) => DisconnectPacket.GetPacketId(ver));
            }

            /// <summary>从客户端发送到服务端Packet</summary>
            public static class Client
            {
                public static readonly PacketType KeepAlive      = new PacketType(nameof(KeepAliveResponsePacket), (ver) => KeepAliveResponsePacket.GetPacketId(ver));
                public static readonly PacketType ChatMessage    = new PacketType(nameof(ClientChatMessagePacket), (ver) => ClientChatMessagePacket.GetPacketId(ver));
                public static readonly PacketType PluginChannel  = new PacketType(nameof(ClientPluginChannelPacket), (ver) => ClientPluginChannelPacket.GetPacketId(ver));
                public static readonly PacketType ClientSettings = new PacketType(nameof(ClientSettingsPacket), (ver) => ClientSettingsPacket.GetPacketId(ver));
            }
        }
    }
}
