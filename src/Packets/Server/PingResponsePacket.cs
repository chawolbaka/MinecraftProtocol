using System;
using System.Collections.Generic;

namespace MinecraftProtocol.Protocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Server_List_Ping#Response
    /// </summary>
    public class PingResponsePacket:Packet
    {
        private const int id = 0x00;
        public string Json { get; }

        private PingResponsePacket(ReadOnlyPacket packet, string json) : base(packet) { this.Json = json; }
        public PingResponsePacket(string json) : base(id)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));
            this.Json = json;
            WriteString(json);
        }
        public static int GetPacketID() => id;

        public static bool Verify(ReadOnlyPacket packet,out PingResponsePacket prp)
        {
            if (packet is null)
                throw new ArgumentNullException(nameof(packet));

            prp = null;
            if (packet.ID != id)
                return false;

            try
            {
                string ResponseJson = packet.ReadString();
                if (packet.IsReadToEnd)
                    prp = new PingResponsePacket(packet, ResponseJson);
                return !(prp is null);
            }
            catch (ArgumentOutOfRangeException) { return false; }
            catch (IndexOutOfRangeException) { return false; }
            catch (OverflowException) { return false; }
        }
    }
}
