using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MinecraftProtocol.DataType.Chat
{
    public class TextComponent : IEquatable<TextComponent>
    {
        [JsonProperty("text",DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Text;

        public TextComponent(string text)
        {
            this.Text = text;
        }
        public override string ToString()
        {
            return Text;
        }

        public static bool operator ==(TextComponent left, TextComponent right) => EqualityComparer<TextComponent>.Default.Equals(left, right);
        public static bool operator !=(TextComponent left, TextComponent right) => !(left == right);
        public override bool Equals(object obj)
        {
            return obj is TextComponent component && Equals(component);
        }
        public bool Equals(TextComponent other)
        {
            if (ReferenceEquals(this, other))
                return true;
            else
                return Text == other.Text;
        }
        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }
    }
}
