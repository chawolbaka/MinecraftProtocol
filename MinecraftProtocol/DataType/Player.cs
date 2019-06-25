using System;

namespace MinecraftProtocol.DataType
{
    public class Player
    {
        public UUID UUID { get; set; }
        public string Name { get; set; }
        public Player(string name, UUID uuid)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentNullException(nameof(name));
            UUID = uuid;
        }
    }
}
