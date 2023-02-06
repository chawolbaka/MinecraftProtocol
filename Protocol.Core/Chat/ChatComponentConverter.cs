using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinecraftProtocol.Chat
{
    public class ChatComponentConverter : JsonConverter<ChatComponent>
    {
        public override void Write(Utf8JsonWriter writer, ChatComponent chatComponent, JsonSerializerOptions options)
        {
            if (chatComponent is null)
                throw new ArgumentNullException(nameof(chatComponent));

            WriteChatComponentObject(writer, chatComponent);
        }

        private void WriteChatComponentObject(Utf8JsonWriter writer, ChatComponent chatComponent)
        {
            writer.WriteStartObject();

            //防止只有一堆{}的json，如果是第一层的那么不管Text是不是null都写入
            if (writer.CurrentDepth == 1 || chatComponent.Text != null)
                writer.WriteString("text", chatComponent.Text ?? "");

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
            if (chatComponent.Color != null)
                writer.WriteString("color", chatComponent.Color);
            if (chatComponent.Insertion != null)
                writer.WriteString("insertion", chatComponent.Insertion);
            if (chatComponent.Translate != null)
                writer.WriteString("translate", chatComponent.Translate);

            if (chatComponent.TranslateParameters != null && chatComponent.TranslateParameters.Count > 0)
            {
                writer.WritePropertyName("with");
                WriteArrayObject(writer, chatComponent.TranslateParameters, true);
            }


            if (chatComponent.Extra != null && chatComponent.Extra.Count > 0)
            {
                writer.WritePropertyName("extra");
                WriteArrayObject(writer, chatComponent.Extra, false);
            }

            if (chatComponent.ClickEvent != null)
            {
                writer.WritePropertyName("clickEvent");
                writer.WriteStartObject();
                writer.WriteString("action", chatComponent.ClickEvent.Action.ToString());
                writer.WriteString("value", chatComponent.ClickEvent.Value[0].Text);
                writer.WriteEndObject();
            }

            if (chatComponent.HoverEvent != null)
            {
                writer.WritePropertyName("clickEvent");
                writer.WriteStartObject();
                writer.WriteString("action", chatComponent.HoverEvent.Action.ToString());
                if(chatComponent.HoverEvent.Value != null)
                {
                    writer.WritePropertyName("value");
                    if (chatComponent.HoverEvent.Value.Count > 1)
                        WriteArrayObject(writer, chatComponent.HoverEvent.Value, false);
                    else
                        WriteChatComponentObject(writer, chatComponent.HoverEvent.Value[0]);
                }
                if (chatComponent.HoverEvent.Contents != null)
                {
                    writer.WritePropertyName("contents");
                    if (chatComponent.HoverEvent.Contents.Count > 1)
                        WriteArrayObject(writer, chatComponent.HoverEvent.Contents, false);
                    else
                        WriteChatComponentObject(writer, chatComponent.HoverEvent.Contents[0]);
                }
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        private void WriteArrayObject(Utf8JsonWriter writer, List<ChatComponent> list, bool writeString)
        {
            writer.WriteStartArray();
            foreach (var cc in list)
            {
                //一般with里面如果只有一个text那么会是string，但我在读取的时候给他改成ChatComponent了所以再这边给他还原回去(但其它几个就不要这么搞了，因为我不确定mc那边的兼容性)
                if (writeString && cc.IsSimpleText) 
                    writer.WriteStringValue(cc.Text);
                else
                    WriteChatComponentObject(writer, cc);
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
                        chatComponent.TranslateParameters = ReadChatComponentArray(ref reader);
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
                            case "bold":          chatComponent.Bold = reader.GetBoolean(); break;
                            case "italic":        chatComponent.Italic = reader.GetBoolean(); break;
                            case "underlined":    chatComponent.Underline = reader.GetBoolean(); break;
                            case "strikethrough": chatComponent.Strikethrough = reader.GetBoolean(); break;
                            case "obfuscated":    chatComponent.Obfuscated = reader.GetBoolean(); break;
                            case "text":          chatComponent.Text = reader.GetString(); break;
                            case "color":         chatComponent.Color = reader.GetString(); break;
                            case "insertion":     chatComponent.Insertion = reader.GetString(); break;
                            case "translate":     chatComponent.Translate = reader.GetString(); break;
                        }
                        propertyName = null;
                    }
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
                else if (reader.TokenType == JsonTokenType.String)
                {
                    list.Add(new ChatComponent(reader.GetString()));
                }
            }

            throw new JsonException("json格式错误");
        }

        private EventComponent ReadEventComponentObject(ref Utf8JsonReader reader)
        {
            EventComponent eventComponent = new EventComponent();

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
                        eventComponent.Value = new List<ChatComponent>
                        {
                            ReadChatComponentObject(ref reader, new ChatComponent())
                        };
                        propertyName = null;
                    }
                    else if (reader.TokenType is JsonTokenType.StartObject && propertyName == "contents")
                    {
                        eventComponent.Contents = new List<ChatComponent>
                        {
                            ReadChatComponentObject(ref reader, new ChatComponent())
                        };
                        propertyName = null;
                    }
                    if (reader.TokenType is JsonTokenType.StartArray && propertyName == "value")
                    {
                        eventComponent.Value = ReadChatComponentArray(ref reader);
                        propertyName = null;
                    }
                    else if (reader.TokenType is JsonTokenType.StartArray && propertyName == "contents")
                    {
                        eventComponent.Contents = ReadChatComponentArray(ref reader);
                        propertyName = null;
                    }
                    else if (reader.TokenType is JsonTokenType.String)
                    {
                        switch (propertyName)
                        {
                            case "action": eventComponent.Action = Enum.Parse<EventAction>(reader.GetString()); break;
                            case "value":  eventComponent.Value  = new List<ChatComponent> { new ChatComponent() { Text = reader.GetString() } }; break;
                        }
                        propertyName = null;
                    }
                }
            }
            throw new JsonException("json格式错误");
        }
    }
}
