using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftProtocol.DataType
{
    public class ClientSettings : IClientSettings, IEquatable<ClientSettings>
    {
        /// <summary>按照原版1.12.2首次启动的配置写的</summary>
        public static ClientSettings Default { get; } = new ClientSettings("e.g. en_GB", 12, ClientChatMode.Full, true, DisplayedSkinParts.All, MainHand.Right);
        /// <summary>只是语言字段的长度不一样，1.12-pre3后最大长度从7变到了16。</summary>
        public static ClientSettings LegacyDefault { get; } = new ClientSettings("en_GB", 12, ClientChatMode.Full, true, DisplayedSkinParts.All, MainHand.Right);

        public string Locale { get; }
        public sbyte ViewDistance { get; }
        public ClientChatMode ChatMode { get; }
        public bool ChatColors { get; }
        public DisplayedSkinParts DisplayedSkinParts { get; }
        public MainHand? MainHandDefine { get; }
        internal ClientSettings() { }

        public ClientSettings(string locale, sbyte viewDistance, ClientChatMode chatMode, bool chatColors, DisplayedSkinParts displayedSkinParts, MainHand? mainHandDefine)
        {
            Locale = locale ?? throw new ArgumentNullException(nameof(locale));
            ViewDistance = viewDistance;
            ChatMode = chatMode;
            ChatColors = chatColors;
            DisplayedSkinParts = displayedSkinParts;
            MainHandDefine = mainHandDefine;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ClientSettings);
        }

        public bool Equals(ClientSettings other)
        {
            return other != null &&
                   Locale == other.Locale &&
                   ViewDistance == other.ViewDistance &&
                   ChatMode == other.ChatMode &&
                   ChatColors == other.ChatColors &&
                   DisplayedSkinParts == other.DisplayedSkinParts &&
                   EqualityComparer<MainHand?>.Default.Equals(MainHandDefine, other.MainHandDefine);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Locale, ViewDistance, ChatMode, ChatColors, DisplayedSkinParts, MainHandDefine);
        }
    }
}
