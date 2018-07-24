using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MinecraftProtocol;
using Newtonsoft.Json;


namespace MinecraftProtocol.DataType
{
    public class Chat
    {
        public string Translate { get; set; }
        public string Text { get; set; }
        public ChatColors? Color { get; set; }
        
        public ChatStyles styles { get; set; } = new ChatStyles();
        public ClickEvet clickEvet { get; set; } = new ClickEvet();

        public class ClickEvet
        {
            public string open_url;
            public string open_file;
            public string run_commmand;
        }
            

        public enum ChatColors
        {
            //名称来源:http://wiki.vg/Chat
            //时间:2018-6-22
            //取了表格里面的Common Name字段
            //上面还写着Old system 有点不理解,这东西还变过的吗
            Black = 0x0,
            DarkBlue = 0x1,
            DarkGreen = 0x2,
            DarkCyan = 0x3,
            DarkRed = 0x4,
            Purple = 0x5,
            Gold = 0x6,
            Gray = 0x7,
            DarkGray = 0x8,
            Blue = 0x9,
            BrightGreen=0xA,
            Cyan = 0xB,
            Red = 0xC,
            Pink = 0xD,
            Yellow = 0xE,
            White = 0xF,
            Unknown = -0xFFFFFF
        }
        public class ChatStyles
        {
            /// <summary>
            /// 随机字符
            /// </summary>
            public bool Random { get; set; } = false;
            /// <summary>
            /// 加粗
            /// </summary>
            public bool Bold { get; set; } = false;
            /// <summary>
            /// 删除线
            /// </summary>
            public bool Strikethrough { get; set; } = false;
            /// <summary>
            /// 下划线
            /// </summary>
            public bool Underlined { get; set; } = false;
            /// <summary>
            /// 斜体
            /// </summary>
            public bool Italic { get; set; } = false;
            /// <summary>
            /// 重置文字样式
            /// </summary>
            public bool PlainWhite { get; set; } = false;
        }
        public Chat()
        {

        }
        public Chat(List<byte> json)
        {
            string FileName = $"{DateTime.Now.ToString("yyyy-MM-dd.hhmmssfff")}.json";
            StreamWriter sw = File.AppendText(FileName);
            sw.WriteLine(Encoding.UTF8.GetString(json.ToArray()));
            sw.Flush();
            sw.Close();

            //if (!string.IsNullOrWhiteSpace(jsonText) && jsonText.StartsWith("{") && jsonText.EndsWith("}"))
            //{
            //    Json.JsonData jsonData = Json.ParseJson(jsonText);
            //    if (jsonData.Properties.ContainsKey("extra"))
            //    {
            //        foreach (var extra in jsonData.Properties["extra"].DataArray)
            //        {
            //            Chat tmp = new Chat();
            //            if (extra.Properties.ContainsKey("text"))
            //                tmp.Text = extra.Properties["text"].StringValue;
            //            if (extra.Properties.ContainsKey("color"))
            //                tmp.Color = GetChatColor(extra.Properties["color"].StringValue);
            //            Message.Add(tmp);
            //        }
            //    }

            //}
        }
        public static ChatColors GetChatColor(string value)
        {
            switch (value)
            {
                case "black":
                    return ChatColors.Black;
                case "dark_blue":
                    return ChatColors.DarkBlue;
                case "dark_green":
                    return ChatColors.DarkGreen;
                case "dark_aqua":
                    return ChatColors.DarkCyan;
                case "dark_red":
                    return ChatColors.DarkRed;
                case "dark_purple":
                    return ChatColors.Purple;
                case "gold":
                    return ChatColors.Gold;
                case "gray":
                    return ChatColors.Gray;
                case "dark_gray":
                    return ChatColors.DarkGray;
                case "blue":
                    return ChatColors.Blue;
                case "green":
                    return ChatColors.BrightGreen;
                case "aqua":
                    return ChatColors.Cyan;
                case "red":
                    return ChatColors.Red;
                case "light_purple":
                    return ChatColors.Pink;
                case "yellow":
                    return ChatColors.Yellow;
                case "white":
                    return ChatColors.White;
                default:
                    return ChatColors.Unknown;
            }
        }
        public static ChatColors GetChatColor(int value)
        {
            throw new NotImplementedException("懒的写数字转颜色枚举了");
        }
        public override string ToString()
        {
            //这里要把所有消息合并(抛掉样式代码
            return base.ToString();
        }
    }
}
