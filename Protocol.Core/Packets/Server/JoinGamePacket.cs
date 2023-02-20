using System;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.IO.NBT.Tags;

namespace MinecraftProtocol.Packets.Server
{
    public partial class JoinGamePacket : DefinedPacket
    {
        [PacketProperty]
        private int _entityId;
        
        [PacketProperty]
        private GameMode _gamemode;

        private byte _rawGamemode;

        [PacketProperty]
        private bool _hardCore;

        [PacketProperty]
        private byte _reviousGamemode; //1.16加入

        [PacketProperty]
        private Identifier[] _worldNames; //1.16加入

        [PacketProperty]
        private CompoundTag _dimensionCodec; //1.16加入

        [PacketProperty] 
        private object _dimension;

        [PacketProperty]
        private CompoundTag _registryCodec; //1.19加入

        [PacketProperty]
        private Identifier _worldName; //1.16加入

        [PacketProperty]
        private long _hashedSeed; //1.15加入

        [PacketProperty]
        private Difficulty _difficulty; //1.14移除
        
        [PacketProperty]
        private int _maxPlayers; //用于绘制Tab,最大只有255

        [PacketProperty]
        private string _levelType; //1.16移除

        [PacketProperty]
        private int _viewDistance; //1.14加入

        [PacketProperty]
        private int _simulationDistance; //1.18.1加入

        [PacketProperty]
        private bool _reducedDebugInfo; //1.8加入

        [PacketProperty]
        private bool _enableRespawnScreen; //1.15加入

        [PacketProperty]
        private bool _isDebug; //1.16加入

        [PacketProperty]
        private bool _isFlat; //1.16加入

        [PacketProperty]
        private bool _hasDeathLocation; //1.19加入

        [PacketProperty]
        private Identifier _deathDimensionName; //1.19加入

        [PacketProperty]
        private Position _deathLocation; //1.19加入

        //protected override void CheckProperty()
        //{
        //    base.CheckProperty();
        //    //根据版本来检测太麻烦了QAQ，让我偷个懒！
        //}
        protected override void Write()
        {
            WriteInt(_entityId);

            if (ProtocolVersion >= ProtocolVersions.V1_16_2)
                WriteBoolean(_hardCore);
            WriteUnsignedByte(_rawGamemode);
            if (ProtocolVersion >= ProtocolVersions.V1_16)
            {
                WriteUnsignedByte(_reviousGamemode);
                WriteIdentifierArray(new ReadOnlySpan<Identifier>(_worldNames));
                if (ProtocolVersion >= ProtocolVersions.V1_19)
                    WriteNBT(_registryCodec);
                else
                    WriteNBT(_dimensionCodec);
            }

            if (ProtocolVersion >= ProtocolVersions.V1_19)
                WriteString((string)_dimension);
            else if (ProtocolVersion >= ProtocolVersions.V1_16_2)
                WriteNBT((NBTTag)_dimension);
            else if (ProtocolVersion >= ProtocolVersions.V1_16)
                WriteIdentifier((Identifier)_dimension);
            else if (ProtocolVersion >= ProtocolVersions.V1_9_1_pre1)
                WriteInt((int)_dimension);
            else
                WriteByte((sbyte)_dimension);

            if (ProtocolVersion <= ProtocolVersions.V1_13_2)
                WriteUnsignedByte((byte)_difficulty);


            if (ProtocolVersion >= ProtocolVersions.V1_16)
                WriteIdentifier(_worldName);
            if (ProtocolVersion >= ProtocolVersions.V1_15)
                WriteLong(_hashedSeed);
            if (ProtocolVersion >= ProtocolVersions.V1_16_2)
                WriteVarInt(_maxPlayers);
            else
                WriteUnsignedByte((byte)_maxPlayers);


            if (ProtocolVersion < ProtocolVersions.V1_16)
                WriteString(_levelType);
            if (ProtocolVersion >= ProtocolVersions.V1_14)
                WriteVarInt(_viewDistance);
            if (ProtocolVersion >= ProtocolVersions.V1_18_1)
                WriteVarInt(_simulationDistance);
            if (ProtocolVersion >= ProtocolVersions.V1_8)
                WriteBoolean(_reducedDebugInfo);
            if (ProtocolVersion >= ProtocolVersions.V1_15)
                WriteBoolean(_enableRespawnScreen);
            if (ProtocolVersion >= ProtocolVersions.V1_16)
            {
                WriteBoolean(_isDebug);
                WriteBoolean(_isFlat);
            }
            if (ProtocolVersion >= ProtocolVersions.V1_19)
            {
                WriteBoolean(_hasDeathLocation);
                if (_hasDeathLocation)
                {
                    WriteIdentifier(_deathDimensionName);
                    WritePosition(_deathLocation);
                }
            }
        }

