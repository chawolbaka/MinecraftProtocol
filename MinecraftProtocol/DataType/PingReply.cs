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
        public PlayersPayload Player { get; set; }

        [JsonIgnore]
        public Description Motd { get; set; }

        [JsonProperty(PropertyName = "modinfo")]
        public ForgePayLoad Forge { get; set; }

        [JsonProperty(PropertyName = "favicon")]
        public string Icon { get; set; }

        [JsonIgnore]
        public long? ElapsedMicroseconds { get; set; }

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
        public class Description
        {
            public string Text { get; set; }
            public List<ExtraPayload> Extra { get; set; }
            
            public override string ToString()
            {
                StringBuilder motd = new StringBuilder();
                motd.Append(Text);
                if (Extra!=null&&Extra.Count>0)
                {
                    foreach (var item in Extra)
                    {
                        if (item.Strikethrough)
                            motd.Append("§m");
                        if (item.Bold)
                            motd.Append("§l");
                        //还有个颜色代码我懒的处理了
                        motd.Append(item.Text);
                    }
                 }
                return motd.ToString();
            }
        }
        public class PlayerSample
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
        }
    }
}
