using System;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace MinecraftProtocol.DataType.Forge
{
    public class ModInfo : IEquatable<ModInfo>
    {
        [JsonPropertyName("modid")]
        public string Name { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        public ModInfo()
        {

        }
        public ModInfo(string name, string version)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }

        public override string ToString()
        {
            //有一种MC就是这种格式的记忆，不过我没找到是在哪里出现的这种格式
            return $"{this.Name}@{this.Version}";
        }

        public override bool Equals(object obj) => obj is ModInfo mi && Equals(mi);

        public static bool operator ==(ModInfo left, ModInfo right) => left.Equals(right);
        public static bool operator !=(ModInfo left, ModInfo right) => !(left == right);

        public bool Equals(ModInfo other)
        {
            return other != null &&
                   Name == other.Name &&
                   Version == other.Version;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Version);
        }
    }
}
