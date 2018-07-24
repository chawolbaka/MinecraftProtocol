using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace MinecraftProtocol.DataType
{

    //这个类直接抄了https://gist.github.com/csh/2480d14fbbb33b4bbae3里面的(我来写代码啦(C/V).jpg)
    public class PingReply
    {
        [JsonProperty(PropertyName = "version")]
        public VersionPayload Version { get; set; }

        [JsonProperty(PropertyName = "players")]
        public PlayersPayload Players { get; set; }

        //[JsonProperty(PropertyName = "description")]
        public string Motd { get; set; }

        [JsonProperty(PropertyName = "modinfo")]
        public ForgePayLoad ModInfo { get; set; }

        [JsonProperty(PropertyName = "favicon")]
        public string Icon { get; set; }

        /// <summary>
        /// 单位:微秒(Microsecond)
        /// 精度:中等?(不是ICMP的Ping,具体的可以看那部分的代码)
        /// null:校验失败
        /// </summary>
        public long? Time { get; set; }

        public class ForgePayLoad
        {
            [JsonProperty(PropertyName = "type")]
            public string Type { get; set; }

            [JsonProperty(PropertyName = "modList")]
            public List<Forge.ForgeInfo> ModList { get; set; }
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
            public List<MinecraftProtocol.DataType.PingReply.Player> Sample { get; set; }
        }
        public class Player
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
        }
    }
}
