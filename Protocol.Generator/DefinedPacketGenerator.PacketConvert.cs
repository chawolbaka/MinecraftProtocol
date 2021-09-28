﻿using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocol.Generator
{
    public partial class DefinedPacketGenerator
    {
        private static string GeneratePacketConvert(List<KeyValuePair<INamedTypeSymbol, GenerateInfo>> infoList)
        {
            StringBuilder source = new StringBuilder(@"
using System;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;

namespace MinecraftProtocol.IO.Extensions
{
    public static partial class PacketConvert
    {
");
            foreach (var pair in infoList)
            {
                string ResultType = pair.Key.Name;

                string ClassName = ResultType.EndsWith("Packet") ? ResultType.Substring(0, ResultType.Length - 6) : ResultType;
                string ReadFormalParameters = pair.Value.ReadPropertyList.Count > 0 ? string.Join(", ", pair.Value.ReadPropertyList) : "";
                string ReadArguments = pair.Value.ReadPropertyNameList.Count > 0 ? string.Join(", ", pair.Value.ReadPropertyNameList) : "";
               
                if (pair.Value.ReadPropertyList.Count > 0)
                {
                    source.Append($"        public static {ResultType} As{ClassName}(this {CompatiblePacket} packet, {ReadFormalParameters}) => As{ClassName}(packet.AsCompatibleReadOnly(), {ReadArguments});\n");
                    source.Append($"        public static {ResultType} As{ClassName}(this {ReadOnlyCompatiblePacket} packet, {ReadFormalParameters})");
                }
                else
                {
                    source.Append($"        public static {ResultType} As{ClassName}(this {CompatiblePacket} packet) => As{ClassName}(packet.AsCompatibleReadOnly());\n");
                    source.Append($"        public static {ResultType} As{ClassName}(this {ReadOnlyCompatiblePacket} packet)");
                }

                source.AppendLine($@"
        {{
            if (packet.ID != {ResultType}.GetPacketId(packet.ProtocolVersion))
                throw new InvalidPacketException(packet);

            try
            {{
                return new {ResultType}(packet{(pair.Value.ReadPropertyList.Count > 0 ? $", {ReadArguments}" : "")});
            }}
            catch (Exception e)
            {{
                throw new InvalidPacketException(e.Message, packet, e);
            }}
        }}
");
            }
            return source.Append("\n    }\n}").ToString();
        }
    }
}
