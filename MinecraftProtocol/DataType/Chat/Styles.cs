using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType.Chat
{
    public class Styles
    {
        /// <summary>
        /// 随机字符
        /// </summary>
        [JsonProperty(PropertyName = "obfuscated")]
        public bool Random { get; set; } = false;
        /// <summary>
        /// 加粗
        /// </summary>
        [JsonProperty(PropertyName = "bold")]
        public bool Bold { get; set; } = false;
        /// <summary>
        /// 删除线
        /// </summary>
        [JsonProperty(PropertyName = "strikethrough")]
        public bool Strikethrough { get; set; } = false;
        /// <summary>
        /// 下划线
        /// </summary>
        [JsonProperty(PropertyName = "underlined")]
        public bool Underlined { get; set; } = false;
        /// <summary>
        /// 斜体
        /// </summary>
        [JsonProperty(PropertyName = "italic")]
        public bool Italic { get; set; } = false;
        /// <summary>
        /// 重置文字样式
        /// </summary>
        [JsonProperty(PropertyName = "reset")]
        public bool PlainWhite { get; set; } = false;
    }
}
