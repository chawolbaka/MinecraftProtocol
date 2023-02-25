using Microsoft.CodeAnalysis;
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
using MinecraftProtocol.Compatible;
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

                source.AppendLine($@"        /// <summary>将当前包转换至{ClassName}</summary>");
                if (pair.Value.ReadPropertyList.Count > 0)
                    source.Append($"        public static {ResultType} To{ClassName}<TPacket>(this TPacket packet, {ReadFormalParameters}) where TPacket : ICompatiblePacket");
                else
                    source.Append($"        public static {ResultType} To{ClassName}<TPacket>(this TPacket packet) where TPacket : ICompatiblePacket");
                
                source.AppendLine($@"
        {{
            if (packet.Id!= {ResultType}.GetPacketId(packet.ProtocolVersion))
                throw new InvalidPacketException(packet);

            try
            {{
                CompatibleByteReader reader = packet.AsCompatibleByteReader();
                return new {ResultType}(ref reader{(pair.Value.ReadPropertyList.Count > 0 ? $", {ReadArguments}" : "")});
            }}
            catch (Exception e)
            {{
                throw new InvalidPacketException(e.Message, packet, e);
            }}
        }}
");
                source.AppendLine($@"        /// <summary> 将当前包转换至<see cref=""{ClassName}""/> </summary>");
                source.AppendLine($@"        /// <summary> 警告：该方法仅适用于更高性能的去读取数据包而不是写入，如果对转换后的包进行过写入操作，那么就请不要再使用原始包（原因是数据部分会使用相同的引用，但Count是独立的）</summary>");
                if (pair.Value.ReadPropertyList.Count > 0)
                    source.Append($"        public static {ResultType} As{ClassName}(this {CompatiblePacket} packet, {ReadFormalParameters})\n");
                else
                    source.Append($"        public static {ResultType} As{ClassName}(this {CompatiblePacket} packet)\n");
               
                source.AppendLine($@"
        {{
            if (packet.Id != {ResultType}.GetPacketId(packet.ProtocolVersion))
                throw new InvalidPacketException(packet);

            try
            {{
                return new {ResultType}(ref packet{(pair.Value.ReadPropertyList.Count > 0 ? $", {ReadArguments}" : "")});
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
