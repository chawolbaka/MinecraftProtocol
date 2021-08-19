using System;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Protocol#Client_Settings
    /// </summary>
    public class ClientSettingsPacket : Packet, IClientSettings
    {
        //1.12-pre3(330): Client Settings (0x05): max length of language was changed from 7 to 16 (see MC-111054)
        //15w31a(49): Added VarInt enum main hand to Client Settings
        //14w03a(6): Client Settings 'show cape' type changed from boolean to unsigned byte
        //14w02a(5): Removed Client Settings' 'Difficulty'

        public string Locale { get; }
        public sbyte ViewDistance { get; }
        public ClientChatMode ChatMode { get; }
        public bool ChatColors { get; }
        public DisplayedSkinParts DisplayedSkinParts { get; }
        public MainHand? MainHandDefine { get; }

        private ClientSettingsPacket(ReadOnlyPacket packet, IClientSettings clientSettings) : base(packet)
        {
            Locale = clientSettings.Locale;
            ViewDistance = clientSettings.ViewDistance;
            ChatMode = clientSettings.ChatMode;
            ChatColors = clientSettings.ChatColors;
            DisplayedSkinParts = clientSettings.DisplayedSkinParts;
            MainHandDefine = clientSettings.MainHandDefine;
        }
        public ClientSettingsPacket(IClientSettings clientSettings, int protocolVersion):base(GetPacketID(protocolVersion))
        {
            if (string.IsNullOrWhiteSpace(clientSettings.Locale))
                throw new ArgumentNullException(nameof(clientSettings.Locale));
            if (protocolVersion >= ProtocolVersions.V1_12_pre3 && clientSettings.Locale.Length > 16)
                throw new ArgumentOutOfRangeException(nameof(clientSettings.Locale), "max length is 16");
            if (protocolVersion < ProtocolVersions.V1_12_pre3 && clientSettings.Locale.Length > 7)
                throw new ArgumentOutOfRangeException(nameof(clientSettings.Locale), "max length is 7");

            Locale = clientSettings.Locale;
            ViewDistance = clientSettings.ViewDistance;
            ChatMode = clientSettings.ChatMode;
            ChatColors = clientSettings.ChatColors;
            DisplayedSkinParts = clientSettings.DisplayedSkinParts;
            MainHandDefine = clientSettings.MainHandDefine;

            WriteString(Locale);
            WriteByte(ViewDistance);
            WriteVarInt((int)ChatMode);
            WriteBoolean(ChatColors);

            if (protocolVersion > ProtocolVersions.V14w03a)
                WriteUnsignedByte((byte)DisplayedSkinParts);
            else
            {
                if (protocolVersion <= ProtocolVersions.V14w02a)
                    WriteByte(0);
                if (protocolVersion <= ProtocolVersions.V14w03a)
                    WriteBoolean((DisplayedSkinParts & DisplayedSkinParts.Cape) == DisplayedSkinParts.Cape);
            }
            if (protocolVersion > ProtocolVersions.V14w03a)
                WriteVarInt((int)MainHandDefine);
        }

        
        public static int GetPacketID(int protocolVersion)
        {
            /*
             * 1.13-pre7(389)
             * Changed ID of Client Settings from 0x04 to 0x03 (back as it was before 1.13)
             * 17w46a(345)
             * Changed ID of Client Settings from 0x04 to 0x03
             * 17w45a(343)
             * Changed ID of Client Settings from 0x04 to 0x03
             * 17w31a(336)
             * Changed ID of Client Settings from 0x05 to 0x04
             * 1.12-pre5(332)
             * Travesty! Packet ID changes! Except they aren't actually that horrible, as they're actually mainly reverting changes from before:
             * Changed ID of Client Settings from 0x04 to 0x05
             * 17w13a(318)
             * Changed ID of Client Settings changed from 0x04 to 0x05
             * 15w43a(80)
             * Changed ID of Client Settings from 0x03 to 0x04
             * 15w36a(67)
             * Changed ID of Client Settings from 0x16 to 0x03
             * 15w31a(49)
             * Changed ID of Client Settings from 0x15 to 0x16
             */
            if (protocolVersion >= ProtocolVersions.V1_14)      return 0x05;
            if (protocolVersion >= ProtocolVersions.V17w45a)    return 0x03;
            if (protocolVersion >= ProtocolVersions.V17w31a)    return 0x04;
            if (protocolVersion >= ProtocolVersions.V1_12_pre5) return 0x05;
            if (protocolVersion >= ProtocolVersions.V15w43a)    return 0x04;
            if (protocolVersion >= ProtocolVersions.V15w36a)    return 0x03;
            if (protocolVersion >= ProtocolVersions.V15w31a)    return 0x16;
            else return 0x15;
            
        }
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion) => Verify(packet, protocolVersion, out _);
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, out ClientSettingsPacket csp)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));
            if (protocolVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(protocolVersion), "协议版本不能使用负数");
            csp = null;

            if (packet.ID != GetPacketID(protocolVersion))
                return false;
            try
            {       
                string Locale = packet.ReadString();
                sbyte ViewDistance = packet.ReadByte();
                ClientChatMode ChatMode = (ClientChatMode)packet.ReadVarInt();
                bool ChatColors = packet.ReadBoolean();
                DisplayedSkinParts DisplayedSkinParts = DisplayedSkinParts.None;
                MainHand? MainHandDefine = null;

                //14w03a(6): Client Settings 'show cape' type changed from boolean to unsigned byte
                if (protocolVersion > ProtocolVersions.V14w03a)
                    DisplayedSkinParts = (DisplayedSkinParts)packet.ReadUnsignedByte();
                //15w31a(49): Added VarInt enum main hand to Client Settings
                if (protocolVersion > ProtocolVersions.V15w31a)
                    MainHandDefine = (MainHand)packet.ReadVarInt();
                else
                {
                    //14w02a(5): Removed Client Settings' 'Difficulty'
                    if (protocolVersion <= ProtocolVersions.V14w02a)
                        packet.ReadByte();
                    //14w03a(6): Client Settings 'show cape' type changed from boolean to unsigned byte
                    if (protocolVersion <= ProtocolVersions.V14w03a)
                        DisplayedSkinParts = packet.ReadBoolean() ? DisplayedSkinParts.Cape : DisplayedSkinParts.None;
                }

                if (packet.IsReadToEnd)
                    csp = new ClientSettingsPacket(packet, new ClientSettings(Locale, ViewDistance, ChatMode, ChatColors, DisplayedSkinParts, MainHandDefine));
                return !(csp is null);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (InvalidCastException) { return false; }
            catch (OverflowException) { return false; }
        }
    }
}
