using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType.Chat
{
    public class ChatPayLoad : Styles
    {

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "color")]
        public string Color { get; set; }

        [JsonProperty(PropertyName = "insertion")]
        public string Insertion { get; set; }

        [JsonProperty(PropertyName = "extra")]
        List<ChatPayLoad> Extra { get; set; }
    }
}
