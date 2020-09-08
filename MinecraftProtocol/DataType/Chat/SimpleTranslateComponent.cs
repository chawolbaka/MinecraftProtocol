using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MinecraftProtocol.DataType.Chat
{
    public class SimpleTranslateComponent : ITranslation ,IEquatable<SimpleTranslateComponent>
    {
        [JsonProperty("translate", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Translate;

        public SimpleTranslateComponent(string translate)
        {
            this.Translate = translate;
        }

        public override string ToString() => this.Translate;
        public string ToString(Dictionary<string, string> lang) => lang.ContainsKey(Translate) ? lang[Translate] : Translate;

        public static bool operator ==(SimpleTranslateComponent left, SimpleTranslateComponent right) => EqualityComparer<SimpleTranslateComponent>.Default.Equals(left, right);
        public static bool operator !=(SimpleTranslateComponent left, SimpleTranslateComponent right) => !(left == right);
        public override bool Equals(object obj) => obj is SimpleTranslateComponent component && Equals(component);
        public bool Equals(SimpleTranslateComponent other) => Translate == other.Translate;

        public override int GetHashCode() => Translate.GetHashCode();

    }
}
