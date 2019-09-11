using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MinecraftProtocol.DataType.Chat
{
    public struct TranslationComponent : ITranslation, IEquatable<TranslationComponent>
    {
        [JsonProperty("translate", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Translate;
        public TranslationComponent(string translate)
        {
            this.Translate = translate;
        }

        public override string ToString() => this.Translate;
        public string ToString(Dictionary<string, string> lang) => ToString(lang, ITranslation.DefaultOption);
        public string ToString(Dictionary<string, string> lang, TranslationOptions option)
        {
            try
            {
                return lang[Translate];
            }
            catch (KeyNotFoundException knfe)
            {
                switch (option)
                {
                    case TranslationOptions.WriteEmpty: return string.Empty;
                    case TranslationOptions.WriteOriginal: return Translate;
                    case TranslationOptions.ThrowException: throw;
                    case TranslationOptions.WriteExceptionMessage: return knfe.Message;
                    default: throw new InvalidCastException();
                }
            }
        }

        public static bool operator ==(TranslationComponent left, TranslationComponent right) => left.Equals(right);
        public static bool operator !=(TranslationComponent left, TranslationComponent right) => !(left == right);
        public override bool Equals(object obj) => obj is TranslationComponent component && Equals(component);
        public bool Equals(TranslationComponent other)
        {
            return Translate == other.Translate;
        }

        public override int GetHashCode()
        {
            return Translate.GetHashCode();
        }
    }
}
