using System;
using System.Collections.Generic;
using System.Linq;
using MinecraftProtocol.Chat;
using MinecraftProtocol.IO.NBT.Tags;
using MinecraftProtocol.Packets;
using MinecraftProtocol.Packets.Client;
using MinecraftProtocol.Packets.Server;

namespace MinecraftProtocol.Compatible
{
    public static class CompatibleReader
    {
        public static bool TryReadClientChatMessage(ReadOnlyCompatiblePacket packet, out string message) => TryReadClientChatMessage(packet, out message, out _);
        public static bool TryReadClientChatMessage(ReadOnlyCompatiblePacket packet, out string message, out DefinedPacket definedPacket)
        {
            definedPacket = null; message = null;
            if(packet.ProtocolVersion >= ProtocolVersions.V1_19&&ChatCommandPacket.TryRead(packet,out ChatCommandPacket ccp))
            {
                message = ccp.Command;
                definedPacket = ccp;
            }
            else if(ClientChatMessagePacket.TryRead(packet,out ClientChatMessagePacket ccmp))
            {
                message = ccmp.Message;
                definedPacket = ccmp;
            }

            return message == null;
        }
        public static bool TryReadServerChatMessage(ReadOnlyCompatiblePacket packet, out ChatComponent message) => TryReadServerChatMessage(packet, Array.Empty<ChatType>(), out message);
        public static bool TryReadServerChatMessage(ReadOnlyCompatiblePacket packet, out ChatComponent message, out DefinedPacket definedPacket) => TryReadServerChatMessage(packet, Array.Empty<ChatType>(), out message, out definedPacket);
        public static bool TryReadServerChatMessage(ReadOnlyCompatiblePacket packet, ChatType[] chatTypes, out ChatComponent message)
        {
            TryReadServerChatMessage(packet, chatTypes, out message, out DefinedPacket definedPacket);
            definedPacket?.Dispose();
            return message == null;
        }
        public static bool TryReadServerChatMessage(ReadOnlyCompatiblePacket packet, ChatType[] chatTypes, out ChatComponent message, out DefinedPacket definedPacket)
        {
            definedPacket = null; message = null;
            if (packet.ProtocolVersion >= ProtocolVersions.V1_19)
            {
                if (PlayerChatMessagePacket.TryRead(packet, out PlayerChatMessagePacket pcmp))
                {
                    if (chatTypes is null)
                        throw new ArgumentNullException(nameof(chatTypes));

                    definedPacket = pcmp;
                    message = CrateChatComponentFromChatType(pcmp.ChatType, chatTypes);
                    ChatComponent sender = ChatComponent.Deserialize(pcmp.NetworkTargetName ?? pcmp.NetworkName);
                    foreach (var parameter in chatTypes[pcmp.ChatType].TranslationParameters)
                    {
                        if (parameter == "target") //抄的，我也不知道为什么要这样子提取
                            message.AddTranslateParameter(sender.TranslateParameters.Count > 0 ? sender.TranslateParameters[0] : new ChatComponent());
                        else if (parameter == "sender")
                            message.AddTranslateParameter(sender);
                        else if (parameter == "content")
                            message.AddTranslateParameter(ChatComponent.Deserialize(pcmp.Message));
                    }
                }
                else if (DisguisedChatMessagePacket.TryRead(packet, out DisguisedChatMessagePacket dcmp))
                {
                    if (chatTypes is null)
                        throw new ArgumentNullException(nameof(chatTypes));

                    definedPacket = dcmp;
                    message = CrateChatComponentFromChatType(dcmp.ChatType, chatTypes);
                    foreach (var parameter in chatTypes[dcmp.ChatType].TranslationParameters)
                    {
                        if (parameter == "sender")
                            message.AddTranslateParameter(ChatComponent.Deserialize(dcmp.TargetName ?? dcmp.ChatTypeName));
                        else if (parameter == "content")
                            message.AddTranslateParameter(ChatComponent.Deserialize(dcmp.Message));
                    }
                }
                else if (SystemChatMessagePacket.TryRead(packet, out SystemChatMessagePacket scmp))
                {
                    definedPacket = scmp;
                    message = scmp.Message;
                }
            }
            else if(ServerChatMessagePacket.TryRead(packet, out ServerChatMessagePacket scmp))
            {
                definedPacket = scmp;
                message = scmp.Message;
            }
            return message is not null;
        }

        private static ChatComponent CrateChatComponentFromChatType(int chatType, ChatType[] chatTypeTable)
        {
            if (chatType >= chatTypeTable.Length)
                throw new ArgumentOutOfRangeException(nameof(chatTypeTable), $"Unknow chat type {chatType}");

            ChatComponent message = new ChatComponent();
            message.Translate = chatTypeTable[chatType].TranslationKey;
            message.SetStyle(chatTypeTable[chatType].Style);
            return message;
        }

        public static bool TryGetChatTypes(this JoinGamePacket jgp,out ChatType[] chatTypes)
        {
            chatTypes = null;
            if (jgp is null || jgp.RegistryCodec is null)
                return false;

            try
            {
                CompoundTag chatTag = jgp.RegistryCodec.Payload.First(x => x.Name == "minecraft:chat_type") as CompoundTag;
                ListTag values = chatTag.Payload.First(x => x.Name == "value") as ListTag;

                chatTypes = new ChatType[values.Payload.Length];
                foreach (CompoundTag value in values.Payload)
                {
                    int id = (value.Payload.First(x => x.Name == "id") as IntTag).Payload;
                    CompoundTag element = value.Payload.First(x => x.Name == "element") as CompoundTag;
                    CompoundTag chat = element.Payload.First(x => x.Name == "chat") as CompoundTag;
                    CompoundTag style = chat.Payload.FirstOrDefault(x => x.Name == "style") as CompoundTag;
                    ChatStyle chatStyle = null;
                    if (style != null && style.Payload.Count > 0)
                    {
                        chatStyle = new ChatStyle();
                        foreach (var item in style.Payload)
                        {
                            switch (item.Name)
                            {
                                case "bold":          chatStyle.Bold          = (item as ByteTag).Payload == 1; break;
                                case "italic":        chatStyle.Italic        = (item as ByteTag).Payload == 1; break;
                                case "underline":     chatStyle.Underline     = (item as ByteTag).Payload == 1; break;
                                case "strikethrough": chatStyle.Strikethrough = (item as ByteTag).Payload == 1; break;
                                case "obfuscated":    chatStyle.Obfuscated    = (item as ByteTag).Payload == 1; break;
                                case "color":         chatStyle.Color         = item.ToString(); break;
                            }
                        }
                    }
                    string translationKey = chat.Payload.First(x => x.Name == "translation_key").ToString();
                    string[] parameters = (chat.Payload.First(x => x.Name == "parameters") as ListTag).Payload.Select(x => x.ToString()).ToArray();
                    chatTypes[id] = new ChatType(translationKey, parameters, chatStyle);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
