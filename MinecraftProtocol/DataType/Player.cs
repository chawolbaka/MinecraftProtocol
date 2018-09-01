using System;
using System.Net;
using System.Text;
using System.Collections.Generic;

namespace MinecraftProtocol.DataType
{
    public class Player
    { 
        public Guid ID { get; set; }
        public string Name { get; set; }
        public Player(string name,Guid uuid)
        {
            Name = !string.IsNullOrWhiteSpace(name)?name:throw new ArgumentNullException(nameof(name));
            ID = uuid != null?uuid:throw new ArgumentNullException(nameof(uuid));
        }
    }
}
