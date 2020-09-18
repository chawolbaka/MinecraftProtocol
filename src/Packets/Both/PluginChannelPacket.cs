using System;
using System.Text;
using System.Collections.Generic;
using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.Compression;
using MinecraftProtocol.Compatible;
using MinecraftProtocol.IO.Extensions;

namespace MinecraftProtocol.Packets.Both
{
    public class PluginChannelPacket : Packet
    {
        public virtual string Channel { get; set; }
        public virtual byte[] Data { get; set; }
        protected int _protocolVersion;
        protected bool HasForge;


        private PluginChannelPacket(ReadOnlyPacket packet, int protocolVersion , bool hasForge,string channel) : base(packet)
        {
            Channel = channel;
            HasForge = hasForge;
            _protocolVersion = protocolVersion;
        }

        public PluginChannelPacket(string channel, IForgeStructure structure, int protocolVersion, Bound bound, bool hasForge = true) :
            this(channel, structure.ToBytes(), protocolVersion, bound, hasForge) { }

        public PluginChannelPacket(string channel, byte[] data, int protocolVersion, Bound bound, bool hasForge = false) : base(GetPacketID(protocolVersion, bound), VarInt.GetLength(channel.Length) + channel.Length + data.Length)
        {
            if (string.IsNullOrEmpty(channel))
                throw new ArgumentNullException(nameof(channel));

            Channel = channel;
            Data = data;
            HasForge = hasForge;
            _protocolVersion = protocolVersion;

            WriteString(channel);
            if (_protocolVersion <= ProtocolVersionNumbers.V14w31a)
            {
                if (HasForge)
                    WriteVarShort(Data.Length).WriteBytes(Data);
                else
                    WriteShort((short)Data.Length).WriteBytes(Data);
            }
            else
                WriteBytes(Data);
        }
        //public override byte[] ToBytes(int compress = -1)
        //{
        //    byte[] channel = Encoding.UTF8.GetBytes(Channel);
        //    byte[] PacketData;
        //    //14w31a: The length prefix on the payload of Plugin Message has been removed (Both ways)
        //    if (_protocolVersion <= ProtocolVersionNumbers.V14w31a)
        //    {
        //        if (HasForge)
        //            PacketData = ProtocolHandler.ConcatBytes(VarInt.GetBytes(channel.Length), channel, VarShort.GetBytes(Data.Count), Data.ToArray());
        //        else
        //            PacketData = ProtocolHandler.ConcatBytes(VarInt.GetBytes(channel.Length), channel, ProtocolHandler.GetBytes((short)Data.Count), Data.ToArray());
        //    }
        //    else
        //        PacketData = ProtocolHandler.ConcatBytes(VarInt.GetBytes(channel.Length), channel, Data.ToArray());
        //    int DataLength = PacketData.Length;

        //    PacketData = ProtocolHandler.ConcatBytes(VarInt.GetBytes(ID), PacketData);

        //    if (compress > 0)
        //    {
        //        if (DataLength >= compress)
        //            PacketData = ProtocolHandler.ConcatBytes(VarInt.GetBytes(PacketData.Length), ZlibUtils.Compress(PacketData));
        //        else
        //            PacketData = ProtocolHandler.ConcatBytes(new byte[] { 0 }, PacketData);
        //    }
        //    return ProtocolHandler.ConcatBytes(VarInt.GetBytes(PacketData.Length), PacketData);
        //}

        public static int GetPacketID(int protocolVersion, Bound bound)
        {
            //17w45a(343): Changed ID of Plugin Message (serverbound) from 0x09 to 0x08 (这条有点奇怪,直接不算进去吧)
            //14w31a: The length prefix on the payload of Plugin Message has been removed (Both ways)
            //14w17a: Increased the max payload size of 'Plugin Message' from 32767 to 1048576 (Broken because of incorrect data type)
            if (bound == Bound.Client)
            {
                /*
                 * 1.13-pre7(389)
                 * Changed ID of Plugin message (serverbound) from 0x09 to 0x0A
                 * 17w31a(336)
                 * Changed ID of Plugin Message (serverbound) from 0x0A to 0x09
                 * 1.12-pre5(332)
                 * Changed ID of Plugin Message (serverbound) from 0x09 to 0x0A
                 * 15w43a(80)
                 * Changed ID of Plugin Message (serverbound) from 0x08 to 0x09
                 * 15w36a(67)
                 * Changed ID of Plugin Message (serverbound) from 0x18 to 0x08
                 * 15w31a(49)
                 * Changed ID of Plugin Message (serverbound) from 0x17 to 0x18
                 */

                if (protocolVersion >= ProtocolVersionNumbers.V1_14)         return 0x0B;
                if (protocolVersion >= ProtocolVersionNumbers.V1_13_pre7)    return 0x0A;
                if (protocolVersion >= ProtocolVersionNumbers.V17w31a)       return 0x09;
                if (protocolVersion >= ProtocolVersionNumbers.V1_12_pre5)    return 0x0A;
                if (protocolVersion >= ProtocolVersionNumbers.V15w43a)       return 0x08;
                if (protocolVersion >= ProtocolVersionNumbers.V15w36a)       return 0x18;
                else                                                         return 0x17;
            }
            else if(bound == Bound.Server)
            {
                /* 
                 * 17w46a(345)
                 * Changed ID of Plugin Message (clientbound) from 0x18 to 0x19
                 * 1.12-pre5(332)
                 * Changed ID of Plugin Message (clientbound) from 0x19 to 0x18
                 * 15w36a(67)
                 * Changed ID of Plugin Message (clientbound) from 0x3F to 0x18
                 */

                if (protocolVersion >= ProtocolVersionNumbers.V1_14)     return 0x09;
                if (protocolVersion >= ProtocolVersionNumbers.V17w46a)   return 0x19;
                if (protocolVersion >= ProtocolVersionNumbers.V15w36a)   return 0x18;
                else                                                     return 0x3F;
            }
            else
                throw new InvalidCastException();
            
        }
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, Bound bound, bool hasForge) => Verify(packet, protocolVersion, bound, hasForge, out _);
        public static bool Verify(ReadOnlyPacket packet, int protocolVersion, Bound bound, bool hasForge, out PluginChannelPacket pcp)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));
            if (protocolVersion < 0)
                throw new ArgumentOutOfRangeException(nameof(protocolVersion), "协议版本不能使用负数");

            pcp = null;
            if (((bound & Bound.Client) == Bound.Client && packet.ID != GetPacketID(protocolVersion, Bound.Client)) ||
                ((bound & Bound.Server) == Bound.Server && packet.ID != GetPacketID(protocolVersion, Bound.Server)))
                return false;

            try
            {
                ReadOnlySpan<byte> buffer = packet.AsSpan();
                pcp = new PluginChannelPacket(
                    packet: packet,
                    protocolVersion: protocolVersion,
                    hasForge: hasForge,
                    channel: buffer.AsString(out buffer));

                if (protocolVersion <= ProtocolVersionNumbers.V14w31a && hasForge)
                    pcp.Data = buffer.Slice(VarShort.GetLength(buffer)).ToArray();
                else if (protocolVersion <= ProtocolVersionNumbers.V14w31a)
                    pcp.Data = buffer.Slice(2).ToArray();
                else
                    pcp.Data = buffer.ToArray();

                return true;
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }
    }
}
