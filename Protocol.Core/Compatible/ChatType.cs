using MinecraftProtocol.Chat;

namespace MinecraftProtocol.Compatible
{
    public class ChatType
    {
        public ChatStyle Style { get; set; }

        public string TranslationKey { get; set; }

        public string[] TranslationParameters { get; set; }
        
        public ChatType(string translationKey, string[] translationParameters, ChatStyle style)
        {
            TranslationKey = translationKey;
            TranslationParameters = translationParameters;
            Style = style;
        }

    }
}
