using System;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using System.Threading.Tasks;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Client;

namespace MinecraftProtocol.Entity
{

    public class Player : IEntity
    {
        private MinecraftClient Client;


        public int EntityID { get; }
        public UUID UUID { get; }
        public string Name { get; }

        public Player(string name, UUID uuid)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentNullException(nameof(name));
            UUID = uuid;
        }
        public void Init(MinecraftClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            else if (!client.Connected)
                throw new ArgumentException("client not conntected");
            else if (Client == null)
                Client = client;
        }
        public Task SendMessageAsync(string message)
        {
            ClientChatMessagePacket cmp = new ClientChatMessagePacket(message, Client.ProtocolVersion);
            return Client.SendPacketAsync(cmp);
        }
    }
}
