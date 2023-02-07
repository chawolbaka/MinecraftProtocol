using System;
using System.Threading.Tasks;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Client;
using MinecraftProtocol.Compatible;

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
            return Client.SendPacketAsync(Client.BuildChatMessage(message));
        }
    }
}