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
        public string ToString(Dictionary<string, string> lang) => lang.ContainsKey(Translate) ? lang[Translate] : Translate;

        public static bool operator ==(TranslationComponent left, TranslationComponent right) => left.Equals(right);
        public static bool operator !=(TranslationComponent left, TranslationComponent right) => !(left == right);
        public override bool Equals(object obj) => obj is TranslationComponent component && Equals(component);
        public bool Equals(TranslationComponent other) => Translate == other.Translate;

        public override int GetHashCode() => Translate.GetHashCode();
        
    }
}
