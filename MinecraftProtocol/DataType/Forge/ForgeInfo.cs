using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MinecraftProtocol.DataType.Forge
{
    public class ForgeInfo
    {
        [JsonProperty(PropertyName = "modid")]
        public string ModID { get; set; }
        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        public override string ToString()
        {
            return $"{this.ModID} {this.Version}";
        }
    }
}
