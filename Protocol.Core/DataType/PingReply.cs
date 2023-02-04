using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.Chat;
using MinecraftProtocol.Packets.Client;
using static MinecraftProtocol.DataType.PingReply;
using System.Diagnostics.Tracing;

namespace MinecraftProtocol.DataType
{

    //这个类直接抄了https://gist.github.com/csh/2480d14fbbb33b4bbae3里面的(我来写代码啦(C/V).jpg)

    [JsonConverter(typeof(Converter))]
    public class PingReply
    {
        [JsonPropertyName("version"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public VersionPayload Version { get; set; }

        [JsonPropertyName("players"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public PlayersPayload Player { get; set; }

        //低版本有可能直接使用string使用不能直接序列化
        [JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault), JsonConverter(typeof(ChatComponentConverter))]
        public ChatComponent Motd { get; set; }

        [JsonPropertyName("modinfo"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public ForgePayLoad Forge { get; set; }

        [JsonPropertyName("favicon"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Icon { get; set; }

        [JsonIgnore]
        public TimeSpan? Elapsed { get; set; }

        [JsonIgnore]
        public string Json { get; set; }

        public class ForgePayLoad
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("modList")]
            public List<ModInfo> ModList { get; set; }

        }
        public class VersionPayload
        {
            [JsonPropertyName("protocol")]
            public int Protocol { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }
        }
        public class PlayersPayload
        {
            [JsonPropertyName("max")]
            public int Max { get; set; }

            [JsonPropertyName("online")]
            public int Online { get; set; }

            /// <summary>
            /// 玩家列表的样品, spigot的默认最大数量是12 (玩家数超出12时会随机取)
            /// </summary>
            [JsonPropertyName("sample")]
            public List<PlayerSample> Samples { get; set; }
        }

        public class PlayerSample
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }
        }

        public override string ToString()
        {
            return Json;
        }

        public class Converter : JsonConverter<PingReply>
        {
            public override PingReply Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                PingReply reply = new PingReply();
                string propertyName = null;
                while (reader.Read())
                {
                    if (string.IsNullOrEmpty(propertyName) && reader.TokenType == JsonTokenType.EndObject)
                        break;
                    if (!string.IsNullOrEmpty(propertyName) && reader.TokenType == JsonTokenType.EndObject)
                        propertyName = null; //未知的object直接无视掉

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        propertyName = reader.GetString();
                    }
                    if (reader.TokenType == JsonTokenType.String)
                    {
                        if (propertyName == "favicon")
                        {
                            reply.Icon = reader.GetString();
                            propertyName = null;
                        }
                        if (propertyName == "description")
                        {
                            //（＞д＜）都怪你可能是object token又有可能是string token，害我还需要专门写一个Converter
                            reply.Motd = new ChatComponent(reader.GetString());
                            propertyName = null;
                        }
                    }

                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        if (propertyName == "version")
                        {
                            reply.Version = JsonSerializer.Deserialize<VersionPayload>(ref reader, options);
                            propertyName = null;
                        }
                        else if (propertyName == "players")
                        {
                            reply.Player = JsonSerializer.Deserialize<PlayersPayload>(ref reader, options);
                            propertyName = null;
                        }
                        else if (propertyName == "modinfo")
                        {
                            reply.Forge = JsonSerializer.Deserialize<ForgePayLoad>(ref reader, options);
                            propertyName = null;
                        }
                        else if (propertyName == "description")
                        {
                            reply.Motd = JsonSerializer.Deserialize<ChatComponent>(ref reader, options);
                            propertyName = null;
                        }
                    }
                }
                return reply;
            }
            public override void Write(Utf8JsonWriter writer, PingReply value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }
    }
}
