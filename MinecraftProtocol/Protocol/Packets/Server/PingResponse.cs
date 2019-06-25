using System;
using System.Collections.Generic;

namespace MinecraftProtocol.Protocol.Packets.Server
{
    /// <summary>
    /// https://wiki.vg/Server_List_Ping#Response
    /// </summary>
    public class PingResponse:Packet
    {
        public const int PacketID = 0x00;
        public PingResponse(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));
            this.ID = PingResponse.PacketID;
            WriteString(json);
        }
        public static bool Verify(Packet packet)
        {
            if (packet.ID != PingResponse.PacketID)
                return false;
            try
            {
                List<byte> buffer = new List<byte>(packet.Data);
                ProtocolHandler.ReadNextString(buffer);
                return buffer.Count == 0;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }
    }
}
