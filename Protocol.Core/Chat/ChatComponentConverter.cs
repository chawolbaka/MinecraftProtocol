using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinecraftProtocol.Chat
{
    public class ChatComponentConverter : JsonConverter<ChatComponent>
    {
        public override void Write(Utf8JsonWriter writer, ChatComponent chatComponent, JsonSerializerOptions options)
        {
            WriteChatComponentObject(writer, chatComponent);
        }

        private void WriteChatComponentObject(Utf8JsonWriter writer, ChatComponent chatComponent)
        {
            writer.WriteStartObject();
            if (chatComponent.Bold != default)
                writer.WriteBoolean("bold", chatComponent.Bold);
            if (chatComponent.Italic != default)
                writer.WriteBoolean("italic", chatComponent.Italic);
            if (chatComponent.Underline != default)
                writer.WriteBoolean("underlined", chatComponent.Underline);
            if (chatComponent.Strikethrough != default)
                writer.WriteBoolean("strikethrough", chatComponent.Strikethrough);
            if (chatComponent.Obfuscated != default)
                writer.WriteBoolean("obfuscated", chatComponent.Obfuscated);
            if (chatComponent.Text != null)
                writer.WriteString("text", chatComponent.Text);
            if (chatComponent.Color != null)
                writer.WriteString("color", chatComponent.Color);
            if (chatComponent.Insertion != null)
                writer.WriteString("insertion", chatComponent.Insertion);
            if (chatComponent.Translate != null)
                writer.WriteString("translate", chatComponent.Translate);

            if (chatComponent.TranslateArguments != null && chatComponent.TranslateArguments.Count > 0)
            {
                writer.WritePropertyName("with");
                WriteArrayObject(writer, chatComponent.TranslateArguments);
            }


            if (chatComponent.Extra != null && chatComponent.Extra.Count > 0)
            {
                writer.WritePropertyName("extra");
                WriteArrayObject(writer, chatComponent.Extra);
            }

            if (chatComponent.ClickEvent != null)
            {
                writer.WritePropertyName("clickEvent");
                writer.WriteStartObject();
                writer.WriteString("action", chatComponent.ClickEvent.Action.ToString());
                writer.WriteString("value", chatComponent.ClickEvent.Value.Text);
                writer.WriteEndObject();
            }

            if (chatComponent.HoverEvent != null)
            {
                writer.WritePropertyName("clickEvent");
                writer.WriteStartObject();
                writer.WriteString("action", chatComponent.HoverEvent.Action.ToString());
                writer.WritePropertyName("value");
                WriteChatComponentObject(writer, chatComponent.HoverEvent.Value);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        private void WriteArrayObject<T>(Utf8JsonWriter writer, List<T> list)
        {
            writer.WriteStartArray();
            foreach (var item in list)
            {
                if (item is ChatComponent)
                    WriteChatComponentObject(writer, item as ChatComponent);
                else if (item is string)
                    writer.WriteStringValue(item.ToString());
            }
            writer.WriteEndArray();
        }


        public override ChatComponent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ChatComponent chatComponent = new ChatComponent();
            ReadChatComponentObject(ref reader, chatComponent);
            return chatComponent;
        }


        private ChatComponent ReadChatComponentObject(ref Utf8JsonReader reader, ChatComponent chatComponent)
        {
            string propertyName = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return chatComponent;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    propertyName = reader.GetString();
                }
                else if (!string.IsNullOrEmpty(propertyName))
                {
                    if (reader.TokenType == JsonTokenType.StartObject && propertyName == "clickEvent")
                    {
                        chatComponent.ClickEvent = ReadEventComponentObject(ref reader);
                        propertyName = null;
                    }
                    else if (reader.TokenType == JsonTokenType.StartObject && propertyName == "hoverEvent")
                    {
                        chatComponent.HoverEvent = ReadEventComponentObject(ref reader);
                        propertyName = null;
                    }
                    else if (reader.TokenType == JsonTokenType.StartArray && propertyName == "with")
                    {
                        chatComponent.TranslateArguments = ReadObjectArray(ref reader);
                        propertyName = null;
                    }
                    else if (reader.TokenType == JsonTokenType.StartArray && propertyName == "extra")
                    {
                        chatComponent.Extra = ReadChatComponentArray(ref reader);
                        propertyName = null;
                    }
                    else if (reader.TokenType is JsonTokenType.String or JsonTokenType.True or JsonTokenType.False)
                    {
                        switch (propertyName)
                        {
                            case "bold": chatComponent.Bold = reader.GetBoolean(); break;
                            case "italic": chatComponent.Italic = reader.GetBoolean(); break;
                            case "underlined": chatComponent.Underline = reader.GetBoolean(); break;
                            case "strikethrough": chatComponent.Strikethrough = reader.GetBoolean(); break;
                            case "obfuscated": chatComponent.Obfuscated = reader.GetBoolean(); break;
                            case "text": chatComponent.Text = reader.GetString(); break;
                            case "color": chatComponent.Color = reader.GetString(); break;
                            case "insertion": chatComponent.Insertion = reader.GetString(); break;
                            case "translate": chatComponent.Translate = reader.GetString(); break;
                        }
                        propertyName = null;
                    }
                }
            }
            throw new JsonException("json格式错误");
        }

        private List<object> ReadObjectArray(ref Utf8JsonReader reader)
        {
            List<object> list = new List<object>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    return list;

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    list.Add(ReadChatComponentObject(ref reader, new ChatComponent()));
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    list.Add(reader.GetString());
                }
            }

            throw new JsonException("json格式错误");
        }


        private List<ChatComponent> ReadChatComponentArray(ref Utf8JsonReader reader)
        {
            List<ChatComponent> list = new List<ChatComponent>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    return list;

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    list.Add(ReadChatComponentObject(ref reader, new ChatComponent()));
                }
            }

            throw new JsonException("json格式错误");
        }

        private EventComponent<ChatComponent> ReadEventComponentObject(ref Utf8JsonReader reader)
        {
            EventComponent<ChatComponent> eventComponent = new EventComponent<ChatComponent>();

            string propertyName = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return eventComponent;


                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    propertyName = reader.GetString();
                }
                else if (!string.IsNullOrEmpty(propertyName))
                {
                    if (reader.TokenType is JsonTokenType.StartObject && propertyName == "value")
                    {
                        eventComponent.Value = ReadChatComponentObject(ref reader, new ChatComponent());
                    }
                    else if (reader.TokenType is JsonTokenType.String)
                    {
                        switch (propertyName)
                        {
                            case "action": eventComponent.Action = Enum.Parse<EventAction>(reader.GetString()); break;
                            case "value": eventComponent.Value = new ChatComponent() { Text = reader.GetString() }; break;
                        }
                    }
                }
            }
            throw new JsonException("json格式错误");
        }
    }
}
