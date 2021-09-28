using System;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Client
{
    /// <summary>
    /// https://wiki.vg/Protocol#Client_Settings
    /// </summary>
    public partial class ClientSettingsPacket : DefinedPacket, IClientSettings
    {
        //1.12-pre3(330): Client Settings (0x05): max length of language was changed from 7 to 16 (see MC-111054)
        //15w31a(49): Added VarInt enum main hand to Client Settings
        //14w03a(6): Client Settings 'show cape' type changed from boolean to unsigned byte
        //14w02a(5): Removed Client Settings' 'Difficulty'

        [PacketProperty]
        private string _locale;
        [PacketProperty]
        private sbyte _viewDistance;
        [PacketProperty]
        private ClientChatMode _chatMode;
        [PacketProperty]
        private bool _chatColors;
        [PacketProperty]
        private DisplayedSkinParts _displayedSkinParts;
        [PacketProperty]
        private MainHand? _mainHandDefine;

        public ClientSettingsPacket(IClientSettings clientSettings, int protocolVersion) : this(
            locale: clientSettings.Locale,
            viewDistance: clientSettings.ViewDistance,
            chatMode: clientSettings.ChatMode,
            chatColors: clientSettings.ChatColors,
            displayedSkinParts: clientSettings.DisplayedSkinParts,
            mainHandDefine: clientSettings.MainHandDefine, protocolVersion) { }

        protected override void CheckProperty()
        {
            base.CheckProperty();
            if (string.IsNullOrWhiteSpace(_locale))
                throw new ArgumentNullException(nameof(_locale));
            if (ProtocolVersion >= ProtocolVersions.V1_12_pre3 && _locale.Length > 16)
                throw new ArgumentOutOfRangeException(nameof(_locale), "max length is 16");
            if (ProtocolVersion < ProtocolVersions.V1_12_pre3 && _locale.Length > 7)
                throw new ArgumentOutOfRangeException(nameof(_locale), "max length is 7");
        }

        protected override void Write()
        {
            WriteString(_locale);
            WriteByte(_viewDistance);
            WriteVarInt((int)_chatMode);
            WriteBoolean(_chatColors);
            if (ProtocolVersion > ProtocolVersions.V14w03a)
            {
                WriteUnsignedByte((byte)_displayedSkinParts);
            }
            else
            {
                if (ProtocolVersion <= ProtocolVersions.V14w02a)
                    WriteByte(0);
                if (ProtocolVersion <= ProtocolVersions.V14w03a)
                    WriteBoolean((_displayedSkinParts & DisplayedSkinParts.Cape) == DisplayedSkinParts.Cape);
            }
            if (ProtocolVersion > ProtocolVersions.V14w03a)
            {
                WriteVarInt((int)_mainHandDefine);
            }
        }

        protected override void Read()
        {
            _locale = Reader.ReadString();
            _viewDistance = Reader.ReadByte();
            _chatMode = (ClientChatMode)Reader.ReadVarInt();
            _chatColors = Reader.ReadBoolean();
            _displayedSkinParts = DisplayedSkinParts.None;

            //14w03a(6): Client Settings 'show cape' type changed from boolean to unsigned byte
            if (ProtocolVersion > ProtocolVersions.V14w03a)
                _displayedSkinParts = (DisplayedSkinParts)Reader.ReadUnsignedByte();
            //15w31a(49): Added VarInt enum main hand to Client Settings
            if (ProtocolVersion > ProtocolVersions.V15w31a)
                _mainHandDefine = (MainHand)Reader.ReadVarInt();
            else
            {
                //14w02a(5): Removed Client Settings' 'Difficulty'
                if (ProtocolVersion <= ProtocolVersions.V14w02a)
                    Reader.ReadByte();
                //14w03a(6): Client Settings 'show cape' type changed from boolean to unsigned byte
                if (ProtocolVersion <= ProtocolVersions.V14w03a)
                    _displayedSkinParts = Reader.ReadBoolean() ? DisplayedSkinParts.Cape : DisplayedSkinParts.None;
            }
        }

        public static int GetPacketId(int protocolVersion)
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

    }
}
