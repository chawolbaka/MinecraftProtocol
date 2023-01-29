using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.Chat;

namespace MinecraftProtocol.DataType
{

    //这个类直接抄了https://gist.github.com/csh/2480d14fbbb33b4bbae3里面的(我来写代码啦(C/V).jpg)
    public class PingReply
    {
        [JsonPropertyName("version"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public VersionPayload Version { get; set; }

        [JsonPropertyName("players"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public PlayersPayload Player { get; set; }

        [JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonConverter(typeof(ChatComponentConverter))]
        public ChatComponent Motd { get; set; }

        [JsonPropertyName("modinfo"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ForgePayLoad Forge { get; set; }

        [JsonPropertyName("favicon"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Icon { get; set; }

        [JsonIgnore]
        public TimeSpan? Elapsed { get; set; }

        [JsonIgnore]
        public string Json { get; set; }

        public class ForgePayLoad
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("modList")]
            public List<ModInfo> ModList { get; set; }

        }
        public class VersionPayload
        {
            [JsonPropertyName("protocol")]
            public int Protocol { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }
        }
        public class PlayersPayload
        {
            [JsonPropertyName("max")]
            public int Max { get; set; }

            [JsonPropertyName("online")]
            public int Online { get; set; }

            /// <summary>
            /// 玩家列表的样品,最大数量是12(随机12个)
            /// </summary>
            [JsonPropertyName("sample")]
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
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }
        }

        public override string ToString()
        {
            return Json;
        }
    }
}
