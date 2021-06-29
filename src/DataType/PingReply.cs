using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.DataType.Chat;

namespace MinecraftProtocol.DataType
{

    //这个类直接抄了https://gist.github.com/csh/2480d14fbbb33b4bbae3里面的(我来写代码啦(C/V).jpg)
    public class PingReply
    {
        [JsonProperty(PropertyName = "version", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public VersionPayload Version { get; set; }

        [JsonProperty(PropertyName = "players", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PlayersPayload Player { get; set; }

        [JsonProperty(PropertyName = "description", DefaultValueHandling = DefaultValueHandling.Ignore)]
        internal ChatMessage SerializeMotd => Motd; //这个因为类型不确定，没办法直接被反序列化，通过这样的操作可以仅允许他被序列化

        [JsonIgnore]
        public ChatMessage Motd { get; set; }

        [JsonProperty(PropertyName = "modinfo", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ForgePayLoad Forge { get; set; }

        [JsonProperty(PropertyName = "favicon", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Icon { get; set; }

        [JsonIgnore]
        public TimeSpan? Elapsed { get; set; }

        [JsonIgnore]
        public string Json { get; set; }

        public class ForgePayLoad
        {
            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }

            [JsonProperty(PropertyName = "modList")]
            public List<ModInfo> ModList { get; set; }

        }
        public class VersionPayload
        {
            [JsonProperty(PropertyName = "protocol")]
            public int Protocol { get; set; }

            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
        }
        public class PlayersPayload
        {
            [JsonProperty(PropertyName = "max")]
            public int Max { get; set; }

            [JsonProperty(PropertyName = "online")]
            public int Online { get; set; }

            /// <summary>
            /// 玩家列表的样品,最大数量是12(随机12个)
            /// </summary>
            [JsonProperty(PropertyName = "sample")]
            public List<PlayerSample> Samples { get; set; }
        }

        /// <summary>
        /// 这个类随时会被我丢到其它地方的或者改名
        /// </summary>
        public class ExtraPayload
        {
            public string Color { get; set; }
            public string Text { get; set; }
            public bool Strikethrough { get; set; }
            public bool Bold { get; set; }
        }
        public class PlayerSample
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
        }

        public override string ToString()
        {
            return Json;
        }
    }
}