        protected override void Read(ref CompatibleByteReader reader)
        {
            _entityId = reader.ReadInt();
            if (ProtocolVersion >= ProtocolVersions.V1_16_2)
                _hardCore = reader.ReadBoolean();
            _rawGamemode = reader.ReadUnsignedByte();
            if (ProtocolVersion >= ProtocolVersions.V1_16)
            {
                _reviousGamemode = reader.ReadUnsignedByte();
                _worldNames = reader.ReadIdentifierArray();
                if (ProtocolVersion >= ProtocolVersions.V1_19)
                    _registryCodec = reader.ReadNBT();
                else
                    _dimensionCodec = reader.ReadNBT();
            }

            if (ProtocolVersion >= ProtocolVersions.V1_19)
                _dimension = reader.ReadString();
            else if (ProtocolVersion >= ProtocolVersions.V1_16_2)
                _dimension = reader.ReadNBT();
            else if (ProtocolVersion >= ProtocolVersions.V1_16)
                _dimension = reader.ReadIdentifier();
            else if (ProtocolVersion >= ProtocolVersions.V1_9_1_pre1)
                _dimension = reader.ReadInt();
            else
                _dimension = reader.ReadByte();

            _difficulty = ProtocolVersion <= ProtocolVersions.V1_13_2 ? new Difficulty(reader.ReadUnsignedByte()) : Difficulty.Unknown;
            if (ProtocolVersion >= ProtocolVersions.V1_16)
                _worldName = reader.ReadIdentifier();            
            if (ProtocolVersion >= ProtocolVersions.V1_15)
                _hashedSeed = reader.ReadLong();
            if (ProtocolVersion >= ProtocolVersions.V1_16_2)
                _maxPlayers = reader.ReadVarInt();
            else
                _maxPlayers = reader.ReadUnsignedByte();

            if (ProtocolVersion < ProtocolVersions.V1_16)
                _levelType = reader.ReadString();
            if (ProtocolVersion >= ProtocolVersions.V1_14)
                _viewDistance = reader.ReadVarInt();
            if (ProtocolVersion >= ProtocolVersions.V1_18_1)
                _simulationDistance = reader.ReadVarInt();
            if (ProtocolVersion >= ProtocolVersions.V1_8)
                _reducedDebugInfo = reader.ReadBoolean();
            if (ProtocolVersion >= ProtocolVersions.V1_15)
                _enableRespawnScreen = reader.ReadBoolean();
            if (ProtocolVersion >= ProtocolVersions.V1_16)
            {
                _isDebug = reader.ReadBoolean();
                _isFlat = reader.ReadBoolean();
            }
            if(ProtocolVersion >= ProtocolVersions.V1_19)
            {
                _hasDeathLocation = reader.ReadBoolean();
                if (_hasDeathLocation)
                {
                    _deathDimensionName = reader.ReadIdentifier();
                    _deathLocation = reader.ReadPosition();
                }
            }

            _gamemode = (GameMode)(_rawGamemode & 0xF7);
            if (ProtocolVersion < ProtocolVersions.V1_16_2)
                _hardCore = (_rawGamemode & 0x8) == 0x8;
        }

        public static int GetPacketId(int protocolVersion)
        {
            /*
             * 1.13-pre7: Changed ID of Join Game from 0x24 to 0x25
             * 17w46a(345): Changed ID of Join Game from 0x23 to 0x24 ???
             * 17w13a(318): Changed ID of Join Game changed from 0x23 to 0x24 ???
             * 1.9.1-pre2, 1.9.1-pre3, 1.9.1(108)
             * Changed dimension in Join Game from a byte enum to an int enum (not VarInt), to the benefit of Minecraft Forge
             * 15w46a(86)
             * Changed ID of Join Game from 0x24 to 0x23(可能是0x01 to 0x23?)
             */

            if (protocolVersion >= ProtocolVersions.V1_19_3)    return 0x24;
            if (protocolVersion >= ProtocolVersions.V1_19_1)    return 0x25;
            if (protocolVersion >= ProtocolVersions.V1_19)      return 0x23;
            if (protocolVersion >= ProtocolVersions.V1_17)      return 0x26;
            if (protocolVersion >= ProtocolVersions.V20w28a)    return 0x24;
            if (protocolVersion >= ProtocolVersions.V1_16)      return 0x25;
            if (protocolVersion >= ProtocolVersions.V1_15)      return 0x26;
            if (protocolVersion >= ProtocolVersions.V1_13_pre7) return 0x25;
            if (protocolVersion >= ProtocolVersions.V17w46a)    return 0x24;
            if (protocolVersion >= ProtocolVersions.V15w46a)    return 0x23;
            else                                                return 0x01;
        }
    }
}
