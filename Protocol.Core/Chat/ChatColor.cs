using System;
using System.Collections.Generic;

namespace MinecraftProtocol.Chat
{
    public struct ChatColor : IEquatable<ChatColor>
    {
        public readonly byte Code;

        /// <summary>
        /// 创建一个<c>ChatColor</c>
        /// </summary>
        /// <param name="code">0-16</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public ChatColor(byte code)
        {
            if (code > 0xf)
                throw new ArgumentOutOfRangeException(nameof(code));
            Code = code;

        }

        /// <summary>黑色</summary>
        public static readonly ChatColor Black = new ChatColor(0x0);
        /// <summary>深蓝色</summary>
        public static readonly ChatColor DarkBlue = new ChatColor(0x1);
        /// <summary>深绿色</summary>
        public static readonly ChatColor DarkGreen = new ChatColor(0x2);
        /// <summary>湖蓝色</summary>
        public static readonly ChatColor DarkAqua = new ChatColor(0x3);
        /// <summary>深红色</summary>
        public static readonly ChatColor DarkRed = new ChatColor(0x4);
        /// <summary>紫色</summary>
        public static readonly ChatColor DarkPurple = new ChatColor(0x5);
        /// <summary>金色</summary>
        public static readonly ChatColor Gold = new ChatColor(0x6);
        /// <summary>灰色</summary>
        public static readonly ChatColor Gray = new ChatColor(0x7);
        /// <summary>深绿色</summary>
        public static readonly ChatColor DarkGray = new ChatColor(0x8);
        /// <summary>蓝色</summary>
        public static readonly ChatColor Blue = new ChatColor(0x9);
        /// <summary>绿色</summary>
        public static readonly ChatColor Green = new ChatColor(0xa);
        /// <summary>天蓝色</summary>
        public static readonly ChatColor Aqua = new ChatColor(0xb);
        /// <summary>红色</summary>
        public static readonly ChatColor Red = new ChatColor(0xc);
        /// <summary>粉红色</summary>
        public static readonly ChatColor LightPurple = new ChatColor(0xd);
        /// <summary>黄色</summary>
        public static readonly ChatColor Yellow = new ChatColor(0xe);
        /// <summary>白色</summary>
        public static readonly ChatColor White = new ChatColor(0Xf);


        public override bool Equals(object obj) => obj is ChatColor color && Equals(color);

        public static bool operator ==(ChatColor left, ChatColor right) => left.Equals(right);
        public static bool operator !=(ChatColor left, ChatColor right) => !(left == right);
        public bool Equals(ChatColor other) => Code == other.Code;
        public override int GetHashCode() => Code;

        public static ChatColor Parse(string name) => new ChatColor(ColorNames[name]);
        public static bool TryParse(string name, out ChatColor? color)
        {
            color = null;
            if (ColorNames.ContainsKey(name))
                color = new ChatColor(ColorNames[name]);
            return color != null;
        }

        public override string ToString() =>
            Code switch
            {
                0x0 => "black",
                0x1 => "dark_blue",
                0x2 => "dark_green",
                0x3 => "dark_aqua",
                0x4 => "dark_red",
                0x5 => "dark_purple",
                0x6 => "gold",
                0x7 => "gray",
                0x8 => "dark_gray",
                0x9 => "blue",
                0xa => "green",
                0xb => "aqua",
                0xc => "red",
                0xd => "light_purple",
                0xe => "yellow",
                0xf => "white",
                _ => throw new InvalidCastException($"unknown color code {Code:x}")
            };

        public int GetColorHex()
        {
            if (ForegroundColorHex.ContainsKey(Code))
                return ForegroundColorHex[Code];
            else
                throw new InvalidCastException($"unknown color code {Code:x}");
        }

        private static readonly Dictionary<byte, int> ForegroundColorHex = new Dictionary<byte, int>() {
            { 0x0, 0x000000 },
            { 0x1, 0x0000AA },
            { 0x2, 0x00AA00 },
            { 0x3, 0x00AAAA },
            { 0x4, 0xAA0000 },
            { 0x5, 0xAA00AA },
            { 0x6, 0xFFAA00 },
            { 0x7, 0xAAAAAA },
            { 0x8, 0x555555 },
            { 0x9, 0x5555FF },
            { 0xa, 0x55FF55 },
            { 0xb, 0x55FFFF },
            { 0xc, 0xFF5555 },
            { 0xd, 0xFF55FF },
            { 0xe, 0xFFFF55 },
            { 0xf, 0xFFFFFF },
        };
        private static readonly Dictionary<string, byte> ColorNames = new Dictionary<string, byte>()
        {
            { "black",          0x0 },
            { "dark_blue",      0x1 },
            { "dark_green",     0x2 },
            { "dark_aqua",      0x3 },
            { "dark_red",       0x4 },
            { "dark_purple",    0x5 },
            { "gold",           0x6 },
            { "gray",           0x7 },
            { "dark_gray",      0x8 },
            { "blue",           0x9 },
            { "green",          0xa },
            { "aqua",           0xb },
            { "red",            0xc },
            { "light_purple",   0xd },
            { "yellow",         0xe },
            { "white",          0xf }
        };
    }
}
