using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MinecraftProtocol.Chat
{
    public class SimpleTranslateComponent : ITranslation, IEquatable<SimpleTranslateComponent>
    {
        [JsonPropertyName("translate"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Translate;

        public SimpleTranslateComponent(string translate)
        {
            Translate = translate;
        }

        public override string ToString() => Translate;
        public string ToString(Dictionary<string, string> lang) => lang.ContainsKey(Translate) ? lang[Translate] : Translate;

        public static bool operator ==(SimpleTranslateComponent left, SimpleTranslateComponent right) => EqualityComparer<SimpleTranslateComponent>.Default.Equals(left, right);
        public static bool operator !=(SimpleTranslateComponent left, SimpleTranslateComponent right) => !(left == right);
        public override bool Equals(object obj) => obj is SimpleTranslateComponent component && Equals(component);
        public bool Equals(SimpleTranslateComponent other) => Translate == other.Translate;

        public override int GetHashCode() => Translate.GetHashCode();

    }
}
