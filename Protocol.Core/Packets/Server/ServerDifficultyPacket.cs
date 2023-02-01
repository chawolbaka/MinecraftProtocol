using System;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Compatible;

namespace MinecraftProtocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Protocol#Server_Difficulty
    /// </summary>
    public partial class ServerDifficultyPacket : DefinedPacket
    {
        [PacketProperty]
        internal Difficulty _difficulty;

        [PacketProperty]
        internal bool _locked;

        protected override void CheckProperty()
        {
            base.CheckProperty();
            //14w02a(5): Added Server Difficulty and Removed Client Settings' 'Difficulty'
            if (ProtocolVersion < ProtocolVersions.V14w02a)
                throw new PacketNotFoundException($"{nameof(ServerDifficultyPacket)} 该版本不存在{nameof(ServerDifficultyPacket)}，至少需要14w02a。", this);
            if (Locked && ProtocolVersion < ProtocolVersions.V1_14)
                throw new NotSupportedException($"至少需要1.14才可以在{nameof(ServerDifficultyPacket)}中设置难度锁定");
        }

        protected override void Write()
        {
            WriteUnsignedByte((byte)_difficulty);
            if (ProtocolVersion >= ProtocolVersions.V1_14 && ProtocolVersion < ProtocolVersions.V1_19_1)
                WriteBoolean(Locked);
        }

        protected override void Read()
        {
            _difficulty = new Difficulty(Reader.ReadUnsignedByte());
            _locked = ProtocolVersion >= ProtocolVersions.V1_14 && ProtocolVersion < ProtocolVersions.V1_19_1 ? Reader.ReadBoolean() : false;
        }

        public static int GetPacketId(int protocolVersion)
        {
            /*
             * 1.12-pre5(332)
             * Changed ID of Server Difficulty from 0x0E to 0x0D
             * 17w13a(318)
             * Changed ID of Server Difficulty from 0x0D to 0x0E
             * 15w36a(67)
             * Changed ID of Server Difficulty from 0x41 to 0x0D
             */
            if (protocolVersion >= ProtocolVersions.V1_19_1)    return 0x02;
            if (protocolVersion >= ProtocolVersions.V1_19)      return 0x0B;
            if (protocolVersion >= ProtocolVersions.V1_17)      return 0x0E;
            if (protocolVersion >= ProtocolVersions.V1_16)      return 0x0D;
            if (protocolVersion >= ProtocolVersions.V1_15)      return 0x0E;
            if (protocolVersion >= ProtocolVersions.V1_12_pre5) return 0x0D;
            if (protocolVersion >= ProtocolVersions.V1_12_pre5) return 0x0D;
            if (protocolVersion >= ProtocolVersions.V17w13a)    return 0x0E;
            if (protocolVersion >= ProtocolVersions.V15w36a)    return 0x0D;
            else                                                return 0x41;   
        }

    }
}
