using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using MinecraftProtocol.DataType.Forge;
using MinecraftProtocol.Chat;

namespace MinecraftProtocol.DataType
{

    //这个类直接抄了https://gist.github.com/csh/2480d14fbbb33b4bbae3里面的(我来写代码啦(C/V).jpg)
    [JsonConverter(typeof(Converter))]
    public class PingReply
    {
        [JsonPropertyName("version"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public VersionPayload Version { get; set; }

        [JsonPropertyName("players"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public PlayerPayload Player { get; set; }

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

        public PingReply()
        {
            Version = new VersionPayload();
            Player = new PlayerPayload();
        }

        public class ForgePayLoad
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("modList")]
            public List<ModInfo> ModList { get; set; }

            public ForgePayLoad()
            {
                ModList = new List<ModInfo>();
            }

            public ForgePayLoad(string type) : this()
            {
                Type = type;
            }

            public ForgePayLoad(string type, List<ModInfo> modList)
            {
                Type = type;
                ModList = modList;
            }

            public ForgePayLoad(string type, ModList modList)
            {
                Type = type;
                ModList = new List<ModInfo>(modList);
            }
        }
        public class VersionPayload
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("protocol")]
            public int Protocol { get; set; }

            
            public VersionPayload()
            {

            }

            public VersionPayload(string name, int protocol)
            {
                Name = name;
                Protocol = protocol;
            }

        }
        public class PlayerPayload
        {
            [JsonPropertyName("max")]
            public int Max { get; set; }

            [JsonPropertyName("online")]
            public int Online { get; set; }

            /// <summary> 玩家列表的样品, spigot的默认最大数量是12 (玩家数超出12时会随机取) </summary>
            [JsonPropertyName("sample"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public List<PlayerSample> Samples { get; set; }

            public PlayerPayload()
            {
                Samples = new List<PlayerSample>();
            }

            public PlayerPayload(int max, int online):this()
            {
                Max = max;
                Online = online;
            }

            public PlayerPayload(int max, int online, List<PlayerSample> samples)
            {
                Max = max;
                Online = online;
                Samples = samples;
            }
        }

        public class PlayerSample
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            public PlayerSample()
            {

            }

            public PlayerSample(string id, string name)
            {
                Name = name;
                Id = id;
            }
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
                            reply.Player = JsonSerializer.Deserialize<PlayerPayload>(ref reader, options);
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
                writer.WriteStartObject();

                if (value.Motd != null)
                {
                    writer.WritePropertyName("description");
                    JsonSerializer.Serialize(writer, value.Motd, options);
                }
                if (value.Version != null)
                {
                    writer.WritePropertyName("version");
                    JsonSerializer.Serialize(writer, value.Version, options);
                    Console.WriteLine(value.Version.Name);
                }
                if (value.Player != null)
                {
                    writer.WritePropertyName("players");
                    JsonSerializer.Serialize(writer, value.Player, options);
                }
                if (value.Forge != null)
                {
                    writer.WritePropertyName("modinfo");
                    JsonSerializer.Serialize(writer, value.Forge, options);
                }
                if (!string.IsNullOrWhiteSpace(value.Icon))
                {
                    writer.WriteString("favicon", value.Icon);
                }
                writer.WriteEndObject();
            }
        }
    }
}
