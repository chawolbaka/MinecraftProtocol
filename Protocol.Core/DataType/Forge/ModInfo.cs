using System;
using Newtonsoft.Json;

namespace MinecraftProtocol.DataType.Forge
{
    public class ModInfo : IEquatable<ModInfo>
    {
        [JsonProperty(PropertyName = "modid")]
        public readonly string Name;
        [JsonProperty(PropertyName = "version")]
        public readonly string Version;

        
        public ModInfo(string modName, string modVersion)
        {
            Name = modName ?? throw new ArgumentNullException(nameof(modName));
            Version = modVersion ?? throw new ArgumentNullException(nameof(modVersion));
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
