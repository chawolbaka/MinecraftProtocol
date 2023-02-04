using MinecraftProtocol.Compatible;
using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.DataType;
using MinecraftProtocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace MinecraftProtocol.Client
{
    public static class ClientCreator
    {
        public static MinecraftClient FromServerListPing(IPEndPoint endPoint) => FromServerListPing(string.Empty, endPoint.Address, (ushort)endPoint.Port);
        public static MinecraftClient FromServerListPing(IPAddress serverIP, ushort serverPort) => FromServerListPing(string.Empty, serverIP, serverPort);
        public static MinecraftClient FromServerListPing(string host, IPAddress serverIP, ushort serverPort)
        {
            ServerListPing slp = new ServerListPing(host, serverIP, serverPort);
            slp.EnableDelayDetect = false;
            slp.EnableDnsRoundRobin = false;

            PingReply PingResult = slp.Send();
            int protocolVersion = PingResult.Version.Protocol;

            if (protocolVersion == -1 && !string.IsNullOrWhiteSpace(PingResult.Version.Name))
                protocolVersion = ProtocolVersions.SearchByName(PingResult.Version.Name);

            
            if (PingResult.Forge == null)
            {
                if (protocolVersion != -1)
                    return new VanillaClient(host, serverIP, serverPort, protocolVersion >= ProtocolVersions.V1_12_pre3 ? ClientSettings.Default : ClientSettings.LegacyDefault, protocolVersion);
                else
                    throw new NotSupportedException("无法从ServerListPing中获取到协议号");
            }
            else
            {
                if (PingResult.Forge.ModList == null)
                    throw new NotSupportedException("无法从ServerListPing中获取到ModList");

                if (protocolVersion == -1)
                    protocolVersion = ProtocolVersions.SearchByName(PingResult.Forge.ModList.First(m => m.Name.ToLower().Trim().StartsWith("minecraft")).Version);
                if (protocolVersion != -1)
                    return new ForgeClient(host, serverIP, serverPort, new ModList(PingResult.Forge.ModList), protocolVersion >= ProtocolVersions.V1_12_pre3 ? ClientSettings.Default : ClientSettings.LegacyDefault, protocolVersion);
                else
                    throw new NotSupportedException("无法从ServerListPing中获取到协议号");
            }
        }
    }
}
